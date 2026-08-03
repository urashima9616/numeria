using System;
using System.Collections.Generic;
using Numeria.Core;

namespace Numeria.Game
{
    public sealed class EncounterEntry
    {
        public string SpeciesId;
        public int Weight;
        public int MinimumLevel;
        public int MinLevelOffset;
        public int MaxLevelOffset;
    }

    public enum ChestRewardType { HealthPotion, GemSnack, EvolutionStone, AttackCharm, DefenseCharm }

    public sealed class ChestRewardDef
    {
        public ChestRewardType Type;
        public string Name;
        public int Amount = 1;
    }

    /// <summary>一张地图的布局、带权生态、Boss、宝箱和美术配置。</summary>
    public class MapDef
    {
        public string Id;
        public string DisplayName;
        public string WelcomeLine;
        public string[] Rows;
        public int Tier;
        public string BattleBg;
        public string CameraBg;
        public string Theme;
        public EncounterEntry[] Encounters;
        public string BossSpeciesId;
        public string BossDisplayName;
        public int BossMinLevel;
        public string BossLine;
        public string GateClearLine;
        public Func<Progress, bool> GateCleared;
        public Action<Progress> ClearGate;
        public string PortalTargetMap;
        public string NextName;
        public Dictionary<string, ChestRewardDef> ChestRewards;

        public CombatantDef RollWildEncounter(int playerLevel, Rng rng)
        {
            int total = 0;
            foreach (var entry in Encounters) total += Math.Max(0, entry.Weight);
            int roll = rng.Pick(1, Math.Max(1, total));
            EncounterEntry selected = Encounters[0];
            foreach (var entry in Encounters)
            {
                roll -= Math.Max(0, entry.Weight);
                if (roll <= 0) { selected = entry; break; }
            }

            int min = Math.Max(selected.MinimumLevel, playerLevel + selected.MinLevelOffset);
            int max = Math.Max(min, playerLevel + selected.MaxLevelOffset);
            return GameData.CreateWild(selected.SpeciesId,
                GrowthSystem.ClampLevel(rng.Pick(min, max)), rng);
        }

        public CombatantDef RollBossEncounter(int playerLevel, Rng rng)
        {
            int level = Math.Max(BossMinLevel, playerLevel + Tier + 1);
            return GameData.CreateBoss(BossSpeciesId, GrowthSystem.ClampLevel(level), Tier, rng, BossDisplayName);
        }

        public List<string> ChestIds()
        {
            var result = new List<string>();
            for (int y = 0; y < Rows.Length; y++)
                for (int x = 0; x < Rows[y].Length; x++)
                    if (Rows[y][x] == 'C') result.Add($"{Id}-chest-{x}-{y}");
            return result;
        }

        public bool AllChestsOpened(Progress progress)
        {
            foreach (string id in ChestIds())
                if (!progress.OpenedChests.Contains(id)) return false;
            return true;
        }
    }

    public static class Maps
    {
        private static EncounterEntry E(string id, int weight, int minimum, int minOffset, int maxOffset) =>
            new EncounterEntry
            {
                SpeciesId = id, Weight = weight, MinimumLevel = minimum,
                MinLevelOffset = minOffset, MaxLevelOffset = maxOffset,
            };

        private static ChestRewardDef R(ChestRewardType type, string name, int amount = 1) =>
            new ChestRewardDef { Type = type, Name = name, Amount = amount };

        public static MapDef Get(string id)
        {
            switch (id)
            {
                case "mountains": return Mountains();
                case "sky": return Sky();
                default: return Forest();
            }
        }

        public static MapDef Forest() => new MapDef
        {
            Id = "forest", DisplayName = "Mystic Forest", WelcomeLine = "Welcome to Mystic Forest!",
            Rows = new[]
            {
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                "T....bbb....T......bbb.......T",
                "T.S..bbb....T......bbb.......T",
                "T...........T...........C....T",
                "T..TT....bbbb....TT..........T",
                "T..T.....bbbb....TT....C.....T",
                "T..T..C..bbbb................T",
                "T..T..........TTTT....bbb....T",
                "T.....bbb.....T.......bbb....T",
                "T.....bbb.....T..C....bbb....T",
                "T.....bbb....................T",
                "T..TT......bbbb.....TT.......T",
                "T..........bbbb.....TT....P..T",
                "T....bbb...bbbb..............T",
                "T............................T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            },
            Tier = 1,
            BattleBg = "generated/NUMERIA_Unity_Battle_Assets/Backgrounds/Sunny_Meadow_2048x1152",
            CameraBg = "#2f4f2f", Theme = "forest",
            Encounters = new[]
            {
                E("countipillar", 32, 1, -1, 1), E("paircub", 24, 1, -1, 2),
                E("subunny", 20, 2, 0, 2), E("pebblit", 14, 3, 0, 3),
                E("numberfly", 10, 5, 1, 3),
            },
            BossSpeciesId = "numberfly", BossMinLevel = 5,
            BossLine = "Numberfly guards the portal!", GateClearLine = "The portal is open! A new world awaits!",
            GateCleared = p => p.BossBeaten, ClearGate = p => p.BossBeaten = true,
            PortalTargetMap = "mountains", NextName = "Silent Peaks",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "forest-chest-24-3", R(ChestRewardType.HealthPotion, "Berry Potion", 2) },
                { "forest-chest-23-5", R(ChestRewardType.GemSnack, "Crystal Cookie", 2) },
                { "forest-chest-6-6", R(ChestRewardType.AttackCharm, "Power Acorn") },
                { "forest-chest-17-9", R(ChestRewardType.HealthPotion, "Forest Tonic", 2) },
            },
        };

