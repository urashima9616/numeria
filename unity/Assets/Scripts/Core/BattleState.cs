using System;
using System.Linq;

namespace Numeria.Core
{
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
