using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class MapEconomyTests
    {
        [Test]
        public void EveryMapHasReachableMathDiscoveriesAndMerchant()
        {
            var allIds = new HashSet<string>();
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky() })
            {
                var map = GridMap.Parse(def.Rows);
                Assert.AreEqual(4, def.Discoveries.Length, def.Id);
                foreach (var discovery in def.Discoveries)
                {
                    Assert.True(allIds.Add(discovery.Id), discovery.Id);
                    Assert.True(map.Walkable(discovery.X, discovery.Y),
                        $"{discovery.Id} is not on a walkable tile");
                    Assert.IsNotEmpty(map.FindPath(map.Spawn, (discovery.X, discovery.Y)),
                        $"{discovery.Id} is unreachable");
                    Assert.Greater(discovery.Coins, 0);
                }

                Assert.NotNull(def.Merchant, def.Id);
                Assert.True(allIds.Add(def.Merchant.Id), def.Merchant.Id);
                Assert.True(map.Walkable(def.Merchant.X, def.Merchant.Y),
                    $"{def.Merchant.Id} is not on a walkable tile");
                Assert.IsNotEmpty(map.FindPath(map.Spawn, (def.Merchant.X, def.Merchant.Y)),
                    $"{def.Merchant.Id} is unreachable");
            }
        }

        [Test]
        public void EveryMerchantHasLimitedBalancedInventoryAndTrainerPartner()
        {
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky() })
            {
                var merchant = def.Merchant;
                Assert.AreEqual(4, merchant.Stock.Length, def.Id);
                Assert.AreEqual(1,
                    System.Array.FindAll(merchant.Stock, item => item.Type == ShopItemType.EvolutionStone).Length);
                foreach (var item in merchant.Stock)
                {
                    Assert.Greater(item.Price, 0, item.Id);
                    Assert.That(item.StockLimit, Is.InRange(1, 4), item.Id);
                    Assert.LessOrEqual(item.AttackBonus + item.DefenseBonus, 2, item.Id);
                }

                var opponent = merchant.Opponent(merchant.MinimumLevel, def.Tier, new Rng(7u));
                Assert.False(opponent.Catchable);
                Assert.False(opponent.IsBoss);
                Assert.GreaterOrEqual(opponent.Level, merchant.MinimumLevel);
            }
        }

        [Test]
        public void EconomyArtworkIsImportedAsSprites()
        {
            Assert.IsNotNull(Resources.Load<Sprite>("generated/Economy/numeria_coin"));
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky() })
                Assert.IsNotNull(Resources.Load<Sprite>(def.Merchant.SpriteResource), def.Merchant.SpriteResource);
        }

        [Test]
        public void EveryMapHasACompleteCrystalGuardianStoryBeat()
        {
            var names = new HashSet<string>();
            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky() })
            {
                Assert.IsNotEmpty(def.CrystalName, def.Id);
                Assert.IsNotEmpty(def.GuardianName, def.Id);
                Assert.True(names.Add(def.GuardianName), def.GuardianName);
                Assert.AreEqual(2, def.GuardianChallengeLines.Length, def.Id);
                Assert.IsNotEmpty(def.GuardianVictoryLine, def.Id);
                Assert.IsNotNull(Resources.Load<Sprite>(def.GuardianSpriteResource),
                    def.GuardianSpriteResource);
            }
            Assert.IsNotNull(Resources.Load<Sprite>("generated/Story/digit_crystal"));
        }
    }
}
