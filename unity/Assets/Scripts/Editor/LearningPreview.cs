using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Progress = Numeria.Core.Progress;

namespace Numeria.Editor
{
    public static class LearningPreview
    {
        private const string Output = "/tmp/numeria-training-preview";
        [MenuItem("Numeria/Preview Training and Learning")]
        public static void Export()
        {
            foreach (string resource in TrainerSystem.Ids.Select(TrainerSystem.Portrait).Concat(new[] {"generated/Heroes/lucas_battle","generated/UI/tackle"}))
            {
                string assetPath="Assets/Resources/"+resource+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(assetPath);
                int limit=resource.EndsWith("/tackle")?256:512;
                if (importer!=null && importer.maxTextureSize!=limit) { importer.maxTextureSize=limit; importer.SaveAndReimport(); }
            }
            Directory.CreateDirectory(Output); ExportSpeech();
            Voice.Enabled = false; Sfx.Enabled = false;
            foreach (var size in new[] { new Vector2Int(1440,1080),new Vector2Int(1920,1080) })
            {
                Scene("battle-"+size.x,size,(root,canvas)=>
                {
                    var p = new Progress(); p.ApplyMigrations(); p.ActiveGrowth.Level=25;
                    var battle = root.AddComponent<BattleController>();
                    battle.Init(TrainerSystem.Boost(GameData.CreateWild("pebblit",25,new Rng(3)),"fern",30),p,2,Maps.Get("mountains").BattleBg,_=>{});
                });
            }
            Scene("system",new Vector2Int(1440,1080),(root,canvas)=>
            {
                MenuUi.Open(canvas,new Progress(),()=>{},_=>{},_=>{},_=>{});
                root.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="SYSTEM").onClick.Invoke();
            });
            Scene("wild-battle",new Vector2Int(1440,1080),(root,canvas)=>
            {
                root.AddComponent<BattleController>().Init(GameData.CreateWild("countipillar",5,new Rng(11)),new Progress(),1,Maps.Get("forest").BattleBg,_=>{});
            });
            foreach (bool release in new[] {false,true})
                Scene(release?"release-confirm":"equipment-transfer",new Vector2Int(1440,1080),(root,canvas)=>
                {
                    var p=new Progress(); p.Catch("countipillar");p.AddAccessory("a","Power Acorn",3,1);p.EquipAccessory("a","countipillar");
                    var menu=Activator.CreateInstance(typeof(MenuUi),BindingFlags.Instance|BindingFlags.NonPublic,null,
                        new object[]{canvas,p,new Action(()=>{}),new Action<bool>(_=>{}),new Action<string>(_=>{}),new Action<Progress>(_=>{})},null);
                    void Call(string name,params object[] args)=>typeof(MenuUi).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(menu,args);
                    Call("Build");
                    if(release) {Call("ShowConversionPicker","countipillar");root.GetComponentsInChildren<Button>().First(b=>b.name=="Coins").onClick.Invoke();}
                    else {Call("ShowAccessoryPicker","addmander",null);root.GetComponentsInChildren<Button>().First(b=>b.transform.parent.name=="Pick-a").onClick.Invoke();}
                });
            Scene("team",new Vector2Int(1440,1080),(root,canvas)=>
            {
                var p=new Progress(); p.Catch("countipillar"); p.Catch("pebblit");
                MenuUi.Open(canvas,p,()=>{},_=>{},_=>{},_=>{});
            });
            foreach(var kind in new[] {LearningKind.PlaceValue,LearningKind.Time,LearningKind.Fractions,LearningKind.Measurement,LearningKind.Data,LearningKind.Arithmetic})
            {
                Scene("puzzle-"+kind,new Vector2Int(1440,1080),(root,canvas)=>
                {
                    var settings=new MathSettings {Grade=kind==LearningKind.PlaceValue?0:kind==LearningKind.Arithmetic?5:2};
                    var puzzles=new PuzzleUi(root.AddComponent<PreviewHost>(),canvas,new Rng(17),_=>{},null,settings,4);
                    var routine=(IEnumerator)typeof(PuzzleUi).GetMethod("RunLearning",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(puzzles,new object[] {new Action<bool>(_=>{}),(LearningKind?)kind});
                    routine.MoveNext();
                });
            }
            Scene("trainers",new Vector2Int(1600,1200),(root,canvas)=>
            {
                for(int i=0;i<TrainerSystem.Ids.Length;i++)
                {
                    var image=Ui.SpriteImg(canvas,TrainerSystem.Ids[i],SpriteLib.One(TrainerSystem.Portrait(TrainerSystem.Ids[i]))); image.preserveAspect=true;
                    Ui.PlaceCentered(image.rectTransform,new Vector2((i%5+.5f)/5,1-(i/5+.5f)/4),new Vector2(0,12),new Vector2(250,260));
                    var label=Ui.Label(canvas,"Name",TrainerSystem.Name(TrainerSystem.Ids[i]),24,Color.white);
                    Ui.PlaceCentered(label.rectTransform,new Vector2((i%5+.5f)/5,1-(i/5+.5f)/4),new Vector2(0,-126),new Vector2(250,32));
                }
            });
            Debug.Log("TRAINING_PREVIEW="+Output);
        }

