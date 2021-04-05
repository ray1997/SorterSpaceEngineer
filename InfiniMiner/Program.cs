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
			Runtime.UpdateFrequency = UpdateFrequency.Update100;

        }

        public void Main(string argument, UpdateType updateSource)
        {

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
