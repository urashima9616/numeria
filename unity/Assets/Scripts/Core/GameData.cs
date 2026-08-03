using System;
using System.Collections.Generic;

namespace Numeria.Core
{
    public enum PuzzleAffinity { Formula, MakeTen, Pattern, Counting, RepeatedAddition, Symmetry }

    public sealed class EvolutionLineDef
    {
        public string BaseId;
        public string Element;
        public string AffinityLabel;
        public PuzzleAffinity Affinity;
        public string[] StageIds;
        public int[] EvolutionLevels;
    }

    /// <summary>
    /// 单形态配置。基础属性是 Lv.1 数值，三个 Growth 字段表示每十级的平均成长量。
    /// BaseXp 同时体现物种稀有度、基础数值与进化阶段差异。
    /// </summary>
    public sealed class SpeciesDef
    {
        public string Id;
        public string Name;
        public int MaxHp;
        public int AttackPower;
        public int DefensePower;
        public int HpGrowth;
        public int AttackGrowth;
        public int DefenseGrowth;
        public int BaseXp;
        public string SkillName;
        public int SkillPower;
        public bool Catchable;
        public double DropChance;
        public ConsumableType PreferredDrop;
    }

    /// <summary>30 只数灵、11 条家族线及所有战斗成长的唯一真理源。</summary>
    public static class GameData
    {
        private static readonly EvolutionLineDef[] EvolutionLines =
        {
            Line("addmander", "FIRE", "equations", PuzzleAffinity.Formula,
                new[] { "addmander", "sumdrake", "equadragon" }, new[] { 8, 15 }),
            Line("tenfin", "WATER", "make-ten combinations", PuzzleAffinity.MakeTen,
                new[] { "tenfin", "decaqua", "tidalten" }, new[] { 8, 15 }),
            Line("shapling", "GRASS", "shape patterns", PuzzleAffinity.Pattern,
                new[] { "shapling", "pattervine", "geoflora" }, new[] { 8, 15 }),
            Line("countipillar", "BUG", "counting and comparison", PuzzleAffinity.Counting,
                new[] { "countipillar", "numberfly" }, new[] { 5 }),
            Line("doublit", "ROCK", "doubling and repeated addition", PuzzleAffinity.RepeatedAddition,
                new[] { "doublit", "duplirock" }, new[] { 5 }),
            Line("mirrowl", "SKY", "symmetry and rotation", PuzzleAffinity.Symmetry,
                new[] { "mirrowl", "symmetrix" }, new[] { 5 }),

            Line("paircub", "MIND", "matching and equality", PuzzleAffinity.Counting,
                new[] { "paircub", "matchbear", "equilibear" }, new[] { 7, 14 }),
            Line("subunny", "EARTH", "subtraction stories", PuzzleAffinity.Formula,
                new[] { "subunny", "differhare", "minuelope" }, new[] { 7, 14 }),
            Line("pebblit", "ROCK", "ordering and tallying", PuzzleAffinity.Counting,
                new[] { "pebblit", "stackstone", "tallytitan" }, new[] { 7, 14 }),
            Line("prismouse", "SKY", "shapes and rotation", PuzzleAffinity.Symmetry,
                new[] { "prismouse", "polygoncat", "geometiger" }, new[] { 7, 14 }),
            Line("seqkit", "GRASS", "sequences and patterns", PuzzleAffinity.Pattern,
                new[] { "seqkit", "patternlynx", "ordinalion" }, new[] { 7, 14 }),
        };

