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
			Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

		public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
			//Extend pistons into projector
			//Detect blocks from sensor if they found an unfinished block yet
			//If found unfinished block, stop piston and let welder working
			//After block finish continue extend piston
			//Repeat checking unfinished block and stop piston
			//After reaching bottom, go back to top and retract piston a bit
			//Repeat on checking block and weld it to bottom
			//Activate connector welder
			//After all blocks finished, turn on merge block, then
			//activate top piston and push it down
			//After fully extended disable top merge block
			//Let bottom merge block merged with drill part
			//retract top piston
			//--Bottom part
			//Let block merge
        }
    }
}
