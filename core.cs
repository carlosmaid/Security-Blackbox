using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common;
// using Sandbox.Common.components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Definitions;
using Sandbox.Engine;
using SpaceEngineers.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.Definitions;
using SpaceEngineers;
using SpaceEngineers.Game;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using SpaceEngineers.Game.ModAPI;
using VRageMath;



namespace spacelatino
{
    


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))]
    public class Exchangerlogic : MyGameLogicComponent
        {
        VRage.ObjectBuilders.MyObjectBuilder_EntityBase m_objectBuilder = null;
        private bool m_greeted = false;
        IMyFunctionalBlock m_block;
        public long OwnerId { get { return m_block.OwnerId; } }
        public bool IsBeaconSecurity { get; private set; }
 
  
        
        public override void Close()
        {

        }

        public override void Init(VRage.ObjectBuilders.MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
                        
            m_objectBuilder = objectBuilder;
            m_block = Entity as IMyFunctionalBlock;

        }

        public override void MarkForClose()
        {
        }

        public override void UpdateAfterSimulation()
        {
        }

        public override void UpdateAfterSimulation10()
        {
        }

        public override void UpdateAfterSimulation100()
        {
        }

        public override void UpdateBeforeSimulation()
        {
                  }

        public override void UpdateBeforeSimulation10()
        {
            if (MyAPIGateway.Session.Player == null)
            {
                return;
            }

            // si esta a menos de 3 metros
           if ((MyAPIGateway.Session.Player.GetPosition() - Entity.GetPosition()).Length() < 3f)
            {
              // si no fue saludado, tiene energia y esta a nombre de alguien
               if (!m_greeted && IsPowered && m_block.OwnerId != 0)
                {
                   // Itera por todos los players 
                   List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, x => x.Controller != null && x.Controller.ControlledEntity != null);
                foreach (IMyPlayer player in players)
                {
                    // y comprueba si el beacon esta a nombre del jugador o de la faccion
                    MyRelationsBetweenPlayerAndBlock relation = m_block.GetUserRelationToOwner(player.PlayerID);
                    if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare)
                        continue;
                    // si el jugador es de la faccion entonces:

                    // muestra un mensaje
                    MyAPIGateway.Utilities.ShowMissionScreen("Titulo", "subtitulo", "", "Bla bla bla...");
                    // setea el radio de transmision a 2000 metros
                    TerminalPropertyExtensions.SetValueFloat((IMyFunctionalBlock)m_block, "Radius", 2000f);
                    m_greeted = true;
                }
             }    
                   
            }   
            
           
        }

        public override void UpdateBeforeSimulation100()
        {
         
              
        }

        public override void UpdateOnceBeforeFrame()
        {
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return m_objectBuilder;
        }


        public bool IsPowered
        {
            // esta funcion chequea si un grid tiene una fuente de energia activada
            // ya sea un reactor, una bateria o un panel solar.
            get
            {
                IMyCubeGrid grid = (IMyCubeGrid)Entity.GetTopMostParent();
                if (grid == null)
                    return false;

                IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                gridTerminal.GetBlocks(blocks);
                foreach (var block in blocks)
                {
                    if (block is IMyReactor)
                    {
                        if (block.IsWorking)
                            return true;
                    }

                    var battery = block as IMyBatteryBlock;
                    if (battery != null)
                    {
                        if (battery.CurrentStoredPower > 0f && battery.IsWorking)
                            return true;
                    }

                    var solar = block as IMySolarPanel;
                    if (solar != null)
                    {
                        if (solar.IsWorking)
                            return true;
                    }
                }
                return false;
            }
           }

    }
}
