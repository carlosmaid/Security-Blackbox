using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Game;
using Sandbox.Common.ObjectBuilders;
using System;
using VRage;

// Very thanks to Elsephire from Le grande nuage de magellan server for helping me with this mod.

[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))] //MyObjectBuilder_CubeGrid
public class SecurityCore : MyGameLogicComponent
{
    private MyObjectBuilder_EntityBase builder;

    private static string messageNotposeEN = "Cant build over a grid with an active security core, destroy it first !";
    private static string messageNotposeFR = "Impossible de poser un block sur cette structure, detruiser l'active security core d'abord !";
    private static string messageNotposeES = "No puedes construir sobre un grid enemigo con un security core activo, destruye el core primero !";

    public override void Init(MyObjectBuilder_EntityBase objectBuilder)
    {
        builder = objectBuilder;
        Entity.NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
    }

    public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
    {
        return copy ? builder.Clone() as MyObjectBuilder_EntityBase : builder;
    }

    public override void UpdateOnceBeforeFrame()
    {
        base.UpdateOnceBeforeFrame();
        ((IMyCubeGrid)Entity).OnBlockAdded += SecurityCore.OnBlockAdded;
    }

    public static void OnBlockAdded(IMySlimBlock block)
    {
        // Server
        if (MyAPIGateway.Multiplayer.IsServer)
        {
            try
            {
                //MyLogger.logger("One block added"); // logger debug

                IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;

                if (grid == null)
                    return;

                // Get position of the block
                VRageMath.Vector3D position;
                block.ComputeWorldCenter(out position);

                // create the sphere
                VRageMath.BoundingSphereD sphere = new VRageMath.BoundingSphereD(position, 15);

                // find all players in the sphere
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, p => sphere.Contains(p.GetPosition()) == VRageMath.ContainmentType.Contains);

                IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                IMyCubeBlock targetFunctionalBlock = block.FatBlock as IMyCubeBlock;

                foreach (IMyPlayer player in players)
                {
                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    gridTerminal.GetBlocks(blocks);

                    foreach (var blc in blocks)
                    {
                        if (blc is IMyBeacon && blc.IsWorking)
                        {
                            MyRelationsBetweenPlayerAndBlock relation = blc.GetUserRelationToOwner(player.PlayerID);

                            // y comprueba si el bloque esta a nombre del jugador o de la faccion
                            if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare && relation != MyRelationsBetweenPlayerAndBlock.NoOwnership)
                            {
                                MyLogger.logger(player.DisplayName + "received this " + "'Cant build over a grid with an active security core, destroy it first' in his own language"); // logger debug
                                //MyAPIGateway.Utilities.ShowNotification("Cant build over a grid with an active security core, destroy it first", 5000, MyFontEnum.Red);
                                (grid as IMyCubeGrid).RemoveBlock(block, true);

                                if (block.FatBlock != null)
                                {
                                    block.FatBlock.Close();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.logger("OnBlockAdded->Exception : " + e.ToString());
            }
        }
        // Client
        else
        {
            try
            {
                //MyLogger.logger("One block added"); // logger debug

                IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;

                if (grid == null)
                    return;

                IMyGridTerminalSystem gridTerminal = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
                IMyCubeBlock targetFunctionalBlock = block.FatBlock as IMyCubeBlock;

                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                gridTerminal.GetBlocks(blocks);

                foreach (var blc in blocks)
                {
                    if (blc is IMyBeacon && blc.IsWorking)
                    {
                        MyRelationsBetweenPlayerAndBlock relation = blc.GetUserRelationToOwner(MyAPIGateway.Session.LocalHumanPlayer.PlayerID);

                        // y comprueba si el bloque esta a nombre del jugador o de la faccion
                        if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare && relation != MyRelationsBetweenPlayerAndBlock.NoOwnership)
                        {
                            if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);

                            else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);
                            else
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeEN, 5000, MyFontEnum.Red);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MyLogger.logger("OnBlockAdded->Exception : " + e.ToString());
            }
        }
    }
}