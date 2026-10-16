using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    public static class HalloweenPreview
    {
        public static void GradeMechanisms()
        {
            HalloweenContentExporter.Export();
            Directory.CreateDirectory("/tmp/numeria-training-preview");
            bool voice=Voice.Enabled,sound=Sfx.Enabled;Voice.Enabled=false;Sfx.Enabled=false;
            try
            {
                var render=typeof(LearningPreview).GetMethod("Scene",BindingFlags.Static|BindingFlags.NonPublic);
                for(int grade=0;grade<=5;grade++)foreach(MathMode mode in new[]{MathMode.FeelGood,MathMode.Normal,MathMode.Challenging})
                foreach(int kind in new[]{1,2,3})foreach(var size in new[]{new Vector2Int(1440,1080),new Vector2Int(1920,1080)})
                {
                    var def=Maps.Get(HalloweenWorlds.Ids[kind==3?1:0]);var discovery=def.Discoveries[kind==3?0:kind];
                    Action<GameObject,RectTransform> puzzle=(root,canvas)=> {
                        var host=new GameObject("GradeMechanismFixture");host.transform.SetParent(root.transform,false);
                        var controller=host.AddComponent<MapController>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                        void Field(string name,object value)=>typeof(MapController).GetField(name,flags).SetValue(controller,value);
                        var p=new Numeria.Core.Progress();p.ApplyMigrations();p.Learning.Grade=grade;p.Learning.Mode=mode;
                        Field("_def",def);Field("_hudCanvasRoot",canvas);Field("_progress",p);Field("_rng",new Rng(41));Field("_voice",host.AddComponent<Voice>());
                        var routine=(System.Collections.IEnumerator)typeof(MapController).GetMethod("HalloweenMechanismRoutine",flags).Invoke(controller,new object[]{discovery});
                        routine.MoveNext();
                    };
                    render.Invoke(null,new object[]{"halloween-grade-"+grade+"-"+mode+"-"+kind+"-"+size.x,size,puzzle});
                }
            }
            finally{Voice.Enabled=voice;Sfx.Enabled=sound;}
            Debug.Log("108 grade/mode/aspect mechanism previews exported without accessing player slots.");
        }
        public static void FinalPass()
        {
            HalloweenAssetImporter.ImportSpellMotifs();
            HalloweenContentExporter.Export();
            WorldPresentationPreview.Seasonal();
            Export();
        }
        // Synthetic fixtures only. Never resume the input-wait coroutine (which would award/save).
        public static void Export()
        {
            Directory.CreateDirectory("/tmp/numeria-training-preview");
            bool voice=Voice.Enabled,sound=Sfx.Enabled;Voice.Enabled=false;Sfx.Enabled=false;
            try
            {
                var scene=typeof(LearningPreview).GetMethod("Scene",BindingFlags.Static|BindingFlags.NonPublic);
                var mapRender=typeof(MapPreviewExporter).GetMethod("Export",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(string id in HalloweenWorlds.Ids)
                {
                    var def=Maps.Get(id);
                    mapRender.Invoke(null,new object[]{def,"/tmp/numeria-training-preview/halloween-map-"+id+".png"});
                    foreach(var size in new[]{new Vector2Int(1440,1080),new Vector2Int(1920,1080)})
                    {
                        Action<GameObject,RectTransform> battle=(root,canvas)=> {
                            var p=new Numeria.Core.Progress();p.ApplyMigrations();p.ActiveGrowth.Level=84;
                            root.AddComponent<BattleController>().Init(def.RollBossEncounter(84,new Rng(73)),p,def.Tier,def.BattleBg,_=>{});
                        };
                        scene.Invoke(null,new object[]{"halloween-battle-"+id+"-"+size.x,size,battle});
                        foreach(var discovery in def.Discoveries)
                        {
                            Action<GameObject,RectTransform> puzzle=(root,canvas)=> {
                                var host=new GameObject("PuzzleFixture");host.transform.SetParent(root.transform,false);
                                var controller=host.AddComponent<MapController>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                                void Field(string name,object value)=>typeof(MapController).GetField(name,flags).SetValue(controller,value);
                                var p=new Numeria.Core.Progress();p.ApplyMigrations();p.Learning.Grade=1;
                                Field("_def",def);Field("_hudCanvasRoot",canvas);Field("_progress",p);Field("_rng",new Rng(41));Field("_voice",host.AddComponent<Voice>());
                                var routine=(System.Collections.IEnumerator)typeof(MapController).GetMethod("HalloweenMechanismRoutine",flags).Invoke(controller,new object[]{discovery});
                                routine.MoveNext();
                            };
                            scene.Invoke(null,new object[]{"halloween-puzzle-"+discovery.Id+"-"+size.x,size,puzzle});
                        }
                    }
                }
                var added=ExpansionCatalog.Families.SelectMany(f=>f.Ids).Concat(DepthCatalog.Worlds.SelectMany(w=>w.Species).SelectMany(s=>ExpansionCatalog.DepthStages(s.Id).Skip(1))).ToArray();
                foreach(var size in new[]{new Vector2Int(1440,1080),new Vector2Int(1920,1080)})foreach(bool unknown in new[]{false,true})
                {
                    Action<GameObject,RectTransform> dex=(root,canvas)=> {
                        var p=new Numeria.Core.Progress();p.ApplyMigrations();p.RecordCollected("pipkin");p.RecordCollected("vinejack");
                        MenuUi.Open(canvas,p,()=>{},_=>{},_=>{},_=>{});
                        void Click(string name)=>root.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name==name).onClick.Invoke();
                        Click("Tab-collection");Click("DexFilter1");Click("Dex-pipkin");Click("DexFormNext");
                        if(unknown)Click("DexFormNext");
                    };
                    scene.Invoke(null,new object[]{"halloween-dex-"+(unknown?"unknown":"known")+"-"+size.x,size,dex});
                }
                for(int page=0;page*20<added.Length;page++)
                {
                    var ids=added.Skip(page*20).Take(20).ToArray();
                    Action<GameObject,RectTransform> portraits=(root,canvas)=> {
                        for(int i=0;i<ids.Length;i++)
                        {
                            var anchor=new Vector2((i%5+.5f)/5,1-(i/5+.5f)/4);
                            var art=Ui.SpriteImg(canvas,ids[i],SpriteLib.LargeIcon(ids[i]));art.preserveAspect=true;
                            Ui.PlaceCentered(art.rectTransform,anchor,new Vector2(0,18),new Vector2(260,245));
                            var text=Ui.Label(canvas,"Name",GameData.ById(ids[i]).Name,22,Color.white);
                            Ui.PlaceCentered(text.rectTransform,anchor,new Vector2(0,-114),new Vector2(310,32));
                        }
                    };
                    scene.Invoke(null,new object[]{"halloween-roster-"+page,new Vector2Int(1600,1200),portraits});
                }
            }
            finally{Voice.Enabled=voice;Sfx.Enabled=sound;}
            Debug.Log("Halloween runtime previews exported; no player slots accessed.");
        }
        public static void ExportBaseRosters()
        {
            Directory.CreateDirectory("/tmp/numeria-training-preview");
            var scene=typeof(LearningPreview).GetMethod("Scene",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var world in DepthCatalog.Worlds)
            {
                Action<GameObject,RectTransform> build=(root,canvas)=> {
                    for(int i=0;i<world.Species.Length;i++)
                    {
                        var mon=world.Species[i];var anchor=new Vector2((i%4+.5f)/4,.83f-i/4*.30f);
                        var art=Ui.SpriteImg(canvas,mon.Id,SpriteLib.LargeIcon(mon.Id));art.preserveAspect=true;
                        Ui.PlaceCentered(art.rectTransform,anchor,Vector2.zero,new Vector2(285,250));
                        var label=Ui.Label(canvas,"Name",mon.Name,25,Color.white);
                        Ui.PlaceCentered(label.rectTransform,anchor,new Vector2(0,-140),new Vector2(360,38));
                    }
                };
                scene.Invoke(null,new object[]{"halloween-reference-"+world.Id,new Vector2Int(1440,1080),build});
            }
        }
    }
}
