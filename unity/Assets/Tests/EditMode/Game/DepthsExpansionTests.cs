using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Game.Tests
{
    public class DepthsExpansionTests
    {
        [Test]
        public void ChapterChainContinuesThroughDarkMinesAndUndergroundTunnels()
        {
            var mines = Maps.Get("dark_mines");
            var tunnels = Maps.Get("underground");
            Assert.AreEqual("dark_mines", Maps.Desert().PortalTargetMap);
            Assert.AreEqual(5, mines.Tier);
            Assert.AreEqual("dark_mines", mines.Theme);
            Assert.AreEqual("underground", mines.PortalTargetMap);
            Assert.AreEqual(6, tunnels.Tier);
            Assert.AreEqual("underground", tunnels.Theme);
            Assert.IsNull(tunnels.PortalTargetMap);
        }

        [Test]
        public void NewRegionsSplitTheSixteenFamiliesByRequestedElements()
        {
            var mineBases = Bases(Maps.DarkMines());
            var tunnelBases = Bases(Maps.UndergroundTunnels());
            CollectionAssert.AreEquivalent(new[]
            {
                "ohmlet", "sparkseed", "charguppy", "circuitick",
                "numite", "gemlet", "shaleling", "cragcub",
            }, mineBases);
            CollectionAssert.AreEquivalent(new[]
            {
                "draddit", "scalip", "digiling", "runelet",
                "embernum", "cindercub", "torchick", "glowgecko",
            }, tunnelBases);
        }

        [Test]
        public void NewRegionsHaveDistinctBossesAndCompleteRewards()
        {
            var mines = Maps.DarkMines();
            var tunnels = Maps.UndergroundTunnels();
            Assert.AreEqual("voltamper", mines.BossSpeciesId);
            Assert.AreEqual("calcularagon", tunnels.BossSpeciesId);
            Assert.AreEqual(5, mines.ChestRewards.Count);
            Assert.AreEqual(5, tunnels.ChestRewards.Count);
            Assert.AreEqual(4, mines.Discoveries.Length);
            Assert.AreEqual(4, tunnels.Discoveries.Length);
            Assert.IsNotNull(mines.Merchant);
            Assert.IsNotNull(tunnels.Merchant);
        }

        [Test]
        public void NewRegionGatesAreIdempotentAndIndependent()
        {
            var progress = new Progress();
            Maps.DarkMines().ClearGate(progress);
            Maps.DarkMines().ClearGate(progress);
            Assert.True(Maps.DarkMines().GateCleared(progress));
            Assert.False(Maps.UndergroundTunnels().GateCleared(progress));
            Assert.AreEqual(1, progress.ClearedGates.FindAll(id => id == "dark_mines").Count);
        }

        private static HashSet<string> Bases(MapDef map)
        {
            var result = new HashSet<string>();
            foreach (var entry in map.Encounters) result.Add(GameData.BaseId(entry.SpeciesId));
            return result;
        }
    }
}
