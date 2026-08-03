using System;
using System.Linq;

namespace Numeria.Core
{
    /// <summary>
    /// 捕捉概率只由敌方剩余 HP 比例决定，便于孩子建立清晰因果：先削弱，再捕捉。
    /// 满血仍有 10% 机会；随失去血量按幂曲线加速增长；理论上限为 95%。
    /// </summary>
    public static class CatchSystem
    {
        public const double MinimumChance = 0.10d;
        public const double MaximumChance = 0.95d;
        public const double CurveExponent = 1.45d;

        public static double Probability(int remainingHp, int maxHp)
        {
            if (maxHp <= 0) return MinimumChance;
            double hpRatio = Math.Max(0d, Math.Min(1d, (double)remainingHp / maxHp));
            double weakened = 1d - hpRatio;
            return MinimumChance + (MaximumChance - MinimumChance) * Math.Pow(weakened, CurveExponent);
        }

        public static int Percent(int remainingHp, int maxHp) =>
            (int)Math.Round(Probability(remainingHp, maxHp) * 100d);
    }

    public enum SkillType { Basic, Formula }

    public class SkillDef
    {
        public string Id;
        public string Name;
        public int Cost;
        public int Power;
        public int BasePower;
        public SkillType Type;
    }

    public class CombatantDef
    {
        public string Id;
        public string Name;
        public int Level = 1;
        public int MaxHp;
        public int AttackPower;
        public int DefensePower;
        public int BaseXp;
        public bool IsBoss;
        public double DropChance;
        public ConsumableType PreferredDrop;
        public int? Shield;
        public bool Catchable;
        public SkillDef[] Skills;
    }

    public enum BattleOutcome { None, Win, Lose }

    public struct SkillResult
    {
        public int Damage;
        public bool Powered;
    }

    /// <summary>
    /// 战斗状态机,移植自 Web 原型 battle.js(已验证的逻辑)。
    /// 零惩罚不变量:答错谜题绝不扣玩家 HP,技能以 BasePower 释放。
    /// </summary>
    public class BattleState
    {
        public CombatantDef Player { get; }
        public CombatantDef Enemy { get; }
        public int PlayerHp;
        public int EnemyHp;
        public int Gems = 2;
        public int MaxGems = 8;
        public bool EnemyShielded;
        public int VulnerableTurns;
        public int PlayerAttackBonus;
        public int PlayerDefenseBonus;
        public BattleOutcome Outcome = BattleOutcome.None;
        private readonly Rng _rng;

        public BattleState(CombatantDef player, CombatantDef enemy, Rng rng = null)
        {
            Player = player;
            Enemy = enemy;
            _rng = rng ?? new Rng(1);
            PlayerHp = player.MaxHp;
            EnemyHp = enemy.MaxHp;
            EnemyShielded = enemy.Shield.HasValue;
        }

        public void StartPlayerTurn() => Gems = Math.Min(MaxGems, Gems + 2);

        public int HealPlayer(int amount)
        {
            int before = PlayerHp;
            PlayerHp = Math.Min(Player.MaxHp, PlayerHp + Math.Max(0, amount));
            return PlayerHp - before;
        }

        public int RestoreGems(int amount)
        {
            int before = Gems;
            Gems = Math.Min(MaxGems, Gems + Math.Max(0, amount));
            return Gems - before;
        }

        public double CatchChance => CatchSystem.Probability(EnemyHp, Enemy.MaxHp);

        /// <summary>数学友谊谜题答对后调用；Boss、不可捕捉目标与已倒下目标永远失败。</summary>
        public bool TryCatch()
        {
            return Enemy.Catchable && !Enemy.IsBoss && EnemyHp > 0 && _rng.Next() < CatchChance;
        }

        /// <summary>
        /// 幼儿可观察的攻防关系:攻击越高、对方防御越低，伤害越大。
        /// 每次只在 -1/0/+1 内浮动，且永远至少造成 1 点，避免随机性压过策略。
        /// </summary>
        public int RollDamage(int attack, int defense)
        {
            int expected = Math.Max(1, attack - defense + 1);
            return Math.Max(1, expected + _rng.Pick(-1, 1));
        }

        public int DamageToEnemy(int attack)
        {
            int damage = RollDamage(attack, Enemy.DefensePower);
            if (VulnerableTurns > 0) return damage * 2;
            if (EnemyShielded) return Math.Max(1, damage / 2);
            return damage;
        }

        public SkillResult UseSkill(string skillId, bool correct = true)
        {
            var skill = Player.Skills.First(s => s.Id == skillId);
            if (Gems < skill.Cost) throw new InvalidOperationException("not enough gems");
            Gems -= skill.Cost;

            bool powered = skill.Type != SkillType.Formula || correct;
            int attack = (powered ? skill.Power : skill.BasePower) +
                         Player.AttackPower + PlayerAttackBonus;
            int dmg = DamageToEnemy(attack);
            EnemyHp = Math.Max(0, EnemyHp - dmg);
            if (EnemyHp == 0) Outcome = BattleOutcome.Win;

            return new SkillResult { Damage = dmg, Powered = powered };
        }

        public void BreakShield()
        {
            EnemyShielded = false;
            VulnerableTurns = 2;
        }

        public int EnemyTurn()
        {
            int dmg = RollDamage(Enemy.AttackPower, Player.DefensePower + PlayerDefenseBonus);
            PlayerHp = Math.Max(0, PlayerHp - dmg);
            if (VulnerableTurns > 0) VulnerableTurns--;
            if (PlayerHp == 0) Outcome = BattleOutcome.Lose;
            return dmg;
        }
    }
}