        private static void Scene(string name,Vector2Int size,Action<GameObject,RectTransform> build)
        {
            var root=new GameObject("Preview"); var texture=new RenderTexture(size.x,size.y,24);
            try
            {
                var camera=new GameObject("Camera").AddComponent<Camera>(); camera.transform.SetParent(root.transform); camera.orthographic=true; camera.targetTexture=texture; camera.aspect=size.x/(float)size.y; camera.backgroundColor=Ui.Hex("#243a39");
                var canvasRoot=Ui.Node(root.transform,"Canvas"); canvasRoot.gameObject.AddComponent<Canvas>();
                var scaler=canvasRoot.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,1200); scaler.matchWidthOrHeight=.5f;
                build(root,canvasRoot);
                foreach(var canvas in root.GetComponentsInChildren<Canvas>()) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; }
                Canvas.ForceUpdateCanvases();
                foreach(var stage in root.GetComponentsInChildren<BattleStageLayout>()) stage.Apply();
                Canvas.ForceUpdateCanvases(); camera.Render();
                var old=RenderTexture.active; RenderTexture.active=texture; var png=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                png.ReadPixels(new Rect(0,0,size.x,size.y),0,0); png.Apply(); File.WriteAllBytes(Path.Combine(Output,name+".png"),png.EncodeToPNG()); RenderTexture.active=old; Object.DestroyImmediate(png);
            }
            finally { Object.DestroyImmediate(root); texture.Release(); Object.DestroyImmediate(texture); }
        }

        public static void ExportSpeech()
        {
            var lines=new HashSet<string> {"A trainer challenges you!","Defend! Take half damage this turn.","Great job!","Let's try again!"};
            var rng=new Rng(731);
            for(int grade=0;grade<6;grade++) for(int mode=0;mode<3;mode++) for(int chapter=1;chapter<=8;chapter++)
            foreach(var kind in MathCurriculum.Available(grade).Concat(new[]{LearningKind.Double,LearningKind.Subtraction,LearningKind.ChainSum})) for(int i=0;i<60;i++)
            {
                var s=new MathSettings {Grade=grade}; s.Mode=(MathMode)mode; var q=MathCurriculum.Generate(s,rng,kind,chapter);
                lines.Add(q.Prompt); lines.Add(q.Hint); foreach(var line in LearningSpeech.Lines(q)) lines.Add(line);
            }
            File.WriteAllLines(Path.GetFullPath(Path.Combine(Application.dataPath,"../../tools/learning-voice-lines.txt")),lines.OrderBy(s=>s));
        }

        public static void ExportCurriculum()
        {
            Directory.CreateDirectory(Output); ExportSpeech();
            bool voice=Voice.Enabled,sound=Sfx.Enabled;
            try
            {
                Voice.Enabled=false;Sfx.Enabled=false;
                for(int grade=0;grade<6;grade++) foreach(var kind in MathCurriculum.Available(grade))
                {
                    var settings=new MathSettings {Grade=grade};settings.Mode=MathMode.Challenging;
                    var question=MathCurriculum.Generate(settings,new Rng((uint)(13+grade*71+(int)kind*3)),kind,8);
                    foreach(var size in grade==0||grade==5?new[]{new Vector2Int(1440,1080),new Vector2Int(1920,1080)}:new[]{new Vector2Int(1440,1080)})
                    Scene("grade-"+grade+"-"+kind+(size.x==1920?"-wide":""),size,(root,canvas)=>
                    {
                        var puzzles=new PuzzleUi(root.AddComponent<PreviewHost>(),canvas,new Rng(7),_=>{},null,settings,8);
                        var routine=(IEnumerator)typeof(PuzzleUi).GetMethod("RunLearningQuestion",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(puzzles,new object[]{question,new Action<bool>(_=>{})});
                        routine.MoveNext();
                    });
                }
            }
            finally {Voice.Enabled=voice;Sfx.Enabled=sound;}
            Debug.Log("CURRICULUM_PREVIEW="+Output);
        }
    }
    public sealed class PreviewHost : MonoBehaviour { }
}
