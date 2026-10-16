using System;
using System.IO;
using System.Linq;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Progress = Numeria.Core.Progress;

namespace Numeria.Editor
{
    /// <summary>Real runtime renderers sampled offscreen. Fresh data only: never reads/writes player slots.</summary>
    public static class WorldPresentationPreview
    {
        private const string Output = "/tmp/numeria-world-presentation";
        private static Texture2D _sheet;
        private static int _sheetColumns, _sheetRows, _sheetIndex;

        [MenuItem("Numeria/Preview All Worlds and Skills")]
        public static void Export()
        {
            Directory.CreateDirectory(Output);
            BeginSheet(2, (Maps.All().Length + 1) / 2);
            foreach (var map in Maps.All()) World(map);
            FinishSheet("worlds-contact");
            BeginSheet(4, (Enum.GetValues(typeof(SkillVisualKind)).Length + 3) / 4);
            foreach (SkillVisualKind kind in Enum.GetValues(typeof(SkillVisualKind))) Spell(kind);
            FinishSheet("skills-contact");
            BattleLayout();
            Coverage();
            Debug.Log("NUMERIA_WORLD_PRESENTATION=" + Output);
        }

        private static void BeginSheet(int columns, int rows)
        {
            _sheetColumns = columns; _sheetRows = rows; _sheetIndex = 0;
            _sheet = new Texture2D(columns * 480, rows * 360, TextureFormat.RGB24, false);
        }

        [MenuItem("Numeria/Preview Afterstory")]
        public static void Afterstory()
        {
            Directory.CreateDirectory(Output);
            BeginSheet(2, 1);
            World(Maps.AncientRuins()); World(Maps.VeilmoonLake());
            FinishSheet("afterstory-worlds");
            BeginSheet(3, 2);
            foreach (SkillVisualKind kind in Enum.GetValues(typeof(SkillVisualKind)))
                if (kind >= SkillVisualKind.MosaicMarch && kind <= SkillVisualKind.LotusSequence) Spell(kind);
            FinishSheet("afterstory-skills");
            bool spoken = Voice.Enabled; Voice.Enabled = false;
            try
            {
                foreach (var map in Maps.All().Where(m => m.IsAfterstory))
                {
                    var root = new GameObject("AfterstoryBattlePreview");
                    try
                    {
                        var camera = CameraFor(root.transform);
                        var progress = new Progress(); progress.ApplyMigrations();
                        progress.ActiveGrowth.Level = map.BossMinLevel;
                        var battle = root.AddComponent<BattleController>();
                        battle.Init(map.RollBossEncounter(map.BossMinLevel, new Rng(7)),
                            progress, map.Tier, map.BattleBg, _ => { });
                        foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                        {
                            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                            canvas.planeDistance = 1; canvas.sortingOrder = 20000;
                        }
                        Capture(camera, "afterstory-battle-" + map.Id, false);
                    }
                    finally { Object.DestroyImmediate(root); }
                }
                var atlasRoot = new GameObject("AtlasPreview");
                try
                {
                    var camera = CameraFor(atlasRoot.transform); var canvas = CanvasFor(atlasRoot.transform, camera);
                    var p = new Progress(); p.ApplyMigrations();
                    foreach (var map in Maps.All()) map.ClearGate(p);
                    new ExplorationHud(canvas, () => { }, () => { }, () => { }).OpenAtlas(p, _ => { }, () => { });
                    Capture(camera, "afterstory-atlas-4x3", false);
                }
                finally { Object.DestroyImmediate(atlasRoot); }
                Coverage();
            }
            finally { Voice.Enabled = spoken; }
        }
        private static void FinishSheet(string name)
        { _sheet.Apply(); File.WriteAllBytes(Path.Combine(Output, name + ".png"), _sheet.EncodeToPNG()); Object.DestroyImmediate(_sheet); }

        public static void Seasonal()
        {
            Directory.CreateDirectory(Output);
            BeginSheet(2,1);
            foreach(string id in HalloweenWorlds.Ids) World(Maps.Get(id));
            FinishSheet("seasonal-worlds");
            var kinds=Enum.GetValues(typeof(SkillVisualKind)).Cast<SkillVisualKind>().Where(HalloweenSpell.Supports).ToArray();
            for(int page=0;page*8<kinds.Length;page++)
            {
                BeginSheet(4,2);
                foreach(var kind in kinds.Skip(page*8).Take(8))Spell(kind);
                FinishSheet("seasonal-skills-"+page);
            }
            Debug.Log("Seasonal gameplay-scale worlds and all 34 signatures rendered without player saves.");
        }

