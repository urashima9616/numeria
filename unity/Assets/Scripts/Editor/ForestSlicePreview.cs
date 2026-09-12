using System.IO;
using Numeria.Core;
using Progress = Numeria.Core.Progress;
using Numeria.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Editor
{
    /// <summary>Deterministic 4:3 renders of the real scene, HUD and sampled spell timelines; no player save access.</summary>
    public static class ForestSlicePreview
    {
        private const string Output = "/tmp/numeria-forest-preview";
        [MenuItem("Numeria/Preview Forest Slice")]
        public static void Export()
        {
            Directory.CreateDirectory(Output);
            AssetDatabase.ImportAsset("Assets/Resources/generated/Exploration/forest_props.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Resources/generated/Exploration/forest_ground.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Resources/generated/Exploration/spell_materials.png", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Resources/generated/Exploration/vine_bridge.png", ImportAssetOptions.ForceUpdate);
            Forest(false);
            Forest(true);
            foreach (var kind in new[] { SkillVisualKind.EquationFlame, SkillVisualKind.MakeTenWave, SkillVisualKind.SymmetryBeam })
                Spell(kind);
            Debug.Log("FOREST_SLICE_PREVIEWS=" + Output);
        }

        private static Camera CameraFor(Transform root)
        {
            var go = new GameObject("PreviewCamera");
            go.transform.SetParent(root, false);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Ui.Hex("#24453c");
            camera.transform.position = new Vector3(8, 11, -10);
            camera.aspect = 4f / 3;
            return camera;
        }

        private static RectTransform CanvasFor(Transform root, Camera camera)
        {
            var rect = Ui.Node(root, "PreviewCanvas");
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.sortingOrder = 20000;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = .5f;
            return rect;
        }

        private static void Forest(bool restored)
        {
            var root = new GameObject("ForestPreview");
            var progress = new Progress { Coins = 24 };
            if (restored)
            {
                progress.CollectDiscovery(ForestJourney.Lanterns);
                progress.CollectDiscovery(ForestJourney.Bridge);
            }
            var def = Maps.Forest();
            var map = GridMap.Parse(def.Rows);
            ForestScene.ApplyPassages(map, progress);
            ForestScene.Build(root.transform, map, progress);
            var camera = CameraFor(root.transform);
            if (restored) camera.transform.position = new Vector3(15, 11, -10);
            var avatar = new GameObject("Lucas");
            avatar.transform.SetParent(root.transform, false);
            avatar.transform.position = restored ? new Vector3(13, 12, 0) : new Vector3(7, 14, 0);
            var sr = avatar.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLib.LucasExplorer();
            sr.sortingOrder = PaintedTerrainRenderer.SortOrder(avatar.transform.position.y) + 40;
            avatar.transform.localScale = Vector3.one * (1.35f / sr.sprite.bounds.size.y);
            foreach (var discovery in def.Discoveries)
            {
                if (progress.CollectedDiscoveries.Contains(discovery.Id)) continue;
                var go = new GameObject("Rune");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(discovery.X, map.Height - 1 - discovery.Y + .22f, 0);
                var rune = go.AddComponent<SpriteRenderer>();
                rune.sprite = SpriteLib.One("generated/Story/digit_crystal");
                rune.sortingOrder = 13000;
                go.transform.localScale = Vector3.one * (.62f / rune.sprite.bounds.size.y);
            }
            var canvas = CanvasFor(root.transform, camera);
            var hud = new ExplorationHud(canvas, () => { }, () => { }, () => { });
            hud.Refresh(progress, def);
            Capture(camera, restored ? "forest-bridge-4x3" : "forest-entry-4x3");
            Object.DestroyImmediate(root);
        }

        private static void Spell(SkillVisualKind kind)
        {
            var root = new GameObject("SpellPreview");
            var camera = CameraFor(root.transform);
            var canvas = CanvasFor(root.transform, camera);
            var bg = Ui.SpriteImg(canvas, "BattleMeadow", SpriteLib.One("generated/NUMERIA_Unity_Battle_Assets/Backgrounds/Sunny_Meadow_2048x1152"));
            Ui.Stretch(bg.rectTransform);
            var dim = Ui.Img(canvas, "Dim", new Color(.02f, .08f, .09f, .28f));
            Ui.Stretch(dim.rectTransform);
            var player = Ui.SpriteImg(canvas, "Caster", SpriteLib.PlayerBattleSprite(
                kind == SkillVisualKind.MakeTenWave ? "tenfin" : kind == SkillVisualKind.SymmetryBeam ? "mirrowl" : "addmander"));
            Ui.PlaceCentered(player.rectTransform, new Vector2(.26f, .32f), Vector2.zero, new Vector2(270, 300));
            player.preserveAspect = true;
            var target = Ui.SpriteImg(canvas, "Target", SpriteLib.EnemyBattleSprite("countipillar"));
            Ui.PlaceCentered(target.rectTransform, new Vector2(.73f, .64f), Vector2.zero, new Vector2(290, 220));
            target.preserveAspect = true;
            Canvas.ForceUpdateCanvases();
            var trace = new SpellTrace { A = 4, B = 6, Total = 10,
                Pattern = new[] { new PatternToken { Shape = ShapeKind.Circle, Color = PatternColor.Blue },
                    new PatternToken { Shape = ShapeKind.Triangle, Color = PatternColor.Gold },
                    new PatternToken { Shape = ShapeKind.Square, Color = PatternColor.Coral } } };
            var spell = SpellSequence.Create(canvas, player.rectTransform, target.rectTransform, kind, trace, true);
            spell.Sample(.52f);
            Capture(camera, kind + "-charge");
            spell.Sample(1.23f);
            Capture(camera, kind + "-impact");
            Object.DestroyImmediate(root);
        }

        private static void Capture(Camera camera, string name)
        {
            var rt = new RenderTexture(1440, 1080, 24);
            var old = RenderTexture.active;
            var capture = new Texture2D(1440, 1080, TextureFormat.RGB24, false);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            camera.Render(); camera.Render();
            RenderTexture.active = rt;
            capture.ReadPixels(new Rect(0, 0, 1440, 1080), 0, 0);
            capture.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), capture.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = old;
            Object.DestroyImmediate(capture);
            rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
