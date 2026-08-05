using System;
using System.Linq;
using Numeria.Game;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>把本地地图素材引用写入 Resources catalog，供运行时安全加载。</summary>
    public static class TinySwordsCatalogBuilder
    {
        private const string Root = "Assets/Tiny Swords/";
        private const string CaveRoot = "Assets/RPGW_Caves/Sliced/";
        private const string PaintedRoot = "Assets/Terrain Tile Hex Samples/Tile Samples/";
        private const string CatalogPath = "Assets/Resources/generated/TinySwordsMapCatalog.asset";

        public static void Rebuild()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TinySwordsCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TinySwordsCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.Terrain1 = Sheet("Terrain/Tileset/Tilemap_color1.png");
            catalog.Terrain2 = Sheet("Terrain/Tileset/Tilemap_color2.png");
            catalog.Terrain3 = Sheet("Terrain/Tileset/Tilemap_color3.png");
            catalog.Terrain4 = Sheet("Terrain/Tileset/Tilemap_color4.png");
            catalog.Terrain5 = Sheet("Terrain/Tileset/Tilemap_color5.png");
            catalog.Water = Single("Terrain/Tileset/Water Background color.png");
            catalog.WaterFoam = First("Terrain/Tileset/Water Foam.png");
            catalog.Shadow = Single("Terrain/Tileset/Shadow.png");
            catalog.Bridge = Single("UI Elements/Wood Table/WoodTable.png");
            catalog.Trees = new[]
            {
                First("Pawn and Resources/Wood/Trees/Tree1.png"),
                First("Pawn and Resources/Wood/Trees/Tree2.png"),
                First("Pawn and Resources/Wood/Trees/Tree3.png"),
                First("Pawn and Resources/Wood/Trees/Tree4.png"),
            };
            catalog.Bushes = new[]
            {
                First("Terrain/Decorations/Bushes/Bush 1.png"),
                First("Terrain/Decorations/Bushes/Bush 2.png"),
                First("Terrain/Decorations/Bushes/Bush 3.png"),
                First("Terrain/Decorations/Bushes/Bush 4.png"),
            };
            catalog.Rocks = new[]
            {
                Single("Terrain/Decorations/Rocks/Rock1.png"),
                Single("Terrain/Decorations/Rocks/Rock2.png"),
                Single("Terrain/Decorations/Rocks/Rock3.png"),
                Single("Terrain/Decorations/Rocks/Rock4.png"),
            };
            catalog.Clouds = new[]
            {
                Single("Terrain/Decorations/Clouds/Clouds_01.png"),
                Single("Terrain/Decorations/Clouds/Clouds_02.png"),
                Single("Terrain/Decorations/Clouds/Clouds_03.png"),
                Single("Terrain/Decorations/Clouds/Clouds_04.png"),
            };
            catalog.Landmarks = new[]
            {
                Single("Buildings/Blue Buildings/House1.png"),
                Single("Buildings/Black Buildings/Tower.png"),
                Single("Buildings/Yellow Buildings/Monastery.png"),
                Single("Buildings/Red Buildings/Castle.png"),
                Single("Buildings/Black Buildings/Monastery.png"),
                Single("Buildings/Purple Buildings/Tower.png"),
            };
            catalog.Treasures = Enumerable.Range(1, 6)
                .Select(index => Single($"Pawn and Resources/Gold/Gold Stones/Gold Stone {index}.png"))
                .ToArray();
            catalog.TreasureOpened = Single("Pawn and Resources/Gold/Gold Resource/Gold_Resource.png");
            catalog.Portals = new[]
            {
                Single("Buildings/Blue Buildings/Monastery.png"),
                Single("Buildings/Black Buildings/Castle.png"),
                Single("Buildings/Yellow Buildings/Tower.png"),
                Single("Buildings/Red Buildings/Monastery.png"),
                Single("Buildings/Black Buildings/Castle.png"),
                Single("Buildings/Purple Buildings/Castle.png"),
            };

            catalog.CaveFloorDark = CaveCells("MainLev2.0.png",
                new Vector2Int(27, 27), new Vector2Int(29, 27), new Vector2Int(31, 27),
                new Vector2Int(33, 27), new Vector2Int(27, 28), new Vector2Int(31, 28));
            catalog.CaveFloorPurple = CaveCells("MainLev2.0.png",
                new Vector2Int(1, 27), new Vector2Int(3, 27), new Vector2Int(5, 27),
                new Vector2Int(7, 27), new Vector2Int(1, 28), new Vector2Int(5, 28));
            catalog.CavePaths = CaveCells("MainLev2.0.png",
                new Vector2Int(26, 32), new Vector2Int(28, 32), new Vector2Int(30, 32),
                new Vector2Int(32, 32), new Vector2Int(34, 32), new Vector2Int(38, 32));
            catalog.CaveWalls = CaveCells("MainLev2.0.png",
                new Vector2Int(29, 1), new Vector2Int(30, 1), new Vector2Int(31, 1),
                new Vector2Int(32, 1), new Vector2Int(29, 2), new Vector2Int(32, 2));
            catalog.CaveVoids = CaveCells("MainLev2.0.png",
                new Vector2Int(46, 1), new Vector2Int(47, 1), new Vector2Int(48, 1),
                new Vector2Int(49, 1), new Vector2Int(46, 2), new Vector2Int(49, 2));
            catalog.CaveCrystals = CaveCells("decorative.png",
                new Vector2Int(0, 18), new Vector2Int(1, 18), new Vector2Int(2, 18),
                new Vector2Int(3, 18), new Vector2Int(0, 19), new Vector2Int(1, 19),
                new Vector2Int(2, 19), new Vector2Int(3, 19), new Vector2Int(1, 21));
            catalog.CaveRocks = CaveCells("decorative.png",
                new Vector2Int(0, 23), new Vector2Int(1, 23), new Vector2Int(3, 23),
                new Vector2Int(0, 24), new Vector2Int(1, 24), new Vector2Int(3, 24),
                new Vector2Int(0, 26), new Vector2Int(2, 26));
            catalog.CaveBridge = CaveAt("MainLev2.0.png", 46, 32);
            catalog.CaveLandmark = CaveAt("decorative.png", 2, 19);
            catalog.CavePortal = CaveAt("decorative.png", 1, 21);
            catalog.CaveGlow = CaveAt("decorative.png", 0, 18);

            catalog.PaintedBase = Painted("base00.png");
            catalog.PaintedBelowDirt = Painted("below_dirt00.png");
            catalog.PaintedBelowWater = Painted("below_water00.png");
            catalog.PaintedDesert = Painted("desertYellowCactiForest00.png");
            catalog.PaintedForest = Painted("forestBroadleaf00.png");
            catalog.PaintedSnowForest = Painted("forestPineSnowCovered00.png");
            catalog.PaintedJungle = Painted("jungle00.png");
            catalog.PaintedMountain = Painted("mountain00.png");
            catalog.PaintedOcean = Painted("oceanCalm00.png");
            catalog.PaintedPlains = Painted("plains00.png");
            catalog.PaintedCastle = Painted("plains_castle00.png");
            catalog.PaintedVolcano = Painted("volcanoActive00.png");

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            int tinyCount = catalog.Terrain1?.Length ?? 0;
            int caveCount = catalog.CaveFloorDark?.Length ?? 0;
            Debug.Log($"MAP_CATALOG_READY painted={(catalog.PaintedPlains != null ? 12 : 0)} " +
                      $"tiny={tinyCount} cave={caveCount} path={CatalogPath}");
        }

        [MenuItem("Numeria/Rebuild Map Asset Catalogs")]
        public static void RebuildAll() => Rebuild();

        public static void RebuildAndExit()
        {
            Rebuild();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static Sprite[] Sheet(string relative) =>
            AssetDatabase.LoadAllAssetsAtPath(Root + relative).OfType<Sprite>()
                .OrderBy(sprite => Suffix(sprite.name)).ToArray();

        private static Sprite First(string relative) => Sheet(relative).FirstOrDefault();

        private static Sprite Single(string relative) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + relative);

        private static Sprite Painted(string relative) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(PaintedRoot + relative);

        private static Sprite[] CaveCells(string relative, params Vector2Int[] cells) =>
            cells.Select(cell => CaveAt(relative, cell.x, cell.y)).Where(sprite => sprite != null).ToArray();

        private static Sprite CaveAt(string relative, int column, int rowFromTop)
        {
            string path = CaveRoot + relative;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return null;
            float x = column * 32f;
            float y = texture.height - (rowFromTop + 1) * 32f;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(sprite =>
                Mathf.Approximately(sprite.rect.x, x) && Mathf.Approximately(sprite.rect.y, y));
        }

        private static int Suffix(string name)
        {
            int split = name.LastIndexOf('_');
            return split >= 0 && int.TryParse(name.Substring(split + 1), out int value) ? value : 0;
        }
    }
}
