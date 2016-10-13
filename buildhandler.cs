using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.ModAPI;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Components;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;



namespace spacelatino

{
    class BuildHandler
    {
        
        public static void grid_OnBlockAdded(IMySlimBlock obj)
        {


              MyCubeGrid grid = obj.CubeGrid as MyCubeGrid;
                if (grid == null)
                    return;

                bool removing = false;
            
            // aca se puede agregar codigo para que chequee faccion y no permita grindear
            //  o dañar un bloque de un grid enemigo, pero si destruirlo con armamento

                
                      removing = true;
                       
                if (removing)
                {
                    (grid as IMyCubeGrid).RemoveBlock(obj, true);
                    if (obj.FatBlock != null)
                        obj.FatBlock.Close();
                }
            
        }

         public static void BeforeDamageHandler(object target, ref MyDamageInformation info)
        {

            MyAPIGateway.Utilities.ShowNotification("grindeando bloque", 2000, MyFontEnum.Green);
            return; 

        }
    }
}