        public static void EvolutionAndRestoration()
        {
            HalloweenAssetImporter.ImportDepthSpellMotifs();
            DepthEvolutions();
            SeasonalRestorations();
        }

        public static void DepthEvolutions()
        {
            Directory.CreateDirectory(Output);
            foreach(var world in DepthCatalog.Worlds)
            {
                // Each family: stage two charge/travel, then stage three charge/travel.
                BeginSheet(4,11);
                foreach(var species in world.Species)foreach(string id in ExpansionCatalog.DepthStages(species.Id).Skip(1))
                {
                    var root=new GameObject("DepthEvolutionPreview-"+id);
                    try
                    {
                        var camera=CameraFor(root.transform);var canvas=CanvasFor(root.transform,camera);
                        var bg=Ui.SpriteImg(canvas,"Battle",SpriteLib.One(Maps.Get(world.ParentId).BattleBg));Ui.Stretch(bg.rectTransform);
                        var caster=Ui.SpriteImg(canvas,"Caster",SpriteLib.PlayerBattleSprite(id));caster.preserveAspect=true;
                        Ui.PlaceCentered(caster.rectTransform,new Vector2(.22f,.28f),Vector2.zero,new Vector2(260,290));
                        var target=Ui.SpriteImg(canvas,"Target",SpriteLib.EnemyBattleSprite(species.Id));target.preserveAspect=true;
                        Ui.PlaceCentered(target.rectTransform,new Vector2(.76f,.65f),Vector2.zero,new Vector2(260,230));
                        var label=Ui.Label(canvas,"Name",id+" / stage "+(GameData.StageIndex(id)+1),30,Color.white);
                        Ui.Place(label.rectTransform,new Vector2(.5f,1),new Vector2(0,-35),new Vector2(1100,50));
                        Canvas.ForceUpdateCanvases();
                        var kind=GameData.PlayerMon(id,GameData.StageIndex(id),80).Skills.Last().Visual;
                        var sequence=SpellSequence.Create(canvas,caster.rectTransform,target.rectTransform,kind,
                            new SpellTrace{A=3,B=4,Total=7},true,id);
                        sequence.Sample(.52f);Capture(camera,"depth-"+id+"-charge",true,960,720);
                        sequence.Sample(1.05f);Capture(camera,"depth-"+id+"-travel",true,960,720);
                        sequence.Sample(1.3f);Capture(camera,"depth-"+id+"-impact",false,960,720);
                        sequence.Cancel();
                    }
                    finally{Object.DestroyImmediate(root);}
                }
                FinishSheet("depth-evolutions-"+world.Id);
            }
            Debug.Log("All 176 evolved depth forms rendered at charge, travel and impact; player slots untouched.");
        }

        public static void SeasonalRestorations()
        {
            Directory.CreateDirectory(Output);BeginSheet(3,6);
            foreach(string id in HalloweenWorlds.Ids)
            {
                var root=new GameObject("RestorationPreview");
                try
                {
                    var def=Maps.Get(id);var p=new Progress();var grid=GridMap.Parse(def.Rows);
                    foreach(var task in def.Discoveries)p.CollectDiscovery(task.Id);
                    HalloweenWorlds.Apply(grid,def,p);HalloweenScene.Build(root.transform,grid,def,p);
                    var scene=root.GetComponentInChildren<HalloweenScene>();var camera=CameraFor(root.transform);
                    camera.orthographicSize=5;
                    foreach(var task in def.Discoveries)
                    {
                        Vector3 focus=scene.RestorationFocus(task.Id);camera.transform.position=new Vector3(focus.x,focus.y,-10);
                        foreach(float t in new[]{0f,.5f,1f})
                        {scene.SampleRestoration(task.Id,t);Capture(camera,"restoration-"+task.Id+"-"+(int)(t*100),true,960,720);}
                    }
                }
                finally{Object.DestroyImmediate(root);}
            }
            FinishSheet("seasonal-restorations");
        }

        private static Camera CameraFor(Transform root)
        {
            var camera = new GameObject("PreviewCamera").AddComponent<Camera>();
            camera.transform.SetParent(root, false);
            camera.orthographic = true; camera.orthographicSize = 5.6f;
            camera.transform.position = new Vector3(10, 10, -10); camera.aspect = 4f / 3;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Ui.Hex("#203d36");
            return camera;
        }
        private static RectTransform CanvasFor(Transform root, Camera camera)
        {
            var rect = Ui.Node(root, "PreviewCanvas");
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            canvas.sortingOrder = 20000;
            var scaler = rect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = .5f;
            return rect;
        }

