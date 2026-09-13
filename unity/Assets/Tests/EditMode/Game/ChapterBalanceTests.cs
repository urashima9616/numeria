using System;
using System.Linq;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Game.Tests
{
    public class ChapterBalanceTests
    {
        [Test]
        public void StoryOpponentsNeverChaseTheActivePartnerLevel()
        {
            foreach (var map in Maps.All())
            {
                var boss = map.RollBossEncounter(1, new Rng(1));
                Assert.AreEqual(map.BossMinLevel, boss.Level);
                TestContext.Out.WriteLine($"BOSS {map.Id},{boss.Level},{boss.MaxHp},{boss.AttackPower},{boss.DefensePower}");
                var merchant = map.Merchant.Opponent(1, map.Tier, new Rng(1));
                foreach (int level in new[] { 1, 5, 20, 43, 50, 60, 99 })
                {
                    AssertSame(boss, map.RollBossEncounter(level, new Rng(99)));
                    AssertSame(merchant, map.Merchant.Opponent(level, map.Tier, new Rng(1)));
                }
            }
        }

        private static void AssertSame(CombatantDef expected, CombatantDef actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Level, actual.Level);
            Assert.AreEqual(expected.MaxHp, actual.MaxHp);
            Assert.AreEqual(expected.AttackPower, actual.AttackPower);
            Assert.AreEqual(expected.DefensePower, actual.DefensePower);
        }

        [Test]
        public void ReturningToAnEarlierChapterDoesNotRaiseWildLevels()
        {
            int[] floors = { 1, 7, 14, 24, 34, 41 };
            int[] caps = { 6, 14, 22, 33, 43, 51 };
            var maps = Maps.All();
            for (int i = 0; i < maps.Length; i++)
                for (uint seed = 1; seed <= 256; seed++)
                {
                    var low = maps[i].RollWildEncounter(1, new Rng(seed));
                    var high = maps[i].RollWildEncounter(99, new Rng(seed));
                    AssertSame(low, high);
                    Assert.That(low.Level, Is.InRange(floors[i], caps[i]), maps[i].Id);
                }
        }

        [Test]
        public void EverySpeciesKeepsItsAuthoredRangeAndRandomVariation()
        {
            foreach (var map in Maps.All())
                foreach (var entry in map.Encounters)
                {
                    Assert.LessOrEqual(entry.MinimumLevel, map.WildMaxLevel);
                    Assert.GreaterOrEqual(entry.MaximumLevel, entry.MinimumLevel);
                    var isolated = new MapDef { Encounters = new[] { entry }, WildMaxLevel = map.WildMaxLevel };
                    var levels = new System.Collections.Generic.HashSet<int>();
                    for (uint seed = 1; seed <= 128; seed++)
                    {
                        var foe = isolated.RollWildEncounter(99, new Rng(seed));
                        Assert.AreEqual(entry.SpeciesId, foe.Id);
                        Assert.That(foe.Level, Is.InRange(entry.MinimumLevel,
                            Math.Min(entry.MaximumLevel, map.WildMaxLevel)));
                        levels.Add(foe.Level);
                    }
                    Assert.Greater(levels.Count, 1, entry.SpeciesId);
                }
        }

        [Test]
        public void TenLevelsOfTrainingImproveDamageAgainstTheSameBoss()
        {
            foreach (var map in Maps.All())
            {
                var boss = map.RollBossEncounter(map.BossMinLevel, new Rng(1));
                var low = GameData.PlayerMon("runelet", 1, map.BossMinLevel);
                var high = GameData.PlayerMon("runelet", 1, map.BossMinLevel + 10);
                var a = new BattleState(low, boss, new Rng(1));
                var b = new BattleState(high, boss, new Rng(1));
                Assert.Greater(b.RollDamage(high.AttackPower + 7, boss.DefensePower),
                    a.RollDamage(low.AttackPower + 7, boss.DefensePower), map.Id);
                Assert.LessOrEqual(b.RollDamage(boss.AttackPower, high.DefensePower),
                    a.RollDamage(boss.AttackPower, low.DefensePower), map.Id);
            }
        }

        [Test]
        public void BossBattleSimulation_ReportsTrainingCurveWithoutRealSaves()
        {
            // A deterministic policy: solve shields, bank gems with basic attacks, use Mega
            // Nova when active. No potions/equipment/captured offsets. All puzzles correct.
            foreach (var map in Maps.All())
                foreach (string family in new[] { "addmander", "tenfin", "shapling", "runelet" })
                    foreach (int offset in new[] { -5, 0, 5, 10 })
                    {
                        int level = Math.Max(1, map.BossMinLevel + offset);
                        var line = GameData.Lines.Single(l => l.BaseId == family);
                        int stage = line.EvolutionLevels.Count(l => l <= level);
                        int wins = 0, totalActions = 0;
                        for (uint seed = 1; seed <= 64; seed++)
                        {
                            var state = new BattleState(GameData.PlayerMon(family, stage, level),
                                map.RollBossEncounter(level, new Rng(seed)), new Rng(seed));
                            int actions = 0;
                            while (state.Outcome == BattleOutcome.None && actions < 160)
                            {
                                state.StartPlayerTurn();
                                state.TryActivateMega(true);
                                if (state.EnemyShielded) state.BreakShield();
                                else
                                {
                                    var skill = state.MegaActive ? state.Mega.Skill : state.Player.Skills[0];
                                    state.UseSkill(skill.Id);
                                }
                                state.ConsumeMegaTurn();
                                if (state.Outcome == BattleOutcome.None && !state.ConsumeEnemySkipTurn())
                                    state.EnemyTurn();
                                actions++;
                            }
                            if (state.Outcome == BattleOutcome.Win) wins++;
                            totalActions += actions;
                        }
                        if (offset == 10)
                            Assert.AreEqual(64, wins, $"Training ten levels above {map.Id} should allow {family} to win with this policy.");
                        TestContext.Out.WriteLine($"BALANCE {map.Id},{family},{level},{wins}/64,{totalActions / 64d:0.0}");
                    }
        }
    }
}