        private static readonly SpeciesDef[] Species =
        {
            // id, name, HP/ATK/DEF, HP/ATK/DEF growth per 10 levels, XP, skill, power, catchable, drop
            Mon("addmander", "Addmander", 10, 1, 1, 8, 2, 2, 7, "Flame Formula", 5),
            Mon("sumdrake", "Sumdrake", 14, 3, 2, 9, 3, 2, 10, "Blaze Equation", 6),
            Mon("equadragon", "Equadragon", 19, 5, 4, 10, 3, 3, 14, "Equalizer Blaze", 7),

            Mon("tenfin", "Tenfin", 11, 1, 2, 9, 2, 3, 7, "Splash Ten", 5),
            Mon("decaqua", "Decaqua", 15, 3, 4, 10, 2, 3, 10, "Decimal Wave", 6),
            Mon("tidalten", "Tidalten", 20, 5, 6, 11, 3, 4, 14, "Tidal Combo", 7),

            Mon("shapling", "Shapling", 10, 1, 2, 9, 2, 3, 7, "Leaf Pattern", 5),
            Mon("pattervine", "Pattervine", 15, 3, 4, 10, 2, 4, 10, "Vine Sequence", 6),
            Mon("geoflora", "Geoflora", 21, 5, 6, 12, 3, 4, 14, "Bloom Symmetry", 7),

            Mon("countipillar", "Countipillar", 8, 1, 1, 8, 2, 2, 6, "Count Crunch", 5, true, .20, ConsumableType.HealthPotion),
            Mon("numberfly", "Numberfly", 12, 3, 2, 9, 3, 2, 10, "Number Wing", 6, false, .30, ConsumableType.GemSnack),
            Mon("doublit", "Doublit", 9, 2, 2, 9, 3, 3, 7, "Double Trouble", 5, true, .22, ConsumableType.HealthPotion),
            Mon("duplirock", "Duplirock", 14, 4, 4, 11, 3, 4, 11, "Double Boulder", 6, false, .32, ConsumableType.HealthPotion),
            Mon("mirrowl", "Mirrowl", 10, 3, 2, 9, 3, 3, 8, "Mirror Pattern", 5, true, .24, ConsumableType.GemSnack),
            Mon("symmetrix", "Symmetrix", 15, 5, 5, 11, 4, 4, 12, "Symmetry Beam", 6, false, .35, ConsumableType.GemSnack),

            Mon("paircub", "Paircub", 10, 2, 2, 9, 2, 3, 7, "Matching Paws", 5, true, .22, ConsumableType.GemSnack),
            Mon("matchbear", "Matchbear", 15, 3, 4, 10, 3, 3, 10, "Equal Embrace", 6, false, .28, ConsumableType.HealthPotion),
            Mon("equilibear", "Equilibear", 21, 5, 6, 12, 3, 4, 14, "Balance Roar", 7, false, .34, ConsumableType.HealthPotion),

            Mon("subunny", "Subunny", 9, 2, 1, 8, 3, 2, 7, "Take Away Hop", 5, true, .20, ConsumableType.HealthPotion),
            Mon("differhare", "Differhare", 14, 4, 3, 9, 3, 3, 10, "Difference Dash", 6, false, .28, ConsumableType.GemSnack),
            Mon("minuelope", "Minuelope", 19, 6, 4, 10, 4, 3, 14, "Minus Meteor", 7, false, .34, ConsumableType.GemSnack),

            Mon("pebblit", "Pebblit", 11, 1, 3, 10, 2, 4, 7, "Tally Toss", 5, true, .24, ConsumableType.HealthPotion),
            Mon("stackstone", "Stackstone", 17, 3, 5, 12, 3, 4, 11, "Order Stack", 6, false, .31, ConsumableType.HealthPotion),
            Mon("tallytitan", "Tallytitan", 24, 5, 8, 14, 3, 5, 15, "Tally Quake", 7, false, .38, ConsumableType.HealthPotion),

            Mon("prismouse", "Prismouse", 9, 2, 2, 8, 3, 3, 8, "Prism Turn", 5, true, .24, ConsumableType.GemSnack),
            Mon("polygoncat", "Polygoncat", 14, 4, 4, 9, 4, 3, 11, "Polygon Pounce", 6, false, .31, ConsumableType.GemSnack),
            Mon("geometiger", "Geometiger", 20, 7, 5, 11, 4, 4, 15, "Geometry Ray", 7, false, .38, ConsumableType.GemSnack),

            Mon("seqkit", "Seqkit", 10, 2, 2, 9, 3, 2, 8, "Sequence Spark", 5, true, .23, ConsumableType.GemSnack),
            Mon("patternlynx", "Patternlynx", 15, 4, 3, 10, 3, 3, 11, "Pattern Prowl", 6, false, .30, ConsumableType.GemSnack),
            Mon("ordinalion", "Ordinalion", 22, 6, 6, 12, 4, 4, 15, "Ordinal Crown", 7, false, .37, ConsumableType.HealthPotion),
        };

        public static IReadOnlyList<EvolutionLineDef> Lines => EvolutionLines;
        public static IReadOnlyList<SpeciesDef> Roster => Species;

        private static EvolutionLineDef Line(string baseId, string element, string affinityLabel,
            PuzzleAffinity affinity, string[] stageIds, int[] levels) => new EvolutionLineDef
        {
            BaseId = baseId, Element = element, AffinityLabel = affinityLabel, Affinity = affinity,
            StageIds = stageIds, EvolutionLevels = levels,
        };

