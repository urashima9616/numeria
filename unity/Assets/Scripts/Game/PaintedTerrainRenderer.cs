using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>
    /// Draws David Baumgart's bottom-anchored square terrain tiles on Numeria's
    /// semantic grid. The source artwork is 256 px wide at 100 PPU, so every
    /// sprite is normalized to one gameplay cell and lower screen rows sort in
    /// front of higher rows as required by the pack.
    /// </summary>
    public static class PaintedTerrainRenderer
    {
        private const float CellWidth = 1.045f;
        private static Sprite _overlaySprite;

        public static void Build(Transform parent, GridMap map, string theme)
        {
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    int hash = (x * 73856093) ^ (y * 19349663);
                    int variant = ((hash % 97) + 97) % 97;
                    Tile tile = map.At(x, y);
                    Sprite sprite = MapArt.Terrain(theme, tile, variant, 0);
                    Vector3 world = new Vector3(x, map.Height - 1 - y, 0);

                    var go = new GameObject($"painted-{theme}-{tile.ToString().ToLowerInvariant()}-{x}-{y}");
                    go.transform.SetParent(parent, false);
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.color = MapArt.TerrainTint(theme, tile, sprite);

                    if (MapArt.PaintedReady && sprite != null)
                    {
                        // Imported sprites use the pack's bottom-centre anchor. Position the
                        // bottom edge half a cell below the logical cell centre.
                        go.transform.position = world + Vector3.down * .5f;
                        float scale = CellWidth / Mathf.Max(.01f, sprite.bounds.size.x);
                        bool mirror = variant % 2 == 1 &&
                                      (tile == Tile.Tree || tile == Tile.Bush || tile == Tile.Cliff);
                        go.transform.localScale = new Vector3(mirror ? -scale : scale, scale, scale);
                        renderer.sortingOrder = SortOrder(world.y);

                        if (tile == Tile.Path || tile == Tile.Bridge)
                            AddPath(parent, map, theme, x, y, world, renderer.sortingOrder + 2);
                    }
                    else
                    {
                        // Keep the legacy renderer usable when a new clone has not imported
                        // the local Asset Store package yet.
                        go.transform.position = world;
                        renderer.sortingOrder = 0;
                    }
                }
        }

        private static void AddPath(Transform parent, GridMap map, string theme, int x, int y,
            Vector3 world, int order)
        {
            Color color = PathColor(theme);
            AddOverlay(parent, world, new Vector2(.58f, .58f), color, order, $"route-centre-{x}-{y}");

            bool Joins(int px, int py)
            {
                if (!map.InBounds(px, py)) return false;
                Tile neighbor = map.At(px, py);
                return neighbor == Tile.Path || neighbor == Tile.Bridge;
            }

            if (Joins(x - 1, y))
                AddOverlay(parent, world + Vector3.left * .39f, new Vector2(.42f, .58f), color, order,
                    $"route-west-{x}-{y}");
            if (Joins(x + 1, y))
                AddOverlay(parent, world + Vector3.right * .39f, new Vector2(.42f, .58f), color, order,
                    $"route-east-{x}-{y}");
            if (Joins(x, y - 1))
                AddOverlay(parent, world + Vector3.up * .39f, new Vector2(.58f, .42f), color, order,
                    $"route-north-{x}-{y}");
            if (Joins(x, y + 1))
                AddOverlay(parent, world + Vector3.down * .39f, new Vector2(.58f, .42f), color, order,
                    $"route-south-{x}-{y}");
        }

        private static void AddOverlay(Transform parent, Vector3 position, Vector2 size, Color color,
            int order, string name)
        {
            if (_overlaySprite == null)
            {
                _overlaySprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                    new Vector2(.5f, .5f), 1f);
                _overlaySprite.hideFlags = HideFlags.HideAndDontSave;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _overlaySprite;
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        private static Color PathColor(string theme)
        {
            string value;
            switch (theme)
            {
                case "mountains": value = "#887966c8"; break;
                case "sky": value = "#f1daa8cf"; break;
                case "desert": value = "#9d623fd2"; break;
                case "dark_mines": value = "#a7835bcc"; break;
                case "underground": value = "#a87587cc"; break;
                default: value = "#9b784fc8"; break;
            }
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }

        public static int SortOrder(float worldY) => 10000 - Mathf.RoundToInt(worldY * 100f);
    }
}
