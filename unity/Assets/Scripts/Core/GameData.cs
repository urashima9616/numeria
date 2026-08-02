using System;
using System.Collections.Generic;

namespace Numeria.Core
{
    public enum PuzzleAffinity { Formula, MakeTen, Pattern }

    /// <summary>一条进化家族的稳定配置。存档只保存 BaseId + Stage，名称和数值从这里解析。</summary>
    public sealed class EvolutionLineDef
    {
        public string BaseId;
        public string Element;
        public string AffinityLabel;
        public PuzzleAffinity Affinity;
        public string[] StageIds;
        public int[] EvolutionLevels;
    }

    /// <summary>首发 15 只数灵的单形态配置。</summary>
    public sealed class SpeciesDef
    {
        public string Id;
        public string Name;
        public int MaxHp;
        public int AttackPower;
        public string SkillName;
        public int SkillPower;
        public bool Catchable;
    }

    /// <summary>
    /// Numeria 首发图鉴。三条御三家为三段进化，三条地图特色线为两段进化，共 15 只。
    /// 进化线和物种数值集中在此，战斗、菜单和存档共享同一份定义。
    /// </summary>
    public static class GameData
    {
        private static readonly EvolutionLineDef[] EvolutionLines =
        {
            Line("addmander", "FIRE", "equations", PuzzleAffinity.Formula,
                new[] { "addmander", "sumdrake", "equadragon" }, new[] { 5, 10 }),
            Line("tenfin", "WATER", "make-ten combinations", PuzzleAffinity.MakeTen,
                new[] { "tenfin", "decaqua", "tidalten" }, new[] { 5, 10 }),
            Line("shapling", "GRASS", "shape patterns", PuzzleAffinity.Pattern,
                new[] { "shapling", "pattervine", "geoflora" }, new[] { 5, 10 }),
            Line("countipillar", "BUG", "counting and comparison", PuzzleAffinity.Formula,
                new[] { "countipillar", "numberfly" }, new[] { 5 }),
            Line("doublit", "ROCK", "doubling and repeated addition", PuzzleAffinity.MakeTen,
                new[] { "doublit", "duplirock" }, new[] { 5 }),
            Line("mirrowl", "SKY", "symmetry and rotation", PuzzleAffinity.Pattern,
                new[] { "mirrowl", "symmetrix" }, new[] { 5 }),
        };

        private static readonly SpeciesDef[] Species =
        {
            Mon("addmander", "Addmander", 10, 1, "Flame Formula", 5),
            Mon("sumdrake", "Sumdrake", 12, 2, "Blaze Equation", 6),
            Mon("equadragon", "Equadragon", 15, 3, "Equalizer Blaze", 7),

            Mon("tenfin", "Tenfin", 10, 1, "Splash Ten", 5),
            Mon("decaqua", "Decaqua", 12, 2, "Decimal Wave", 6),
            Mon("tidalten", "Tidalten", 15, 3, "Tidal Combo", 7),

            Mon("shapling", "Shapling", 10, 1, "Leaf Pattern", 5),
            Mon("pattervine", "Pattervine", 12, 2, "Vine Sequence", 6),
            Mon("geoflora", "Geoflora", 15, 3, "Bloom Symmetry", 7),

            Mon("countipillar", "Countipillar", 8, 1, "Count Crunch", 5, true),
            Mon("numberfly", "Numberfly", 12, 2, "Number Wing", 6),
            Mon("doublit", "Doublit", 9, 2, "Double Trouble", 5, true),
            Mon("duplirock", "Duplirock", 13, 3, "Double Boulder", 6),
            Mon("mirrowl", "Mirrowl", 10, 2, "Mirror Pattern", 5, true),
            Mon("symmetrix", "Symmetrix", 14, 3, "Symmetry Beam", 6),
        };

        public static IReadOnlyList<EvolutionLineDef> Lines => EvolutionLines;
        public static IReadOnlyList<SpeciesDef> Roster => Species;

        private static EvolutionLineDef Line(string baseId, string element, string affinityLabel,
            PuzzleAffinity affinity, string[] stageIds, int[] levels) => new EvolutionLineDef
        {
            BaseId = baseId,
            Element = element,
            AffinityLabel = affinityLabel,
            Affinity = affinity,
            StageIds = stageIds,
            EvolutionLevels = levels,
        };

        private static SpeciesDef Mon(string id, string name, int hp, int attack, string skill, int power,
            bool catchable = false) => new SpeciesDef
        {
            Id = id,
            Name = name,
            MaxHp = hp,
            AttackPower = attack,
            SkillName = skill,
            SkillPower = power,
            Catchable = catchable,
        };

