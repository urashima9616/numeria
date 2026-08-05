using System;
using System.Linq;
using Numeria.Game;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>把 Assets/Tiny Swords 的 Sprite 引用写入 Resources catalog，供运行时安全加载。</summary>
    public static class TinySwordsCatalogBuilder
    {
        private const string Root = "Assets/Tiny Swords/";
        private const string CatalogPath = "Assets/Resources/generated/TinySwordsMapCatalog.asset";

        [MenuItem("Numeria/Rebuild Tiny Swords Catalog")]
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
            };

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"TINY_SWORDS_CATALOG_READY terrain={catalog.Terrain1.Length} path={CatalogPath}");
        }

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

        private static int Suffix(string name)
        {
            int split = name.LastIndexOf('_');
            return split >= 0 && int.TryParse(name.Substring(split + 1), out int value) ? value : 0;
        }
    }
}
