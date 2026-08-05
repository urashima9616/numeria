using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>
    /// Tiny Swords 运行时选图器。64px 原始精灵保持 Point filtering；
    /// 3×3 地形块按四向邻接自动选择边、角、窄条和孤岛切片。
    /// </summary>
    public static class MapArt
    {
        private static TinySwordsCatalog _catalog;
        private static TinySwordsCatalog Catalog =>
            _catalog != null ? _catalog : _catalog = Resources.Load<TinySwordsCatalog>("generated/TinySwordsMapCatalog");

        public static bool Ready => Catalog != null && Catalog.Terrain1 != null && Catalog.Terrain1.Length >= 44;

        public static Sprite Terrain(string theme, Tile tile, int variant, int neighbors)
        {
            if (!Ready) return null;
            if (tile == Tile.Water || tile == Tile.Bridge) return Catalog.Water;

            Sprite[] set = TerrainSet(theme, tile);
            bool elevated = tile == Tile.Cliff;
            int index = AutotileIndex(neighbors, elevated);
            return index >= 0 && index < set.Length ? set[index] : set[9];
        }

        public static Sprite Prop(string theme, string kind, int variant)
        {
            if (!Ready) return null;
            Sprite[] choices;
            switch (kind)
            {
                case "bridge": return Catalog.Bridge;
                case "portal-glow": return Catalog.WaterFoam;
                case "treasure-opened": return Catalog.TreasureOpened;
                case "encounter": choices = Catalog.Bushes; break;
                case "landmark": return ThemeChoice(Catalog.Landmarks, theme);
                case "portal": return ThemeChoice(Catalog.Portals, theme);
                case "treasure": choices = Catalog.Treasures; break;
                default:
                    choices = theme == "forest" ? Catalog.Trees :
                        theme == "sky" ? Catalog.Clouds : Catalog.Rocks;
                    break;
            }
            if (choices == null || choices.Length == 0) return null;
            return choices[Mathf.Abs(variant) % choices.Length];
        }

        public static Color Tint(string theme, Tile tile, string kind = null)
        {
            if (kind == "bridge" || kind == "landmark" || kind == "portal" ||
                kind == "portal-glow" || kind == "treasure" || kind == "treasure-opened")
                return Color.white;
            if (theme == "desert")
            {
                if (tile == Tile.Water) return Hex("#65d2c5");
                if (kind == "encounter") return Hex("#b8ce62");
                if (kind == "obstacle") return Hex("#d59a62");
                return tile == Tile.Path ? Hex("#d48b5c") : Hex("#edc27d");
            }
            if (theme == "mountains")
                return tile == Tile.Water ? Hex("#7daec0") : Hex("#c6d1cb");
            if (theme == "sky")
                return tile == Tile.Water ? Hex("#8ce2ef") : Hex("#d6f2ed");
            return tile == Tile.Water ? Hex("#70d7cc") : Color.white;
        }

        public static float PropHeight(string theme, string kind)
        {
            if (kind == "landmark") return theme == "sky" ? 2.8f : 2.5f;
            if (kind == "portal") return 2.15f;
            if (kind == "portal-glow") return 1.15f;
            if (kind == "treasure") return .9f;
            if (kind == "treasure-opened") return .42f;
            if (kind == "bridge") return 1f;
            if (kind == "encounter") return .9f;
            if (theme == "forest") return 1.8f;
            if (theme == "sky") return .9f;
            return .8f;
        }

        private static Sprite[] TerrainSet(string theme, Tile tile)
        {
            if (tile == Tile.Path)
            {
                switch (theme)
                {
                    case "mountains": return Catalog.Terrain2;
                    case "sky": return Catalog.Terrain1;
                    case "desert": return Catalog.Terrain2;
                    default: return Catalog.Terrain1;
                }
            }
            if (tile == Tile.Cliff)
            {
                if (theme == "desert") return Catalog.Terrain4;
                if (theme == "forest") return Catalog.Terrain3;
                return Catalog.Terrain5;
            }
            switch (theme)
            {
                case "mountains": return Catalog.Terrain4;
                case "sky": return Catalog.Terrain5;
                case "desert": return Catalog.Terrain4;
                default: return Catalog.Terrain3;
            }
        }

        private static Sprite ThemeChoice(Sprite[] choices, string theme)
        {
            if (choices == null || choices.Length == 0) return null;
            int index = theme == "mountains" ? 1 : theme == "sky" ? 2 : theme == "desert" ? 3 : 0;
            return choices[Mathf.Min(index, choices.Length - 1)];
        }

        // Tiny Swords 切片布局：flat 0..3/8..11/16..19，elevated 每行向右偏移 4。
        private static int AutotileIndex(int connected, bool elevated)
        {
            bool north = (connected & 1) != 0;
            bool east = (connected & 2) != 0;
            bool south = (connected & 4) != 0;
            bool west = (connected & 8) != 0;
            int row;
            if (!north && !south) row = 3;
            else if (!north) row = 0;
            else if (!south) row = 2;
            else row = 1;

            int col;
            if (!west && !east) col = 3;
            else if (!west) col = 0;
            else if (!east) col = 2;
            else col = 1;

            int flat = row == 0 ? col : row == 1 ? 8 + col : row == 2 ? 16 + col : 24 + col;
            return elevated ? flat + 4 : flat;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
