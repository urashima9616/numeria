using System;
using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Game.Tests
{
    public class DesertTests
    {
        [Test]
        public void SkyPortalLeadsToTierFourFeverDesert()
        {
            var desert = Maps.Get("desert");
            Assert.AreEqual("desert", Maps.Sky().PortalTargetMap);
            Assert.AreEqual("Fever Desert", desert.DisplayName);
            Assert.AreEqual(4, desert.Tier);
            Assert.AreEqual("desert", desert.Theme);
            Assert.AreEqual("totalisk", desert.BossSpeciesId);
            Assert.AreEqual(40, PuzzleGenerator.MaxForTier(desert.Tier));
        }

        [Test]
        public void DesertEcologyFeaturesEveryNewFamilyAtAnEvolvedStage()
        {
            var encounteredBases = new HashSet<string>();
            foreach (var entry in Maps.Desert().Encounters)
            {
                encounteredBases.Add(GameData.BaseId(entry.SpeciesId));
                Assert.Greater(GameData.StageIndex(entry.SpeciesId), 0, entry.SpeciesId);
                Assert.Greater(entry.Weight, 0, entry.SpeciesId);
            }

            string[] expected =
            {
                "glimlet", "moonmote", "charmite", "wishwink", "pixipip",
                "addling", "dracount", "loopling", "twinsting", "shardrake",
                "voltlet", "sparkit", "chargecub", "flickerfin", "switchick",
                "budsum", "clovercub", "sprouturn", "mossbit", "seedseq",
                "numblet",
            };
            CollectionAssert.AreEquivalent(expected, encounteredBases);
        }

        [Test]
        public void AllNewFamiliesAreEncounterableAcrossTheFourChapters()
        {
            var encounteredBases = new HashSet<string>();
            foreach (var map in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky(), Maps.Desert() })
                foreach (var entry in map.Encounters)
                    encounteredBases.Add(GameData.BaseId(entry.SpeciesId));

            foreach (var line in GameData.Lines)
            {
                if (line.Element == "FAIRY" || line.Element == "DRAGON" ||
                    line.Element == "ELECTRIC" ||
                    (line.Element == "GRASS" && Array.IndexOf(new[] { "shapling", "seqkit" }, line.BaseId) < 0) ||
                    line.Element == "FLYING")
                    Assert.True(encounteredBases.Contains(line.BaseId), line.BaseId);
            }
        }

        [Test]
        public void DesertHasCompleteRewardsMerchantAndFinalGate()
        {
            var desert = Maps.Desert();
            Assert.AreEqual(5, desert.ChestRewards.Count);
            Assert.AreEqual(4, desert.Discoveries.Length);
            Assert.NotNull(desert.Merchant);
            Assert.AreEqual("mirrorvolt", desert.Merchant.PartnerSpeciesId);
            Assert.IsNull(desert.PortalTargetMap);

            var progress = new Progress();
            Assert.False(desert.GateCleared(progress));
            desert.ClearGate(progress);
            Assert.True(desert.GateCleared(progress));
        }
    }
}
