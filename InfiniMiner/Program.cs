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
		//Top most
		List<IMyExtendedPistonBase> Far_Top_Pistons;
		IMyShipMergeBlock Far_PistonEnd_Merge;
		//Top
		IMyProjector Far_Projector;
		IMyShipMergeBlock Far_ProjectorEnd_Merge;
		List<IMyShipWelder> Far_Array_Welders;
		//Bottom
		IMyShipConnector Far_Right_Connector;

		//At the bottom of the pit
		List<IMyShipDrill> Far_Array_Drills;

		public Program()
        {
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
			Far_Top_Pistons = GetBlockGroupAsList<IMyExtendedPistonBase>("Far_Top_Pistons");
			Far_PistonEnd_Merge = GetBlock<IMyShipMergeBlock>("Far_PistonEnd_Merge");
			//
			Far_Projector = GetBlock<IMyProjector>("Far_Projector");
			Far_ProjectorEnd_Merge = GetBlock<IMyShipMergeBlock>("Far_ProjectorEnd_Merge");
			Far_Array_Welders = GetBlockGroupAsList<IMyShipWelder>("Far_Array_Welders");
			//
			Far_Right_Connector = GetBlock<IMyShipConnector>("Far_Right_Connector");
			//
			Far_Array_Drills = GetBlockGroupAsList<IMyShipDrill>("Far_Array_Drills");
		}

		public bool Pause;
        public void Main(string argument, UpdateType updateSource)
        {
			Pause = string.IsNullOrEmpty(argument) ? false : (argument == "Pause");
			if (Pause)
			{
				Echo("System paused");
				return;
			}
			else
				Echo("System operational");

			State CurrentPistonState = GetPistonState(Far_Top_Pistons);
			Echo($"Piston status: {CurrentPistonState}");
			bool isWeldersWorking = IsAllWelderStillWorking();
			Echo($"All welder currently {(isWeldersWorking ? "running" : "offline")}");
			bool anyDrillHasStone = IsAnyDrillHasStone(Far_Array_Drills);
			Echo($"Any drills has stone? {anyDrillHasStone}");

			if (CurrentPistonState == State.Extended && Far_Right_Connector.Status == MyShipConnectorStatus.Connected)
			{
				//Check for stone before proceed
				if (anyDrillHasStone)
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
					return;
				}
				//At max... Disconnect merge block and retract piston
				Far_PistonEnd_Merge.SetValueBool("OnOff", false);
				SetPiston(Far_Top_Pistons, "Velocity", -0.25f);
			}
			else if (CurrentPistonState == State.Retracted && !Far_Array_Welders[0].GetValue<bool>("OnOff")
				&& !Far_PistonEnd_Merge.GetValue<bool>("OnOff"))
			{
				//Begin welding new extender core
				//Turn on projector merge block
				Far_ProjectorEnd_Merge.SetValue("OnOff", true);
				//Turn on welders
				SetWelder(Far_Array_Welders, "OnOff", true);
			}
			else if (CurrentPistonState == State.Retracted && Far_Array_Welders[0].GetValue<bool>("OnOff")
				&& Far_Projector.BuildableBlocksCount < 1 && Far_Projector.RemainingBlocks < 1
				&& !isWeldersWorking && !Far_PistonEnd_Merge.GetValue<bool>("OnOff"))
			{
				//All welding complete?
				//Extend piston first
				Far_PistonEnd_Merge.SetValue("OnOff", false);
				SetPiston(Far_Top_Pistons, "Velocity", 0.1f);
			}
			else if (CurrentPistonState == State.ConnectedNewBlock && Far_PistonEnd_Merge.IsConnected)
			{
				//Stop all welders
				SetWelder(Far_Array_Welders, "OnOff", false);
				//Stop projector merge block
				Far_ProjectorEnd_Merge.SetValue("OnOff", false);
			}
			else if (CurrentPistonState == State.ConnectedOldGrid && Far_Right_Connector.Status == MyShipConnectorStatus.Connected)
			{
				//Stop connector and extend it further
				Far_Right_Connector.Disconnect();
			}
			else if (CurrentPistonState == State.Extended && Far_Right_Connector.Status == MyShipConnectorStatus.Connectable)
			{
				//Connect connector
				Far_Right_Connector.Connect();
			}
        }

		public bool IsAllWelderStillWorking()
		{
			bool isWorking = false;
			foreach (var welder in Far_Array_Welders)
			{
				if (welder.IsWorking)
				{
					isWorking = true;
					return isWorking;
				}
			}
			return isWorking;
		}

		public void SetWelder<T>(List<IMyShipWelder> welders, string property, T value)
		{
			foreach (var welder in welders)
			{
				welder.SetValue(property, value);
			}
		}

		public void SetPiston<T>(List<IMyExtendedPistonBase> pistons, string property, T value)
		{
			foreach (var piston in pistons)
			{
				piston.SetValue(property, value);
			}
		}

		public State GetPistonState(List<IMyExtendedPistonBase> pistons)
		{
			var averagePosition = pistons.Select(p => p.CurrentPosition).Average();
			if (averagePosition >= 5.9f)
				return State.Extended;
			else if (averagePosition > 0.25f && averagePosition <= 0.4f)
				return State.Extending;
			else if (averagePosition > 0.4f && averagePosition <= 0.5f)
				return State.ConnectedNewBlock;
			else if (averagePosition > 0.5f && averagePosition <= 2.2f)
				return State.Extending;
			else if (averagePosition > 2.2f && averagePosition <= 2.4f)
				return State.ConnectedOldGrid;
			else if (averagePosition > 2.24 && averagePosition < 5.9f)
				return State.Extending;
			else if (averagePosition <= 0.25f)
				return State.Retracted;
			return State.Extending;
		}

		public enum State
		{
			Retracted,
			ConnectedNewBlock,
			ConnectedOldGrid,
			Extending,
			Extended
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
				//TODO:Deal with full storage system
			}
			return false;
		}
	}
}
