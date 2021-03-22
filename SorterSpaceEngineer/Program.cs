using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

class Program : MyGridProgram
{
	public Program()
	{
		Runtime.UpdateFrequency = UpdateFrequency.Update100;
		if (SorterOfRequested == null) SorterOfRequested = GridTerminalSystem.GetBlockWithName("RequestSorter") as IMyConveyorSorter;
		Report(SorterOfRequested == null ? "Not found sorter" : "Found sorter");
		//Get assembler
		if (MainAssembler == null) MainAssembler = GridTerminalSystem.GetBlockWithName("MainAssembler") as IMyAssembler;
		Report(MainAssembler == null ? "Not found main assembler" : "Found assembler");
		if (reportScreen == null) reportScreen = GridTerminalSystem.GetBlockGroupWithName("ReportScreen") as IMyTextSurface;

	}
	public void Save() { }

	IMyTextSurface textScreen;
	IMyTextSurface reportScreen;
	public void Report(string message, bool echo = true)
	{
		if (textScreen == null)
			textScreen = GridTerminalSystem.GetBlockWithName("ReportScreen2") as IMyTextSurface;
		textScreen.WriteText($"\r\n{message}", true);
		if (echo)
			Echo(message);
	}

	public void ClearScreen()
	{
		if (textScreen == null)
			textScreen = Me.GetSurface(0);
		textScreen.WriteText("", false);
	}

