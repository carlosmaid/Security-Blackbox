using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace spacelatino
{
     [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
     {

    void Init()
        {

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BuildHandler.BeforeDamageHandler);

           try
           {
               MyCubeBlockDefinition custom = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Beacon), "LargeBlockBeacon"));
               if (custom != null)
               {
                   custom = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Beacon), "LargeBlockBeacon"));
                   custom.Public = false;
               }
               
           }
           catch (Exception ex) { }

           
         }

         public override void UpdateBeforeSimulation()
        {
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null) // exit if api is not ready
                    return;

            
               
                    HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(entities, x => x is IMyCubeGrid);
                    foreach (IMyEntity entity in entities)
                    {
                        IMyCubeGrid grid = entity as IMyCubeGrid;
                        if (grid == null)
                            continue;
                        grid.OnBlockAdded += BuildHandler.OnBlockAdded;
                                                    
                        
                    }
              
            }
        }



         public static bool isActiveBeaconSecurity(IMyCubeGrid grid)
             // esta funcion devuelve true si el beacon de seguridad esta presente
             //  en el grid y esta a nombre de un jugador y esta enecendido.
         {
             if (grid == null)
                 return false;

             IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
             if (gridTerminal == null)
                 return false;
             List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
             gridTerminal.GetBlocks(blocks);
             foreach (var block in blocks)
             {
                Exchangerlogic bs = block.GameLogic as Exchangerlogic;
                 if (bs != null && bs.IsBeaconSecurity && bs.OwnerId != 0 && bs.IsPowered) 
                     return true;
             }
             return false;
         }

 }
    }


            

   

           