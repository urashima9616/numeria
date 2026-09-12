using System.Collections;
using System.Collections.Generic;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>Character-scale biome renderer; decoration never changes the saved gameplay grid.</summary>
    public sealed class WorldScene : MonoBehaviour
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private readonly List<SpriteRenderer> _atmosphere = new List<SpriteRenderer>();
        private readonly Dictionary<string, Transform> _discoveries = new Dictionary<string, Transform>();
        private Texture2D _surfaceTexture;
        private Sprite _surface;
        private string _biome;
        public static bool Covers(Tile t) => t == Tile.Tree || t == Tile.Cliff || t == Tile.Bush ||
            t == Tile.Landmark || t == Tile.Bridge;

        public static Sprite Prop(string biome, int index)
        {
            if (biome == "forest") return ForestScene.Prop(index);
            string key = biome + "/" + index;
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("generated/Exploration/" + biome + "_props");
            if (texture == null) return null;
            // Upper sprites on the delivered sky/alpine atlases extend slightly below the halfway line.
            float split = biome == "mountains" || biome == "sky" ? .44f : .50f;
            float bottom = index < 2 ? split : 0;
            float height = index < 2 ? 1 - split : split;
            var rect = new Rect(index % 2 * texture.width / 2, Mathf.Floor(bottom * texture.height),
                texture.width / 2, Mathf.Floor(height * texture.height));
            var sprite = Sprite.Create(texture, rect, new Vector2(.5f, .12f), 100, 0, SpriteMeshType.FullRect);
            Sprites[key] = sprite;
            return sprite;
        }

        public static Sprite BridgeSprite(string biome)
        {
            string key = biome + "/TopDownBridge";
            if (Sprites.TryGetValue(key, out var cached) && cached != null) return cached;
            var texture = Resources.Load<Texture2D>("generated/Exploration/world_bridges");
            if (texture == null) return null;
            int index = System.Array.IndexOf(new[] { "forest", "mountains", "sky", "desert", "dark_mines", "underground" }, biome);
            if (index < 0) return null;
            float w = texture.width / 2f, h = texture.height / 3f;
            var sprite = Sprite.Create(texture, new Rect(Mathf.Floor(index % 2 * w), Mathf.Floor((2 - index / 2) * h),
                Mathf.Floor(w), Mathf.Floor(h)), Vector2.one * .5f, 100);
            Sprites[key] = sprite; return sprite;
        }

        public static WorldScene Build(Transform parent, GridMap map, MapDef def, Progress progress)
        {
            var root = new GameObject("World-" + def.Id);
            root.transform.SetParent(parent, false);
            var scene = root.AddComponent<WorldScene>();
            scene._biome = def.Id;
            if (def.Id == "forest") ForestScene.Build(root.transform, map, progress);
            else
            {
                scene.Terrain(map);
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        Tile t = map.At(x, y);
                        var p = new Vector3(x, map.Height - 1 - y, 0);
                        int seed = (x * 37 + y * 19) % 17;
                        if (t == Tile.Tree && (x + y) % 2 == 0)
                            scene.Add(Prop(def.Id, 0), p + Vector3.down * .35f, 2.1f + seed * .025f, Sort(p.y), "BiomeObstacle");
                        else if (t == Tile.Cliff && (x + y) % 2 == 0)
                        {
                            bool landmarkPlant = seed % 3 == 0;
                            var crown = scene.Add(landmarkPlant ? Prop(def.Id, 0) : def.Id == "mountains" ? Prop(def.Id, 2) :
                                SpellChoreography.Material("impact", 2), p + new Vector3((seed % 3 - 1) * .12f, -.3f, 0),
                                landmarkPlant ? 2 : .95f + seed * .025f, Sort(p.y), "CliffCrown");
                            if (!landmarkPlant && (def.Id == "dark_mines" || def.Id == "underground")) crown.color = new Color(.55f, .62f, .68f);
                        }
                        else if (t == Tile.Bush && (x + y) % 2 == 0)
                            scene.Add(Prop(def.Id, 2), p + Vector3.down * .3f, 1, Sort(p.y), "EncounterPatch");
                        else if (t == Tile.Landmark || t == Tile.Portal)
                            scene.Add(Prop(def.Id, 1), p + Vector3.down * .55f, t == Tile.Portal ? 4 : 3.3f,
                                Sort(p.y) - 12, "ChapterLandmark");
                        else if (t == Tile.Grass && seed == 5)
                            scene.Add(Prop(def.Id, 2), p + Vector3.down * .2f, .35f, -10, "BiomeDetail");
                    }
                scene.Bridges(map, def.Id);
                foreach (var discovery in def.Discoveries)
                {
                    var node = new GameObject("Restored-" + discovery.Id).transform;
                    node.SetParent(root.transform, false);
                    node.position = new Vector3(discovery.X, map.Height - 1 - discovery.Y, 0);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * Mathf.PI / 3;
                        var mark = scene.Add(SpellSequence.Glow(), node.position +
                            new Vector3(Mathf.Cos(a) * .7f, Mathf.Sin(a) * .35f, 0), .24f, 12900, "RestorationLight");
                        mark.color = scene.Accent;
                        mark.transform.SetParent(node, true);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        Sprite restored = def.Id == "sky" ? SpellChoreography.Material("impact", 3) : Prop(def.Id, 2);
                        var decoration = scene.Add(restored, node.position + new Vector3((i - 1) * .7f, .25f, 0),
                            .48f, Sort(node.position.y) - 25, "RestoredBiome");
                        decoration.transform.SetParent(node, true);
                    }
                    node.gameObject.SetActive(progress.CollectedDiscoveries.Contains(discovery.Id));
                    scene._discoveries[discovery.Id] = node;
                }
            }
            // Lightweight, deterministic drift; no gameplay RNG or persistent state is consumed.
            for (int i = 0; i < 42; i++)
            {
                var dot = scene.Add(SpellSequence.Glow(), new Vector3((i * 7.13f) % map.Width,
                    (i * 3.71f) % map.Height, 0), def.Id == "sky" ? 1.3f : .06f, 13500, "Weather");
                dot.color = scene.Accent;
                if (def.Id == "sky") dot.transform.localScale = Vector3.Scale(dot.transform.localScale, new Vector3(2, .3f, 1));
                scene._atmosphere.Add(dot);
            }
            return scene;
        }

        private Color Accent => Ui.Hex(_biome == "desert" ? "#ffe0a0" : _biome == "underground" ? "#ffa247" :
            _biome == "dark_mines" ? "#66f0eb" : _biome == "forest" ? "#f9e680" : "#eafaff");
        private static int Sort(float y) => PaintedTerrainRenderer.SortOrder(y);
        private SpriteRenderer Add(Sprite sprite, Vector3 p, float height, int order, string label)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            go.transform.position = p;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order;
            if (sprite != null) go.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            return sr;
        }

        private void Terrain(GridMap map)
        {
            var ground = Resources.Load<Sprite>("generated/Exploration/" + _biome + "_ground");
            for (int y = 0; y < map.Height; y += 6)
                for (int x = 0; x < map.Width; x += 6)
                    Add(ground, new Vector3(x + 2.5f, y + 2.5f, 0), 6.015f, -100, "BiomeFloor");
            const int density = 24;
            int width = map.Width * density, height = map.Height * density;
            var pixels = new Color[width * height];
            Color pathColor = Ui.Hex(_biome == "mountains" ? "#a5b5bd" : _biome == "sky" ? "#d4c698" :
                _biome == "desert" ? "#d39b58" : _biome == "dark_mines" ? "#777278" : "#8d7366");
            Color waterColor = Ui.Hex(_biome == "sky" ? "#82c7e6" : _biome == "underground" ? "#e77829" :
                _biome == "dark_mines" ? "#163844" : "#328a98");
            Color cliffColor = Ui.Hex(_biome == "sky" ? "#7f9cae" : _biome == "mountains" ? "#8b9ba7" :
                _biome == "desert" ? "#b88043" : _biome == "dark_mines" ? "#252a34" : "#302726");
            for (int py = 0; py < height; py++)
                for (int px = 0; px < width; px++)
                {
                    float x = px / (float)density - .5f, y = map.Height - .5f - py / (float)density;
                    int cx = Mathf.Clamp(Mathf.RoundToInt(x), 0, map.Width - 1), cy = Mathf.Clamp(Mathf.RoundToInt(y), 0, map.Height - 1);
                    bool inside = Wet(map.At(cx, cy));
                    float water = 9, land = 9, path = 9, cliff = 9;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int tx = cx + dx, ty = cy + dy;
                            if (!map.InBounds(tx, ty)) continue;
                            Tile t = map.At(tx, ty);
                            var q = new Vector2(Mathf.Abs(x - tx), Mathf.Abs(y - ty));
                            float box = new Vector2(Mathf.Max(0, q.x - .5f), Mathf.Max(0, q.y - .5f)).magnitude;
                            if (Wet(t)) water = Mathf.Min(water, box); else land = Mathf.Min(land, box);
                            if (t == Tile.Cliff) cliff = Mathf.Min(cliff,
                                new Vector2(Mathf.Max(0, q.x - .32f), Mathf.Max(0, q.y - .32f)).magnitude - .23f);
                            if (t != Tile.Path) continue;
                            path = Mathf.Min(path, q.magnitude - .30f);
                            for (int axis = 0; axis < 2; axis++)
                            {
                                int nx = tx + (axis == 0 ? 1 : 0), ny = ty + (axis == 1 ? 1 : 0);
                                if (!map.InBounds(nx, ny)) continue;
                                var next = map.At(nx, ny);
                                if (next != Tile.Path && next != Tile.Bridge) continue;
                                var closest = axis == 0 ? new Vector2(Mathf.Clamp(x, tx, nx), ty) : new Vector2(tx, Mathf.Clamp(y, ty, ny));
                                path = Mathf.Min(path, Vector2.Distance(new Vector2(x, y), closest) - .30f);
                            }
                        }
                    float noise = Mathf.PerlinNoise(x * 3.3f, y * 3.3f);
                    Color color = Color.clear;
                    if (path < .14f) { color = pathColor * (.86f + noise * .23f); color.a = .78f * (1 - Mathf.SmoothStep(0, 1, path / .14f)); }
                    if (cliff < .10f)
                    {
                        color = Color.Lerp(cliffColor * .85f, cliffColor, Mathf.PerlinNoise(x * 12, y * 12));
                        color.a = .42f * (1 - Mathf.SmoothStep(0, 1, cliff / .10f));
                    }
                    water = inside ? -land : water;
                    water += Mathf.Sin(y * 2.1f) * .035f;
                    if (water < .15f)
                    {
                        float flow = Mathf.PerlinNoise(x * .7f, y * 2);
                        color = Color.Lerp(waterColor * .65f, waterColor, flow);
                        if (water > -.12f) color = Color.Lerp(pathColor, waterColor, Mathf.Clamp01(-water * 8));
                        color.a = 1 - Mathf.SmoothStep(0, 1, water / .15f);
                    }
                    pixels[py * width + px] = color;
                }
            _surfaceTexture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            _surfaceTexture.SetPixels(pixels); _surfaceTexture.Apply(false, true);
            _surface = Sprite.Create(_surfaceTexture, new Rect(0, 0, width, height), Vector2.one * .5f, density);
            Add(_surface, new Vector3((map.Width - 1) * .5f, (map.Height - 1) * .5f, 0), map.Height, -90, "BiomeSurface");
        }

        private static bool Wet(Tile tile) => tile == Tile.Water || tile == Tile.Bridge;

        private void Bridges(GridMap map, string biome)
        {
            var visited = new HashSet<(int x, int y)>();
            for (int y = 0; y < map.Height; y++)
                for (int x = 0; x < map.Width; x++)
                {
                    if (map.At(x, y) != Tile.Bridge || visited.Contains((x, y))) continue;
                    int left = x, right = x, top = y, bottom = y;
                    var queue = new Queue<(int x, int y)>(); queue.Enqueue((x, y)); visited.Add((x, y));
                    while (queue.Count > 0)
                    {
                        var p = queue.Dequeue(); left = Mathf.Min(left, p.x); right = Mathf.Max(right, p.x);
                        top = Mathf.Min(top, p.y); bottom = Mathf.Max(bottom, p.y);
                        foreach (var d in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                        {
                            var n = (x: p.x + d.Item1, y: p.y + d.Item2);
                            if (map.InBounds(n.x, n.y) && map.At(n.x, n.y) == Tile.Bridge && visited.Add(n)) queue.Enqueue(n);
                        }
                    }
                    var source = BridgeSprite(biome);
                    float worldY = map.Height - 1 - (top + bottom) * .5f;
                    var sr = Add(source, new Vector3((left + right) * .5f, worldY, 0), 1, Sort(worldY) - 25, "ChapterBridge");
                    bool horizontal = right - left >= bottom - top;
                    if (left == right && top == bottom)
                        horizontal = !(map.InBounds(x - 1, y) && Wet(map.At(x - 1, y)));
                    float length = Mathf.Max(right - left, bottom - top) + 1.6f;
                    sr.transform.rotation = Quaternion.Euler(0, 0, horizontal ? 0 : 90);
                    sr.transform.localScale = new Vector3(length / source.bounds.size.x, 1.4f / source.bounds.size.y, 1);
                }
        }

        public IEnumerator Reveal(Progress progress, string id)
        {
            if (!_discoveries.TryGetValue(id, out var node)) yield break;
            node.gameObject.SetActive(true);
            for (float t = 0; t < 1; t += Time.deltaTime / .8f)
            { node.localScale = Vector3.one * Mathf.SmoothStep(.05f, 1, t); yield return null; }
            node.localScale = Vector3.one;
        }

        private void Update()
        {
            for (int i = 0; i < _atmosphere.Count; i++)
            {
                var sr = _atmosphere[i];
                var p = sr.transform.localPosition;
                float drift = _biome == "underground" || _biome == "dark_mines" ? .12f : -.12f;
                p.x = Mathf.Repeat(p.x + Time.deltaTime * .07f, 32);
                p.y = Mathf.Repeat(p.y + Time.deltaTime * drift, 18);
                sr.transform.localPosition = p;
                Color c = Accent; c.a = .16f + .22f * (.5f + .5f * Mathf.Sin(Time.time + i)); sr.color = c;
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying) { if (_surface != null) Destroy(_surface); if (_surfaceTexture != null) Destroy(_surfaceTexture); }
            else { if (_surface != null) DestroyImmediate(_surface); if (_surfaceTexture != null) DestroyImmediate(_surfaceTexture); }
        }
    }
}
