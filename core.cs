using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using System.Text.RegularExpressions;
using VRage.Game;

[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
public class RenameCubeGrid : MyGameLogicComponent
{
    private MyObjectBuilder_EntityBase builder;

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
        ((IMyCubeGrid)Entity).OnBlockAdded += RenameCubeGrid.OnBlockAdded;
    }

    public static void OnBlockAdded(IMySlimBlock block)
    {

        IMyCubeGrid grid = block.CubeGrid as IMyCubeGrid;
        if (grid == null)
            return;

        
        // On récupère la position du block
        VRageMath.Vector3D position;
        block.ComputeWorldCenter(out position);
            
        // Création de la sphère
        VRageMath.BoundingSphereD sphere = new VRageMath.BoundingSphereD(position, 15);
            
        // Recherche des joueurs présent dans la sphère
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

                  if (blc is IMyBeacon && blc.IsWorking){
                      
                 MyRelationsBetweenPlayerAndBlock relation = blc.GetUserRelationToOwner(player.PlayerID); //(player.PlayerID)

                      // y comprueba si el bloque esta a nombre del jugador o de la faccion
                     if (relation != MyRelationsBetweenPlayerAndBlock.Owner && relation != MyRelationsBetweenPlayerAndBlock.FactionShare && relation != MyRelationsBetweenPlayerAndBlock.NoOwnership)
                     {
                         MyAPIGateway.Utilities.ShowNotification("cant build over a grid with an active security core, destroy it first", 2000, MyFontEnum.Red);
                         (grid as IMyCubeGrid).RemoveBlock(block, true);
                         if (block.FatBlock != null)
                             block.FatBlock.Close();
                     
                     }

                  }

             }
            
        }
    }

}

    
  
 

