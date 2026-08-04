using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class EconomyTests
    {
        [Test]
        public void VictoryCoinsScaleByTierAndBattleType()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
                var rng = new Rng(91u);
                for (int i = 0; i < 30; i++)
                    Assert.That(EconomySystem.VictoryCoins(tier, false, false, rng),
                        Is.InRange(2 + tier, 4 + tier * 2));
                Assert.AreEqual(10 + tier * 6,
                    EconomySystem.VictoryCoins(tier, false, true, rng));
                Assert.AreEqual(20 + tier * 10,
                    EconomySystem.VictoryCoins(tier, true, false, rng));
            }
        }

        [Test]
        public void PurchaseSpendsCoinsGrantsItemAndHonorsPermanentStockLimit()
        {
            var progress = new Progress();
            progress.AddCoins(30);
            var potion = new ShopItemDef
            {
                Id = "test-potion", Name = "Potion", Type = ShopItemType.HealthPotion,
                Price = 6, StockLimit = 2, Amount = 2,
            };

            Assert.AreEqual(PurchaseResult.Purchased, EconomySystem.Buy(progress, potion));
            Assert.AreEqual(PurchaseResult.Purchased, EconomySystem.Buy(progress, potion));
            Assert.AreEqual(PurchaseResult.SoldOut, EconomySystem.Buy(progress, potion));
            Assert.AreEqual(18, progress.Coins);
            Assert.AreEqual(4, progress.HealthPotions);
            Assert.AreEqual(2, progress.PurchaseCount(potion.Id));
            Assert.AreEqual(12, progress.Records.CoinsSpent);
        }

        [Test]
        public void FailedPurchaseDoesNotMutateProgress()
        {
            var progress = new Progress();
            progress.AddCoins(4);
            var accessory = new ShopItemDef
            {
                Id = "test-charm", Name = "Test Charm", Type = ShopItemType.Accessory,
                Price = 5, StockLimit = 1, AttackBonus = 1, DefenseBonus = 1,
            };

            Assert.AreEqual(PurchaseResult.NotEnoughCoins, EconomySystem.Buy(progress, accessory));
            Assert.AreEqual(4, progress.Coins);
            Assert.Zero(progress.Accessories.Count);
            Assert.Zero(progress.PurchaseCount(accessory.Id));
        }

        [Test]
        public void EvolutionStoneAndAccessoryPurchasesGrantConfiguredRewards()
        {
            var progress = new Progress();
            progress.AddCoins(100);
            var stone = new ShopItemDef
            {
                Id = "stone", Name = "Evolution Stone", Type = ShopItemType.EvolutionStone,
                Price = 10, StockLimit = 1,
            };
            var charm = new ShopItemDef
            {
                Id = "charm", Name = "Prism Charm", Type = ShopItemType.Accessory,
                Price = 12, StockLimit = 1, AttackBonus = 1, DefenseBonus = 1,
            };

            Assert.AreEqual(PurchaseResult.Purchased, EconomySystem.Buy(progress, stone));
            Assert.AreEqual(PurchaseResult.Purchased, EconomySystem.Buy(progress, charm));
            Assert.AreEqual(1, progress.EvolutionStones);
            Assert.AreEqual(1, progress.Accessories.Count);
            Assert.AreEqual(1, progress.Accessories[0].AttackBonus);
            Assert.AreEqual(1, progress.Accessories[0].DefenseBonus);
        }

        [Test]
        public void DiscoveryAndMerchantRecordsAreIdempotent()
        {
            var progress = new Progress();
            Assert.True(progress.CollectDiscovery("forest-rune-1"));
            Assert.False(progress.CollectDiscovery("forest-rune-1"));
            Assert.True(progress.DefeatMerchant("forest-tessa"));
            Assert.False(progress.DefeatMerchant("forest-tessa"));
            Assert.AreEqual(1, progress.Records.DiscoveriesSolved);
            Assert.AreEqual(1, progress.Records.MerchantsDefeated);
        }

        [Test]
        public void DigitCrystalsAreUniqueStoryMilestones()
        {
            var progress = new Progress();
            Assert.True(progress.CollectDigitCrystal("forest"));
            Assert.False(progress.CollectDigitCrystal("forest"));
            Assert.True(progress.CollectDigitCrystal("mountains"));
            Assert.True(progress.CollectDigitCrystal("sky"));
            Assert.True(progress.CollectDigitCrystal("desert"));
            Assert.AreEqual(4, progress.DigitCrystalCount);
            Assert.AreEqual(4, progress.Records.DigitCrystalsRestored);
        }

        [Test]
        public void VersionSevenBossProgressMigratesToMatchingDigitCrystals()
        {
            var progress = new Progress
            {
                SaveVersion = 7,
                BossBeaten = true,
                ClearedGates = new System.Collections.Generic.List<string> { "mountains" },
            };
            progress.ApplyMigrations();
            CollectionAssert.AreEquivalent(new[] { "forest", "mountains" }, progress.DigitCrystals);
            Assert.AreEqual(2, progress.Records.DigitCrystalsRestored);
            Assert.AreEqual(Progress.CurrentSaveVersion, progress.SaveVersion);
        }
    }
}