	public int MinimumAmount = 100;
	List<MyInventoryItemFilter> RequestedItems;
	Dictionary<string, int> RequestedAndAmount;
	Dictionary<string, int> RequestedAndAmountCustom;
	IMyConveyorSorter SorterOfRequested;
	IMyAssembler MainAssembler;
	IMyCargoContainer AnyCargo;
	bool allDone = false;
	public void Main(string argument, UpdateType updateSource)
	{
		//Normal update
		if (updateSource == UpdateType.Update100)
		{
			//Update on screen if main assembler is in production
			ClearScreen();
			//Move all item out of main assembler
			var output = new List<MyInventoryItem>();
			MainAssembler.OutputInventory.GetItems(output);
			foreach (var item in output)
			{
				if (AnyCargo != null)
					MainAssembler.OutputInventory.TransferItemTo(AnyCargo.GetInventory(), item);
			}
			//Max characters: 53
			Dictionary<string, int> itemsAndAmount = new Dictionary<string, int>();
			Dictionary<string, int> itemsAndQueue = new Dictionary<string, int>();
			if (RequestedAndAmountCustom == null)
				RequestedAndAmountCustom = new Dictionary<string, int>();
			//Check sorter for requests
			var requests = new List<MyInventoryItemFilter>();
			SorterOfRequested.GetFilterList(requests);
			Report($"Request: {requests.Count} | {(allDone ? "(All done)" : "(Working)")}");
			foreach (var item in requests)
			{
				itemsAndAmount.Add(item.ItemType.SubtypeId, 0);
				itemsAndQueue.Add(item.ItemType.SubtypeId, 0);
			}
			//Check main assembly for queues
			var queue = new List<MyProductionItem>();
			MainAssembler.GetQueue(queue);
			Report($"Queue: {queue.Count} | {(MainAssembler.IsProducing ? "ACTIVE" : "INACTIVE")}");
			//Check if assembler is in disassembling moded
			if (MainAssembler.Mode == MyAssemblerMode.Disassembly)
				MainAssembler.Mode = MyAssemblerMode.Assembly;
			foreach (var item in queue)
			{
				if (!itemsAndQueue.ContainsKey(item.BlueprintId.SubtypeName))
					itemsAndQueue.Add(item.BlueprintId.SubtypeName, item.Amount.ToIntSafe());
				else
					itemsAndQueue[item.BlueprintId.SubtypeName] += item.Amount.ToIntSafe();
			}
			//Checking items
			List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocks(allBlocks);
			foreach (var block in allBlocks)
			{
				if (block == SorterOfRequested)
					continue;
				else if (block == Me)
					continue;
				if (AnyCargo == null)
				{
					if (block is IMyCargoContainer && !block.GetInventory().IsFull)
						AnyCargo = block as IMyCargoContainer;
				}
				else
				{
					if (AnyCargo.GetInventory().IsFull)
						AnyCargo = null;
				}

				if (block.InventoryCount > 0)
				{
					for (int i = 0; i < block.InventoryCount; i++)
					{
						var allItems = block.GetInventory(i);
						if (allItems != null && allItems.ItemCount > 0)
						{
							var actualAllItems = new List<MyInventoryItem>();
							allItems.GetItems(actualAllItems);
							foreach (var item2 in actualAllItems)
							{
								if (itemsAndAmount.ContainsKey(item2.Type.SubtypeId))
									itemsAndAmount[item2.Type.SubtypeId] += item2.Amount.ToIntSafe();
								else
									itemsAndAmount.Add(item2.Type.SubtypeId, item2.Amount.ToIntSafe());
							}
						}
					}
				}
			}
			Report($"AM=Amount | AS=Assembling | REQ=Required");
			Report($"{"Name",-22} | {"AM",-6} | {"AS",-6} | {"REQ",-6} |\r\n" +
				   $"-----------------------------------------------------");
			foreach (var pair in itemsAndAmount)
			{
				if (!itemsAndQueue.ContainsKey(pair.Key))
					continue;
				if (RequestedAndAmountCustom.ContainsKey(pair.Key))
					Report($"{pair.Key,-22} | {pair.Value,-6} | {itemsAndQueue[pair.Key],-6} | {RequestedAndAmountCustom[pair.Key],-6} |\r\n");
				else
					Report($"{pair.Key,-22} | {pair.Value,-6} | {itemsAndQueue[pair.Key],-6} | {MinimumAmount,-6} |\r\n");
				if ((RequestedAndAmountCustom.ContainsKey(pair.Key) && pair.Value < RequestedAndAmountCustom[pair.Key]) ||
					(!RequestedAndAmountCustom.ContainsKey(pair.Key) && pair.Value < MinimumAmount))
					allDone = false;
			}
			if (!MainAssembler.IsProducing && MainAssembler.IsQueueEmpty && !allDone)
			{
				Runtime.UpdateFrequency = UpdateFrequency.None;
				Main("CheckAndRequest", UpdateType.Script);
			}
			ReportAllItemsInfo();
		}
		else if (argument == "CheckAndRequest" && updateSource == UpdateType.Script)
		{
			ClearScreen();
			Report("Begin checking items");
			//Get sorter
			//Find item list on Conveyer sorter filter list
			RequestedItems = new List<MyInventoryItemFilter>();
			SorterOfRequested.GetFilterList(RequestedItems);
			Report("Gathered list of items from sorter of request");
			Report($"Found {RequestedItems.Count} items");
			RequestedAndAmount = new Dictionary<string, int>();
			RequestedAndAmountCustom = new Dictionary<string, int>();
			//Read all custom amounts
			if (!string.IsNullOrEmpty(Me.CustomData))
			{
				var linesOfCustom = Me.CustomData.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				foreach (var line in linesOfCustom)
				{
					var info = line.Split('=');
					RequestedAndAmountCustom.Add(info[0], int.Parse(info[1]));
				}
			}
			//Load requested amount from custom data or write into it if empty
			string input = "";
			foreach (var item in RequestedItems)
			{
				RequestedAndAmount.Add(item.ItemType.SubtypeId, 0);
				Report($"Add {item.ItemType.SubtypeId} into requested list");
				if (RequestedAndAmountCustom.ContainsKey(item.ItemType.SubtypeId))
					input += $"{item.ItemType.SubtypeId}={RequestedAndAmountCustom[item.ItemType.SubtypeId]}\r\n";
				else
				{
					input += $"{item.ItemType.SubtypeId}={MinimumAmount}\r\n";
					RequestedAndAmountCustom.Add(item.ItemType.SubtypeId, MinimumAmount);
				}
			}
			Me.CustomData = input;
			//Check all inventories
			Report("Began checking inventories");
			List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocks(allBlocks);
			Report($"Began checking {allBlocks.Count} blocks");
			foreach (var item in allBlocks)
			{
				Report($"Checking {item.CustomName}");
				if (item == SorterOfRequested)
					continue;
				else if (item == Me)
					continue;

				if (item.InventoryCount > 0)
				{
					for (int i = 0; i < item.InventoryCount; i++)
					{
						Report($"Found inventory on {item.CustomName} {i + 1}/{item.InventoryCount}");
						var allItems = item.GetInventory(i);
						Report($"{item.CustomName}'s inventory {(allItems == null ? "is null" : "has an item")} {allItems.ItemCount}");
						if (allItems != null && allItems.ItemCount > 0)
						{
							var actualAllItems = new List<MyInventoryItem>();
							allItems.GetItems(actualAllItems);
							Report($"Found {actualAllItems.Count} item on inventory");
							foreach (var req in RequestedItems)
							{
								foreach (var actual in actualAllItems)
								{
									Report($"{req.ItemType.SubtypeId} vs {actual.Type.SubtypeId}");
									if (req.ItemType.SubtypeId == actual.Type.SubtypeId)
									{
										Report($"Add {req.ItemType.SubtypeId} into list");
										Report($"Add {actual.Amount.ToIntSafe()} into dictionary");
										if (!RequestedAndAmount.ContainsKey(req.ItemType.SubtypeId))
											RequestedAndAmount.Add(req.ItemType.SubtypeId, 0);
										RequestedAndAmount[req.ItemType.SubtypeId] += actual.Amount.ToIntSafe();
									}
								}
							}
						}
					}
				}
			}
			ClearScreen();
			Report("Began requesting assembler");
			var queue = new List<MyProductionItem>();
			MainAssembler.GetQueue(queue);
			//If it has less than MinimumAmount then send request to assembly
			//TODO: Take all slaves assembly into account
			bool unfinished = false;
			foreach (var pair in RequestedAndAmount)
			{
				//Check if it's already in queue or not
				foreach (var item in queue)
				{
					if (item.BlueprintId.SubtypeName == pair.Key)
						continue;
				}
				if (RequestedAndAmountCustom.ContainsKey(pair.Key))
				{
					Report($"{pair.Key}: R={RequestedAndAmountCustom[pair.Key]} Now={pair.Value}");
					if (pair.Value <= RequestedAndAmountCustom[pair.Key])
					{
						Report($"Parsing item id: {pair.Key}");
						MyDefinitionId itemID = MyDefinitionId.Parse($"MyObjectBuilder_BlueprintDefinition/{pair.Key}");
						Report($"Item ID parsing: {(itemID == null ? "ItemID parse failed" : "ItemID parse complete")}");
						Report($"Requested item \"{itemID.SubtypeName}\" still missing about {RequestedAndAmountCustom[pair.Key] - pair.Value}");
						try
						{
							MainAssembler.AddQueueItem(itemID, Convert.ToDecimal(RequestedAndAmountCustom[itemID.SubtypeName] - pair.Value));
						}
						catch (Exception e)
						{
							Report($"ERROR: {e.Message}");
						}
						unfinished = true;
					}
				}
				else
				{
					Report($"{pair.Key}: R={MinimumAmount} Now={pair.Value}");
					if (pair.Value <= MinimumAmount)
					{
						Report($"Parsing item id: {pair.Key}");
						MyDefinitionId itemID = MyDefinitionId.Parse($"MyObjectBuilder_BlueprintDefinition/{pair.Key}");
						Report($"Item ID parsing: {(itemID == null ? "ItemID parse failed" : "ItemID parse complete")}");
						Report($"Requested item \"{itemID.SubtypeName}\" still missing about {(MinimumAmount - pair.Value)}");
						MainAssembler.AddQueueItem(itemID, Convert.ToDecimal(MinimumAmount - pair.Value));
						unfinished = true;
					}
				}
			}
			if (unfinished)
			{
				//Set back updater
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			}
			else
			{
				allDone = true;
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			}
		}
	}

