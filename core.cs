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


// For Damage Handling
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using VRage.Game.Entity;

// Very thanks to Elsephire from Le grande nuage de magellan server for helping me with this mod.

[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
public class SecurityCore : MyGameLogicComponent
{
    private MyObjectBuilder_EntityBase builder;

    private static string messageNotposeEN = "Cant build over a grid with an active security core, destroy it first !";
    private static string messageNotposeFR = "Impossible de poser un bloc sur cette structure, détruisez l'active security core d'abord !";
    private static string messageNotposeES = "No puedes construir sobre un grid enemigo con un security core activo, destruye el core primero !";
    public static bool Inited { get; private set; }
    public static bool IsServer { get; private set; }

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
        if (MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null)
        {
            MyLogger.logger("Api Not ready");
            return;
        }

        if (!Inited)
            Init();

        base.UpdateOnceBeforeFrame();
        ((IMyCubeGrid)Entity).OnBlockAdded += SecurityCore.OnBlockAdded;
    }


    void Init()
    {
        MyLogger.logger("Initializing");

        IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnBlockRemoved);

        Inited = true;

    }

    public static void OnBlockAdded(IMySlimBlock block)
    {

        // Server
        // if (MyAPIGateway.Multiplayer.IsServer) //|| local MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE
        if (IsServer)
        {
            try
            {
                //MyLogger.logger("Server: One block added"); //  logger debug

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
                bool isNotFriendly = false;

                foreach (IMyPlayer player in players)
                {
                    foreach (long owner in grid.BigOwners)
                    {
                        if (player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.FactionShare || player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.Owner)
                        {
                            isFriendly = true;
                        }
                        else
                        {
                            isNotFriendly = true;
                        }
                    }
                }

                List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();

                grid.GetBlocks(slimBlocks, b => b.FatBlock != null && b.FatBlock is IMyCubeBlock && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)
                    && b.FatBlock.BlockDefinition.SubtypeId.Contains("Beacon") && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsFunctional && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsWorking);

                bool haveBLCFonctional = false;

                foreach (IMySlimBlock oneBlock in slimBlocks)
                {
                    if (((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsWorking && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsFunctional)
                    {
                        haveBLCFonctional = true;
                        break;
                    }
                }

                if (haveBLCFonctional && isNotFriendly)
                {
                    foreach (IMyPlayer player in players)
                    {
                        if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                        {
                            if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);

                            else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain || MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_HispanicAmerica)
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeES, 5000, MyFontEnum.Red);
                            else
                                MyAPIGateway.Utilities.ShowNotification(messageNotposeEN, 5000, MyFontEnum.Red);
                        }
                        // MyLogger.logger("Server: "+ player.DisplayName + " a essaye de poser un block sur une cubegrid qui ne lui appartient pas"); // logger debug
                    }

                    (grid as IMyCubeGrid).RemoveBlock(block, true);
                }
            }
            catch (Exception e)
            {
                MyLogger.logger("Server: OnBlockAdded->Exception : " + e.ToString());
            }
        }
        // Client
        else
        {
            try
            {


                IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;
                IMyPlayer player = MyAPIGateway.Session.LocalHumanPlayer;

                if (grid == null || player == null)
                    return;

                bool isFriendly = false;
                bool isNotFriendly = false;

                foreach (long owner in grid.BigOwners)
                {
                    if (player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.FactionShare || player.GetRelationTo(owner) == MyRelationsBetweenPlayerAndBlock.Owner)
                    {
                        isFriendly = true;
                    }
                    else
                    {
                        isNotFriendly = true;
                    }
                }

                List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
                grid.GetBlocks(
                    slimBlocks, b => b.FatBlock != null &&
                    b.FatBlock is IMyCubeBlock &&
                    b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon) &&
                    b.FatBlock.BlockDefinition.SubtypeId.Contains("Beacon") &&
                    ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsFunctional &&
                    ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsWorking);

                bool haveBLCFonctional = false;
                foreach (IMySlimBlock oneBlock in slimBlocks)
                {
                    if (((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsWorking &&
                        ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)oneBlock.FatBlock).IsFunctional)
                    {
                        haveBLCFonctional = true;
                        break;
                    }
                }

                if (haveBLCFonctional && isNotFriendly)
                {
                    //MyLogger.logger("Client: llego a la comprobacion : " + messageNotposeES);

                    IMyPlayer actualplayer = MyAPIGateway.Session.Player;
                    if (actualplayer != null) // check this for dedicated servers
                    {

                        if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                        {
                            MyLogger.logger(messageNotposeFR); // logger debug
                            MyAPIGateway.Utilities.ShowNotification(messageNotposeFR, 5000, MyFontEnum.Red);
                        }
                        else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain ||
                            MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_HispanicAmerica)
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

            }
            catch (Exception e)
            {
                MyLogger.logger("Client: OnBlockAdded->Exception : " + e.ToString());
            }
        }
    }



    public static void OnBlockRemoved(object target, ref MyDamageInformation info)
    {
        try
        {
            MyLogger.logger(" Damage Handler TARGET:" + target + " AMOUNT:" + info.Amount + " TYPE:" + info.Type + " ATTACKERID:" + info.AttackerId);

            IMySlimBlock targetBlock = target as IMySlimBlock;
            if (targetBlock == null)
            {
                MyLogger.logger("targetblock null");
                return;
            }

            MyCubeGrid targetGrid = targetBlock.CubeGrid as MyCubeGrid;
            if (targetGrid == null)
                {
                MyLogger.logger("targetgrid null");
                return;
               } 
            /*
               if (!targetGrid.DestructibleBlocks)
               {
                   MyLogger.logger(" Not destructable blocks: " + targetGrid.DestructibleBlocks);
                   info.Amount = 0f;
               }
            
             */

            bool owner = false;
            IMyCubeBlock targetFunctionalBlock = targetBlock.FatBlock as IMyCubeBlock;
            IMyEntity attackerEntity;
            MyLogger.logger(" targetFunctionalBlock: " + targetFunctionalBlock);
            if (targetFunctionalBlock != null && MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attackerEntity))
            {
                if (info.Type == MyDamageType.Grind)
                {
                    //Ship grinding
                    IMyPlayer player = null;
                    if (attackerEntity is IMyShipGrinder)
                        player = MyAPIGateway.Players.GetPlayerControllingEntity(attackerEntity.GetTopMostParent());

                    if (player == null)
                    {
                        List<IMyPlayer> players = new List<IMyPlayer>();
                        MyAPIGateway.Players.GetPlayers(players);

                        double nearestDistance = 5.0f;
                        foreach (var pl in players)
                        {
                            IMyEntity character = pl.Controller.ControlledEntity as IMyEntity;
                            if (character != null)
                            {
                                var distance = (character.GetPosition() - attackerEntity.GetPosition()).LengthSquared();
                                if (distance > nearestDistance)
                                    continue;
                                nearestDistance = distance;
                                player = pl;
                            }
                        }
                    }


                    MyRelationsBetweenPlayerAndBlock relation = targetFunctionalBlock.GetUserRelationToOwner(player.IdentityId);
                    MyLogger.logger(" relation " + relation + " is " + player.IdentityId + " == " + targetFunctionalBlock.OwnerId);
                    owner = (relation == MyRelationsBetweenPlayerAndBlock.Owner || relation == MyRelationsBetweenPlayerAndBlock.FactionShare) ? true : false;

                    if (owner)
                    {
                        MyLogger.logger(" * Target: " + targetGrid.DisplayName + " accepted");

                    }
                    else
                    {
                        MyLogger.logger(" * Target: " + targetGrid.DisplayName + " rejected");
                        info.Amount = 0f;

                    }

                }
            }


        }

        catch (Exception e)
        {
            MyLogger.logger("Error: damage handler: " + e.Message);
        }
    }

}