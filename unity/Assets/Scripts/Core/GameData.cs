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

        /// <summary>按 id 取数灵定义(图鉴/菜单用)。未知 id 返回 null。</summary>
        public static CombatantDef ById(string id)
        {
            switch (id)
            {
                case "addmander": return Addmander();
                case "duplirock": return Duplirock();
                case "countipillar": return Countipillar();
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
