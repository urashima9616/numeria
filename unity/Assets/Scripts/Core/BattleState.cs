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

        /// <summary>
        /// Converting a stronger duplicate still rewards the successful catch, while adopting
        /// the captured creature remains the more valuable long-term roster choice.
        /// </summary>
        public static int ConversionXp(int baseCatchXp) =>
            Math.Max(1, (int)Math.Round(Math.Max(0, baseCatchXp) * 1.25d));
    }

    public enum SkillType { Basic, Formula }

    /// <summary>
    /// 主题技能的演出语言。Core 只保存语义，Game 层据此选择弹道、颜色与命中节奏。
    /// </summary>
    public enum SkillVisualKind
    {
        Physical,
        EquationFlame,
        MakeTenWave,
        PatternLeaf,
        CountCrunch,
        DoubleBoulder,
        SymmetryBeam,
        MatchingPaws,
        SubtractionDash,
        TallyStone,
        GeometryPrism,
        SequenceSpark,
        FairyGlimmer,
        DragonSpiral,
        ElectricBolt,
        GrassBloom,
        FlyingGust,
    }

    public class SkillDef
    {
        public string Id;
        public string Name;
        public int Cost;
        public int Power;
        public int BasePower;
        public SkillType Type;
        public string IconResource;
        public SkillVisualKind Visual;
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
        public bool BreakBonusApplied;
    }

    /// <summary>
    /// 每个 Mathmon 都由物种 ID 稳定派生一份 Mega 配置。无需逐个维护 141 份数据，
    /// 新增物种也会自动获得 25–35% 增幅、外形变体和专属强力技能。
    /// </summary>
    public sealed class MegaProfile
    {
        public int BonusPercent;
        public int AppearanceVariant;
        public SkillDef Skill;
    }

    public static class MegaSystem
    {
        public const int RequiredGems = 7;
        public const int MinimumBonusPercent = 25;
        public const int MaximumBonusPercent = 35;

        public static MegaProfile For(CombatantDef combatant)
        {
            if (combatant == null) throw new ArgumentNullException(nameof(combatant));
            uint hash = StableHash(combatant.Id ?? combatant.Name ?? "mathmon");
            int bonus = MinimumBonusPercent +
                        (int)(hash % (MaximumBonusPercent - MinimumBonusPercent + 1));
            var theme = combatant.Skills?
                .Where(skill => skill != null && skill.Type == SkillType.Formula)
                .OrderByDescending(skill => skill.Power)
                .FirstOrDefault();
            var strongest = combatant.Skills?
                .Where(skill => skill != null)
                .OrderByDescending(skill => skill.Power)
                .FirstOrDefault();
            int power = Math.Max(9, (strongest?.Power ?? 4) + 5);

            return new MegaProfile
            {
                BonusPercent = bonus,
                AppearanceVariant = (int)((hash / 11u) % 3u),
                Skill = new SkillDef
                {
                    Id = $"mega-{combatant.Id}-nova",
                    Name = $"{combatant.Name} Nova",
                    Cost = 0,
                    Power = power,
                    BasePower = power,
                    Type = SkillType.Basic,
                    IconResource = theme?.IconResource ?? strongest?.IconResource,
                    Visual = theme?.Visual ?? strongest?.Visual ?? SkillVisualKind.Physical,
                }
            };
        }

        public static int BoostedStat(int baseStat, int bonusPercent)
        {
            if (baseStat <= 0) return 0;
            int clamped = Math.Max(MinimumBonusPercent,
                Math.Min(MaximumBonusPercent, bonusPercent));
            return (baseStat * (100 + clamped) + 99) / 100;
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }
                return hash;
            }
        }
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
        public bool BreakBonusReady;
        public int EnemySkipTurns;
        public int PlayerAttackBonus;
        public int PlayerDefenseBonus;
        public MegaProfile Mega { get; }
        public bool MegaActive { get; private set; }
        public int MegaActivationCount { get; private set; }
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
            Mega = MegaSystem.For(player);
        }

        public int EffectivePlayerMaxHp => MegaActive
            ? MegaSystem.BoostedStat(Player.MaxHp, Mega.BonusPercent)
            : Player.MaxHp;

        public int EffectivePlayerAttack =>
            (MegaActive ? MegaSystem.BoostedStat(Player.AttackPower, Mega.BonusPercent) : Player.AttackPower) +
            PlayerAttackBonus;

        public int EffectivePlayerDefense =>
            (MegaActive ? MegaSystem.BoostedStat(Player.DefensePower, Mega.BonusPercent) : Player.DefensePower) +
            PlayerDefenseBonus;

        public bool CanMegaEvolve => Outcome == BattleOutcome.None && !MegaActive && Gems >= MegaSystem.RequiredGems;
        public bool CanRestoreGems => !MegaActive;

        public int SkillCost(SkillDef skill) => MegaActive ? 0 : Math.Max(0, skill?.Cost ?? 0);

        public void StartPlayerTurn()
        {
            // Mega 必须持续净消耗 Gem；否则每回合自动 +2 会令形态永不结束。
            if (!MegaActive) Gems = Math.Min(MaxGems, Gems + 2);
        }

        /// <summary>数学谜题答对且拥有至少 7 Gem 才能激活；激活不消耗当前行动。</summary>
        public bool TryActivateMega(bool puzzleSolved)
        {
            if (!puzzleSolved || !CanMegaEvolve) return false;
            int oldMax = Player.MaxHp;
            MegaActive = true;
            MegaActivationCount++;
            PlayerHp += EffectivePlayerMaxHp - oldMax;
            return true;
        }

        /// <summary>
        /// 每个玩家行动结束后调用。返回 true 表示本次消耗令 Gem 归零并触发退化。
        /// </summary>
        public bool ConsumeMegaTurn()
        {
            if (!MegaActive) return false;
            Gems = Math.Max(0, Gems - 1);
            if (Gems > 0) return false;
            MegaActive = false;
            PlayerHp = Math.Min(PlayerHp, Player.MaxHp);
            return true;
        }

        public int HealPlayer(int amount)
        {
            int before = PlayerHp;
            PlayerHp = Math.Min(EffectivePlayerMaxHp, PlayerHp + Math.Max(0, amount));
            return PlayerHp - before;
        }

        public int RestoreGems(int amount)
        {
            if (!CanRestoreGems) return 0;
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
            if (EnemyShielded) return Math.Max(1, damage / 2);
            if (BreakBonusReady) return damage * 2;
            return damage;
        }

        public SkillResult UseSkill(string skillId, bool correct = true)
        {
            var skill = Player.Skills.FirstOrDefault(s => s.Id == skillId);
            if (skill == null && MegaActive && Mega.Skill.Id == skillId) skill = Mega.Skill;
            if (skill == null) throw new InvalidOperationException("skill is not available");
            int cost = SkillCost(skill);
            if (Gems < cost) throw new InvalidOperationException("not enough gems");
            Gems -= cost;

            bool powered = skill.Type != SkillType.Formula || correct;
            int attack = (powered ? skill.Power : skill.BasePower) +
                         EffectivePlayerAttack;
            bool breakBonusApplied = Enemy.Shield.HasValue && !EnemyShielded && BreakBonusReady;
            int dmg = DamageToEnemy(attack);
            EnemyHp = Math.Max(0, EnemyHp - dmg);
            if (breakBonusApplied)
            {
                // 破盾奖励只作用于第一次攻击；命中后若敌人仍存活，护盾立即重置。
                BreakBonusReady = false;
                EnemyShielded = EnemyHp > 0;
            }
            if (EnemyHp == 0) Outcome = BattleOutcome.Win;

            return new SkillResult
            {
                Damage = dmg,
                Powered = powered,
                BreakBonusApplied = breakBonusApplied
            };
        }

        public void BreakShield()
        {
            if (!Enemy.Shield.HasValue || !EnemyShielded) return;
            EnemyShielded = false;
            BreakBonusReady = true;
            EnemySkipTurns = 1;
        }

        public bool ConsumeEnemySkipTurn()
        {
            if (EnemySkipTurns <= 0) return false;
            EnemySkipTurns--;
            return true;
        }

        public int EnemyTurn()
        {
            int dmg = RollDamage(Enemy.AttackPower, EffectivePlayerDefense);
            PlayerHp = Math.Max(0, PlayerHp - dmg);
            if (PlayerHp == 0) Outcome = BattleOutcome.Lose;
            return dmg;
        }
    }
}
