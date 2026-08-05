using System.Collections.Generic;
using Numeria.Core;

namespace Numeria.Game
{
    /// <summary>第五、六章：沙漠之下的矿脉与通向 Numeria 核心的古老隧道。</summary>
    public static partial class Maps
    {
        public static MapDef DarkMines() => new MapDef
        {
            Id = "dark_mines", DisplayName = "Dark Mines", WelcomeLine = "Welcome to the Dark Mines!",
            Rows = new[]
            {
                Row("########", "########", "########", "########"),
                Row("#S======", "==..####", "......C.", ".......#"),
                Row("#.bbb...", ".=..####", "..bbb...", ".###...#"),
                Row("#.bbb###", ".=======", "==bbb...", ".###...#"),
                Row("#....###", "....#...", ".=......", ".......#"),
                Row("#..C....", "....#..=", "===..L..", ".......#"),
                Row("#~~~~~~~", "~~~B~~~~", "~~~B~~~~", "~~~~~~~#"),
                Row("#.......", "...=....", "...=....", ".......#"),
                Row("#..####.", "...=bbb.", "...=###.", "...C...#"),
                Row("#..#....", "...=bbb.", "...=###.", ".......#"),
                Row("#..#..C.", "...=====", "====###.", ".......#"),
                Row("#..####.", "........", "...=....", ".#####.#"),
                Row("#.......", "..bbb...", "...=....", ".#####.#"),
                Row("#.#####.", "..bbb...", "...=====", "=====..#"),
                Row("#.......", "........", "......bb", "...=...#"),
                Row("#..C..##", "####....", "......bb", "...=...#"),
                Row("#.......", "........", "........", "...==P.#"),
                Row("########", "########", "########", "########"),
            },
            Tier = 5,
            BattleBg = "generated/Backgrounds/Dark_Mines_2048x1152",
            CameraBg = "#171c2d", Theme = "dark_mines",
            Encounters = new[]
            {
                E("ohmlet", 16, 34, -2, 2), E("currabbit", 8, 38, -1, 3),
                E("sparkseed", 16, 34, -2, 2), E("coilvine", 8, 38, -1, 3),
                E("charguppy", 16, 35, -2, 2), E("ampfin", 8, 39, -1, 3),
                E("circuitick", 16, 35, -2, 2), E("relayhawk", 8, 39, -1, 3),
                E("numite", 16, 35, -2, 2), E("factorock", 8, 39, -1, 3),
                E("gemlet", 16, 35, -2, 2), E("prismine", 8, 39, -1, 3),
                E("shaleling", 16, 36, -2, 2), E("layerock", 8, 40, -1, 3),
                E("cragcub", 16, 36, -2, 2), E("boulderbear", 8, 40, -1, 3),
            },
            BossSpeciesId = "voltamper", BossDisplayName = "Master Voltamper", BossMinLevel = 42,
            BossLine = "Master Voltamper guards the mine crystal!",
            GateClearLine = "The Mine Digit Crystal lights a path into the Underground Tunnels!",
            CrystalName = "Mine Digit Crystal", GuardianName = "Engineer Vesper",
            GuardianSpriteResource = "generated/Story/guardian_vesper",
            GuardianChallengeLines = new[]
            {
                "Every crystal circuit in this mine follows a number pattern.",
                "Trace the current, steady the stones, and meet Master Voltamper bravely.",
            },
            GuardianVictoryLine = "The Mine Digit Crystal is yours. One last light waits in Numeria's depths.",
            GateCleared = p => p.ClearedGates.Contains("dark_mines"),
            ClearGate = p => { if (!p.ClearedGates.Contains("dark_mines")) p.ClearedGates.Add("dark_mines"); },
            PortalTargetMap = "underground", NextName = "Underground Tunnels",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "dark_mines-chest-22-1", R(ChestRewardType.EvolutionStone, "Voltstone") },
                { "dark_mines-chest-3-5", R(ChestRewardType.DefenseCharm, "Obsidian Guard", 2) },
                { "dark_mines-chest-27-8", R(ChestRewardType.GemSnack, "Spark Candy", 5) },
                { "dark_mines-chest-6-10", R(ChestRewardType.AttackCharm, "Charged Pick", 2) },
                { "dark_mines-chest-3-15", R(ChestRewardType.HealthPotion, "Miner Tonic", 5) },
            },
            Discoveries = new[]
            {
                D("dark-mine-rune-1", "Circuit Sum Rune", 5, 2, 22),
                D("dark-mine-rune-2", "Crystal Pattern Rune", 18, 4, 24, ConsumableType.GemSnack, 1),
                D("dark-mine-rune-3", "Twin Ore Rune", 9, 12, 25),
                D("dark-mine-rune-4", "Rail Balance Rune", 25, 14, 27, ConsumableType.HealthPotion, 1),
            },
            Merchant = new MerchantDef
            {
                Id = "dark-mines-mara", Name = "Mara", X = 25, Y = 15,
                SpriteResource = "generated/Economy/merchant_mara", PartnerSpeciesId = "factorock", MinimumLevel = 40,
                ChallengeLine = "Mara taps her helmet. Solve my Factorock's challenge and the supply cart opens!",
                Stock = new[]
                {
                    S("mine-potion", "Miner Tonic", "Restore 40% HP in battle", ShopItemType.HealthPotion, 18, 5),
                    S("mine-gem-snack", "Spark Candy", "Restore 3 gems in battle", ShopItemType.GemSnack, 22, 5),
                    S("mine-obsidian", "Obsidian Guard", "Accessory: DEF +2", ShopItemType.Accessory, 54, 1, defense: 2),
                    S("mine-evo-stone", "Voltstone", "Used for evolution trials", ShopItemType.EvolutionStone, 96, 2),
                },
            },
        };

        public static MapDef UndergroundTunnels() => new MapDef
        {
            Id = "underground", DisplayName = "Underground Tunnels",
            WelcomeLine = "Welcome to the Underground Tunnels!",
            Rows = new[]
            {
                Row("########", "########", "########", "########"),
                Row("#S======", "==....~~", "~~..C...", ".......#"),
                Row("#..####.", "..bbb.~~", "~~..bbb.", ".#####.#"),
                Row("#..#....", "..bbb.~~", "~~..bbb.", ".......#"),
                Row("#..#....", "......~~", "~~......", "...###.#"),
                Row("#..####.", "..====BB", "BB====..", "...###.#"),
                Row("#.......", "..=...~~", "~~...=..", ".......#"),
                Row("#.#####.", "..=bbb~~", "~~bbb=..", ".#####.#"),
                Row("#C......", "..=bbb~~", "~~bbb=..", "......C#"),
                Row("#.#####.", "..====BB", "BB====..", ".#####.#"),
                Row("#.......", "......~~", "~~......", ".......#"),
                Row("#..###..", ".C....~~", "~~....L.", "..###..#"),
                Row("#..###..", "..bbb.~~", "~~..bbb.", "..###..#"),
                Row("#.......", "..====BB", "BB====..", ".......#"),
                Row("#.#####.", "..=...~~", "~~...=..", ".#####.#"),
                Row("#..C....", "..=...~~", "~~...===", "====...#"),
                Row("#.......", "..=====B", "B=======", "===P...#"),
                Row("########", "########", "########", "########"),
            },
            Tier = 6,
            BattleBg = "generated/Backgrounds/Underground_Tunnels_2048x1152",
            CameraBg = "#241733", Theme = "underground",
            Encounters = new[]
            {
                E("draddit", 16, 41, -2, 2), E("sumwyrm", 8, 45, -1, 3),
                E("scalip", 16, 41, -2, 2), E("patternake", 8, 45, -1, 3),
                E("digiling", 16, 42, -2, 2), E("tenswyrm", 8, 46, -1, 3),
                E("runelet", 16, 42, -2, 2), E("mirrorwyrm", 8, 46, -1, 3),
                E("embernum", 16, 41, -2, 2), E("plusprite", 8, 45, -1, 3),
                E("cindercub", 16, 42, -2, 2), E("douburn", 8, 46, -1, 3),
                E("torchick", 16, 42, -2, 2), E("patternix", 8, 46, -1, 3),
                E("glowgecko", 16, 43, -2, 2), E("balablaze", 8, 47, -1, 3),
            },
            BossSpeciesId = "calcularagon", BossDisplayName = "Ancient Calcularagon", BossMinLevel = 50,
            BossLine = "Ancient Calcularagon guards Numeria's deepest crystal!",
            GateClearLine = "All six Digit Crystals shine. The gate home is awake!",
            CrystalName = "Core Digit Crystal", GuardianName = "Keeper Echo",
            GuardianSpriteResource = "generated/Story/guardian_echo",
            GuardianChallengeLines = new[]
            {
                "These tunnels carry every number song back to Numeria's heart.",
                "Follow the mirrored paths and show Ancient Calcularagon all you have learned.",
            },
            GuardianVictoryLine = "The Core Digit Crystal is yours. All six lights now sing together.",
            GateCleared = p => p.ClearedGates.Contains("underground"),
            ClearGate = p => { if (!p.ClearedGates.Contains("underground")) p.ClearedGates.Add("underground"); },
            PortalTargetMap = null, NextName = "The gate home",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "underground-chest-20-1", R(ChestRewardType.EvolutionStone, "Corestone") },
                { "underground-chest-1-8", R(ChestRewardType.HealthPotion, "Glowcap Tonic", 5) },
                { "underground-chest-30-8", R(ChestRewardType.GemSnack, "Echo Candy", 5) },
                { "underground-chest-9-11", R(ChestRewardType.DefenseCharm, "Echo Mantle", 2) },
                { "underground-chest-3-15", R(ChestRewardType.AttackCharm, "Dragon Ember", 2) },
            },
            Discoveries = new[]
            {
                D("underground-rune-1", "Echo Sequence Rune", 5, 3, 28),
                D("underground-rune-2", "Mirror Tunnel Rune", 22, 6, 30, ConsumableType.GemSnack, 1),
                D("underground-rune-3", "Magma Double Rune", 6, 12, 32),
                D("underground-rune-4", "Core Equality Rune", 24, 15, 35, ConsumableType.HealthPotion, 1),
            },
            Merchant = new MerchantDef
            {
                Id = "underground-rune", Name = "Rune", X = 24, Y = 15,
                SpriteResource = "generated/Economy/merchant_rune", PartnerSpeciesId = "balablaze", MinimumLevel = 47,
                ChallengeLine = "Rune raises a lantern. Match Balablaze's rhythm and my deep-cache shop opens!",
                Stock = new[]
                {
                    S("tunnel-potion", "Glowcap Tonic", "Restore 40% HP in battle", ShopItemType.HealthPotion, 22, 5),
                    S("tunnel-gem-snack", "Echo Candy", "Restore 3 gems in battle", ShopItemType.GemSnack, 26, 5),
                    S("tunnel-core-charm", "Core Charm", "Accessory: ATK +1, DEF +1", ShopItemType.Accessory, 64, 1, attack: 1, defense: 1),
                    S("tunnel-evo-stone", "Corestone", "Used for evolution trials", ShopItemType.EvolutionStone, 112, 2),
                },
            },
        };
    }
}
