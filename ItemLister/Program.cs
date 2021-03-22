using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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

namespace IngameScript
{
	/// <summary>
	/// Item counter and display
	/// </summary>
	partial class Program : MyGridProgram
	{
		public Program() { Runtime.UpdateFrequency = UpdateFrequency.Update100; }
		public void Save() { }

		IMyTextSurface reportScreen;

		public void Main(string argument, UpdateType updateSource)
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
			List<string> itemInfo = new List<string>();
			int row = 0;
			int counter = 1;
			foreach (var pair in itemsAndAmount)
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
}