        private static void World(MapDef def)
        {
            var root = new GameObject("Preview-" + def.Id);
            try
            {
                var progress = new Progress { Coins = 42, CurrentMap = def.Id };
                var map = GridMap.Parse(def.Rows);
                if (def.Id == "forest") ForestScene.ApplyPassages(map, progress);
                if (def.IsAfterstory) AfterstoryJourney.Apply(map, def, progress);
                if (Array.IndexOf(HalloweenWorlds.Ids,def.Id)>=0) HalloweenWorlds.Apply(map,def,progress);
                WorldScene.Build(root.transform, map, def, progress);
                var camera = CameraFor(root.transform);
                camera.backgroundColor = Ui.Hex(def.CameraBg);
                // Focus on the authored landmark while retaining one coherent gameplay-scale view.
                Vector3 focus = new Vector3(10, 10, 0);
                for (int y = 0; y < map.Height; y++)
                    for (int x = 0; x < map.Width; x++)
                    {
                        Tile tile = map.At(x, y);
                        var p = new Vector3(x, map.Height - 1 - y, 0);
                        if (tile == Tile.Landmark) focus = p + Vector3.up * 1.5f;
                        if (tile == Tile.Chest) Actor(root.transform, MapArt.Prop(def.Theme, "treasure", 0), p, .7f);
                    }
                focus.x = Mathf.Clamp(focus.x, 7, map.Width - 8);
                focus.y = Mathf.Clamp(focus.y, 5.6f, map.Height - 6.1f);
                camera.transform.position = new Vector3(focus.x, focus.y, -10);
                // Pick a genuinely walkable cell near the focal point.
                var location = map.Spawn;
                float best = float.MaxValue;
                for (int y = 1; y < map.Height - 1; y++)
                    for (int x = 1; x < map.Width - 1; x++)
                        if (map.Walkable(x, y))
                        {
                            float distance = Vector2.Distance(new Vector2(x, map.Height - 1 - y), (Vector2)focus + Vector2.down * 2);
                            if (distance < best) { location = (x, y); best = distance; }
                        }
                var avatar=Actor(root.transform, SpriteLib.LucasExplorer(), new Vector3(location.x, map.Height - 1 - location.y, 0), 1.35f);
                root.GetComponentInChildren<HalloweenScene>()?.UpdateOcclusion(avatar.transform.position+Vector3.up*.45f,avatar.sortingOrder,1);
                if(def.Merchant!=null)Actor(root.transform, SpriteLib.One(def.Merchant.SpriteResource), new Vector3(def.Merchant.X, map.Height - 1 - def.Merchant.Y, 0), 1.4f);
                foreach (var d in def.Discoveries)
                    Actor(root.transform, SpriteLib.One("generated/Story/digit_crystal"), new Vector3(d.X, map.Height - 1 - d.Y, 0), .55f);
                var canvas = CanvasFor(root.transform, camera);
                new ExplorationHud(canvas, () => { }, () => { }, () => { }).Refresh(progress, def);
                Capture(camera, def.Id + "-4x3", true);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static SpriteRenderer Actor(Transform parent, Sprite sprite, Vector3 p, float height)
        {
            var sr = new GameObject("PreviewActor").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false); sr.transform.position = p;
            sr.sprite = sprite; sr.sortingOrder = PaintedTerrainRenderer.SortOrder(p.y) + 40;
            if (sprite != null) sr.transform.localScale = Vector3.one * (height / sprite.bounds.size.y);
            return sr;
        }

        private static void BattleLayout()
        {
            bool spoken = Voice.Enabled;
            Voice.Enabled = false;
            var root = new GameObject("BattleLayoutPreview");
            try
            {
                var camera = CameraFor(root.transform);
                var battle = root.AddComponent<BattleController>();
                battle.Init(GameData.CreateBoss("numberfly", 5, 1, new Rng(7)), new Progress(), 1, Maps.Forest().BattleBg, _ => { });
                foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = 1; canvas.sortingOrder = 20000;
                }
                Canvas.ForceUpdateCanvases();
                RectTransform Field(string name) => (RectTransform)typeof(BattleController).GetField(name,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(battle);
                var sequence = SpellSequence.Create(Field("_canvasRoot"), Field("_playerSprite"), Field("_enemySprite"),
                    SkillVisualKind.EquationFlame, new SpellTrace { A = 4, B = 6, Total = 10 }, true, "addmander");
                sequence.Sample(1.23f); Capture(camera, "battle-ui-fire", false); sequence.Cancel();
            }
            finally { Object.DestroyImmediate(root); Voice.Enabled = spoken; }
        }

        [MenuItem("Numeria/Preview Silent Peaks Battle")]
        public static void SilentPeaksBattle()
        {
            Directory.CreateDirectory(Output);
            bool spoken = Voice.Enabled;
            Voice.Enabled = false;
            var root = new GameObject("SilentPeaksBattlePreview");
            try
            {
                var camera = CameraFor(root.transform);
                // Match the reported species without reading, overwriting or importing a real save.
                var progress = new Progress { ActiveMonId = "mirrorwyrm" };
                var growth = progress.EnsureGrowth("mirrorwyrm");
                growth.Level = 43;
                growth.Stage = GameData.StageIndex("mirrorwyrm");
                var map = Maps.Mountains();
                var battle = root.AddComponent<BattleController>();
                battle.Init(GameData.CreateWild("pebblit", 43, new Rng(7)), progress, map.Tier, map.BattleBg, _ => { });
                foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = 1; canvas.sortingOrder = 20000;
                }
                Capture(camera, "silent-peaks-battle-4x3", false);
                Capture(camera, "silent-peaks-battle-16x9", false, 1920, 1080);
                Capture(camera, "silent-peaks-battle-wide", false, 2048, 992);
            }
            finally { Object.DestroyImmediate(root); Voice.Enabled = spoken; }
            Debug.Log("NUMERIA_SILENT_PEAKS_PREVIEW=" + Output);
        }

