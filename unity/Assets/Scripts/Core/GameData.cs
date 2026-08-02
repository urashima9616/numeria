namespace Numeria.Core
{
    /// <summary>
    /// 首发数值,与 Web 原型 data.js 一致。
    /// P3 阶段迁移到 StreamingAssets JSON 配置(DLC 架构)。
    /// </summary>
    public static class GameData
    {
        public static CombatantDef Addmander() => new CombatantDef
        {
            Id = "addmander",
            Name = "Addmander",
            MaxHp = 10,
            AttackPower = 0,
            Shield = null,
            Skills = new[]
            {
                new SkillDef { Id = "tackle", Name = "Tackle", Cost = 0, Power = 2, BasePower = 2, Type = SkillType.Basic },
                new SkillDef { Id = "flame-formula", Name = "Flame Formula", Cost = 3, Power = 5, BasePower = 2, Type = SkillType.Formula },
            },
        };

        public static CombatantDef Duplirock() => new CombatantDef
        {
            Id = "duplirock",
            Name = "Duplirock",
            MaxHp = 10,
            AttackPower = 2,
            Shield = 10,
            Catchable = false,
            Skills = new SkillDef[0],
        };

        /// <summary>玩家当前形态(随进化切换)。</summary>
        public static CombatantDef Player(bool evolved) => evolved ? Sumdrake() : Addmander();

        /// <summary>出战数灵的玩家侧配置:每只都有普攻 + 主题算式技。进化只作用于御三家。</summary>
        public static CombatantDef PlayerMon(string id, bool evolved)
        {
            switch (id)
            {
                case "countipillar":
                    return new CombatantDef
                    {
                        Id = "countipillar", Name = "Countipillar", MaxHp = 10, AttackPower = 0,
                        Skills = new[]
                        {
                            new SkillDef { Id = "tackle", Name = "Tackle", Cost = 0, Power = 2, BasePower = 2, Type = SkillType.Basic },
                            new SkillDef { Id = "flame-formula", Name = "Count Crunch", Cost = 3, Power = 5, BasePower = 2, Type = SkillType.Formula },
                        },
                    };
                case "doublit":
                    return new CombatantDef
                    {
                        Id = "doublit", Name = "Doublit", MaxHp = 10, AttackPower = 0,
                        Skills = new[]
                        {
                            new SkillDef { Id = "tackle", Name = "Tackle", Cost = 0, Power = 2, BasePower = 2, Type = SkillType.Basic },
                            new SkillDef { Id = "flame-formula", Name = "Double Trouble", Cost = 3, Power = 5, BasePower = 2, Type = SkillType.Formula },
                        },
                    };
                default:
                    return Player(evolved);
            }
        }

        public static CombatantDef Sumdrake() => new CombatantDef
        {
            Id = "sumdrake", Name = "Sumdrake", MaxHp = 10, AttackPower = 0,
            Shield = null, Catchable = false,
            Skills = new[]
            {
                new SkillDef { Id = "tackle", Name = "Tackle", Cost = 0, Power = 2, BasePower = 2, Type = SkillType.Basic },
                new SkillDef { Id = "flame-formula", Name = "Blaze Equation", Cost = 3, Power = 6, BasePower = 2, Type = SkillType.Formula },
            },
        };

        public static CombatantDef Doublit() => new CombatantDef
        {
            Id = "doublit", Name = "Doublit", MaxHp = 9, AttackPower = 2,
            Shield = null, Catchable = true, Skills = new SkillDef[0],
        };

        /// <summary>山脉 Boss:凑十二护盾(复用 Duplirock 立绘)。</summary>
        public static CombatantDef DuplirockElder() => new CombatantDef
        {
            Id = "duplirock", Name = "Duplirock Elder", MaxHp = 10, AttackPower = 2,
            Shield = 12, Catchable = false, Skills = new SkillDef[0],
        };

        /// <summary>按 id 取数灵定义(图鉴/菜单用)。未知 id 返回 null。</summary>
        public static CombatantDef ById(string id)
        {
            switch (id)
            {
                case "addmander": return Addmander();
                case "sumdrake": return Sumdrake();
                case "duplirock": return Duplirock();
                case "countipillar": return Countipillar();
                case "doublit": return Doublit();
                default: return null;
            }
        }

        public static CombatantDef Countipillar() => new CombatantDef
        {
            Id = "countipillar",
            Name = "Countipillar",
            MaxHp = 8,
            AttackPower = 1,
            Shield = null,
            Catchable = true,
            Skills = new SkillDef[0],
        };
    }
}
