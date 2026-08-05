using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class SkyCityTests
    {
        [Test]
        public void MountainsPortal_LeadsToTierThreeSkyCity()
        {
            Assert.AreEqual("sky", Maps.Mountains().PortalTargetMap);
            var sky = Maps.Get("sky");
            Assert.AreEqual("Azure Sky City", sky.DisplayName);
            Assert.AreEqual(3, sky.Tier);
            Assert.AreEqual("sky", sky.Theme);
            CollectionAssert.Contains(System.Array.ConvertAll(sky.Encounters, e => e.SpeciesId), "mirrowl");
            CollectionAssert.Contains(System.Array.ConvertAll(sky.Encounters, e => e.SpeciesId), "numblet");
            Assert.AreEqual("symmetrix", sky.BossSpeciesId);
            Assert.AreEqual("desert", sky.PortalTargetMap);
            Assert.AreEqual("numberfly", Maps.Forest().BossSpeciesId);
            Assert.AreEqual("duplirock", Maps.Mountains().BossSpeciesId);
            Assert.True(sky.ChestRewards.ContainsKey("sky-chest-21-2"));
        }

        [Test]
        public void Mirrowl_IsCatchableAndCanBecomeActivePlayer()
        {
            Assert.True(GameData.Mirrowl().Catchable);
            Assert.False(GameData.Symmetrix().Catchable);

            var player = GameData.PlayerMon("mirrowl", false);
            Assert.AreEqual("Mirrowl", player.Name);
            Assert.AreEqual(2, player.Skills.Length);
            Assert.AreEqual("Mirror Pattern", player.Skills[1].Name);
        }

        [Test]
        public void SkyGate_UsesProgressGateCollection()
        {
            var progress = new Progress();
            var sky = Maps.Sky();
            Assert.False(sky.GateCleared(progress));
            sky.ClearGate(progress);
            sky.ClearGate(progress);
            Assert.True(sky.GateCleared(progress));
            Assert.AreEqual(1, progress.ClearedGates.FindAll(id => id == "sky").Count);
        }

        [Test]
        public void AllFourChapterMaps_HaveReachablePortalAndChests()
        {
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky(), Maps.Desert() })
            {
                var map = GridMap.Parse(def.Rows);
                Assert.GreaterOrEqual(map.Width * map.Height, 480, $"{def.Id} must be at least twice the old 240 tiles");
                int portals = 0;
                int chests = 0;
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        if (map.At(x, y) != Tile.Portal && map.At(x, y) != Tile.Chest) continue;
                        Assert.IsNotEmpty(map.FindPath(map.Spawn, (x, y)),
                            $"{def.Id} objective at ({x},{y}) is unreachable");
                        if (map.At(x, y) == Tile.Portal) portals++;
                        else chests++;
                    }
                Assert.AreEqual(1, portals, $"{def.Id} must have one portal");
                Assert.That(chests, Is.GreaterThanOrEqualTo(2), $"{def.Id} needs exploration rewards");
            }
        }

        [Test]
        public void ChapterMapsHaveUniqueReadableBiomeStructure()
        {
            var layouts = new System.Collections.Generic.HashSet<string>();
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky(), Maps.Desert() })
            {
                Assert.AreEqual(18, def.Rows.Length, def.Id);
                foreach (string row in def.Rows) Assert.AreEqual(32, row.Length, def.Id);
                Assert.True(layouts.Add(string.Join("\n", def.Rows)), $"{def.Id} reuses another chapter layout");

                var map = GridMap.Parse(def.Rows);
                int paths = 0, bridges = 0, landmarks = 0, themedBoundaries = 0;
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        switch (map.At(x, y))
                        {
                            case Tile.Path: paths++; break;
                            case Tile.Bridge: bridges++; break;
                            case Tile.Landmark: landmarks++; break;
                            case Tile.Water:
                            case Tile.Cliff: themedBoundaries++; break;
                        }
                    }

                Assert.GreaterOrEqual(paths, 12, $"{def.Id} needs a readable route");
                Assert.GreaterOrEqual(bridges, 2, $"{def.Id} needs a memorable crossing");
                Assert.AreEqual(1, landmarks, $"{def.Id} needs one chapter landmark");
                Assert.Greater(themedBoundaries, 20, $"{def.Id} needs biome-defining terrain");
                CollectionAssert.AreEquivalent(def.ChestIds(), def.ChestRewards.Keys,
                    $"{def.Id} chest rewards must track authored coordinates exactly");
            }
        }

        [Test]
        public void EveryMapHasWeightedEnemyEcologyAndBossRequiresAllChests()
        {
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky(), Maps.Desert() })
            {
                Assert.GreaterOrEqual(def.Encounters.Length, 5);
                var ids = new System.Collections.Generic.HashSet<string>();
                foreach (var entry in def.Encounters)
                {
                    Assert.Greater(entry.Weight, 0);
                    Assert.True(ids.Add(entry.SpeciesId));
                }
                var progress = new Progress();
                Assert.False(def.AllChestsOpened(progress));
                foreach (string chest in def.ChestIds()) progress.OpenChest(chest);
                Assert.True(def.AllChestsOpened(progress));
            }
        }

        [Test]
        public void AllLaunchSpeciesAndMapBattles_HaveRuntimeArtwork()
        {
            foreach (var species in GameData.Roster)
                Assert.IsNotNull(Resources.Load<Sprite>($"generated/{species.Id}_large_icon"),
                    $"missing large icon for {species.Id}");

            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky(), Maps.Desert() })
                Assert.IsNotNull(Resources.Load<Sprite>(def.BattleBg),
                    $"missing battle background for {def.Id}: {def.BattleBg}");

            Assert.IsNotNull(Resources.Load<Sprite>("generated/puzzle_firefly"));
            Assert.IsNotNull(Resources.Load<Sprite>("generated/puzzle_mushroom"));
        }
    }
}