        public static EvolutionLineDef LineFor(string id)
        {
            foreach (var line in EvolutionLines)
                foreach (string stageId in line.StageIds)
                    if (stageId == id) return line;
            return null;
        }

        public static string BaseId(string id)
        {
            var line = LineFor(id);
            return line != null ? line.BaseId : id;
        }

        public static int StageIndex(string id)
        {
            var line = LineFor(id);
            if (line == null) return 0;
            return Array.IndexOf(line.StageIds, id);
        }

        public static string FormId(string id, int stage)
        {
            var line = LineFor(id);
            if (line == null) return id;
            return line.StageIds[Math.Max(0, Math.Min(stage, line.StageIds.Length - 1))];
        }

        public static int NextEvolutionLevel(string id, int stage)
        {
            var line = LineFor(id);
            return line == null || stage >= line.EvolutionLevels.Length ? 0 : line.EvolutionLevels[stage];
        }

        public static SpeciesDef SpeciesById(string id)
        {
            foreach (var species in Species)
                if (species.Id == id) return species;
            return null;
        }

        public static CombatantDef ById(string id)
        {
            var species = SpeciesById(id);
            return species == null ? null : ToCombatant(species, species.Catchable, null, false);
        }

        /// <summary>玩家出战配置。id 可以是家族基础 id，也可以是任一形态 id。</summary>
        public static CombatantDef PlayerMon(string id, int stage)
        {
            string formId = FormId(id, stage);
            var species = SpeciesById(formId) ?? Species[0];
            return ToCombatant(species, false, null, true);
        }

        /// <summary>旧调用兼容：历史 Evolved=true 表示 Addmander 的第二形态。</summary>
        public static CombatantDef PlayerMon(string id, bool evolved) =>
            PlayerMon(id, evolved && BaseId(id) == "addmander" ? 1 : StageIndex(id));

        public static CombatantDef Player(bool evolved) => PlayerMon("addmander", evolved);
        public static CombatantDef Addmander() => PlayerMon("addmander", 0);
        public static CombatantDef Sumdrake() => PlayerMon("addmander", 1);
        public static CombatantDef Equadragon() => PlayerMon("addmander", 2);
        public static CombatantDef Tenfin() => PlayerMon("tenfin", 0);
        public static CombatantDef Decaqua() => PlayerMon("tenfin", 1);
        public static CombatantDef Tidalten() => PlayerMon("tenfin", 2);
        public static CombatantDef Shapling() => PlayerMon("shapling", 0);
        public static CombatantDef Pattervine() => PlayerMon("shapling", 1);
        public static CombatantDef Geoflora() => PlayerMon("shapling", 2);
        public static CombatantDef Countipillar() => ById("countipillar");
        public static CombatantDef Numberfly() => Boss("numberfly", 10);
        public static CombatantDef Doublit() => ById("doublit");
        public static CombatantDef Duplirock() => Boss("duplirock", 10, null, 10, 2);
        public static CombatantDef DuplirockElder() => Boss("duplirock", 12, "Duplirock Elder", 10, 2);
        public static CombatantDef Mirrowl() => ById("mirrowl");
        public static CombatantDef Symmetrix() => Boss("symmetrix", 12);

        private static CombatantDef Boss(string id, int shield, string displayName = null,
            int? maxHp = null, int? attack = null)
        {
            var species = SpeciesById(id);
            var result = ToCombatant(species, false, shield, false);
            if (displayName != null) result.Name = displayName;
            if (maxHp.HasValue) result.MaxHp = maxHp.Value;
            if (attack.HasValue) result.AttackPower = attack.Value;
            return result;
        }

        private static CombatantDef ToCombatant(SpeciesDef species, bool catchable, int? shield,
            bool includeSkills)
        {
            return new CombatantDef
            {
                Id = species.Id,
                Name = species.Name,
                MaxHp = includeSkills ? Math.Max(10, species.MaxHp) : species.MaxHp,
                AttackPower = includeSkills ? 0 : species.AttackPower,
                Shield = shield,
                Catchable = catchable,
                Skills = includeSkills
                    ? new[]
                    {
                        new SkillDef
                        {
                            Id = "tackle", Name = "Tackle", Cost = 0, Power = 2,
                            BasePower = 2, Type = SkillType.Basic
                        },
                        new SkillDef
                        {
                            // BattleController/PuzzleUi 仍以稳定技能 id 路由，显示名来自物种配置。
                            Id = "flame-formula", Name = species.SkillName, Cost = 3,
                            Power = species.SkillPower, BasePower = 2, Type = SkillType.Formula
                        },
                    }
                    : Array.Empty<SkillDef>(),
            };
        }
    }
}
