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


using System.Text.RegularExpressions;




namespace spacelatino
     
{
    class BuildHandler
    {
     
        public static void OnBlockAdded(IMySlimBlock block)
        {

            
            
              MyCubeGrid grid = block.CubeGrid as MyCubeGrid;
              if (grid == null)
                  return;

            

               // IMyFunctionalBlock targetFunctionalBlock = block.FatBlock as IMyFunctionalBlock;
             

                // IMyGridTerminalSystem gridfuncional = block.FatBlock as IMyGridTerminalSystem;
               

               IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
             
            
                 // On récupère la position du block
                  // Recupera la posicion de un bloque
                  VRageMath.Vector3D position;
                  block.ComputeWorldCenter(out position);

                  // Création de la sphère
                  // Creacion de la esfera
                  VRageMath.BoundingSphereD sphere = new VRageMath.BoundingSphereD(position, 10);

                  // Recherche des joueurs présent dans la sphère
                  // Busca jugadores en la esfera
                  List<IMyPlayer> players = new List<IMyPlayer>();
                  MyAPIGateway.Players.GetPlayers(players, p => sphere.Contains(p.GetPosition()) == VRageMath.ContainmentType.Contains);

                 
           

   
               foreach (IMyPlayer player in players)
            {
              
              
             List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
             gridTerminal.GetBlocks(blocks);
             foreach (var blc in blocks)
             {


                 MyRelationsBetweenPlayerAndBlock relation = blc.GetUserRelationToOwner(player.PlayerID); //(player.PlayerID)

                 
                     // y comprueba si el bloque esta a nombre del jugador o de la faccion
                     if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare && relation != MyRelationsBetweenPlayerAndBlock.NoOwnership)
                     {
                         MyAPIGateway.Utilities.ShowNotification("no sos owner", 2000, MyFontEnum.Green);
                         (grid as IMyCubeGrid).RemoveBlock(block, true);
                         if (block.FatBlock != null)
                             block.FatBlock.Close();
                     
                     }
                     else
                    {
                         MyAPIGateway.Utilities.ShowNotification(" sos owner", 2000, MyFontEnum.Blue);
                       
                    }
    
      
             }
           }

                                
            
        }

                     
                       
                
        
    
         

         public static void BeforeDamageHandler(object target, ref MyDamageInformation info)
        {

            MyAPIGateway.Utilities.ShowNotification("grindeando bloque", 2000, MyFontEnum.Green);
            return; 

        }
    }
}