        private static void Coverage()
        {
            var rows = new System.Text.StringBuilder("Species,Family,Stage,Skill,VisualKind,Normal,Mega,Enemy\n");
            foreach (var species in GameData.Roster)
            {
                var combatant = GameData.PlayerMon(species.Id, GameData.StageIndex(species.Id), 20);
                var skill = combatant.Skills.First(s => s.Type == SkillType.Formula);
                rows.AppendLine($"{species.Name},{GameData.BaseId(species.Id)},{GameData.StageIndex(species.Id) + 1},{skill.Name},{skill.Visual},covered,covered,covered");
            }
            File.WriteAllText(Path.Combine(Output, "skill-coverage.csv"), rows.ToString());
        }

        [MenuItem("Numeria/Preview All Battle Backgrounds")]
        public static void AllBattleBackgrounds()
        {
            Directory.CreateDirectory(Output);
            bool spoken = Voice.Enabled;
            Voice.Enabled = false;
            BeginSheet(2, (Maps.All().Length + 1) / 2);
            try
            {
                foreach (var map in Maps.All())
                {
                    var root = new GameObject("BattlePreview-" + map.Id);
                    try
                    {
                        var camera = CameraFor(root.transform);
                        var battle = root.AddComponent<BattleController>();
                        battle.Init(GameData.CreateWild("pebblit", 5, new Rng(7)), new Progress(), map.Tier, map.BattleBg, _ => { });
                        foreach (var canvas in root.GetComponentsInChildren<Canvas>())
                        {
                            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                            canvas.planeDistance = 1; canvas.sortingOrder = 20000;
                        }
                        Capture(camera, "battle-" + map.Id + "-16x9", false, 1920, 1080);
                        Capture(camera, "battle-" + map.Id + "-4x3", true);
                        Capture(camera, "battle-" + map.Id + "-wide", false, 2048, 992);
                        if (map.Id == "forest")
                        {
                            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                            var state = (BattleState)typeof(BattleController).GetField("_state", flags).GetValue(battle);
                            state.Gems = 8; state.TryActivateMega(true);
                            typeof(BattleController).GetMethod("RenderAll", flags).Invoke(battle, null);
                            Capture(camera, "battle-forest-mega-4x3", false);
                            RectTransform Field(string name) => (RectTransform)typeof(BattleController).GetField(name, flags).GetValue(battle);
                            var spell = SpellSequence.Create(Field("_canvasRoot"), Field("_playerSprite"), Field("_enemySprite"),
                                SkillVisualKind.EquationFlame, new SpellTrace { A = 4, B = 6, Total = 10 }, true, "addmander", true);
                            spell.Sample(1.23f);
                            Capture(camera, "battle-forest-mega-cast-4x3", false);
                            spell.Cancel();
                        }
                    }
                    finally { Object.DestroyImmediate(root); }
                }
                FinishSheet("battle-backgrounds-contact");
                SilentPeaksBattle();
            }
            finally { Voice.Enabled = spoken; }
            Debug.Log("NUMERIA_BATTLE_BACKGROUNDS=" + Output);
        }

