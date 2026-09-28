using System;
using System.Linq;
using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class CollectionDepthTests
    {
        [Test] public void DexCountsFamiliesOnceButPreservesIndividualFormHistory()
        {
            var p=new Progress();p.ApplyMigrations();
            Assert.AreEqual(1,p.CollectionCount);
            p.RecordCollected("sumdrake");p.RecordCollected("equadragon");
            Assert.AreEqual(1,p.CollectionCount);
            p.AddCaught("numberfly",8);Assert.AreEqual(2,p.CollectionCount);
            Assert.True(p.HasCollectedFamily("countipillar"));Assert.False(p.HasCollected("countipillar"));
            var family=CollectionBook.FamilyEntry(p,"numberfly");
            Assert.AreEqual("countipillar",family.Id);Assert.True(family.Collected);
            Assert.AreEqual("numberfly",family.PortraitId);
            var forms=CollectionBook.Forms(p,"countipillar");
            Assert.AreEqual(GameData.LineFor("countipillar").StageIds.Length,forms.Length);
            Assert.True(forms.All(f=>f.Number==family.Number));
            Assert.AreEqual("???",forms[0].Name);Assert.IsNull(forms[0].PortraitId);
            p.ConvertCaught("countipillar",MathmonConversionReward.Coins);Assert.AreEqual(2,p.CollectionCount);
            Assert.True(p.HasCollectedFamily("numberfly"));
        }

        [Test] public void FamilyGuideContains142UniqueEntriesAnd247FormPages()
        {
            var p=new Progress();p.ApplyMigrations();
            var entries=GameData.Lines.Select(l=>CollectionBook.FamilyEntry(p,l.BaseId)).ToArray();
            Assert.AreEqual(142,entries.Length);Assert.AreEqual(142,entries.Select(e=>e.Id).Distinct().Count());
            CollectionAssert.AreEqual(Enumerable.Range(1,142),entries.Select(e=>e.Number));
            Assert.AreEqual(247,entries.Sum(e=>CollectionBook.Forms(p,e.Id).Length));
            foreach(var species in GameData.Roster)p.RecordCollected(species.Id);
            Assert.AreEqual(142,p.CollectionCount);
            foreach(var world in DepthCatalog.Worlds)foreach(var s in world.Species)
                Assert.AreEqual(1,CollectionBook.Forms(p,s.Id).Length);
        }
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
            for(int i=0;i<Progress.TeamCapacity-1;i++)p.AddCaught("future-family-"+i);
            Assert.True(p.TeamIsFull);Assert.True(p.AwardGiant(id));Assert.False(p.AwardGiant(id));
            Assert.True(p.HasCollected(id));Assert.True(p.PendingGiants.Contains(id));Assert.False(p.ClaimGiant(id));
            p.ConvertCaught(p.CaughtIds[0],MathmonConversionReward.Coins);Assert.True(p.ClaimGiant(id));
            Assert.True(p.Owns(id));Assert.False(p.PendingGiants.Contains(id));Assert.False(p.ClaimGiant(id));
        }

        [Test] public void EntireExistingCatalogFitsWithoutReleasingAnyFamily()
        {
            var p=new Progress();p.ApplyMigrations();
            foreach(var line in GameData.Lines.Where(l=>l.BaseId!="addmander"))
                Assert.AreEqual(CatchRosterResult.Added,p.AddCaught(line.BaseId),line.BaseId);
            Assert.AreEqual(142,p.TeamCount);Assert.False(p.TeamIsFull);
            Assert.AreEqual(142,p.CollectionCount);
        }

        [Test] public void ExistingMegasRemainAvailableAndDepthFormsDoNotUsePlaceholderMegas()
        {
            Assert.True(MegaSystem.Supports("addmander"));Assert.True(MegaSystem.Supports("moonveil"));
            foreach(var mon in DepthCatalog.Worlds.SelectMany(w=>w.Species))Assert.False(MegaSystem.Supports(mon.Id));
            Assert.AreEqual(159,GameData.Roster.Count(s=>MegaSystem.Supports(s.Id)));
        }
    }
}
