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

[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
public class SecurityCore : MyGameLogicComponent
{
    private MyObjectBuilder_EntityBase builder;

    private static string messageNotposeEN = "Cant build over a grid with an active security core, destroy it first !";
    private static string messageNotposeFR = "Impossible de poser un bloc sur cette structure, détruisez l'active security core d'abord !";
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

    private static bool GetPlayerRelation(IMyPlayer player, IMyCubeGrid grid)
    {
        MyRelationsBetweenPlayerAndBlock playerRelation;
        foreach (long owner in grid.BigOwners)
        {
            playerRelation = player.GetRelationTo(owner);
            if (playerRelation == MyRelationsBetweenPlayerAndBlock.FactionShare || playerRelation == MyRelationsBetweenPlayerAndBlock.Owner)
            {
                return true;
            }
        }

        return false;
    }

    public static void OnBlockAdded(IMySlimBlock block)
    {
        if (MyAPIGateway.Multiplayer == null || MyAPIGateway.Utilities == null || MyAPIGateway.Session == null)
        {
            MyLogger.logger("Api not ready. Skipping event.");
            return;
        }

        MyLogger.logger("One block added"); // logger debug

        IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;

        if (grid == null)
        {
            MyLogger.logger("Failed to obtain grid from added block");
            return;
        }

        bool isFriendly = false;
        bool haveBLCFunctional = false;

        // If at least one block in this list there is at least one active BlockSecurityBlackbox
        List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
        grid.GetBlocks(
                    slimBlocks, b => b.FatBlock != null &&
                    b.FatBlock is IMyCubeBlock &&
                    b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon) &&
                    b.FatBlock.BlockDefinition.SubtypeId.Contains("BlockSecurityBlackbox") &&
                    ((IMyFunctionalBlock)b.FatBlock).IsFunctional &&
                    ((IMyFunctionalBlock)b.FatBlock).IsWorking);

        if (slimBlocks.Count > 0) haveBLCFunctional = true;

        // Server
        if (MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();

            try
            {
                // Get position of the block
                VRageMath.Vector3D position;
                block.ComputeWorldCenter(out position);

                // create the sphere
                VRageMath.BoundingSphereD sphere = new VRageMath.BoundingSphereD(position, 15);

                // find all players in the sphere                
                MyAPIGateway.Players.GetPlayers(players, p => sphere.Contains(p.GetPosition()) == VRageMath.ContainmentType.Contains);

                foreach (IMyPlayer player in players)
                {
                    if(GetPlayerRelation(player, grid))
                    {
                        isFriendly = true;
                        break;
                    }
                }

                if (haveBLCFunctional && !isFriendly)
                {
                    foreach (IMyPlayer player in players)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);

                            else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeES, 5000, MyFontEnum.Red);
                            else
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeEN, 5000, MyFontEnum.Red);
                        }
                        MyLogger.logger(player.DisplayName + "has attempted to build a block on a grid that does not belong to him."); // logger debug
                    }

                    (grid as IMyCubeGrid).RemoveBlock(block, true);
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
                IMyPlayer player = MyAPIGateway.Session.LocalHumanPlayer;

                if (player == null)
                {
                    MyLogger.logger("Failed to obtain LocalHumanPlayer");
                    return;
                }

                isFriendly = GetPlayerRelation(player, grid);

                if (haveBLCFunctional && !isFriendly)
                {
                    if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                    {
                        MyLogger.logger(messageNotposeFR); // logger debug
                        MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);
                    }
                    else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain)
                    {
                        MyLogger.logger(messageNotposeES); // logger debug
                        MyAPIGateway.Utilities.ShowNotification(messageNotposeES, 5000, MyFontEnum.Red);
                    }
                    else
                    {
                        MyLogger.logger(messageNotposeEN); // logger debug
                        MyAPIGateway.Utilities.ShowNotification(messageNotposeEN, 5000, MyFontEnum.Red);
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