	public void ReportAllItemsInfo()
	{
		if (reportScreen == null)
			reportScreen = GridTerminalSystem.GetBlockWithName("ReportScreen") as IMyTextSurface;
		List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
		Dictionary<string, int> itemsAndAmount = new Dictionary<string, int>();
		GridTerminalSystem.GetBlocks(allBlocks);
		foreach (var block in allBlocks)
		{
			if (block == Me)
				continue;

			if (block.InventoryCount > 0)
			{
				for (int i = 0; i < block.InventoryCount; i++)
				{
					var allItems = block.GetInventory(i);
					if (allItems != null)
					{
						var actualAllItems = new List<MyInventoryItem>();
						allItems.GetItems(actualAllItems);
						foreach (var item2 in actualAllItems)
						{
							if (itemsAndAmount.ContainsKey(item2.Type.SubtypeId))
							{
								itemsAndAmount[item2.Type.SubtypeId] += item2.Amount.ToIntSafe();
							}
							else
							{
								itemsAndAmount.Add(item2.Type.SubtypeId, item2.Amount.ToIntSafe());
							}
						}
					}
				}
			}
		}
		string report = $"All items type ({itemsAndAmount.Count})\r\n";
		//Sort dictionary
		var sorted = new Dictionary<string, int>();
		foreach (var item in itemsAndAmount.Keys.OrderBy(key => key))
		{
			sorted.Add(item, itemsAndAmount[item]);
		}
		int row = 0;
		int counter = 1;
		foreach (var pair in sorted)
		{
			if (row > 2)
			{
				row = 0;
				report += $"\r\n";
			}
			string now = $"{counter}.{pair.Key}: {pair.Value} ";
			if (now.Length > 22)
			{
				if (row < 2)
				{
					report += $"{now,-44}";
					row += 1;
				}
				else if (row == 2)
				{
					report += $"\r\n{now,-44}";
					row = 1;
				}
			}
			else
				report += $"{now,-22}";
			row++;
			counter++;
		}
		reportScreen.WriteText(report, false);
	}
}