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
            Assert.AreEqual(10, s.EnemyHp);
            Assert.AreEqual(2, s.Gems);
            Assert.True(s.EnemyShielded);
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
            Assert.AreEqual(2, s.DamageToEnemy(5));
            Assert.AreEqual(1, s.DamageToEnemy(1));
        }

        [Test]
        public void Vulnerability_DoublesDamage()
        {
            var s = Fresh();
            s.BreakShield();
            Assert.False(s.EnemyShielded);
            Assert.AreEqual(2, s.VulnerableTurns);
            Assert.AreEqual(10, s.DamageToEnemy(5));
        }

        [Test]
        public void FormulaSkill_ZeroPunishment()
        {
            var s1 = Fresh();
            s1.Gems = 3;
            s1.EnemyShielded = false;
            var r1 = s1.UseSkill("flame-formula", correct: true);
            Assert.AreEqual(5, r1.Damage);
            Assert.True(r1.Powered);
            Assert.AreEqual(5, s1.EnemyHp);
            Assert.AreEqual(0, s1.Gems);

            var s2 = Fresh();
            s2.Gems = 3;
            s2.EnemyShielded = false;
            int hpBefore = s2.PlayerHp;
            var r2 = s2.UseSkill("flame-formula", correct: false);
            Assert.AreEqual(2, r2.Damage);
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
            Assert.AreEqual(2, dmg);
            Assert.AreEqual(8, s.PlayerHp);
            Assert.AreEqual(1, s.VulnerableTurns);
        }

        [Test]
        public void Outcomes_WinAndLose()
        {
            var s = Fresh();
            s.EnemyShielded = false;
            s.EnemyHp = 2;
            s.UseSkill("tackle");
            Assert.AreEqual(BattleOutcome.Win, s.Outcome);

            var s2 = Fresh();
            s2.PlayerHp = 2;
            s2.EnemyTurn();
            Assert.AreEqual(BattleOutcome.Lose, s2.Outcome);
        }
    }
}
