using System;
using System.IO;
using System.Reflection;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>Read-only visual fixtures: synthetic progress only, never SaveSystem.Load/Save.</summary>
    public static class DepthPreview
    {
        [MenuItem("Numeria/Preview Family Field Guide")]
        public static void ExportFamilyCollection()
        {
            bool oldSfx=Sfx.Enabled,oldVoice=Voice.Enabled;Sfx.Enabled=false;Voice.Enabled=false;
            try
            {
                Directory.CreateDirectory("/tmp/numeria-training-preview");
                var scene=typeof(LearningPreview).GetMethod("Scene",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(int stage in new[]{0,1,2})
                {
                    Action<GameObject,RectTransform> build=(root,canvas)=>
                    {
                        var p=new Numeria.Core.Progress();p.ApplyMigrations();p.RecordCollected("sumdrake");
                        MenuUi.Open(canvas,p,()=>{},_=>{},_=>{},_=>{});
                        void Click(string name)
                        {foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>())if(button.name==name){button.onClick.Invoke();break;}}
                        Click("Tab-collection");for(int i=0;i<stage;i++)Click("DexFormNext");
                    };
                    scene.Invoke(null,new object[]{"family-dex-form-"+stage,new Vector2Int(1440,1080),build});
                }
            }
            finally {Sfx.Enabled=oldSfx;Voice.Enabled=oldVoice;}
        }

        [MenuItem("Numeria/Preview Field Guide and Depths")]
        public static void Export()
        {
            NormalizeImports();
            bool oldSfx=Sfx.Enabled,oldVoice=Voice.Enabled;Sfx.Enabled=false;Voice.Enabled=false;
            try
            {
            DepthContentExporter.Export();
            Directory.CreateDirectory("/tmp/numeria-training-preview");
            Directory.CreateDirectory("/tmp/numeria-depth-preview");
            var scene=typeof(LearningPreview).GetMethod("Scene",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(bool collected in new[]{false,true})
            {
                Action<GameObject,RectTransform> build=(root,canvas)=>
                {
                    var p=new Numeria.Core.Progress();p.ApplyMigrations();
                    if(collected)foreach(var s in GameData.Roster)p.RecordCollected(s.Id);
                    MenuUi.Open(canvas,p,()=>{},_=>{},_=>{},_=>{});
                    var tabs=root.GetComponentsInChildren<UnityEngine.UI.Button>();
                    foreach(var button in tabs)if(button.name=="Tab-collection"){button.onClick.Invoke();break;}
                };
                scene.Invoke(null,new object[]{"depth-dex-"+collected,new Vector2Int(1440,1080),build});
            }
            var render=typeof(MapPreviewExporter).GetMethod("Export",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var world in DepthCatalog.Worlds)
            {
                render.Invoke(null,new object[]{Maps.Get(world.Id),"/tmp/numeria-depth-preview/"+world.Id+".png"});
                Action<GameObject,RectTransform> mechanism=(root,canvas)=>
                {
                    var host=new GameObject("MechanismFixture");host.transform.SetParent(root.transform,false);
                    var controller=host.AddComponent<MapController>();
                    var def=Maps.Get(world.Id);
                    var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                    typeof(MapController).GetField("_def",flags).SetValue(controller,def);
                    typeof(MapController).GetField("_hudCanvasRoot",flags).SetValue(controller,canvas);
                    typeof(MapController).GetField("_voice",flags).SetValue(controller,host.AddComponent<Voice>());
                    var routine=(System.Collections.IEnumerator)typeof(MapController)
                        .GetMethod("DepthMechanismRoutine",flags).Invoke(controller,new object[]{def.Discoveries[2]});
                    // Only create the view, stopping at its first input wait. Never finish or save.
                    routine.MoveNext();
                };
                scene.Invoke(null,new object[]{"depth-puzzle-"+world.Id,new Vector2Int(1440,1080),mechanism});
                Action<GameObject,RectTransform> portraits=(root,canvas)=>
                {
                    var title=Ui.Label(canvas,"World",world.Name.ToUpperInvariant(),40,Color.white);
                    Ui.PlaceCentered(title.rectTransform,new Vector2(.5f,.96f),Vector2.zero,new Vector2(1500,60));
                    for(int i=0;i<world.Species.Length;i++)
                    {
                        var mon=world.Species[i];var anchor=new Vector2((i%4+.5f)/4,.79f-i/4*.29f);
                        var art=Ui.SpriteImg(canvas,mon.Id,SpriteLib.LargeIcon(mon.Id));art.preserveAspect=true;
                        Ui.PlaceCentered(art.rectTransform,anchor,new Vector2(0,12),new Vector2(285,280));
                        var label=Ui.Label(canvas,"Name",mon.Name+(mon.Giant?" (GIANT)":""),27,Color.white);
                        Ui.PlaceCentered(label.rectTransform,anchor,new Vector2(0,-150),new Vector2(375,36));
                    }
                };
                scene.Invoke(null,new object[]{"depth-roster-"+world.Id,new Vector2Int(1440,1080),portraits});
                Action<GameObject,RectTransform> battle=(root,canvas)=>
                {
                    var p=new Numeria.Core.Progress();p.ApplyMigrations();p.ActiveGrowth.Level=world.Level;
                    root.AddComponent<BattleController>().Init(Maps.Get(world.Id).RollBossEncounter(world.Level,new Rng(73)),
                        p,Maps.Get(world.Id).Tier,Maps.Get(world.Id).BattleBg,_=>{});
                };
                scene.Invoke(null,new object[]{"depth-giant-"+world.Id,new Vector2Int(1440,1080),battle});
            }
            Debug.Log("Depth visual fixtures exported without player save access.");
            }
            finally { Sfx.Enabled=oldSfx;Voice.Enabled=oldVoice; }
        }

        [MenuItem("Numeria/Normalize Depth Sprite Imports")]
        public static void NormalizeImports()
        {
            void Limit(string path,int size)
            {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null && importer.maxTextureSize!=size)
                {importer.maxTextureSize=size;importer.SaveAndReimport();}
            }
            foreach(var world in DepthCatalog.Worlds)foreach(var mon in world.Species)
                Limit("Assets/Resources/generated/"+mon.Id+"_large_icon.png",512);
            Limit("Assets/Resources/generated/UI/field_guide.png",128);
        }
    }
}
