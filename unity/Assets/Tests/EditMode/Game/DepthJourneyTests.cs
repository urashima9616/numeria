using System;
using System.Linq;
using Numeria.Core;
using NUnit.Framework;

namespace Numeria.Game.Tests
{
    public class DepthJourneyTests
    {
        [Test] public void EveryDepthHasSequentialReachableMechanismsAndAnAlwaysOpenExit()
        {
            Assert.AreEqual(16,Maps.AllPlayable().Length);
            Assert.AreEqual(8,DepthCatalog.Worlds.Select(w=>string.Join("\n",Maps.Get(w.Id).Rows)).Distinct().Count());
            foreach(var world in DepthCatalog.Worlds)
            {
                var def=Maps.Get(world.Id);var p=new Progress();p.ApplyMigrations();var grid=GridMap.Parse(def.Rows);
                Assert.False(Maps.CanVisit(world.Id,p));
                Assert.AreEqual(10,def.Encounters.Length);Assert.AreEqual(world.Giant.Id,def.BossSpeciesId);
                var boss=def.RollBossEncounter(1,new Rng(1));var trained=def.RollBossEncounter(99,new Rng(1));
                Assert.AreEqual(boss.Level,trained.Level);Assert.AreEqual(world.Level,boss.Level);
                for(int stage=0;stage<3;stage++)
                {
                    DepthJourney.Apply(grid,def,p);
                    var d=def.Discoveries[stage];Assert.IsNotEmpty(grid.FindPath(grid.Spawn,(d.X,d.Y)),world.Id+" stage "+stage);
                    Assert.IsNotEmpty(grid.FindPath(grid.Spawn,DepthJourney.Entrance(def).Value));
                    Assert.IsEmpty(grid.FindPath(grid.Spawn,(34,4)),world.Id+" giant must wait");
                    p.CollectDiscovery(d.Id);
                }
                DepthJourney.Apply(grid,def,p);Assert.True(def.GuardianReady(p));
                Assert.IsNotEmpty(grid.FindPath(grid.Spawn,(34,4)));
                for(int y=0;y<grid.Height;y++)for(int x=0;x<grid.Width;x++)
                    if(grid.At(x,y)==Tile.Chest||grid.At(x,y)==Tile.Bush)Assert.IsNotEmpty(grid.FindPath(grid.Spawn,(x,y)),world.Id+" inaccessible "+x+","+y);
                var parent=Maps.Get(world.ParentId);Assert.NotNull(DepthJourney.Entrance(parent));
            }
        }
    }
}
