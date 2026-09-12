using System.Collections;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>Character-scale forest. Terrain, canopy, living water and quest changes share the gameplay grid.</summary>
    public sealed class ForestScene : MonoBehaviour
    {
        private static readonly Sprite[] Props = new Sprite[4];
        private Transform _bridge;
        private Transform _lanterns;
        private Transform _mirror;
        private Sprite _surface;
        private Texture2D _surfaceTexture;
        private SpriteRenderer[] _motes;

        public static Sprite Prop(int index)
        {
            if (Props[index] != null) return Props[index];
            var texture = Resources.Load<Texture2D>("generated/Exploration/forest_props");
            if (texture == null) return null;
            // Rects follow the delivered atlas's actual margins, not an assumed perfect 2x2 grid.
            Rect[] regions = { new Rect(0, .50f, .46f, .50f), new Rect(.46f, .41f, .54f, .59f),
                new Rect(0, 0, .49f, .40f), new Rect(.54f, 0, .46f, .40f) };
            Rect r = regions[index];
            Props[index] = Sprite.Create(texture,
                new Rect(Mathf.Round(r.x * texture.width), Mathf.Round(r.y * texture.height),
                    Mathf.Floor(r.width * texture.width), Mathf.Floor(r.height * texture.height)),
                new Vector2(.5f, .12f), 100);
            return Props[index];
        }

        public static void ApplyPassages(GridMap map, Progress progress)
        {
            for (int x = 14; x <= 16; x++)
                map.SetTile(x, 5, ForestJourney.BridgeRestored(progress) ? Tile.Bridge : Tile.Water);
        }

        public static ForestScene Build(Transform parent, GridMap map, Progress progress)
        {
            var root = new GameObject("LivingForest");
            root.transform.SetParent(parent, false);
            var scene = root.AddComponent<ForestScene>();
            scene.BuildTerrain(map);
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    Tile tile = map.At(x, y);
                    Vector3 p = new Vector3(x, map.Height - 1 - y, 0);
                    int seed = (x * 37 + y * 19) % 17;
                    if (tile == Tile.Tree && (x + y) % 2 == 0)
                        scene.Add(Prop(0), p + Vector3.down * .45f, 2.3f + seed * .025f,
                            PaintedTerrainRenderer.SortOrder(p.y), "Oak");
                    else if (tile == Tile.Bush && (x + y) % 2 == 0)
                        scene.Add(Prop(2), p + Vector3.down * .35f, 1.05f,
                            PaintedTerrainRenderer.SortOrder(p.y), "WildFern");
                    else if (tile == Tile.Landmark)
                        scene.Add(Prop(1), p + Vector3.down * .45f, 4.4f,
                            PaintedTerrainRenderer.SortOrder(p.y), "LanternOak");
                    else if (tile == Tile.Portal)
                        scene.Add(Prop(1), p + Vector3.down * .5f, 5.4f,
                            PaintedTerrainRenderer.SortOrder(p.y) - 10, "GuardianOak");
                    else if (tile == Tile.Grass && seed == 3)
                        scene.Add(Prop(2), p + Vector3.down * .2f, .4f, -5, "GroundCover");
                }
            scene._bridge = scene.BuildBridge(new Vector3(15, map.Height - 6, 0));
            scene.BuildBridge(new Vector3(15, map.Height - 13, 0));
            for (int y = 2; y < 16; y++)
            {
                var ripple = scene.Add(SpellSequence.Glow(), new Vector3(14.6f + .4f * Mathf.Sin(y * 3), y, 0),
                    .08f, -70, "WaterRipple");
                ripple.transform.localScale = new Vector3(1.3f / ripple.sprite.bounds.size.x, .045f / ripple.sprite.bounds.size.y, 1);
                ripple.color = new Color(.68f, .95f, .89f, .4f);
            }
            scene._lanterns = scene.BuildMotes(new Vector3(6, 10, 0), "FireflyLanterns", 9, Ui.Hex("#ffe99a"));
            scene._mirror = scene.BuildMotes(new Vector3(23, 7, 0), "MirrorGrove", 6, Ui.Hex("#a4f6ef"));
            scene._motes = root.GetComponentsInChildren<SpriteRenderer>(true);
            scene.SetProgress(progress);
            return scene;
        }

        private SpriteRenderer Add(Sprite sprite, Vector3 point, float height, int order, string label)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.position = point;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            return sr;
        }

        private void BuildTerrain(GridMap map)
        {
            var grass = Resources.Load<Sprite>("generated/Exploration/forest_ground");
            for (int y = 0; y < map.Height; y += 6)
                for (int x = 0; x < map.Width; x += 6)
                {
                    var floor = Add(grass, new Vector3(x + 2.5f, y + 2.5f, 0), 6.02f, -100, "MossFloor");
                    if (grass == null) floor.color = Ui.Hex("#698058");
                }
            const int density = 20;
            int width = map.Width * density, height = map.Height * density;
            _surfaceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _surfaceTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[width * height];
            for (int py = 0; py < height; py++)
                for (int px = 0; px < width; px++)
                {
                    float x = px / (float)density - .5f, y = map.Height - .5f - py / (float)density;
                    float water = 9, land = 9, path = 9;
                    int cx = Mathf.RoundToInt(x), cy = Mathf.RoundToInt(y);
                    bool insideWater = map.InBounds(cx, cy) &&
                        (map.At(cx, cy) == Tile.Water || map.At(cx, cy) == Tile.Bridge);
                    for (int oy = -2; oy <= 2; oy++)
                        for (int ox = -2; ox <= 2; ox++)
                        {
                            int tx = cx + ox, ty = cy + oy;
                            if (!map.InBounds(tx, ty)) continue;
                            Tile t = map.At(tx, ty);
                            Vector2 q = new Vector2(Mathf.Abs(x - tx), Mathf.Abs(y - ty));
                            float d = new Vector2(Mathf.Max(0, q.x - .5f), Mathf.Max(0, q.y - .5f)).magnitude;
                            if (t == Tile.Water || t == Tile.Bridge) water = Mathf.Min(water, d);
                            else land = Mathf.Min(land, d);
                            if (t == Tile.Path)
                            {
                                path = Mathf.Min(path, q.magnitude - .34f);
                                for (int dir = 0; dir < 2; dir++)
                                {
                                    int nx = tx + (dir == 0 ? 1 : 0), ny = ty + (dir == 1 ? 1 : 0);
                                    if (!map.InBounds(nx, ny)) continue;
                                    Tile next = map.At(nx, ny);
                                    if (next != Tile.Path && next != Tile.Bridge) continue;
                                    Vector2 closest = dir == 0 ? new Vector2(Mathf.Clamp(x, tx, nx), ty) :
                                        new Vector2(tx, Mathf.Clamp(y, ty, ny));
                                    path = Mathf.Min(path, Vector2.Distance(new Vector2(x, y), closest) - .34f);
                                }
                            }
                        }
                    water = insideWater ? -land : water;
                    float noise = Mathf.PerlinNoise(x * 8.2f, y * 8.2f) * .4f + Mathf.PerlinNoise(x * 1.2f, y * 1.2f) * .6f;
                    Color c = Color.clear;
                    if (path < .10f)
                    {
                        c = Color.Lerp(Ui.Hex("#9a885b"), Ui.Hex("#c0af7b"), noise);
                        c.a = 1 - Mathf.SmoothStep(0, 1, (path + noise * .035f) / .10f);
                    }
                    water += (Mathf.Sin(y * 1.6f) + Mathf.Sin(y * 3.1f)) * .035f;
                    if (water < .16f)
                    {
                        c = water > -.06f ? Ui.Hex("#b3b582") : Color.Lerp(Ui.Hex("#2e7575"), Ui.Hex("#62afa0"), noise * .7f);
                        c.a = 1 - Mathf.SmoothStep(0, 1, water / .16f);
                    }
                    pixels[py * width + px] = c;
                }
            _surfaceTexture.SetPixels(pixels);
            _surfaceTexture.Apply();
            _surface = Sprite.Create(_surfaceTexture, new Rect(0, 0, width, height), Vector2.one * .5f, density);
            Add(_surface, new Vector3((map.Width - 1) * .5f, (map.Height - 1) * .5f, 0), map.Height, -90, "RiverAndTrails");
        }

        private Transform BuildBridge(Vector3 center)
        {
            var root = new GameObject("VineBridge").transform;
            root.SetParent(transform, false);
            root.position = center;
            var bridge = Add(Resources.Load<Sprite>("generated/Exploration/vine_bridge"), center, 1.42f,
                PaintedTerrainRenderer.SortOrder(center.y) - 25, "VineDeck");
            bridge.transform.SetParent(root, true);
            return root;
        }

        private Transform BuildMotes(Vector3 center, string name, int count, Color tint)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            root.position = center;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count;
                var sr = Add(SpriteLib.One("generated/Story/digit_crystal"),
                    center + new Vector3(Mathf.Cos(a) * 1.4f, Mathf.Sin(a) * .8f + .7f, 0), .2f, 13000, "LivingMote");
                sr.color = tint;
                sr.transform.SetParent(root, true);
            }
            return root;
        }

        public void SetProgress(Progress p)
        {
            _bridge.gameObject.SetActive(ForestJourney.BridgeRestored(p));
            _lanterns.gameObject.SetActive(p.CollectedDiscoveries.Contains(ForestJourney.Lanterns));
            _mirror.gameObject.SetActive(p.CollectedDiscoveries.Contains(ForestJourney.Mirror));
        }

        public IEnumerator Reveal(Progress p, string id)
        {
            SetProgress(p);
            Transform target = id == ForestJourney.Bridge ? _bridge : id == ForestJourney.Lanterns ? _lanterns : _mirror;
            for (float t = 0; t < 1; t += Time.deltaTime / 1.15f)
            {
                float s = Mathf.SmoothStep(.02f, 1, t);
                target.localScale = id == ForestJourney.Bridge ? new Vector3(s, 1, 1) : Vector3.one * s;
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private void Update()
        {
            if (_motes == null) return;
            foreach (var sr in _motes)
                if (sr != null && sr.name == "LivingMote")
                {
                    Color c = sr.color;
                    c.a = .6f + .4f * Mathf.Sin(Time.time * 2 + sr.transform.position.x);
                    sr.color = c;
                }
                else if (sr != null && sr.name == "WaterRipple")
                {
                    Color c = sr.color;
                    c.a = .18f + .15f * Mathf.Sin(Time.time * 1.3f + sr.transform.position.y);
                    sr.color = c;
                }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (_surface != null) Destroy(_surface);
                if (_surfaceTexture != null) Destroy(_surfaceTexture);
            }
            else
            {
                if (_surface != null) DestroyImmediate(_surface);
                if (_surfaceTexture != null) DestroyImmediate(_surfaceTexture);
            }
        }
    }
}
