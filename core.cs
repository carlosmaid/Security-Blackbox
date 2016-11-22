using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

// Very thanks to Elsephire from Le grande nuage de magellan server for helping me with this mod.
[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
public class SecurityCore : MyGameLogicComponent
{
    private MyObjectBuilder_EntityBase builder;

    public static bool IsInitialized { get; private set; }
    public static bool IsServer { get; private set; }

    public override void Init(MyObjectBuilder_EntityBase objectBuilder)
    {
        base.Init(objectBuilder);
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

        if (!IsInitialized)
            Init();

        base.UpdateOnceBeforeFrame();
    }


    void Init()
    {
        MyLogger.logger("Initializing");

        IsServer = MyAPIGateway.Multiplayer.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

        MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, OnBlockRemoved);

        ((IMyCubeGrid)Entity).OnBlockAdded += SecurityCore.OnBlockAdded;

        IsInitialized = true;
    }

    public static void OnBlockAdded(IMySlimBlock block)
    {
        IMyPlayer player = null;
        string message = "Security Beacon in range. Cannot build on an enemy area. Canceling construction.";

        MyLogger.logger("Server: One block added"); //  logger debug
        List<IMyPlayer> players = new List<IMyPlayer>();

        if (!IsServer)
        {
            player = MyAPIGateway.Session.LocalHumanPlayer;
            IMyPlayer actualplayer = MyAPIGateway.Session.Player;

            if (player == null)
            {
                MyLogger.logger("Local Human Player is null. Core failing. Cancel construction.");
                block.CubeGrid.RemoveBlock(block, true);

                if (block.FatBlock != null)
                    block.FatBlock.Close();
                return;
            }
            else
            {
                players.Add(player);
                if (actualplayer != null) // check this for dedicated servers
                {
                    if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.French)
                    {
                        message = "Impossible de poser un bloc sur cette structure, détruisez l'active security core d'abord !";
                    }
                    else if (MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_Spain ||
                        MyAPIGateway.Session.Config.Language == MyLanguagesEnum.Spanish_HispanicAmerica)
                    {
                        message = "No puedes construir sobre un grid enemigo con un security core activo, destruye el core primero !";
                    }
                    else
                    {
                        message = "Cant build over a grid with an active security core, destroy it first !";
                    }
                }
            }
        }

        try
        {
            // Get position of the block
            Vector3D position;
            block.ComputeWorldCenter(out position);

            // create the sphere
            BoundingSphereD sphere = new BoundingSphereD(position, 500);

            if (players.Count < 1)
            {
                MyAPIGateway.Players.GetPlayers(players, p => sphere.Contains(p.GetPosition()) == ContainmentType.Contains);
            }

            IMyCubeBlock securityCore = GetSecurityCoreInRadius(ref sphere);

            if (securityCore != null)
            {
                bool allowConstruction = true;

                foreach(var p in players)
                {
                    if (securityCore.OwnerId != p.IdentityId)
                    {
                        MyRelationsBetweenPlayerAndBlock relation = securityCore.GetUserRelationToOwner(p.IdentityId);
                        MyLogger.logger(" relation " + relation + " is " + p.IdentityId + " == " + securityCore.OwnerId);

                        if (relation != MyRelationsBetweenPlayerAndBlock.NoOwnership
                                || relation != MyRelationsBetweenPlayerAndBlock.FactionShare)
                        {
                            MyLogger.logger("Enemy Security Beacon found. Canceling construction.");
                            Utilities.SendMessageTo(Utilities.clientIdMessage, message, p);
                            allowConstruction = false;
                            break;
                        }
                    }
                }

                if(!allowConstruction)
                {
                    block.CubeGrid.RemoveBlock(block, true);

                    if (block.FatBlock != null)
                        block.FatBlock.Close();
                }
            }
            else
            {
                MyLogger.logger(String.Format("Cannot find any Security Core(s) in the radius. Assuming no cores or enemy bases nearby. Position: {0}, Radius: 500", position));
            }
        }
        catch (Exception e)
        {
            MyLogger.logger("Server: OnBlockAdded->Exception : " + e.Message);
        }
    }

    // Get ANY Security Core from ANY grid within the radius. To contemplate for someone building on the ground within 500 meters of the core or it's grid.
    private static IMyCubeBlock GetSecurityCoreInRadius(ref BoundingSphereD sphere)
    {
        List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
        List<IMyCubeGrid> gridsInSphere = entities.Where(e => e is IMyCubeGrid).Cast<IMyCubeGrid>().ToList();

        MyLogger.logger(String.Format("grids in sphere: {0}", gridsInSphere.Count));
        if (gridsInSphere.Count < 1)
        {
            MyLogger.logger("No grids found in radius. Security Core null");
            return null;
        }

        List<IMySlimBlock> blocksInSphere = null;
        foreach (var g in gridsInSphere)
        {
            blocksInSphere = g.GetBlocksInsideSphere(ref sphere);
            MyLogger.logger(String.Format("blocks in sphere: {0}", blocksInSphere.Count));

            IMyCubeGrid grid;
            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            foreach (var block in blocksInSphere)
            {
                grid = block.CubeGrid;

                if (grid != null)
                {
                    grid.GetBlocks(blocks, b => b.FatBlock != null
                    & b.FatBlock is IMyCubeBlock
                    && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)
                    && b.FatBlock.BlockDefinition.SubtypeId.Contains("Beacon")
                    && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsFunctional
                    && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsWorking);

                    MyLogger.logger(String.Format("blocks in grid: {0}", blocks.Count));
                    if (blocks.Count > 0)
                    {
                        return blocks[0].FatBlock;
                    }
                }
            }
        }

        return null;
    }

    public static void OnBlockRemoved(object target, ref MyDamageInformation info)
    {
        if (info.Type == MyDamageType.Deformation
                || info.Type == MyDamageType.Drill
                || info.Type == MyDamageType.Grind)
        {
            try
            {
                MyLogger.logger(" Damage Handler TARGET:" + target + " AMOUNT:" + info.Amount + " TYPE:" + info.Type + " ATTACKERID:" + info.AttackerId);

                IMyEntity attackerEntity;

                if (MyAPIGateway.Entities.TryGetEntityById(info.AttackerId, out attackerEntity))
                {
                    MyLogger.logger(String.Format("Attacker entity acquired: {0}", attackerEntity.Name));
                    List<IMyPlayer> players = new List<IMyPlayer>();

                    IMyPlayer player = null;

                    if (attackerEntity is IMyShipGrinder || attackerEntity is IMyShipDrill)
                        player = MyAPIGateway.Players.GetPlayerControllingEntity(attackerEntity.GetTopMostParent());

                    if (attackerEntity is IMyPlayer)
                        player = attackerEntity as IMyPlayer;

                    if (player == null)
                    {
                        // create the sphere
                        BoundingSphereD sphere = new BoundingSphereD(attackerEntity.GetPosition(), 50);

                        MyAPIGateway.Players.GetPlayers(players, p => sphere.Contains(p.GetPosition()) == ContainmentType.Contains);
                    }
                    else
                    {
                        players.Add(player);
                    }

                    if (players.Count < 1)
                    {
                        MyLogger.logger("No players found on ship or in radius.");
                    }

                    IMySlimBlock targetBlock = target as IMySlimBlock;
                    if (targetBlock == null)
                    {
                        IMyVoxelBase voxel = target as IMyVoxelBase;
                        if (voxel == null)
                        {
                            MyLogger.logger("targetblock and voxel are null");
                            return;
                        }
                        else
                        {
                            HandleVoxelDamage(target, ref info, voxel, players);
                        }
                    }
                    else
                    {
                        HandleGridDamage(target, ref info, targetBlock, players);
                    }
                }
                else
                {
                    MyLogger.logger(String.Format("Could not find Attacker Entity by AttackerId: {0}", info.AttackerId));
                }
            }
            catch (Exception e)
            {
                MyLogger.logger("Error: damage handler: " + e.Message);
            }
        }
    }

    private static void HandleVoxelDamage(object target, ref MyDamageInformation info, IMyVoxelBase voxel, List<IMyPlayer> players)
    {
        // create the sphere
        BoundingSphereD sphere = new BoundingSphereD(voxel.GetPosition(), 500);
        List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);
        List<IMySlimBlock> blocksInSphere = entities.Where(e => e is IMySlimBlock).Cast<IMySlimBlock>().ToList();

        IMyCubeBlock securityCore = blocksInSphere.Where(b => b.FatBlock != null
            && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)
            && b.FatBlock.BlockDefinition.SubtypeId.Contains("Beacon")
            && ((IMyFunctionalBlock)b.FatBlock).IsFunctional
            && ((IMyFunctionalBlock)b.FatBlock).IsWorking).Cast<IMyCubeBlock>().FirstOrDefault();

        if (securityCore == null)
        {
            MyLogger.logger(String.Format("Could not find a security core within 500m radius. Center: {0}. Allowing damage.", voxel.GetPosition()));
        }
        else
        {
            HandleDamage(target, ref info, securityCore, players);
        }
    }

    private static void HandleGridDamage(object target, ref MyDamageInformation info, IMySlimBlock targetBlock, List<IMyPlayer> players)
    {
        IMyCubeGrid targetGrid = targetBlock.CubeGrid as IMyCubeGrid;
        if (targetGrid == null)
        {
            MyLogger.logger("targetgrid null");
            return;
        }

        IMyCubeBlock securityCore = null;
        List<IMySlimBlock> slimBlocks = new List<IMySlimBlock>();
        targetGrid.GetBlocks(slimBlocks, b => b.FatBlock != null
                & b.FatBlock is IMyCubeBlock
                && b.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Beacon)
                && b.FatBlock.BlockDefinition.SubtypeId.Contains("Beacon")
                && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsFunctional
                && ((Sandbox.ModAPI.Ingame.IMyFunctionalBlock)b.FatBlock).IsWorking);
        if (slimBlocks.Count > 0)
            securityCore = slimBlocks[0].FatBlock;

        if (securityCore == null)
        {
            MyLogger.logger("Cannot find a security beacon in the grid. Allowing normal damage.");
        }
        else
        {
            HandleDamage(target, ref info, securityCore, players);
        }
    }

    public static void HandleDamage(object target, ref MyDamageInformation info, IMyCubeBlock securityCore, List<IMyPlayer> players)
    {
        MyRelationsBetweenPlayerAndBlock relation;
        bool damageAllowed = true;

        foreach (var pl in players)
        {
            if (securityCore.OwnerId != pl.IdentityId)
            {
                relation = securityCore.GetUserRelationToOwner(pl.IdentityId);
                MyLogger.logger(" relation " + relation + " is " + pl.IdentityId + " == " + securityCore.OwnerId);

                if (relation != MyRelationsBetweenPlayerAndBlock.NoOwnership
                        //|| relation != MyRelationsBetweenPlayerAndBlock.Owner
                        || relation != MyRelationsBetweenPlayerAndBlock.FactionShare)
                    damageAllowed = false;
            }
        }

        if (damageAllowed)
        {
            MyLogger.logger(" * Target: " + target + " accepted");
        }
        else
        {
            MyLogger.logger(" * Target: " + target + " rejected");
            info.Amount = 0f;
        }
    }
}