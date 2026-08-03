using System;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class BattleTests
    {
        private static BattleState Fresh() => new BattleState(GameData.Addmander(), GameData.Numberfly());

        [Test]
        public void InitialState()
        {
            var s = Fresh();
            Assert.AreEqual(10, s.PlayerHp);
            Assert.Greater(s.EnemyHp, 20);
            Assert.AreEqual(2, s.Gems);
            Assert.True(s.EnemyShielded);
            Assert.AreEqual(5, s.Enemy.Level);
            Assert.True(s.Enemy.IsBoss);
            Assert.AreEqual(BattleOutcome.None, s.Outcome);
        }

        [Test]
        public void Gems_GainPlus2_CappedAt8()
        {
            var s = Fresh();
            s.Gems = 7;
            s.StartPlayerTurn();
            Assert.AreEqual(8, s.Gems);
        }

        [Test]
        public void BattleItemsRestoreHpAndGemsWithoutExceedingCaps()
        {
            var s = Fresh();
            s.PlayerHp = 3;
            Assert.AreEqual(6, s.HealPlayer(6));
            Assert.AreEqual(9, s.PlayerHp);
            Assert.AreEqual(1, s.HealPlayer(99));
            Assert.AreEqual(s.Player.MaxHp, s.PlayerHp);

            s.Gems = 6;
            Assert.AreEqual(2, s.RestoreGems(3));
            Assert.AreEqual(8, s.Gems);
        }

        [Test]
        public void CatchChanceIsAlwaysAvailableAndRisesAsEnemyWeakens()
        {
            Assert.AreEqual(10, CatchSystem.Percent(100, 100));
            Assert.That(CatchSystem.Percent(50, 100), Is.InRange(40, 42));
            Assert.That(CatchSystem.Percent(20, 100), Is.InRange(71, 73));
            Assert.That(CatchSystem.Percent(1, 100), Is.InRange(93, 95));

            double previous = CatchSystem.Probability(100, 100);
            for (int hp = 99; hp >= 1; hp--)
            {
                double current = CatchSystem.Probability(hp, 100);
                Assert.Greater(current, previous);
                previous = current;
            }
        }

        [Test]
        public void CatchRollUsesHealthCurveAndRejectsBosses()
        {
            var fullHp = new BattleState(GameData.Addmander(), GameData.Countipillar(), new Rng(1));
            Assert.False(fullHp.TryCatch()); // first roll is about 23.6%, above the full-HP 10% chance

            var lowHp = new BattleState(GameData.Addmander(), GameData.Countipillar(), new Rng(1));
            lowHp.EnemyHp = 1;
            Assert.True(lowHp.TryCatch());

            var boss = new BattleState(GameData.Addmander(), GameData.Numberfly(), new Rng(1));
            boss.EnemyHp = 1;
            Assert.False(boss.TryCatch());
        }

        [Test]
        public void Shield_HalvesDamage_FloorMin1()
        {
            var s = Fresh();
            Assert.That(s.DamageToEnemy(8), Is.InRange(2, 3));
            Assert.AreEqual(1, s.DamageToEnemy(1));
        }

        [Test]
        public void BrokenShieldStunsThenFirstAttackDoublesAndResetsShield()
        {
            var s = Fresh();
            s.BreakShield();
            Assert.False(s.EnemyShielded);
            Assert.True(s.BreakBonusReady);
            Assert.AreEqual(1, s.EnemySkipTurns);
            Assert.True(s.ConsumeEnemySkipTurn());
            Assert.False(s.ConsumeEnemySkipTurn());
            Assert.That(s.DamageToEnemy(8), Is.InRange(10, 14));

            var result = s.UseSkill("tackle");
            Assert.GreaterOrEqual(result.Damage, 2);
            Assert.False(s.BreakBonusReady);
            Assert.True(s.EnemyShielded);

            s.BreakShield();
            Assert.False(s.EnemyShielded);
            Assert.True(s.BreakBonusReady);
            Assert.AreEqual(1, s.EnemySkipTurns);
        }

        [Test]
        public void FormulaSkill_ZeroPunishment()
        {
            var s1 = Fresh();
            s1.Gems = 3;
            s1.EnemyShielded = false;
            var r1 = s1.UseSkill("equation-flame", correct: true);
            Assert.That(r1.Damage, Is.InRange(3, 5));
            Assert.True(r1.Powered);
            Assert.AreEqual(s1.Enemy.MaxHp - r1.Damage, s1.EnemyHp);
            Assert.AreEqual(0, s1.Gems);

            var s2 = Fresh();
            s2.Gems = 3;
            s2.EnemyShielded = false;
            int hpBefore = s2.PlayerHp;
            var r2 = s2.UseSkill("equation-flame", correct: false);
            Assert.That(r2.Damage, Is.InRange(1, 2));
            Assert.False(r2.Powered);
            Assert.AreEqual(hpBefore, s2.PlayerHp); // 答错绝不扣玩家血
        }

        [Test]
        public void NotEnoughGems_Throws()
        {
            var s = Fresh();
            s.Gems = 1;
            var ex = Assert.Throws<InvalidOperationException>(() => s.UseSkill("equation-flame"));
            StringAssert.Contains("not enough gems", ex.Message);
        }

        [Test]
        public void EnemyTurn_DamagesPlayer_AfterShieldStunIsConsumed()
        {
            var s = Fresh();
            s.BreakShield();
            Assert.True(s.ConsumeEnemySkipTurn());
            Assert.AreEqual(s.Player.MaxHp, s.PlayerHp);
            int dmg = s.EnemyTurn();
            Assert.That(dmg, Is.InRange(4, 6));
            Assert.AreEqual(10 - dmg, s.PlayerHp);
        }

        [Test]
        public void Outcomes_WinAndLose()
        {
            var s = Fresh();
            s.EnemyShielded = false;
            s.EnemyHp = 1;
            s.UseSkill("tackle");
            Assert.AreEqual(BattleOutcome.Win, s.Outcome);

            var s2 = Fresh();
            s2.PlayerHp = 1;
            s2.EnemyTurn();
            Assert.AreEqual(BattleOutcome.Lose, s2.Outcome);
        }

        [Test]
        public void AttackDefenseRelationship_HasOnlySmallBoundedVariance()
        {
            var s = new BattleState(GameData.Addmander(), GameData.Countipillar(), new Rng(99));
            for (int i = 0; i < 60; i++)
                Assert.That(s.RollDamage(7, 3), Is.InRange(4, 6));

            for (int i = 0; i < 20; i++)
                Assert.That(s.RollDamage(2, 8), Is.InRange(1, 2));
        }

        [Test]
        public void WildStatsScaleWithLevelAndKeepSmallHpVariance()
        {
            var low = GameData.CreateWild("countipillar", 1, new Rng(7));
            var high = GameData.CreateWild("countipillar", 30, new Rng(7));
            Assert.Greater(high.MaxHp, low.MaxHp);
            Assert.Greater(high.AttackPower, low.AttackPower);
            Assert.Greater(high.DefensePower, low.DefensePower);

            int expected = GrowthSystem.StatAtLevel(GameData.SpeciesById("countipillar").MaxHp,
                GameData.SpeciesById("countipillar").HpGrowth, 20);
            for (uint seed = 1; seed <= 80; seed++)
                Assert.That(GameData.CreateWild("countipillar", 20, new Rng(seed)).MaxHp,
                    Is.InRange((expected * 92) / 100, (expected * 108 + 99) / 100));
        }

        [Test]
        public void BossesScaleAboveSameLevelWildMonsters()
        {
            var wild = GameData.CreateWild("duplirock", 20, new Rng(5));
            var boss = GameData.CreateBoss("duplirock", 20, 2, new Rng(5));
            Assert.Greater(boss.MaxHp, wild.MaxHp * 1.7f);
            Assert.Greater(boss.AttackPower, wild.AttackPower);
            Assert.Greater(boss.DefensePower, wild.DefensePower);
        }
    }
}
