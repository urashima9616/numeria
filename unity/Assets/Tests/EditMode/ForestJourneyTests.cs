using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class ForestJourneyTests
    {
        [Test]
        public void OldChestsStayOpenAndMigrationNeverGrantsTheirRewardsAgain()
        {
            var p = new Progress { SaveVersion = 9, Coins = 311 };
            p.OpenedChests.Add("forest-chest-2-9");
            p.ApplyMigrations();
            p.ApplyMigrations();
            Assert.Contains("forest-cache-roots", p.OpenedChests);
            Assert.False(p.OpenChest("forest-cache-roots"));
            Assert.AreEqual(311, p.Coins);
            Assert.False(ForestJourney.GuardianReady(p));
        }

        [Test]
        public void ExistingGuardianUnlockAndCapturedStatsSurviveMigration()
        {
            var p = new Progress { SaveVersion = 9 };
            foreach (string id in ForestJourney.ChestAliases.Keys) p.OpenedChests.Add(id);
            p.ActiveGrowth.CapturedHpOffset = 3;
            p.ApplyMigrations();
            Assert.True(ForestJourney.GuardianReady(p));
            Assert.True(ForestJourney.BridgeRestored(p));
            Assert.AreEqual(3, p.ActiveGrowth.CapturedHpOffset);
        }

        [Test]
        public void NewSaveCannotSkipWorldEventsByCollectingAllTreasure()
        {
            var p = new Progress();
            foreach (string id in ForestJourney.ChestAliases.Values) p.OpenedChests.Add(id);
            p.ApplyMigrations();
            Assert.False(ForestJourney.GuardianReady(p));
        }

        [Test]
        public void NewAdventureUsesWorldEventsNotMandatoryTreasureHunting()
        {
            var p = new Progress();
            p.ApplyMigrations();
            Assert.False(ForestJourney.BridgeRestored(p));
            foreach (string id in new[] { ForestJourney.Lanterns, ForestJourney.Bridge, ForestJourney.Mirror })
                p.CollectDiscovery(id);
            Assert.True(ForestJourney.GuardianReady(p));
            Assert.IsEmpty(p.OpenedChests);
        }
    }
}
