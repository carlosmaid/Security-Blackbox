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

                bool isFriendly = false;

                foreach (IMyPlayer player in players)
                {
                    foreach (long owner in grid.BigOwners)
                    {
                        if (player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.FactionShare || player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.Owner)
                        {
                            isFriendly = true;
                        }
                    }
                }

                List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();

                grid.GetBlocks(slimBlocks, b => b.FatBlock != null && b.FatBlock is IMyCubeBlock && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon) 
                    && b.FatBlock.BlockDefinition.SubtypeId.Contains("BlockSecurityBlackbox") && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsFunctional && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsWorking);

                bool haveBLCFonctional = false;

                foreach (IMySlimBlock oneBlock in slimBlocks)
                {
                    if (((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsWorking && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsFunctional)
                    {
                        haveBLCFonctional = true;
                    }
                }

                if (haveBLCFonctional && !isFriendly)
                {
                    foreach (IMyPlayer player in players)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);

                            else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);
                            else
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeEN, 5000, MyFontEnum.Red);
                        }
                        MyLogger.logger(player.DisplayName + "a essaye de poser un block sur une cubegrid qui ne lui appartient pas"); // logger debug
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
                MyLogger.logger("One block added"); // logger debug

                IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;
                IMyPlayer player = MyAPIGateway.Session.LocalHumanPlayer;

                if (grid == null || player == null)
                    return;

                bool isFriendly = false;
                foreach (long owner in grid.BigOwners)
                {
                    if (player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.FactionShare || player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.Owner)
                    {
                        isFriendly = true;
                    }
                }

                List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
                grid.GetBlocks(slimBlocks, b => b.FatBlock != null && b.FatBlock is IMyCubeBlock && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)
                    && b.FatBlock.BlockDefinition.SubtypeId.Contains("BlockSecurityBlackbox") && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsFunctional && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsWorking);

                bool haveBLCFonctional = false;
                foreach (IMySlimBlock oneBlock in slimBlocks)
                {
                    if (((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsWorking && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsFunctional)
                    {
                        haveBLCFonctional = true;
                        break;
                    }
                }

                if (haveBLCFonctional && !isFriendly)
                {
                    if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                        MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);

                    else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain)
                        MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);
                    else
                        MyAPIGateway.Utilities.ShowNotification(messageNotposeEN, 5000, MyFontEnum.Red);
                }
            }
            catch (Exception e)
            {
                MyLogger.logger("OnBlockAdded->Exception : " + e.ToString());
            }
        }
    }
}