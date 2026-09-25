using System;
using System.Linq;
using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class CollectionDepthTests
    {
        [Test] public void CatalogHasEightyOrdinaryAndEightUniqueGiants()
        {
            Assert.AreEqual(8,DepthCatalog.Worlds.Length);
            Assert.AreEqual(247,GameData.Roster.Count);Assert.AreEqual(142,GameData.Lines.Count);
            Assert.AreEqual(247,GameData.Roster.Select(s=>s.Id).Distinct().Count());
            foreach(var world in DepthCatalog.Worlds)
            {
                Assert.AreEqual(10,world.Species.Count(s=>!s.Giant));Assert.AreEqual(1,world.Species.Count(s=>s.Giant));
                foreach(var s in world.Species){Assert.NotNull(GameData.SpeciesById(s.Id));Assert.IsNotEmpty(s.Description);}
            }
            foreach(var s in GameData.Roster)Assert.IsNotEmpty(CollectionBook.Description(s.Id));
        }

        [Test] public void CollectionPersistsAfterReleaseAndDoesNotRevealUnownedForms()
        {
            var p=new Progress();p.ApplyMigrations();Assert.True(p.HasCollected("addmander"));Assert.False(p.HasCollected("sumdrake"));
            p.AddCaught("numberfly",8);Assert.True(p.HasCollected("numberfly"));Assert.False(p.HasCollected("countipillar"));
            p.ConvertCaught("countipillar",MathmonConversionReward.Coins);Assert.True(p.HasCollected("numberfly"));
            Assert.False(p.Owns("countipillar"));
            var entry=CollectionBook.Entry(p,"equadragon");Assert.AreEqual("???",entry.Name);Assert.IsNull(entry.Description);
            Assert.IsNull(entry.Element);Assert.IsNull(entry.PortraitId);
        }

        [Test] public void MigrationSeedsOnlyEvidenceAndIsIdempotent()
        {
            var p=new Progress {SaveVersion=12,Coins=1234,CurrentMap="mountains"};p.CaughtIds.Add("paircub");
            p.MonGrowth.Add(new MonGrowth {BaseId="paircub",Stage=2,Level=44});p.OpenedChests.Add("forest-chest-6-6");
            p.ApplyMigrations();Assert.True(p.HasCollected("equilibear"));Assert.False(p.HasCollected("matchbear"));
            Assert.AreEqual(1234,p.Coins);Assert.AreEqual(44,p.FindGrowth("paircub").Level);
            var ids=p.CollectedForms.ToArray();p.ApplyMigrations();CollectionAssert.AreEqual(ids,p.CollectedForms);
            CollectionAssert.Contains(p.OpenedChests,"forest-chest-6-6");Assert.AreEqual("mountains",p.CurrentMap);
        }

        [Test] public void GiantRewardsArePermanentClaimableAndIdempotent()
        {
            var p=new Progress();p.ApplyMigrations();string id=DepthCatalog.Worlds[0].Giant.Id;
            foreach(var line in GameData.Lines.Where(l=>l.BaseId!="addmander"&&l.BaseId!=id).Take(98))p.AddCaught(line.BaseId);
            Assert.True(p.TeamIsFull);Assert.True(p.AwardGiant(id));Assert.False(p.AwardGiant(id));
            Assert.True(p.HasCollected(id));Assert.True(p.PendingGiants.Contains(id));Assert.False(p.ClaimGiant(id));
            p.ConvertCaught(p.CaughtIds[0],MathmonConversionReward.Coins);Assert.True(p.ClaimGiant(id));
            Assert.True(p.Owns(id));Assert.False(p.PendingGiants.Contains(id));Assert.False(p.ClaimGiant(id));
        }

        [Test] public void ExistingMegasRemainAvailableAndDepthFormsDoNotUsePlaceholderMegas()
        {
            Assert.True(MegaSystem.Supports("addmander"));Assert.True(MegaSystem.Supports("moonveil"));
            foreach(var mon in DepthCatalog.Worlds.SelectMany(w=>w.Species))Assert.False(MegaSystem.Supports(mon.Id));
            Assert.AreEqual(159,GameData.Roster.Count(s=>MegaSystem.Supports(s.Id)));
        }
    }
}
