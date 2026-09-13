using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Numeria.Core;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>Mechanical atlas import: retain generated alpha and split disconnected sprites, never redraw them.</summary>
    public static class AfterstoryAssetImporter
    {
        [MenuItem("Numeria/Import Afterstory Sprite Sheets")]
        public static void Import()
        {
            string staging = "/tmp/numeria-afterstory-atlases";
            foreach (string family in new[] { "tiletot", "abacub", "coglet", "gliminnow", "wisplet", "lilypadlet" })
                foreach (string suffix in new[] { "_large_icon", "_mega_icon" })
                    Split(Path.Combine(staging, family + suffix + ".png"), GameData.LineFor(family).StageIds, suffix);
            AssetDatabase.Refresh();
        }

        private sealed class Part
        {
            public int Left, Right, Bottom, Top, Count;
            public readonly List<int> Pixels = new List<int>();
            public float Center => (Left + Right) * .5f;
            public void Include(Part p)
            { Left = Math.Min(Left, p.Left); Right = Math.Max(Right, p.Right); Bottom = Math.Min(Bottom, p.Bottom); Top = Math.Max(Top, p.Top); }
        }

        private static void Split(string path, string[] names, string suffix)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!source.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException(path);
            var pixels = source.GetPixels32(); int w = source.width, h = source.height;
            if (!pixels.Any(p => p.a == 0)) throw new InvalidDataException("Atlas has no genuine transparency: " + path);
            var seen = new bool[pixels.Length]; var parts = new List<Part>(); var queue = new Queue<int>();
            for (int n = 0; n < pixels.Length; n++)
            {
                if (seen[n] || pixels[n].a < 32) continue;
                var part = new Part { Left = w, Right = 0, Bottom = h, Top = 0 };
                queue.Enqueue(n); seen[n] = true;
                while (queue.Count > 0)
                {
                    int v = queue.Dequeue(), x = v % w, y = v / w;
                    part.Pixels.Add(v);
                    part.Count++; part.Left = Math.Min(part.Left, x); part.Right = Math.Max(part.Right, x);
                    part.Bottom = Math.Min(part.Bottom, y); part.Top = Math.Max(part.Top, y);
                    void Add(int next) { if (!seen[next] && pixels[next].a >= 32) { seen[next] = true; queue.Enqueue(next); } }
                    if (x > 0) Add(v - 1); if (x < w - 1) Add(v + 1);
                    if (y > 0) Add(v - w); if (y < h - 1) Add(v + w);
                }
                if (part.Count >= 60) parts.Add(part);
            }
            var anchors = parts.OrderByDescending(p => p.Count).Take(3).OrderBy(p => p.Left).ToArray();
            if (anchors.Length != 3) throw new InvalidDataException("Expected three separate creatures: " + path);
            var centers = anchors.Select(p => p.Center).ToArray();
            var owner = Enumerable.Repeat(-1, pixels.Length).ToArray();
            foreach (var part in parts)
            {
                int nearest = Enumerable.Range(0, 3).OrderBy(i => Math.Abs(centers[i] - part.Center)).First();
                foreach (int pixel in part.Pixels) owner[pixel] = nearest;
                anchors[nearest].Include(part);
            }
            for (int i = 0; i < 3; i++)
            {
                var box = anchors[i]; int bw = box.Right - box.Left + 1, bh = box.Top - box.Bottom + 1;
                int side = Mathf.CeilToInt(Math.Max(bw, bh) * 1.16f);
                var icon = new Texture2D(512, 512, TextureFormat.RGBA32, false);
                var result = new Color32[512 * 512];
                for (int y = 0; y < 512; y++)
                    for (int x = 0; x < 512; x++)
                    {
                        int sx = Mathf.FloorToInt(x * side / 512f - (side - bw) * .5f), sy = Mathf.FloorToInt(y * side / 512f - (side - bh) * .5f);
                        if (sx >= 0 && sx < bw && sy >= 0 && sy < bh)
                        {
                            int index = (box.Bottom + sy) * w + box.Left + sx;
                            // Interleaving tails can share a bounding rectangle, but not a sprite.
                            if (owner[index] == i) result[y * 512 + x] = pixels[index];
                        }
                    }
                icon.SetPixels32(result); icon.Apply();
                string destination = "Assets/Resources/generated/" + names[i] + suffix + ".png";
                if (File.Exists(destination) && Environment.GetEnvironmentVariable("NUMERIA_REIMPORT_AFTERSTORY") != "1")
                    throw new IOException("Refusing to overwrite existing art: " + destination);
                File.WriteAllBytes(destination, icon.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(icon);
            }
            UnityEngine.Object.DestroyImmediate(source);
        }
    }
}
