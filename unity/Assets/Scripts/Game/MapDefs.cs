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

    public sealed class DiscoveryDef
    {
        public string Id;
        public string Name;
        public int X;
        public int Y;
        public int Coins;
        public ConsumableType? BonusConsumable;
        public int BonusAmount;
    }

    public sealed class MerchantDef
    {
        public string Id;
        public string Name;
        public int X;
        public int Y;
        public string SpriteResource;
        public string PartnerSpeciesId;
        public int MinimumLevel;
        public string ChallengeLine;
        public ShopItemDef[] Stock;

        public CombatantDef Opponent(int playerLevel, int tier, Rng rng)
        {
            int level = Math.Max(MinimumLevel, playerLevel + 1);
            return GameData.CreateTrainerOpponent(PartnerSpeciesId, level, tier, rng);
        }
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
        public string CrystalName;
        public string GuardianName;
        public string GuardianSpriteResource;
        public string[] GuardianChallengeLines;
        public string GuardianVictoryLine;
        public Func<Progress, bool> GateCleared;
        public Action<Progress> ClearGate;
        public string PortalTargetMap;
        public string NextName;
        public Dictionary<string, ChestRewardDef> ChestRewards;
        public DiscoveryDef[] Discoveries;
        public MerchantDef Merchant;

        public DiscoveryDef DiscoveryAt(int x, int y) =>
            Array.Find(Discoveries ?? Array.Empty<DiscoveryDef>(), item => item.X == x && item.Y == y);

        public bool MerchantAt(int x, int y) => Merchant != null && Merchant.X == x && Merchant.Y == y;

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

        private static DiscoveryDef D(string id, string name, int x, int y, int coins,
            ConsumableType? bonus = null, int amount = 0) => new DiscoveryDef
        {
            Id = id, Name = name, X = x, Y = y, Coins = coins,
            BonusConsumable = bonus, BonusAmount = amount,
        };

        private static ShopItemDef S(string id, string name, string description, ShopItemType type,
            int price, int limit, int amount = 1, int attack = 0, int defense = 0) => new ShopItemDef
        {
            Id = id, Name = name, Description = description, Type = type, Price = price,
            StockLimit = limit, Amount = amount, AttackBonus = attack, DefenseBonus = defense,
        };

        public static MapDef Get(string id)
        {
            switch (id)
            {
                case "mountains": return Mountains();
                case "sky": return Sky();
                case "desert": return Desert();
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
                E("glimlet", 12, 2, 0, 2), E("charmite", 10, 2, 0, 2),
                E("budsum", 12, 2, 0, 2), E("clovercub", 10, 3, 0, 3),
                E("mossbit", 8, 3, 0, 3),
            },
            BossSpeciesId = "numberfly", BossMinLevel = 5,
            BossLine = "Numberfly guards the portal!", GateClearLine = "The portal is open! A new world awaits!",
            CrystalName = "Forest Digit Crystal", GuardianName = "Elder Rowan",
            GuardianSpriteResource = "generated/Story/guardian_rowan",
            GuardianChallengeLines = new[]
            {
                "Lucas, the Forest Crystal answers only to a kind and clever heart.",
                "Show Numberfly what you have learned. Mistakes are steps, not failures.",
            },
            GuardianVictoryLine = "You have earned the Forest Digit Crystal. Carry its light wisely.",
            GateCleared = p => p.BossBeaten, ClearGate = p => p.BossBeaten = true,
            PortalTargetMap = "mountains", NextName = "Silent Peaks",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "forest-chest-24-3", R(ChestRewardType.HealthPotion, "Berry Potion", 2) },
                { "forest-chest-23-5", R(ChestRewardType.GemSnack, "Crystal Cookie", 2) },
                { "forest-chest-6-6", R(ChestRewardType.AttackCharm, "Power Acorn") },
                { "forest-chest-17-9", R(ChestRewardType.HealthPotion, "Forest Tonic", 2) },
            },
            Discoveries = new[]
            {
                D("forest-rune-1", "Firefly Number Rune", 8, 3, 4),
                D("forest-rune-2", "Mushroom Pattern Rune", 15, 5, 5, ConsumableType.HealthPotion, 1),
                D("forest-rune-3", "Leaf Symmetry Rune", 25, 8, 5),
                D("forest-rune-4", "Acorn Counting Rune", 10, 13, 6, ConsumableType.GemSnack, 1),
            },
            Merchant = new MerchantDef
            {
                Id = "forest-tessa", Name = "Tessa", X = 26, Y = 13,
                SpriteResource = "generated/Economy/merchant_tessa", PartnerSpeciesId = "paircub", MinimumLevel = 4,
                ChallengeLine = "Tessa smiles. Beat my Paircub and my shop is yours to browse!",
                Stock = new[]
                {
                    S("forest-potion", "Berry Potion", "Restore 40% HP in battle", ShopItemType.HealthPotion, 6, 3),
                    S("forest-gem-snack", "Crystal Cookie", "Restore 3 gems in battle", ShopItemType.GemSnack, 8, 2),
                    S("forest-power-acorn", "Power Acorn", "Accessory: ATK +1", ShopItemType.Accessory, 16, 1, attack: 1),
                    S("forest-evo-stone", "Evolution Stone", "Used for evolution trials", ShopItemType.EvolutionStone, 32, 1),
                },
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
                E("addling", 12, 8, -1, 2), E("dracount", 10, 9, 0, 3),
                E("twinsting", 10, 9, 0, 3), E("shardrake", 8, 10, 0, 3),
                E("sprouturn", 10, 8, -1, 2),
            },
            BossSpeciesId = "duplirock", BossDisplayName = "Duplirock Elder", BossMinLevel = 12,
            BossLine = "Duplirock guards the portal!", GateClearLine = "The gate is cleared! Azure Sky City awaits!",
            CrystalName = "Peaks Digit Crystal", GuardianName = "Keeper Orin",
            GuardianSpriteResource = "generated/Story/guardian_orin",
            GuardianChallengeLines = new[]
            {
                "The mountain remembers every brave attempt.",
                "Match your strength with Duplirock Elder, and the Peaks Crystal will shine.",
            },
            GuardianVictoryLine = "The Peaks Digit Crystal is yours. Your courage gave it light.",
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
            Discoveries = new[]
            {
                D("mountains-rune-1", "Echo Addition Rune", 5, 2, 7),
                D("mountains-rune-2", "Twin Stone Rune", 12, 6, 8, ConsumableType.HealthPotion, 1),
                D("mountains-rune-3", "Peak Order Rune", 26, 9, 9),
                D("mountains-rune-4", "Crystal Difference Rune", 7, 14, 10, ConsumableType.GemSnack, 1),
            },
            Merchant = new MerchantDef
            {
                Id = "mountains-bram", Name = "Bram", X = 26, Y = 14,
                SpriteResource = "generated/Economy/merchant_bram", PartnerSpeciesId = "stackstone", MinimumLevel = 11,
                ChallengeLine = "Bram nods. Show my Stackstone your strongest math magic, then we can trade!",
                Stock = new[]
                {
                    S("mountains-potion", "Peak Potion", "Restore 40% HP in battle", ShopItemType.HealthPotion, 9, 4),
                    S("mountains-gem-snack", "Gem Biscuit", "Restore 3 gems in battle", ShopItemType.GemSnack, 11, 3),
                    S("mountains-granite-guard", "Granite Guard", "Accessory: DEF +1", ShopItemType.Accessory, 22, 1, defense: 1),
                    S("mountains-evo-stone", "Evolution Stone", "Used for evolution trials", ShopItemType.EvolutionStone, 45, 1),
                },
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
                E("voltlet", 10, 15, -1, 2), E("sparkit", 10, 15, -1, 2),
                E("chargecub", 9, 16, 0, 3), E("flickerfin", 9, 16, 0, 3),
                E("switchick", 8, 17, 0, 3), E("moonmote", 8, 15, -1, 2),
                E("wishwink", 8, 16, 0, 3), E("pixipip", 8, 16, 0, 3),
                E("seedseq", 8, 15, -1, 2),
            },
            BossSpeciesId = "symmetrix", BossMinLevel = 20,
            BossLine = "Symmetrix guards the sky gate!", GateClearLine = "The sky gate shines! Fever Desert awaits!",
            CrystalName = "Sky Digit Crystal", GuardianName = "Astronomer Lyra",
            GuardianSpriteResource = "generated/Story/guardian_lyra",
            GuardianChallengeLines = new[]
            {
                "Patterns guide every star in Numeria.",
                "Read Symmetrix's sky pattern, and the third crystal will be yours.",
            },
            GuardianVictoryLine = "The Sky Digit Crystal is yours. One last light burns beyond the clouds.",
            GateCleared = p => p.ClearedGates.Contains("sky"),
            ClearGate = p => { if (!p.ClearedGates.Contains("sky")) p.ClearedGates.Add("sky"); },
            PortalTargetMap = "desert", NextName = "Fever Desert",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "sky-chest-15-2", R(ChestRewardType.EvolutionStone, "Evolution Stone") },
                { "sky-chest-13-5", R(ChestRewardType.GemSnack, "Cloud Candy", 3) },
                { "sky-chest-26-9", R(ChestRewardType.DefenseCharm, "Mirror Feather") },
                { "sky-chest-5-13", R(ChestRewardType.HealthPotion, "Sky Elixir", 3) },
            },
            Discoveries = new[]
            {
                D("sky-rune-1", "Cloud Sequence Rune", 6, 3, 10),
                D("sky-rune-2", "Prism Rotation Rune", 14, 4, 11, ConsumableType.GemSnack, 1),
                D("sky-rune-3", "Mirror Wing Rune", 25, 11, 12),
                D("sky-rune-4", "Starlight Pattern Rune", 12, 14, 14, ConsumableType.HealthPotion, 1),
            },
            Merchant = new MerchantDef
            {
                Id = "sky-ari", Name = "Ari", X = 27, Y = 14,
                SpriteResource = "generated/Economy/merchant_ari", PartnerSpeciesId = "polygoncat", MinimumLevel = 19,
                ChallengeLine = "Ari opens a star map. Outsmart my Polygoncat and the sky market opens!",
                Stock = new[]
                {
                    S("sky-potion", "Sky Elixir", "Restore 40% HP in battle", ShopItemType.HealthPotion, 12, 4),
                    S("sky-gem-snack", "Cloud Candy", "Restore 3 gems in battle", ShopItemType.GemSnack, 15, 3),
                    S("sky-prism-charm", "Prism Charm", "Accessory: ATK +1, DEF +1", ShopItemType.Accessory, 30, 1, attack: 1, defense: 1),
                    S("sky-evo-stone", "Evolution Stone", "Used for evolution trials", ShopItemType.EvolutionStone, 65, 1),
                },
            },
        };

        public static MapDef Desert() => new MapDef
        {
            Id = "desert", DisplayName = "Fever Desert", WelcomeLine = "Welcome to Fever Desert!",
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
            Tier = 4,
            BattleBg = "generated/Backgrounds/Fever_Desert_2048x1152",
            CameraBg = "#d8aa5c", Theme = "desert",
            Encounters = new[]
            {
                E("twinkelle", 8, 24, -2, 2), E("lunafae", 8, 24, -2, 2),
                E("pairabelle", 8, 24, -2, 2), E("starwhisp", 8, 24, -2, 2),
                E("prismfae", 8, 24, -2, 2),
                E("sumscale", 8, 26, -1, 3), E("tallywyrm", 8, 26, -1, 3),
                E("spirake", 8, 26, -1, 3), E("doublescale", 8, 26, -1, 3),
                E("prismwyrm", 8, 26, -1, 3),
                E("sumvolt", 8, 24, -2, 2), E("patternzap", 8, 24, -2, 2),
                E("doublebolt", 8, 24, -2, 2), E("neonray", 8, 24, -2, 2),
                E("mirrorvolt", 8, 24, -2, 2),
                E("vineplus", 8, 24, -2, 2), E("fourleaf", 8, 24, -2, 2),
                E("spiralfern", 8, 24, -2, 2), E("doublmoss", 8, 24, -2, 2),
                E("patternpod", 8, 24, -2, 2),
            },
            BossSpeciesId = "totalisk", BossDisplayName = "Solar Totalisk", BossMinLevel = 32,
            BossLine = "Solar Totalisk guards the final crystal!",
            GateClearLine = "The desert crystal blazes! The gate home is awake!",
            CrystalName = "Desert Digit Crystal", GuardianName = "Sage Solara",
            GuardianSpriteResource = "generated/Story/guardian_solara",
            GuardianChallengeLines = new[]
            {
                "The desert sun hides patterns in every dune.",
                "Join the four kinds of magic, and show Solar Totalisk how brightly you can think.",
            },
            GuardianVictoryLine = "The Desert Digit Crystal is yours. All four lights now sing together.",
            GateCleared = p => p.ClearedGates.Contains("desert"),
            ClearGate = p => { if (!p.ClearedGates.Contains("desert")) p.ClearedGates.Add("desert"); },
            PortalTargetMap = null, NextName = "The gate home",
            ChestRewards = new Dictionary<string, ChestRewardDef>
            {
                { "desert-chest-27-2", R(ChestRewardType.EvolutionStone, "Sunstone") },
                { "desert-chest-3-3", R(ChestRewardType.HealthPotion, "Cactus Tonic", 4) },
                { "desert-chest-17-5", R(ChestRewardType.AttackCharm, "Solar Fang") },
                { "desert-chest-15-10", R(ChestRewardType.GemSnack, "Dune Candy", 4) },
                { "desert-chest-3-13", R(ChestRewardType.DefenseCharm, "Mirage Mantle") },
            },
            Discoveries = new[]
            {
                D("desert-rune-1", "Dune Addition Rune", 5, 2, 15),
                D("desert-rune-2", "Sun Rotation Rune", 12, 6, 16, ConsumableType.HealthPotion, 1),
                D("desert-rune-3", "Cactus Sequence Rune", 26, 9, 18),
                D("desert-rune-4", "Mirage Symmetry Rune", 7, 14, 20, ConsumableType.GemSnack, 1),
            },
            Merchant = new MerchantDef
            {
                Id = "desert-nia", Name = "Nia", X = 26, Y = 14,
                SpriteResource = "generated/Economy/merchant_nia", PartnerSpeciesId = "mirrorvolt", MinimumLevel = 29,
                ChallengeLine = "Nia raises her sun goggles. Match my Mirrorvolt and the oasis market is yours!",
                Stock = new[]
                {
                    S("desert-potion", "Cactus Tonic", "Restore 40% HP in battle", ShopItemType.HealthPotion, 15, 5),
                    S("desert-gem-snack", "Dune Candy", "Restore 3 gems in battle", ShopItemType.GemSnack, 18, 4),
                    S("desert-solar-fang", "Solar Fang", "Accessory: ATK +2", ShopItemType.Accessory, 42, 1, attack: 2),
                    S("desert-evo-stone", "Sunstone", "Used for evolution trials", ShopItemType.EvolutionStone, 82, 2),
                },
            },
        };
    }
}
