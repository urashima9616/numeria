using System;
using System.Linq;
using Numeria.Core;
using NUnit.Framework;

namespace Numeria.Game.Tests
{
    public class CollectionEncounterTests
    {
        private static MapDef Ecology() => new MapDef
        {
            Id = "collection-test", WildMaxLevel = 8,
            Encounters = new[]
            {
                new EncounterEntry { SpeciesId = "acornib", Weight = 90, MinimumLevel = 3, MaximumLevel = 5 },
                new EncounterEntry { SpeciesId = "dewhopper", Weight = 10, MinimumLevel = 6, MaximumLevel = 8 },
            },
        };

        [Test] public void TenPercentMissingFormBecomesSixtyFourPercent()
        {
            var map = Ecology(); var p = new Progress(); p.RecordCollected("acornib");
            var weights = map.WildEncounterWeights(p);
            Assert.AreEqual(36, weights[0], 1e-9);
            Assert.AreEqual(64, weights[1], 1e-9);
            Assert.AreEqual(100, weights.Sum(), 1e-9);
        }

        [Test] public void EmptyAndCompleteLocalCollectionsKeepOriginalWeights()
        {
            var map = Ecology(); var p = new Progress();
            CollectionAssert.AreEqual(new double[] {90, 10}, map.WildEncounterWeights());
            CollectionAssert.AreEqual(map.WildEncounterWeights(), map.WildEncounterWeights(p));
            foreach (var entry in map.Encounters) p.RecordCollected(entry.SpeciesId);
            CollectionAssert.AreEqual(map.WildEncounterWeights(), map.WildEncounterWeights(p));
        }

        [Test] public void CaptureImmediatelyMovesBonusAndReleaseDoesNotRestoreIt()
        {
            var map = Ecology(); var p = new Progress(); p.ApplyMigrations();
            p.AddCaught("acornib", 3);
            Assert.AreEqual(64, map.WildEncounterWeights(p)[1], 1e-9);
            p.AddCaught("dewhopper", 6);
            CollectionAssert.AreEqual(map.WildEncounterWeights(), map.WildEncounterWeights(p));
            p.ConvertCaught("dewhopper", MathmonConversionReward.Coins);
            Assert.False(p.Owns("dewhopper"));
            CollectionAssert.AreEqual(map.WildEncounterWeights(), map.WildEncounterWeights(p));
        }

        [Test] public void AnyCollectedEvolutionRemovesBonusForTheWholeFamily()
        {
            var map = Ecology(); map.Encounters[0].SpeciesId = "countipillar";
            map.Encounters[1].SpeciesId = "numberfly";
            var p = new Progress(); p.RecordCollected("countipillar");
            CollectionAssert.AreEqual(new double[] {90,10}, map.WildEncounterWeights(p));
            p.CollectedForms.Clear();p.RecordCollected("numberfly");
            CollectionAssert.AreEqual(new double[] {90,10}, map.WildEncounterWeights(p));
            // A genuinely new family remains eligible even alongside an uncollected
            // form belonging to an already collected family.
            map.Encounters[1].SpeciesId="dewhopper";
            Assert.AreEqual(64, map.WildEncounterWeights(p)[1], 1e-9);
        }

        [Test] public void RelativeRarityAndDisabledEntriesArePreserved()
        {
            var map = Ecology(); var p = new Progress(); p.RecordCollected("acornib");
            map.Encounters = map.Encounters.Concat(new[]
            {
                new EncounterEntry { SpeciesId = "fernfawn", Weight = 20 },
                new EncounterEntry { SpeciesId = "arboracle", Weight = 0 },
                new EncounterEntry { SpeciesId = "mushmellow", Weight = -10 },
            }).ToArray();
            var weights = map.WildEncounterWeights(p);
            Assert.AreEqual(2, weights[2] / weights[1], 1e-9);
            Assert.AreEqual(0, weights[3]); Assert.AreEqual(0, weights[4]);
            Assert.AreEqual(120, weights.Sum(), 1e-9);
        }

        [Test] public void AllSixteenMapsRetainLocalEcologyLevelsAndBosses()
        {
            var p = new Progress(); p.ApplyMigrations(); var rng = new Rng(8132);
            foreach (var map in Maps.AllPlayable())
            {
                foreach (var entry in map.Encounters.Skip(1)) p.RecordCollected(entry.SpeciesId);
                for (int i = 0; i < 100; i++)
                {
                    var wild = map.RollWildEncounter(99, rng, p);
                    var entry = map.Encounters.Single(e => e.SpeciesId == wild.Id);
                    Assert.That(wild.Level, Is.InRange(entry.MinimumLevel, entry.MaximumLevel), map.Id);
                    Assert.True(wild.Catchable); Assert.False(wild.IsBoss);
                    Assert.False(DepthCatalog.Species(wild.Id)?.Giant ?? false);
                }
                var boss = map.RollBossEncounter(99, new Rng(19));
                Assert.AreEqual(map.BossSpeciesId, boss.Id);
                Assert.AreEqual(map.BossMinLevel, boss.Level);
            }
        }

        [Test] public void RuntimePathHelpsCatchableWildsWithoutBoostingMasterSpecies()
        {
            var map = Ecology(); var p = new Progress(); p.ApplyMigrations(); p.RecordCollected("acornib");
            var rng = new Rng(761231); int wilds = 0, newWilds = 0, masters = 0, newMasters = 0, streak = 0;
            for (int i = 0; i < 8000; i++)
            {
                var foe = map.RollRandomEncounter(p, rng);
                if (string.IsNullOrEmpty(foe.TrainerId))
                {
                    wilds++; streak++; if (foe.Id == "dewhopper") newWilds++;
                    Assert.True(foe.Catchable);
                    Assert.LessOrEqual(streak, TrainerEncounterSession.MaximumOrdinaryStreak);
                }
                else
                {
                    masters++; streak = 0; if (foe.Id == "dewhopper") newMasters++;
                    Assert.False(foe.Catchable);
                    Assert.That(foe.TrainerBonusPercent, Is.InRange(20, 40));
                }
            }
            Assert.That((double)newWilds / wilds, Is.InRange(.60, .68));
            Assert.That((double)newMasters / masters, Is.InRange(.07, .13));
            // 30% base chance plus the unchanged fifth-encounter drought guarantee.
            Assert.That(masters / 8000d, Is.InRange(.33, .40));
        }

        [Test] public void InvalidEcologyFailsClearlyInsteadOfSpawningDisabledSpecies()
        {
            var map = Ecology(); map.Encounters[0].Weight = 0; map.Encounters[1].Weight = -1;
            Assert.Throws<InvalidOperationException>(() => map.RollWildEncounter(1, new Rng(0), new Progress()));
            map.Encounters = Array.Empty<EncounterEntry>();
            Assert.Throws<InvalidOperationException>(() => map.WildEncounterWeights());
        }
    }
}