        private static SpeciesDef Mon(string id, string name, int hp, int attack, int defense,
            int hpGrowth, int attackGrowth, int defenseGrowth, int baseXp, string skill, int power,
            bool catchable = false, double dropChance = .18,
            ConsumableType preferredDrop = ConsumableType.HealthPotion) => new SpeciesDef
        {
            Id = id, Name = name, MaxHp = hp, AttackPower = attack, DefensePower = defense,
            HpGrowth = hpGrowth, AttackGrowth = attackGrowth, DefenseGrowth = defenseGrowth,
            BaseXp = baseXp, SkillName = skill, SkillPower = power, Catchable = catchable,
            DropChance = dropChance, PreferredDrop = preferredDrop,
        };

        public static EvolutionLineDef LineFor(string id)
        {
            foreach (var line in EvolutionLines)
                foreach (string stageId in line.StageIds)
                    if (stageId == id) return line;
            return null;
        }

        public static string BaseId(string id) => LineFor(id)?.BaseId ?? id;

        public static int StageIndex(string id)
        {
            var line = LineFor(id);
            return line == null ? 0 : Array.IndexOf(line.StageIds, id);
        }

        public static string FormId(string id, int stage)
        {
            var line = LineFor(id);
            return line == null ? id : line.StageIds[Math.Max(0, Math.Min(stage, line.StageIds.Length - 1))];
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
            return species == null ? null : ToCombatant(species, 1, species.Catchable, null, false, false);
        }

        public static CombatantDef PlayerMon(string id, int stage) => PlayerMon(id, stage, 1);

        public static CombatantDef PlayerMon(string id, int stage, int level)
        {
            var species = SpeciesById(FormId(id, stage)) ?? Species[0];
            return ToCombatant(species, level, false, null, true, false);
        }

        public static CombatantDef PlayerMon(string id, bool evolved) =>
            PlayerMon(id, evolved && BaseId(id) == "addmander" ? 1 : StageIndex(id), 1);

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
        public static CombatantDef Countipillar() => CreateWild("countipillar", 1, new Rng(1));
        public static CombatantDef Doublit() => CreateWild("doublit", 1, new Rng(1));
        public static CombatantDef Mirrowl() => CreateWild("mirrowl", 1, new Rng(1));
        public static CombatantDef Numberfly() => CreateBoss("numberfly", 5, 1, new Rng(1));
        public static CombatantDef Duplirock() => CreateBoss("duplirock", 10, 2, new Rng(1));
        public static CombatantDef DuplirockElder() => CreateBoss("duplirock", 12, 2, new Rng(1), "Duplirock Elder");
        public static CombatantDef Symmetrix() => CreateBoss("symmetrix", 20, 3, new Rng(1));

        public static CombatantDef CreateWild(string id, int level, Rng rng)
        {
            var species = SpeciesById(id) ?? Species[9];
            var result = ToCombatant(species, level, true, null, false, false);
            int variance = rng.Pick(92, 108);
            result.MaxHp = Math.Max(3, (result.MaxHp * variance + 50) / 100);
            return result;
        }

        public static CombatantDef CreateBoss(string id, int level, int tier, Rng rng, string displayName = null)
        {
            var species = SpeciesById(id) ?? Species[10];
            int shield = Math.Max(10, Math.Min(30, tier * 10));
            var result = ToCombatant(species, level, false, shield, false, true);
            double hpMultiplier = 1.60d + Math.Max(1, tier) * .25d;
            result.MaxHp = Math.Max(result.MaxHp + 8, (int)Math.Round(result.MaxHp * hpMultiplier));
            result.AttackPower += Math.Max(1, tier);
            result.DefensePower += Math.Max(0, tier - 1);
            result.DropChance = 1d;
            if (!string.IsNullOrEmpty(displayName)) result.Name = displayName;
            return result;
        }

        /// <summary>商人伙伴比同级野生数灵更耐打，但没有 Boss 护盾且不可捕捉。</summary>
        public static CombatantDef CreateTrainerOpponent(string id, int level, int tier, Rng rng, string displayName = null)
        {
            var species = SpeciesById(id) ?? Species[9];
            var result = ToCombatant(species, level, false, null, false, false);
            int trainedHp = (int)Math.Round(result.MaxHp * (1.20d + tier * .08d));
            result.MaxHp = Math.Max(result.MaxHp + 4, trainedHp + rng.Pick(-2, 2));
            result.AttackPower += Math.Max(1, tier - 1);
            result.DefensePower += Math.Max(0, tier - 2);
            result.DropChance = 0d;
            if (!string.IsNullOrEmpty(displayName)) result.Name = displayName;
            return result;
        }

