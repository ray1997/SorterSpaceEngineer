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
	partial class Program : MyGridProgram
	{
		public Program()
		{
			Runtime.UpdateFrequency = UpdateFrequency.None;
			Far_Top_Welders = GetBlockGroupAsList<IMyShipWelder>("Far_Top_Welders");
			Far_Left_Welders = GetBlockGroupAsList<IMyShipWelder>("Far_Left_Welders");
			Far_Right_Welders = GetBlockGroupAsList<IMyShipWelder>("Far_Right_Welders");
			Far_Top_Pistons = GetBlockGroupAsList<IMyExtendedPistonBase>("Far_Top_Pistons");
			Far_PistonEnd_Merge_Top = GetBlock<IMyShipMergeBlock>("Far_PistonEnd_Merge_Top");
			Far_PistonEnd_Merge_Side = GetBlock<IMyShipMergeBlock>("Far_PistonEnd_Merge_Side");
			Far_Right_Connector = GetBlock<IMyShipConnector>("Far_Right_Connector");
			Far_Projector_Merge = GetBlock<IMyShipMergeBlock>("Far_Projector_Merge");
			Far_Piston_Side_Welders = GetBlock<IMyExtendedPistonBase>("Far_Piston_Side_Welders");
			Far_Projector = GetBlock<IMyProjector>("Far_Projector");
			Far_Array_Drills = GetBlockGroupAsList<IMyShipDrill>("Far_Array_Drills");
		}

		public List<T> GetBlockGroupAsList<T>(string name)
		{
			List<T> group = new List<T>();
			var imygroup = GridTerminalSystem.GetBlockGroupWithName(name);
			var blocks = new List<IMyTerminalBlock>();
			imygroup.GetBlocks(blocks);
			group = blocks.Select(i => (T)i).ToList();
			return group;
		}

		public T GetBlock<T>(string name)
		{
			return (T)GridTerminalSystem.GetBlockWithName(name);
		}

		public void Save() { }

		List<IMyShipWelder> Far_Top_Welders;
		List<IMyShipWelder> Far_Left_Welders;
		List<IMyShipWelder> Far_Right_Welders;
		List<IMyExtendedPistonBase> Far_Top_Pistons;
		List<IMyShipDrill> Far_Array_Drills;
		IMyShipConnector Far_Right_Connector;
		IMyExtendedPistonBase Far_Piston_Side_Welders;
		/// <summary>
		/// A merge block that stick at the end of top pistons (ABC pistons)
		/// </summary>
		IMyShipMergeBlock Far_PistonEnd_Merge_Top;
		IMyShipMergeBlock Far_PistonEnd_Merge_Side;
		IMyShipMergeBlock Far_Projector_Merge;
		IMyProjector Far_Projector;

		bool IsPaused = false;
		public void Main(string argument, UpdateType updateSource)
		{
			Echo(IsPaused ? "System paused" : "System working");
			if (argument == "Pause")
				IsPaused = true;
			else if (argument == "Resume")
				IsPaused = false;
			if (IsPaused)
				return;
			//While connector is connected, move stone while at it
			if (Far_Right_Connector.Status == MyShipConnectorStatus.Connected)
			{
				//Move all stones out of drill
				if (GetAnyCargo())
				{
					//Move all stone
					foreach (var drill in Far_Array_Drills)
					{
						if (drill.GetInventory(0).ItemCount > 0)
						{
							//Move stone
							var inv = drill.GetInventory(0);
							List<MyInventoryItem> items = new List<MyInventoryItem>();
							inv.GetItems(items);
							foreach (var item in items)
							{
								inv.TransferItemTo(AnyCargo.GetInventory(0), item);
							}
						}
					}
				}
			}
			//Check connector can connect now
			bool isAllExtended = IsAllPistonExtended(Far_Top_Pistons);
			bool isAllRetracted = IsAllPistonRetracted(Far_Top_Pistons);
			bool isMidPush = IsPistonMidPush(Far_Top_Pistons);
			bool isPistonMergeOn = Far_PistonEnd_Merge_Top.GetValueBool("OnOff");
			bool isAnyDrillHasStone = IsAnyDrillHasStone(Far_Array_Drills);
			Echo($"Currently all piston: {(isAllExtended ? "Extended" : "")}{(isAllRetracted ? "Retracted" : "")}");
			//Assuming this already push to it fullest
			if (isAllExtended && !isPistonMergeOn && Far_Right_Connector.Status == MyShipConnectorStatus.Connectable && !isMidPush)
			{
				//If it can connect, connect it and turn on side merge block, and turn off top merge block
				Far_Right_Connector.Connect();
				Far_PistonEnd_Merge_Side.SetValueBool("OnOff", true);
				Far_PistonEnd_Merge_Top.SetValueBool("OnOff", false);
				Echo("Connector connected, turn on side merge block, and turn on top merge block");
			}
			else if (!isAllExtended && isPistonMergeOn)
			{
				//Merge block at the tip of piston is off. Time to pull it up
				foreach (var piston in Far_Top_Pistons)
				{
					piston.SetValueFloat("Velocity", -0.3f);
				}
			}
			//All piston retracted, turn on projector merge block and all welders
			else if (isAllRetracted && !Far_Projector_Merge.GetValueBool("OnOff"))
			{
				Far_Projector_Merge.SetValueBool("OnOff", true);
				//Turn on all welder
				var welders = Far_Top_Welders.Concat(Far_Left_Welders).Concat(Far_Right_Welders).ToList();
				foreach (var welder in welders)
				{
					welder.SetValueBool("OnOff", true);
				}
				//Extend a piston to build the other side of crane
				Far_Piston_Side_Welders.SetValueFloat("Velocity", 0.3f);
			}
			//Check projector status
			else if (isAllRetracted && Far_Projector.RemainingBlocks < 1 && !isPistonMergeOn)
			{
				//All done and welded. Turn off all welders and retract side welder
				var welders = Far_Top_Welders.Concat(Far_Left_Welders).Concat(Far_Right_Welders).ToList();
				foreach (var welder in welders)
				{
					welder.SetValueBool("OnOff", false);
				}
				Far_Piston_Side_Welders.SetValueFloat("Velocity", -0.3f);
				//Then extend top piston!
				foreach (var piston in Far_Top_Pistons)
				{
					piston.SetValueFloat("Velocity", 0.1f);
					Far_PistonEnd_Merge_Top.SetValueBool("OnOff", true);
				}
			}
			else if (isPistonMergeOn && Far_PistonEnd_Merge_Top.IsConnected)
			{
				//If top merge block is connected, then disable projector merge block
				//Letting it push down
				//Disable side merger, if it connected
				Far_Projector_Merge.SetValueBool("OnOff", false);
				Far_PistonEnd_Merge_Side.SetValueBool("OnOff", false);
			}
			//After it push down for a while
			else if (isPistonMergeOn && isMidPush)
			{
				//Disconnect connector
				Far_Right_Connector.Disconnect();
			}
		}

		public IMyCargoContainer AnyCargo;

		public bool GetAnyCargo()
		{
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(blocks);
			bool allFull = true;
			foreach (var block in blocks)
			{
				if (!(block as IMyCargoContainer).GetInventory(0).IsFull)
				{
					AnyCargo = block as IMyCargoContainer;
					allFull = false;
					return true;
				}
			}
			if (allFull)
			{
				//Do something?
				IsPaused = true;
			}
			return false;
		}

		public bool IsAnyDrillHasStone(List<IMyShipDrill> drills)
		{
			bool hasStone = false;
			foreach (var drill in drills)
			{
				var inv = drill.GetInventory(0);
				if (inv.ItemCount > 0)
				{
					hasStone = true;
					return hasStone;
				}
			}
			return hasStone;
		}

		public bool IsPistonMidPush(List<IMyExtendedPistonBase> pistons)
		{
			bool IsMidPush = true;
			foreach (var piston in pistons)
			{
				if (piston.CurrentPosition > 6f && piston.CurrentPosition < 7f)
				{
					//Yes
				}
				else
					IsMidPush = false;
			}
			return IsMidPush;
		}

		public bool IsAllPistonExtended(List<IMyExtendedPistonBase> pistons)
		{
			bool extended = true;
			foreach (var piston in pistons)
			{
				if (piston.CurrentPosition <= 9.25f)
					extended = false;
			}
			return extended;
		}

		public bool IsAllPistonRetracted(List<IMyExtendedPistonBase> pistons)
		{
			bool retracted = true;
			foreach (var piston in pistons)
			{
				if (piston.CurrentPosition > 0f)
					retracted = false;
			}
			return retracted;
		}
	}
}