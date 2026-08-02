using System;
using Numeria.Core;

namespace Numeria.Game
{
    /// <summary>一张地图的全部配置:布局、难度层、敌人、门与美术。</summary>
    public class MapDef
    {
        public string Id;
        public string DisplayName;
        public string WelcomeLine;
        public string[] Rows;
        public int Tier;
        public string BattleBg;
        public string CameraBg;
        public Func<CombatantDef> Wild;
        public Func<CombatantDef> Boss;
        public string BossLine;
        public string GateClearLine;
        public Func<Progress, bool> GateCleared;
        public Action<Progress> ClearGate;
        public string PortalTargetMap; // null = 下一区域未实装,显示预告横幅
        public string NextName;
        public string EvoChestId;      // 此图哪个宝箱掉进化石(null 无)
        public System.Collections.Generic.Dictionary<string, string> ChestItems; // 宝箱 id → 道具名
    }

    public static class Maps
    {
        public static MapDef Get(string id) => id == "mountains" ? Mountains() : Forest();

        public static MapDef Forest() => new MapDef
        {
            Id = "forest",
            DisplayName = "Mystic Forest",
            WelcomeLine = "Welcome to Mystic Forest!",
            Rows = new[]
            {
                "TTTTTTTTTTTTTTTTTTTT",
                "T....bb....T...bb..T",
                "T.S..bb........bb..T",
                "T..........T.......T",
                "T...T..bbb.....C...T",
                "T...T..bbb.........T",
                "T......bbb...TT....T",
                "T.C.........TTT..P.T",
                "T....bb............T",
                "T....bb....bbb.....T",
                "T..........bbb.....T",
                "TTTTTTTTTTTTTTTTTTTT",
            },
            Tier = 1,
            BattleBg = "generated/NUMERIA_Unity_Battle_Assets/Backgrounds/Sunny_Meadow_2048x1152",
            CameraBg = "#2f4f2f",
            Wild = GameData.Countipillar,
            Boss = GameData.Duplirock,
            BossLine = "Duplirock guards the portal!",
            GateClearLine = "The portal is open! A new world awaits!",
            GateCleared = p => p.BossBeaten,
            ClearGate = p => p.BossBeaten = true,
            PortalTargetMap = "mountains",
            NextName = "Silent Peaks",
            EvoChestId = null,
            ChestItems = new System.Collections.Generic.Dictionary<string, string>
            {
                { "forest-chest-15-4", "Power Sword" },
                { "forest-chest-2-7", "Lucky Charm" },
            },
        };

        public static MapDef Mountains() => new MapDef
        {
            Id = "mountains",
            DisplayName = "Silent Peaks",
            WelcomeLine = "Welcome to Silent Peaks!",
            Rows = new[]
            {
                "TTTTTTTTTTTTTTTTTTTT",
                "T..bb.....T....bb..T",
                "T....bb.......C....T",
                "T....bb...T........T",
                "T.T.....bbb....T...T",
                "T.T..C..bbb........T",
                "T.......bbb..TT....T",
                "T..bb........TT..P.T",
                "T..bb..T...........T",
                "T......T..bbb......T",
                "TS........bbb......T",
                "TTTTTTTTTTTTTTTTTTTT",
            },
            Tier = 2,
            BattleBg = "Art/Backgrounds/mountain-battle",
            CameraBg = "#4a5a6a",
            Wild = GameData.Doublit,
            Boss = GameData.DuplirockElder,
            BossLine = "Duplirock guards the portal!",
            GateClearLine = "The gate is cleared! Azure Sky City awaits!",
            GateCleared = p => p.ClearedGates.Contains("mountains"),
            ClearGate = p => { if (!p.ClearedGates.Contains("mountains")) p.ClearedGates.Add("mountains"); },
            PortalTargetMap = null,
            NextName = "Azure Sky City",
            EvoChestId = "mountains-chest-14-2",
            ChestItems = new System.Collections.Generic.Dictionary<string, string>
            {
                { "mountains-chest-5-5", "Brave Ring" },
                { "mountains-chest-14-2", "Evolution Stone" },
            },
        };
    }
}