        private static void Spell(SkillVisualKind kind)
        {
            var root = new GameObject("Preview-" + kind);
            try
            {
                var camera = CameraFor(root.transform); var canvas = CanvasFor(root.transform, camera);
                var bg = Ui.SpriteImg(canvas, "Battle", SpriteLib.One("generated/NUMERIA_Unity_Battle_Assets/Backgrounds/Sunny_Meadow_2048x1152"));
                Ui.Stretch(bg.rectTransform);
                var species = GameData.Roster.First(s => GameData.PlayerMon(s.Id, GameData.StageIndex(s.Id), 1).Skills.Any(k => k.Visual == kind));
                var player = Ui.SpriteImg(canvas, "Caster", SpriteLib.PlayerBattleSprite(species.Id)); player.preserveAspect = true;
                Ui.PlaceCentered(player.rectTransform, new Vector2(.26f, .32f), Vector2.zero, new Vector2(270, 300));
                var target = Ui.SpriteImg(canvas, "Target", SpriteLib.EnemyBattleSprite("countipillar")); target.preserveAspect = true;
                Ui.PlaceCentered(target.rectTransform, new Vector2(.73f, .64f), Vector2.zero, new Vector2(290, 220));
                var title = Ui.Label(canvas, "Caption", species.Name + "  /  " + kind, 36, Ui.Hex("#213a31"));
                Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -35), new Vector2(1100, 50));
                Canvas.ForceUpdateCanvases();
                var trace = new SpellTrace { A = 4, B = 6, Total = 10 };
                if (kind == SkillVisualKind.Physical) trace = null;
                if (kind == SkillVisualKind.SubtractionDash) { trace.A = 10; trace.B = 4; trace.Total = 6; trace.Operation = '-'; }
                if (kind == SkillVisualKind.SymmetryBeam || kind == SkillVisualKind.PatternLeaf || kind == SkillVisualKind.GeometryPrism)
                    trace = new SpellTrace { Pattern = new[] { new PatternToken(ShapeKind.Circle, PatternColor.Blue),
                        new PatternToken(ShapeKind.Triangle, PatternColor.Gold), new PatternToken(ShapeKind.Square, PatternColor.Coral) } };
                if (kind == SkillVisualKind.SequenceSpark || kind == SkillVisualKind.TallyStone || kind == SkillVisualKind.FlyingGust)
                    trace = new SpellTrace { Numbers = new[] { 2, 4, 6, 8 } };
                var sequence = SpellSequence.Create(canvas, player.rectTransform, target.rectTransform, kind, trace, true, species.Id);
                sequence.Sample(.52f); Capture(camera, kind + "-charge", false);
                sequence.Sample(.97f); Capture(camera, kind + "-travel", kind == SkillVisualKind.DoubleBoulder || kind == SkillVisualKind.PatternLeaf);
                sequence.Sample(1.23f); Capture(camera, kind + "-impact", kind != SkillVisualKind.DoubleBoulder && kind != SkillVisualKind.PatternLeaf);
                sequence.Cancel();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void Capture(Camera camera, string name, bool sheet, int width = 1440, int height = 1080)
        {
            camera.aspect = width / (float)height;
            var rt = new RenderTexture(width, height, 24); var old = RenderTexture.active;
            var capture = new Texture2D(width, height, TextureFormat.RGB24, false);
            camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); camera.Render();
            RenderTexture.active = rt; capture.ReadPixels(new Rect(0, 0, width, height), 0, 0); capture.Apply();
            File.WriteAllBytes(Path.Combine(Output, name + ".png"), capture.EncodeToPNG());
            if (sheet)
            {
                var small = RenderTexture.GetTemporary(480, 360, 0); Graphics.Blit(rt, small); RenderTexture.active = small;
                _sheet.ReadPixels(new Rect(0, 0, 480, 360), _sheetIndex % _sheetColumns * 480,
                    (_sheetRows - 1 - _sheetIndex / _sheetColumns) * 360);
                _sheetIndex++; RenderTexture.ReleaseTemporary(small);
            }
            camera.targetTexture = null; RenderTexture.active = old;
            Object.DestroyImmediate(capture); rt.Release(); Object.DestroyImmediate(rt);
        }
    }
}
