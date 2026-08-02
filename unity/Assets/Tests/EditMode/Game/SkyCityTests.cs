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
            Assert.AreEqual("mirrowl", sky.Wild().Id);
            Assert.AreEqual("symmetrix", sky.Boss().Id);
            Assert.AreEqual("numberfly", Maps.Forest().Boss().Id);
            Assert.AreEqual("duplirock", Maps.Mountains().Boss().Id);
            Assert.AreEqual("sky-chest-15-4", sky.EvoChestId);
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
        public void AllThreeLaunchMaps_HaveReachablePortalAndChests()
        {
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky() })
            {
                var map = GridMap.Parse(def.Rows);
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
        public void AllLaunchSpeciesAndMapBattles_HaveRuntimeArtwork()
        {
            foreach (var species in GameData.Roster)
                Assert.IsNotNull(Resources.Load<Sprite>($"generated/{species.Id}_large_icon"),
                    $"missing large icon for {species.Id}");

            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky() })
                Assert.IsNotNull(Resources.Load<Sprite>(def.BattleBg),
                    $"missing battle background for {def.Id}: {def.BattleBg}");

            Assert.IsNotNull(Resources.Load<Sprite>("generated/puzzle_firefly"));
            Assert.IsNotNull(Resources.Load<Sprite>("generated/puzzle_mushroom"));
        }
    }
}