        /// <summary>旧调用兼容；新地图使用 MapDef.RollWildEncounter。</summary>
        public static CombatantDef RollWild(CombatantDef template, int tier, Rng rng)
        {
            int level = tier <= 1 ? 1 : tier == 2 ? 8 : 15;
            return CreateWild(template.Id, level, rng);
        }

        private static CombatantDef ToCombatant(SpeciesDef species, int level, bool catchable, int? shield,
            bool includeSkills, bool boss)
        {
            int clamped = GrowthSystem.ClampLevel(level);
            SkillDef themeSkill = ThemeSkill(species);
            return new CombatantDef
            {
                Id = species.Id,
                Name = species.Name,
                Level = clamped,
                MaxHp = GrowthSystem.StatAtLevel(species.MaxHp, species.HpGrowth, clamped),
                AttackPower = GrowthSystem.StatAtLevel(species.AttackPower, species.AttackGrowth, clamped),
                DefensePower = GrowthSystem.StatAtLevel(species.DefensePower, species.DefenseGrowth, clamped),
                BaseXp = species.BaseXp,
                IsBoss = boss,
                DropChance = species.DropChance,
                PreferredDrop = species.PreferredDrop,
                Shield = shield,
                Catchable = catchable,
                Skills = includeSkills
                    ? new[]
                    {
                        new SkillDef { Id = "tackle", Name = "Tackle", Cost = 0, Power = 2,
                            BasePower = 2, Type = SkillType.Basic,
                            IconResource = "generated/NUMERIA_Unity_Battle_Assets/UI/Icons/Tackle",
                            Visual = SkillVisualKind.Physical },
                        themeSkill,
                    }
                    : Array.Empty<SkillDef>(),
            };
        }

        /// <summary>
        /// 每条进化家族共享一种数学魔法语义；形态升级只改变名称与威力，不偷换视觉语言。
        /// </summary>
        private static SkillDef ThemeSkill(SpeciesDef species)
        {
            string baseId = BaseId(species.Id);
            string skillId;
            string icon;
            SkillVisualKind visual;
            switch (baseId)
            {
                case "tenfin":
                    skillId = "make-ten-wave"; icon = "make_ten_wave"; visual = SkillVisualKind.MakeTenWave; break;
                case "shapling":
                    skillId = "pattern-leaf"; icon = "pattern_leaf"; visual = SkillVisualKind.PatternLeaf; break;
                case "countipillar":
                    skillId = "count-crunch"; icon = "count_crunch"; visual = SkillVisualKind.CountCrunch; break;
                case "doublit":
                    skillId = "double-boulder"; icon = "double_boulder"; visual = SkillVisualKind.DoubleBoulder; break;
                case "mirrowl":
                    skillId = "symmetry-beam"; icon = "symmetry_beam"; visual = SkillVisualKind.SymmetryBeam; break;
                case "paircub":
                    skillId = "matching-paws"; icon = "matching_paws"; visual = SkillVisualKind.MatchingPaws; break;
                case "subunny":
                    skillId = "subtraction-dash"; icon = "subtraction_dash"; visual = SkillVisualKind.SubtractionDash; break;
                case "pebblit":
                    skillId = "tally-stone"; icon = "tally_stone"; visual = SkillVisualKind.TallyStone; break;
                case "prismouse":
                    skillId = "geometry-prism"; icon = "geometry_prism"; visual = SkillVisualKind.GeometryPrism; break;
                case "seqkit":
                    skillId = "sequence-spark"; icon = "sequence_spark"; visual = SkillVisualKind.SequenceSpark; break;
                default:
                    skillId = "equation-flame"; icon = "equation_flame"; visual = SkillVisualKind.EquationFlame; break;
            }

            return new SkillDef
            {
                Id = skillId,
                Name = species.SkillName,
                Cost = 3,
                Power = species.SkillPower,
                BasePower = 2,
                Type = SkillType.Formula,
                IconResource = $"generated/Skills/{icon}",
                Visual = visual,
            };
        }
    }
}
