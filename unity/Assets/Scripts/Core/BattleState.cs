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
        public int MaxHp;
        public int AttackPower;
        public int? Shield;
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
        public BattleOutcome Outcome = BattleOutcome.None;

        public BattleState(CombatantDef player, CombatantDef enemy)
        {
            Player = player;
            Enemy = enemy;
            PlayerHp = player.MaxHp;
            EnemyHp = enemy.MaxHp;
            EnemyShielded = enemy.Shield.HasValue;
        }

        public void StartPlayerTurn() => Gems = Math.Min(MaxGems, Gems + 2);

        public int DamageToEnemy(int baseDamage)
        {
            if (VulnerableTurns > 0) return baseDamage * 2;
            if (EnemyShielded) return Math.Max(1, baseDamage / 2);
            return baseDamage;
        }

        public SkillResult UseSkill(string skillId, bool correct = true)
        {
            var skill = Player.Skills.First(s => s.Id == skillId);
            if (Gems < skill.Cost) throw new InvalidOperationException("not enough gems");
            Gems -= skill.Cost;

            bool powered = skill.Type != SkillType.Formula || correct;
            int dmg = DamageToEnemy(powered ? skill.Power : skill.BasePower);
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
            int dmg = Enemy.AttackPower;
            PlayerHp = Math.Max(0, PlayerHp - dmg);
            if (VulnerableTurns > 0) VulnerableTurns--;
            if (PlayerHp == 0) Outcome = BattleOutcome.Lose;
            return dmg;
        }
    }
}
