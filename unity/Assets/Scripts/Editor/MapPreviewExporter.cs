using System;
using System.IO;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>
    /// 使用游戏真实的 MapArt/SpriteLib 生成四章全景图。
    /// 菜单与 CI 共用，便于在没有进入 Play Mode 时做视觉回归。
    /// </summary>
    public static class MapPreviewExporter
    {
        private const int Width = 1600;
        private const int Height = 900;

        [MenuItem("Numeria/Export Map Previews")]
        public static void ExportAll()
        {
            string output = Environment.GetEnvironmentVariable("NUMERIA_MAP_PREVIEW_DIR");
            if (string.IsNullOrEmpty(output)) output = "/tmp/numeria-map-previews";
            Directory.CreateDirectory(output);

            foreach (var def in new[] { Maps.Forest(), Maps.Mountains(), Maps.Sky(), Maps.Desert() })
                Export(def, Path.Combine(output, def.Id + ".png"));

            Debug.Log($"NUMERIA_MAP_PREVIEWS={output}");
            AssetDatabase.Refresh();
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Export(MapDef def, string path)
        {
            GridMap map = GridMap.Parse(def.Rows);
            var root = new GameObject("MapPreview-" + def.Id);
            var cameraGo = new GameObject("MapPreviewCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = map.Height * .5f;
            camera.transform.position = new Vector3((map.Width - 1) * .5f, (map.Height - 1) * .5f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex(def.CameraBg);
            camera.cullingMask = ~0;

            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    Vector3 world = new Vector3(x, map.Height - 1 - y, 0);
                    int hash = (x * 73856093) ^ (y * 19349663);
                    int variant = ((hash % 97) + 97) % 97;
                    Tile tile = map.At(x, y);
                    int neighbors = NeighborMask(map, x, y, tile);
                    var terrain = Add(root.transform, MapArt.Terrain(def.Theme, tile, variant, neighbors), world, 0,
                        $"terrain-{x}-{y}");
                    terrain.color = MapArt.Tint(def.Theme, tile);

                    switch (tile)
                    {
                        case Tile.Water:
                            if (def.Theme == "sky" && variant % 17 == 0)
                            {
                                var cloud = Add(root.transform, MapArt.Prop(def.Theme, "obstacle", variant),
                                    world + Vector3.up * .08f, 1, "sky-cloud");
                                cloud.color = new Color(1f, 1f, 1f, .86f);
                                ScaleToHeight(cloud, .58f);
                            }
                            break;
                        case Tile.Cliff:
                            if ((def.Theme == "mountains" || def.Theme == "desert") && variant % 3 == 0)
                            {
                                var rock = Add(root.transform, MapArt.Prop(def.Theme, "obstacle", variant),
                                    world + Vector3.up * .06f, SortOrder(world.y), "rock");
                                rock.color = MapArt.Tint(def.Theme, tile, "obstacle");
                                ScaleToHeight(rock, def.Theme == "mountains" ? .72f : .62f);
                            }
                            break;
                        case Tile.Tree:
                            var obstacle = Add(root.transform, MapArt.Prop(def.Theme, "obstacle", variant),
                                world + Vector3.up * .2f, SortOrder(world.y), "obstacle");
                            obstacle.color = MapArt.Tint(def.Theme, tile, "obstacle");
                            ScaleToHeight(obstacle, MapArt.PropHeight(def.Theme, "obstacle"));
                            break;
                        case Tile.Bush:
                            var encounter = Add(root.transform, MapArt.Prop(def.Theme, "encounter", variant),
                                world + Vector3.up * .08f, SortOrder(world.y), "encounter");
                            encounter.color = MapArt.Tint(def.Theme, tile, "encounter");
                            ScaleToHeight(encounter, MapArt.PropHeight(def.Theme, "encounter"));
                            break;
                        case Tile.Landmark:
                            var landmark = Add(root.transform, MapArt.Prop(def.Theme, "landmark", variant),
                                world + Vector3.up * .52f, SortOrder(world.y) + 2, "landmark");
                            ScaleToHeight(landmark, MapArt.PropHeight(def.Theme, "landmark"));
                            break;
                        case Tile.Bridge:
                            var bridge = Add(root.transform, MapArt.Prop(def.Theme, "bridge", variant), world, 2, "bridge");
                            ScaleToHeight(bridge, MapArt.PropHeight(def.Theme, "bridge"));
                            break;
                        case Tile.Chest:
                            var treasure = Add(root.transform, MapArt.Prop(def.Theme, "treasure", variant),
                                world + Vector3.up * .08f, SortOrder(world.y) + 2, "treasure");
                            ScaleToHeight(treasure, MapArt.PropHeight(def.Theme, "treasure"));
                            break;
                        case Tile.Portal:
                            var portal = Add(root.transform, MapArt.Prop(def.Theme, "portal", variant),
                                world + Vector3.up * .48f, SortOrder(world.y) - 4, "portal");
                            ScaleToHeight(portal, MapArt.PropHeight(def.Theme, "portal"));
                            var glow = Add(root.transform, MapArt.Prop(def.Theme, "portal-glow", variant),
                                world + Vector3.up * .03f, SortOrder(world.y) - 3, "portal-glow");
                            ScaleToHeight(glow, MapArt.PropHeight(def.Theme, "portal-glow"));
                            break;
                    }
                }

            foreach (var discovery in def.Discoveries ?? Array.Empty<DiscoveryDef>())
            {
                Vector3 world = new Vector3(discovery.X, map.Height - 1 - discovery.Y, 0);
                var marker = Add(root.transform, SpriteLib.One("generated/Economy/numeria_coin"),
                    world + Vector3.up * .22f, SortOrder(world.y) + 5, "discovery");
                ScaleToHeight(marker, .62f);
            }

            if (def.Merchant != null)
            {
                Vector3 world = new Vector3(def.Merchant.X, map.Height - 1 - def.Merchant.Y, 0);
                var merchant = Add(root.transform, SpriteLib.One(def.Merchant.SpriteResource),
                    world + Vector3.up * .32f, SortOrder(world.y) + 7, "merchant");
                ScaleToHeight(merchant, 1.35f);
            }

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
            };
            camera.targetTexture = rt;
            // 首次离屏渲染会触发 Sprite/材质上传；第二帧才是稳定的视觉基线。
            camera.Render();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            var capture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            capture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            capture.Apply();
            File.WriteAllBytes(path, capture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(capture);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(cameraGo);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static int NeighborMask(GridMap map, int x, int y, Tile tile)
        {
            bool Joins(int nx, int ny)
            {
                if (!map.InBounds(nx, ny)) return false;
                Tile other = map.At(nx, ny);
                if (tile == Tile.Path || tile == Tile.Bridge)
                    return other == Tile.Path || other == Tile.Bridge;
                if (tile == Tile.Water)
                    return other == Tile.Water || other == Tile.Bridge;
                if (tile == Tile.Cliff) return other == Tile.Cliff;
                return other != Tile.Water && other != Tile.Cliff;
            }

            int mask = 0;
            if (Joins(x, y - 1)) mask |= 1;
            if (Joins(x + 1, y)) mask |= 2;
            if (Joins(x, y + 1)) mask |= 4;
            if (Joins(x - 1, y)) mask |= 8;
            return mask;
        }

        private static SpriteRenderer Add(Transform parent, Sprite sprite, Vector3 position, int order, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void ScaleToHeight(SpriteRenderer renderer, float height)
        {
            if (renderer == null || renderer.sprite == null) return;
            float scale = height / Mathf.Max(.01f, renderer.sprite.bounds.size.y);
            renderer.transform.localScale = Vector3.one * scale;
        }

        private static int SortOrder(float worldY) => 1000 - (int)(worldY * 10);

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
