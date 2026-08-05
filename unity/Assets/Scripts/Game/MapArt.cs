using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>
    /// 六章地图统一使用 Tiles and Hexes: 2D Painted Terrain Samples。
    /// 旧 Tiny Swords / RPG Worlds Caves 引用只在 Painted 素材尚未导入时作为安全回退。
    /// </summary>
    public static class MapArt
    {
        private static TinySwordsCatalog _catalog;
        private static TinySwordsCatalog Catalog =>
            _catalog != null ? _catalog : _catalog = Resources.Load<TinySwordsCatalog>("generated/TinySwordsMapCatalog");

        public static bool Ready => Catalog != null && Catalog.Terrain1 != null && Catalog.Terrain1.Length >= 44;
        public static bool PaintedReady => Catalog != null && Catalog.PaintedBase != null &&
                                           Catalog.PaintedDesert != null && Catalog.PaintedForest != null &&
                                           Catalog.PaintedSnowForest != null && Catalog.PaintedJungle != null &&
                                           Catalog.PaintedMountain != null && Catalog.PaintedOcean != null &&
                                           Catalog.PaintedPlains != null && Catalog.PaintedCastle != null &&
                                           Catalog.PaintedVolcano != null;
        public static bool CaveReady => Catalog != null && Catalog.CaveFloorDark != null &&
                                        Catalog.CaveFloorDark.Length >= 4 && Catalog.CaveWalls != null &&
                                        Catalog.CaveWalls.Length >= 4;

        public static Sprite Terrain(string theme, Tile tile, int variant, int neighbors)
        {
            if (PaintedReady) return PaintedTerrain(theme, tile, variant);
            if (IsCave(theme) && CaveReady) return CaveTerrain(theme, tile, variant);
            if (!Ready) return null;
            if (tile == Tile.Water || tile == Tile.Bridge) return Catalog.Water;

            Sprite[] set = TerrainSet(theme, tile);
            bool elevated = tile == Tile.Cliff;
            int index = AutotileIndex(neighbors, elevated);
            return index >= 0 && index < set.Length ? set[index] : set[9];
        }

        public static Sprite Prop(string theme, string kind, int variant)
        {
            if (PaintedReady) return PaintedProp(theme, kind, variant);
            if (IsCave(theme) && CaveReady) return CaveProp(kind, variant);
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
            if (PaintedReady && kind == null) return Color.white;
            if (IsCave(theme)) return Color.white;
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
            if (theme == "forest")
                return tile == Tile.Water ? Hex("#5fabc1") : tile == Tile.Path ? Hex("#c9b78c") : Color.white;
            return tile == Tile.Water ? Hex("#70d7cc") : Color.white;
        }

        public static float PropHeight(string theme, string kind)
        {
            if (PaintedReady)
            {
                if (kind == "landmark") return 1.8f;
                if (kind == "portal") return 1.45f;
                if (kind == "portal-glow") return .85f;
                if (kind == "treasure" || kind == "treasure-opened") return .8f;
                if (kind == "encounter") return .72f;
                if (kind == "bridge") return 1f;
                return theme == "forest" ? 1.35f : .92f;
            }
            if (IsCave(theme))
            {
                if (kind == "landmark") return 1.9f;
                if (kind == "portal") return 1.8f;
                if (kind == "portal-glow") return .82f;
                if (kind == "treasure") return .8f;
                if (kind == "treasure-opened") return .55f;
                if (kind == "encounter") return .72f;
                return kind == "bridge" ? 1f : .88f;
            }
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

        private static bool IsCave(string theme) => theme == "dark_mines" || theme == "underground";

        private static Sprite CaveTerrain(string theme, Tile tile, int variant)
        {
            if (tile == Tile.Water) return Pick(Catalog.CaveVoids, variant);
            if (tile == Tile.Cliff) return Pick(Catalog.CaveWalls, variant);
            if (tile == Tile.Path || tile == Tile.Bridge) return Pick(Catalog.CavePaths, variant);
            return Pick(theme == "underground" ? Catalog.CaveFloorPurple : Catalog.CaveFloorDark, variant);
        }

        private static Sprite CaveProp(string kind, int variant)
        {
            switch (kind)
            {
                case "bridge": return Catalog.CaveBridge;
                case "portal-glow": return Catalog.CaveGlow;
                case "landmark": return Catalog.CaveLandmark;
                case "portal": return Catalog.CavePortal;
                case "encounter": return Pick(Catalog.CaveCrystals, variant);
                case "treasure": return SpriteLib.Cainos("TX Props", "TX Props Chest");
                case "treasure-opened": return SpriteLib.Cainos("TX Props", "TX Props Chest Opened");
                default: return Pick(Catalog.CaveRocks, variant);
            }
        }

        public static Color TerrainTint(string theme, Tile tile, Sprite sprite)
        {
            if (!PaintedReady || sprite == null) return Tint(theme, tile);
            switch (theme)
            {
                case "mountains":
                    if (sprite == Catalog.PaintedBase) return Hex("#8796a0");
                    return Color.white;
                case "sky":
                    if (sprite == Catalog.PaintedOcean) return Hex("#92d8ee");
                    if (sprite == Catalog.PaintedBase) return Hex("#9cc8d2");
                    return Hex("#edf7f5");
                case "desert":
                    if (sprite == Catalog.PaintedBase) return Hex("#c9935f");
                    if (sprite == Catalog.PaintedPlains) return Hex("#d4b46d");
                    return Color.white;
                case "dark_mines":
                    if (sprite == Catalog.PaintedOcean) return Hex("#273347");
                    if (sprite == Catalog.PaintedBase) return Hex("#555b6a");
                    if (sprite == Catalog.PaintedVolcano) return Hex("#725f76");
                    return Hex("#8b8593");
                case "underground":
                    if (sprite == Catalog.PaintedOcean) return Hex("#3b2852");
                    if (sprite == Catalog.PaintedBase) return Hex("#725d7a");
                    if (sprite == Catalog.PaintedVolcano) return Hex("#8d6578");
                    return Hex("#807087");
                default:
                    if (sprite == Catalog.PaintedBase) return Hex("#b8aa82");
                    return Color.white;
            }
        }

        private static Sprite PaintedTerrain(string theme, Tile tile, int variant)
        {
            if (tile == Tile.Water) return Catalog.PaintedOcean;
            if (tile == Tile.Landmark) return Catalog.PaintedCastle;
            if (tile == Tile.Portal)
                return IsCave(theme) ? Catalog.PaintedVolcano : Catalog.PaintedCastle;
            if (tile == Tile.Path || tile == Tile.Bridge)
                return theme == "desert" ? Catalog.PaintedPlains :
                    IsCave(theme) ? Catalog.PaintedBase : Catalog.PaintedPlains;

            switch (theme)
            {
                case "forest":
                    if (tile == Tile.Tree)
                        return variant % 5 == 0 ? Catalog.PaintedJungle : Catalog.PaintedForest;
                    if (tile == Tile.Bush)
                        return variant % 3 == 0 ? Catalog.PaintedForest : Catalog.PaintedJungle;
                    if (tile == Tile.Cliff) return Catalog.PaintedMountain;
                    return Catalog.PaintedPlains;
                case "mountains":
                    if (tile == Tile.Cliff) return Catalog.PaintedMountain;
                    if (tile == Tile.Tree) return Catalog.PaintedSnowForest;
                    if (tile == Tile.Bush && variant % 3 == 0) return Catalog.PaintedSnowForest;
                    return Catalog.PaintedPlains;
                case "sky":
                    if (tile == Tile.Cliff || tile == Tile.Tree) return Catalog.PaintedMountain;
                    return Catalog.PaintedPlains;
                case "desert":
                    if (tile == Tile.Cliff)
                        return variant % 5 == 0 ? Catalog.PaintedMountain : Catalog.PaintedDesert;
                    if (tile == Tile.Bush || tile == Tile.Tree) return Catalog.PaintedDesert;
                    return Catalog.PaintedPlains;
                case "dark_mines":
                    if (tile == Tile.Cliff || tile == Tile.Tree)
                        return variant % 4 == 0 ? Catalog.PaintedVolcano : Catalog.PaintedMountain;
                    return Catalog.PaintedBase;
                case "underground":
                    if (tile == Tile.Cliff || tile == Tile.Tree)
                        return variant % 4 == 0 ? Catalog.PaintedMountain : Catalog.PaintedVolcano;
                    return Catalog.PaintedBase;
                default:
                    return Catalog.PaintedPlains;
            }
        }

        private static Sprite PaintedProp(string theme, string kind, int variant)
        {
            switch (kind)
            {
                case "bridge":
                    return null;
                case "portal-glow":
                    return SpriteLib.Cainos("TX Props", $"TX Props Altar Rune {(variant % 4) + 1}");
                case "treasure":
                    return SpriteLib.Cainos("TX Props", "TX Props Chest");
                case "treasure-opened":
                    return SpriteLib.Cainos("TX Props", "TX Props Chest Opened");
                case "portal":
                    return null;
                case "landmark":
                    // Landmark / portal architecture is embedded in the Painted terrain tile.
                    return null;
                case "encounter":
                    if (theme == "forest")
                        return SpriteLib.Cainos("TX Plant", $"TX Bush T{(variant % 6) + 1}");
                    if (theme == "sky" || theme == "dark_mines" || theme == "underground")
                        return SpriteLib.Cainos("TX Props", $"TX Props Altar Rune {(variant % 4) + 1}");
                    return SpriteLib.Cainos("TX Plant", $"TX Plant - Grass {((variant % 15) + 1):00}");
                default:
                    // Forests, cliffs and mountains are already represented by full Painted tiles.
                    return null;
            }
        }

        private static Sprite Pick(Sprite[] sprites, int variant)
        {
            if (sprites == null || sprites.Length == 0) return null;
            return sprites[Mathf.Abs(variant) % sprites.Length];
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
                    case "dark_mines": return Catalog.Terrain2;
                    case "underground": return Catalog.Terrain2;
                    default: return Catalog.Terrain1;
                }
            }
            if (tile == Tile.Cliff)
            {
                if (theme == "desert") return Catalog.Terrain4;
                if (theme == "forest") return Catalog.Terrain3;
                if (theme == "dark_mines") return Catalog.Terrain5;
                if (theme == "underground") return Catalog.Terrain4;
                return Catalog.Terrain5;
            }
            switch (theme)
            {
                case "mountains": return Catalog.Terrain4;
                case "sky": return Catalog.Terrain5;
                case "desert": return Catalog.Terrain4;
                case "dark_mines": return Catalog.Terrain5;
                case "underground": return Catalog.Terrain4;
                default: return Catalog.Terrain3;
            }
        }

        private static Sprite ThemeChoice(Sprite[] choices, string theme)
        {
            if (choices == null || choices.Length == 0) return null;
            int index = theme == "mountains" ? 1 : theme == "sky" ? 2 : theme == "desert" ? 3 :
                theme == "dark_mines" ? 4 : theme == "underground" ? 5 : 0;
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
