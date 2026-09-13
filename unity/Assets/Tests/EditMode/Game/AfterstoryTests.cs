using System.Linq;
using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class AfterstoryTests
    {
        [Test]
        public void V10ProgressUpgradesWithoutChangingExistingAdventureData()
        {
            var p = new Progress { Coins = 493, CurrentMap = "underground", StoryIntroSeen = true };
            p.ApplyMigrations();
            p.AddCaught(GameData.CreateWild("mirrorwyrm", 43, new Rng(91)));
            foreach (var map in Maps.All().Where(m => !m.IsAfterstory))
            {
                map.ClearGate(p); p.CollectDigitCrystal(map.Id);
                foreach (var chest in map.ChestIds()) p.OpenChest(chest);
            }
            p.ApplyMigrations(); p.SaveVersion = 10;
            string original = JsonUtility.ToJson(p);
            var loaded = JsonUtility.FromJson<Progress>(original); loaded.ApplyMigrations();
            Assert.AreEqual(original.Replace("\"SaveVersion\":10", "\"SaveVersion\":11"), JsonUtility.ToJson(loaded));
            Assert.AreEqual(6, loaded.DigitCrystalCount);
            Assert.True(Maps.CanVisit("ancient_ruins", loaded));
        }

        [Test]
        public void CapturedExpansionStatsAndMechanismsRoundTrip()
        {
            var p = new Progress { CurrentMap = "veilmoon_lake" }; p.ApplyMigrations();
            var caught = GameData.CreateWild("veilkit", 63, new Rng(7));
            p.AddCaught(caught); p.CollectDiscovery("lake-mirror");
            var loaded = JsonUtility.FromJson<Progress>(JsonUtility.ToJson(p)); loaded.ApplyMigrations();
            Assert.AreEqual("veilmoon_lake", loaded.CurrentMap);
            CollectionAssert.Contains(loaded.CollectedDiscoveries, "lake-mirror");
            CollectionAssert.AreEqual(p.MonGrowth.Select(g => JsonUtility.ToJson(g)).ToArray(),
                loaded.MonGrowth.Select(g => JsonUtility.ToJson(g)).ToArray());
            CollectionAssert.Contains(loaded.CaughtIds, "wisplet");
        }

        [Test]
        public void ExpansionUnlocksPreserveTheSixCrystalEnding()
        {
            var p = new Progress();
            Assert.False(Maps.CanVisit("ancient_ruins", p));
            Maps.UndergroundTunnels().ClearGate(p);
            Assert.True(Maps.CanVisit("ancient_ruins", p));
            Assert.False(Maps.CanVisit("veilmoon_lake", p));
            Maps.AncientRuins().ClearGate(p);
            Assert.True(Maps.CanVisit("veilmoon_lake", p));
            Assert.True(Maps.AncientRuins().IsAfterstory);
            Assert.True(Maps.VeilmoonLake().IsAfterstory);
            Assert.AreEqual(0, p.DigitCrystalCount);
            Assert.IsNull(Maps.UndergroundTunnels().PortalTargetMap);
        }

        [Test]
        public void NewFamiliesHaveCompleteEvolutionAndUniqueSpells()
        {
            var ids = new[] { "tiletot", "abacub", "coglet", "gliminnow", "wisplet", "lilypadlet" };
            Assert.AreEqual(6, ids.Select(id => GameData.PlayerMon(id, 0, 50).Skills[1].Visual).Distinct().Count());
            foreach (var id in ids)
            {
                var line = GameData.LineFor(id);
                Assert.NotNull(line);
                Assert.AreEqual(3, line.StageIds.Length);
                foreach (var form in line.StageIds)
                {
                    Assert.True(GameData.SpeciesById(form).Catchable);
                    Assert.AreEqual(id, GameData.BaseId(form));
                }
            }
        }

        [Test]
        public void EveryMechanismCanBeSolvedInOrderAndSurvivesReload()
        {
            foreach (var def in new[] { Maps.AncientRuins(), Maps.VeilmoonLake() })
            {
                var p = new Progress();
                var grid = GridMap.Parse(def.Rows);
                AfterstoryJourney.Apply(grid, def, p);
                Assert.False(def.GuardianReady(p));
                foreach (var d in def.Discoveries)
                {
                    Assert.IsNotEmpty(grid.FindPath(grid.Spawn, (d.X, d.Y)), d.Id);
                    p.CollectDiscovery(d.Id);
                    AfterstoryJourney.Apply(grid, def, p);
                }
                Assert.True(def.GuardianReady(p));
                var loaded = GridMap.Parse(def.Rows);
                AfterstoryJourney.Apply(loaded, def, p);
                for (int y = 0; y < grid.Height; y++)
                    for (int x = 0; x < grid.Width; x++) Assert.AreEqual(grid.At(x, y), loaded.At(x, y));
            }
        }
    }
}
