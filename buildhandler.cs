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

           
                // y comprueba si el beacon esta a nombre del jugador o de la faccion
                
             //  MyRelationsBetweenPlayerAndBlock relation = block.GetUserRelationToOwner(player.PlayerID);
               // if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare)

                    continue;
                // si el jugador es de la faccion entonces:

                // muestra un mensaje si el que construye no es el owner
                MyAPIGateway.Utilities.ShowMissionScreen("Titulo", "subtitulo", "", "Bla bla bla...");
            }

        }

        /*
        public static void grid_OnBlockAdded(IMySlimBlock obj)
        {


              MyCubeGrid grid = obj.CubeGrid as MyCubeGrid;
                if (grid == null)
                    return;

                bool removing = false;
            
            // aca se puede agregar codigo para que chequee faccion y no permita grindear
            //  o dañar un bloque de un grid enemigo, pero si destruirlo con armamento
            // Comentario de prueba DSM #1, solo para test de COMMIT.
                
                      removing = true;
                       
                if (removing)
                {
                    (grid as IMyCubeGrid).RemoveBlock(obj, true);
                    if (obj.FatBlock != null)
                        obj.FatBlock.Close();
                }
            
        }
         */

         public static void BeforeDamageHandler(object target, ref MyDamageInformation info)
        {

            MyAPIGateway.Utilities.ShowNotification("grindeando bloque", 2000, MyFontEnum.Green);
            return; 

        }
    }
}
