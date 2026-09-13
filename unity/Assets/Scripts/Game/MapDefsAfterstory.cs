using System.Collections.Generic;
using Numeria.Core;

namespace Numeria.Game
{
    /// <summary>Optional afterstory; never adds to or invalidates the six-crystal ending.</summary>
    public static partial class Maps
    {
        public static MapDef AncientRuins() => Afterstory(false);
        public static MapDef VeilmoonLake() => Afterstory(true);

        private static MapDef Afterstory(bool lake)
        {
            string id = lake ? "veilmoon_lake" : "ancient_ruins";
            var def = new MapDef
            {
                Id = id, IsAfterstory = true, Tier = lake ? 8 : 7,
                DisplayName = lake ? "Veilmoon Lake" : "Ancient Ruins",
                WelcomeLine = lake ? "Welcome to Veilmoon Lake!" : "Welcome to the Ancient Ruins!",
                WildMaxLevel = lake ? 66 : 58,
                Theme = id, CameraBg = lake ? "#273450" : "#778d78",
                BattleBg = "generated/Backgrounds/Painted/" + (lake ? "Veilmoon_Lake" : "Ancient_Ruins"),
                Rows = lake ? new[]
                {
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                "T..............................T",
                "T....C.........................T",
                "T..=...~~~~~~~~~~~~~~~~~~~~.=..T",
                "T..=bb.~~~~~~~~~~~~~~~~~~~~.=C.T",
                "T..=bb.~~~~.bbb.......~~~~~.=..T",
                "T..=...~~~~.bbb..P.L..~~~~~.=..T",
                "T..=...~~~~...........~~~~~.=..T",
                "T..====BBBB===========BBBBB.=..T",
                "T..=...~~~~...........~~~~~.=..T",
                "T..=...~~~~..C.....bb.~~~~~.bb.T",
                "T..=bb.~~~~........bb.~~~~~.bb.T",
                "T..=bb.~~~~~~~~~B~~~~~~~~~~.=..T",
                "T.C=...~~~~~~~~~B~~~~~~~~~~.=..T",
                "T..=...~~~~~~~~~B~~~~~~~~~~.=..T",
                "T.S========================C=..T",
                "T..............................T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                } : new[]
                {
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                "T.............~~~......#.......T",
                "T.S.####..C...~~~.bbb..#...C...T",
                "T...####......~~~.bbb..#.bbbb..T",
                "T.............~~~......#.bbbb..T",
                "T.============BBB=====.#.......T",
                "T.............~~~...=..#....P..T",
                "T.............~~~...=..#...L...T",
                "T.............~~~...=..#.......T",
                "T..bbb........~~~..C=..#.......T",
                "T..bbb........~~~...=..#.......T",
                "T.............~~~...=..=.......T",
                "T.......bbb...~~~...=========..T",
                "T.......bbb...BBB......#.......T",
                "T..C..........~~~......#..bbb..T",
                "T.............~~~......#..bbC..T",
                "T.............~~~......#.......T",
                "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
                },
                Encounters = lake ? new[]
                {
                    E("gliminnow", 25, 58, 61), E("luminfin", 15, 61, 64), E("lanternray", 3, 64, 66),
                    E("wisplet", 25, 58, 61), E("veilkit", 15, 61, 64), E("moonveil", 3, 64, 66),
                    E("lilypadlet", 25, 58, 61), E("bloomboat", 15, 61, 64), E("lotustide", 3, 64, 66),
                } : new[]
                {
                    E("tiletot", 25, 50, 53), E("mosaicub", 15, 53, 56), E("tessellion", 3, 56, 58),
                    E("abacub", 25, 50, 53), E("beadwarden", 15, 53, 56), E("abacoracle", 3, 56, 58),
                    E("coglet", 25, 50, 53), E("gearling", 15, 53, 56), E("clockondor", 3, 56, 58),
                },
                BossSpeciesId = lake ? "moonveil" : "abacoracle",
                BossDisplayName = lake ? "Lady Moonveil" : "Grand Abacoracle",
                BossMinLevel = lake ? 66 : 58,
                BossLine = lake ? "Lady Moonveil dances across the moonlit lake!" : "Grand Abacoracle awakens in the observatory!",
                GateClearLine = lake ? "The Moonlight Badge is yours. Numeria has many more stories to share!" :
                    "The Mosaic Badge reveals a path to Veilmoon Lake!",
                CrystalName = lake ? "Moonlight Badge" : "Mosaic Badge",
                GuardianName = lake ? "Lady Moonveil" : "Grand Abacoracle",
                GuardianSpriteResource = "generated/" + (lake ? "moonveil" : "abacoracle") + "_large_icon",
                GuardianChallengeLines = lake ? new[] {
                    "The lanterns are awake and the mirrored path is whole.",
                    "Show me the gentle magic of moonlight and numbers."
                } : new[] {
                    "You have restored our mosaics, gears, and ancient balance.",
                    "Let us see how your math magic brings the observatory to life."
                },
                GuardianVictoryLine = lake ? "Keep your Moonlight Badge. The lake will always welcome you." :
                    "Keep your Mosaic Badge. A moonlit lake waits beyond these ruins.",
                GateCleared = p => p.ClearedGates.Contains(id),
                ClearGate = p => { if (!p.ClearedGates.Contains(id)) p.ClearedGates.Add(id); },
                PortalTargetMap = lake ? "ancient_ruins" : "veilmoon_lake",
                NextName = lake ? "Ancient Ruins" : "Veilmoon Lake",
                Discoveries = lake ? new[] {
                    D("lake-lanterns", "Lantern Number Bonds", 5, 8, 35),
                    D("lake-mirror", "Moon Mirror", 15, 8, 38),
                    D("lake-lotus", "Lotus Pattern", 28, 8, 40),
                    D("lake-memory", "Lakeside Balance", 4, 15, 30, ConsumableType.GemSnack, 1),
                } : new[] {
                    D("ruins-mosaic", "Mosaic Shapes", 10, 5, 30),
                    D("ruins-gears", "Gear Pattern", 20, 5, 33),
                    D("ruins-balance", "Ancient Balance", 27, 12, 35),
                    D("ruins-memory", "Courtyard Difference", 6, 14, 28, ConsumableType.HealthPotion, 1),
                },
                Merchant = new MerchantDef
                {
                    Id = id + "-keeper", Name = lake ? "Lotus Keeper" : "Mosaic Keeper",
                    X = lake ? 3 : 10, Y = lake ? 14 : 15,
                    SpriteResource = "generated/" + (lake ? "bloomboat" : "mosaicub") + "_large_icon",
                    PartnerSpeciesId = lake ? "bloomboat" : "mosaicub", MinimumLevel = lake ? 62 : 54,
                    ChallengeLine = lake ? "The Lotus Keeper has a friendly challenge and supplies for you!" :
                        "The Mosaic Keeper has a friendly challenge and supplies for you!",
                    Stock = new[] {
                        S(id + "-potion", "HP Potion", "Restore 40% HP in battle", ShopItemType.HealthPotion, 22, 5),
                        S(id + "-snack", "Gem Snack", "Restore 3 gems in battle", ShopItemType.GemSnack, 26, 5),
                        S(id + "-charm", lake ? "Moonlit Charm" : "Mosaic Charm", "Accessory: ATK +1, DEF +1", ShopItemType.Accessory, 70, 1, attack: 1, defense: 1),
                        S(id + "-stone", lake ? "Moonstone" : "Mosaic Stone", "Used for evolution trials", ShopItemType.EvolutionStone, 112, 4),
                    },
                },
            };
            def.ChestRewards = new Dictionary<string, ChestRewardDef>();
            var puzzles = lake ? new[] { MapPuzzleKind.MakeTen, MapPuzzleKind.Symmetry, MapPuzzleKind.Pattern, MapPuzzleKind.Balance } :
                new[] { MapPuzzleKind.Shape, MapPuzzleKind.Pattern, MapPuzzleKind.Balance, MapPuzzleKind.Formula };
            for (int d = 0; d < puzzles.Length; d++) def.Discoveries[d].Puzzle = puzzles[d];
            int i = 0;
            foreach (string chest in def.ChestIds())
                def.ChestRewards[chest] = ++i == 1 ? R(ChestRewardType.EvolutionStone, lake ? "Moonstone" : "Mosaic Stone") :
                    R(i % 2 == 0 ? ChestRewardType.HealthPotion : ChestRewardType.GemSnack, i % 2 == 0 ? "HP Potion" : "Gem Snack", 3);
            return def;
        }

        public static bool CanVisit(string id, Progress p)
        {
            var maps = All();
            for (int i = 0; i < maps.Length; i++)
                if (maps[i].Id == id) return i == 0 || maps[i - 1].GateCleared(p);
            return false;
        }
    }
}
