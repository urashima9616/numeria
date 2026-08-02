using System;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class BattleTests
    {
        private static BattleState Fresh() => new BattleState(GameData.Addmander(), GameData.Duplirock());

        [Test]
        public void InitialState()
        {
            var s = Fresh();
            Assert.AreEqual(10, s.PlayerHp);
            Assert.AreEqual(30, s.EnemyHp);
            Assert.AreEqual(2, s.Gems);
            Assert.True(s.EnemyShielded);
            Assert.AreEqual(4, s.Enemy.DefensePower);
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
        public void Shield_HalvesDamage_FloorMin1()
        {
            var s = Fresh();
            Assert.That(s.DamageToEnemy(8), Is.InRange(2, 3));
            Assert.AreEqual(1, s.DamageToEnemy(1));
        }

        [Test]
        public void Vulnerability_DoublesDamage()
        {
            var s = Fresh();
            s.BreakShield();
            Assert.False(s.EnemyShielded);
            Assert.AreEqual(2, s.VulnerableTurns);
            Assert.That(s.DamageToEnemy(8), Is.InRange(8, 12));
        }

        [Test]
        public void FormulaSkill_ZeroPunishment()
        {
            var s1 = Fresh();
            s1.Gems = 3;
            s1.EnemyShielded = false;
            var r1 = s1.UseSkill("flame-formula", correct: true);
            Assert.That(r1.Damage, Is.InRange(2, 4));
            Assert.True(r1.Powered);
            Assert.AreEqual(30 - r1.Damage, s1.EnemyHp);
            Assert.AreEqual(0, s1.Gems);

            var s2 = Fresh();
            s2.Gems = 3;
            s2.EnemyShielded = false;
            int hpBefore = s2.PlayerHp;
            var r2 = s2.UseSkill("flame-formula", correct: false);
            Assert.That(r2.Damage, Is.InRange(1, 2));
            Assert.False(r2.Powered);
            Assert.AreEqual(hpBefore, s2.PlayerHp); // 答错绝不扣玩家血
        }

        [Test]
        public void NotEnoughGems_Throws()
        {
            var s = Fresh();
            s.Gems = 1;
            var ex = Assert.Throws<InvalidOperationException>(() => s.UseSkill("flame-formula"));
            StringAssert.Contains("not enough gems", ex.Message);
        }

        [Test]
        public void EnemyTurn_DamagesPlayer_TicksVulnerability()
        {
            var s = Fresh();
            s.BreakShield();
            int dmg = s.EnemyTurn();
            Assert.That(dmg, Is.InRange(3, 5));
            Assert.AreEqual(10 - dmg, s.PlayerHp);
            Assert.AreEqual(1, s.VulnerableTurns);
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
        public void WildHpRangesAndBossCurve_IncreaseByMap()
        {
            for (uint seed = 1; seed <= 80; seed++)
            {
                Assert.That(GameData.RollWild(GameData.Countipillar(), 1, new Rng(seed)).MaxHp,
                    Is.InRange(8, 12));
                Assert.That(GameData.RollWild(GameData.Doublit(), 2, new Rng(seed)).MaxHp,
                    Is.InRange(14, 20));
                Assert.That(GameData.RollWild(GameData.Mirrowl(), 3, new Rng(seed)).MaxHp,
                    Is.InRange(22, 30));
            }
            Assert.Less(GameData.Numberfly().MaxHp, GameData.DuplirockElder().MaxHp);
            Assert.Less(GameData.DuplirockElder().MaxHp, GameData.Symmetrix().MaxHp);
            CollectionAssert.AreEqual(new[] { 20, 36, 54 }, new[]
            {
                GameData.Numberfly().MaxHp,
                GameData.DuplirockElder().MaxHp,
                GameData.Symmetrix().MaxHp,
            });
        }
    }
}