        public static MapDef Mountains() => new MapDef
        {
            Id = "mountains", DisplayName = "Silent Peaks", WelcomeLine = "Welcome to Silent Peaks!",
            Rows = new[]
            {
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                "T.S....TTTT....bbb....TTTT.....T",
                "T......T..T....bbb....T..T.C...T",
                "T..C...T..T...........T..T.....T",
                "T......T..TTTT....TTTTT..T.....T",
                "TTTT...T.........C......TT.....T",
                "T......TTTTT..bbb..TTTT........T",
                "T..bbb.......TbbbT.......bbb...T",
                "T..bbb..TT...T...T..TT...bbb...T",
                "T.......TT...T...T..TT.........T",
                "T..TTTT......T.C.T......TTTT...T",
                "T.....T..bbb.T...T.bbb..T......T",
                "T.....T..bbb.....T.bbb..T...P..T",
                "T..C..TTTT.......TTTT...T......T",
                "T..............................T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            },
            Tier = 2, BattleBg = "Art/Backgrounds/mountain-battle", CameraBg = "#4a5a6a", Theme = "mountains",
            Encounters = new[]
            {
                E("doublit", 28, 7, -2, 2), E("pebblit", 24, 7, -1, 2),
                E("subunny", 16, 7, -1, 2), E("stackstone", 13, 10, 0, 3),
                E("differhare", 11, 10, 0, 3), E("duplirock", 8, 12, 1, 4),
            },
            BossSpeciesId = "duplirock", BossDisplayName = "Duplirock Elder", BossMinLevel = 12,
            BossLine = "Duplirock guards the portal!", GateClearLine = "The gate is cleared! Azure Sky City awaits!",
            GateCleared = p => p.ClearedGates.Contains("mountains"),
            ClearGate = p => { if (!p.ClearedGates.Contains("mountains")) p.ClearedGates.Add("mountains"); },
            PortalTargetMap = "sky", NextName = "Azure Sky City",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "mountains-chest-27-2", R(ChestRewardType.EvolutionStone, "Evolution Stone") },
                { "mountains-chest-3-3", R(ChestRewardType.HealthPotion, "Peak Potion", 2) },
                { "mountains-chest-17-5", R(ChestRewardType.DefenseCharm, "Granite Guard") },
                { "mountains-chest-15-10", R(ChestRewardType.GemSnack, "Gem Biscuit", 3) },
                { "mountains-chest-3-13", R(ChestRewardType.HealthPotion, "Warm Cocoa", 3) },
            },
        };

        public static MapDef Sky() => new MapDef
        {
            Id = "sky", DisplayName = "Azure Sky City", WelcomeLine = "Welcome to Azure Sky City!",
            Rows = new[]
            {
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                "T.S..bbb....T......bbb....T....T",
                "T....bbb....T..C...bbb....T....T",
                "T...........T.............T....T",
                "T..TTTT.........TTTTT..........T",
                "T..T....bbb..C..T...T....bbb...T",
                "T..T....bbb.....T...T....bbb...T",
                "T..T............T...T..........T",
                "T..TTTT..TTTT...T...TTTT.......T",
                "T........T..T...T.........C....T",
                "T..bbb...T..T...TTTT..bbb......T",
                "T..bbb...T..T.........bbb......T",
                "T........T..TTTTTT...........P.T",
                "T....C...T........T............T",
                "T..............................T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            },
            Tier = 3,
            BattleBg = "generated/NUMERIA_Unity_Battle_Assets/Backgrounds/Azure_Sky_City_2048x1152",
            CameraBg = "#8ed8f5", Theme = "sky",
            Encounters = new[]
            {
                E("mirrowl", 25, 14, -2, 2), E("prismouse", 24, 14, -1, 2),
                E("seqkit", 24, 14, -1, 2), E("polygoncat", 10, 18, 0, 3),
                E("patternlynx", 10, 18, 0, 3), E("symmetrix", 7, 20, 1, 4),
            },
            BossSpeciesId = "symmetrix", BossMinLevel = 20,
            BossLine = "Symmetrix guards the sky gate!", GateClearLine = "The sky gate shines! More adventures await!",
            GateCleared = p => p.ClearedGates.Contains("sky"),
            ClearGate = p => { if (!p.ClearedGates.Contains("sky")) p.ClearedGates.Add("sky"); },
            PortalTargetMap = null, NextName = "More Numeria adventures",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "sky-chest-15-2", R(ChestRewardType.EvolutionStone, "Evolution Stone") },
                { "sky-chest-13-5", R(ChestRewardType.GemSnack, "Cloud Candy", 3) },
                { "sky-chest-26-9", R(ChestRewardType.DefenseCharm, "Mirror Feather") },
                { "sky-chest-5-13", R(ChestRewardType.HealthPotion, "Sky Elixir", 3) },
            },
        };
    }
}
