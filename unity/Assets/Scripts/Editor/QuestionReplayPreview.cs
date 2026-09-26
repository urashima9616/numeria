using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Numeria.Core;
using Numeria.Game;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    public static class QuestionReplayPreview
    {
        public static void Export()
        {
            Directory.CreateDirectory("/tmp/numeria-training-preview");
            DepthContentExporter.Export();
            bool voice=Voice.Enabled,sfx=Sfx.Enabled;Voice.Enabled=false;Sfx.Enabled=false;
            try
            {
                var scene=typeof(LearningPreview).GetMethod("Scene",BindingFlags.Static|BindingFlags.NonPublic);
                foreach(var size in new[]{new Vector2Int(1440,1080),new Vector2Int(1920,1080)})
                foreach(string type in new[]{"story","clock","legacy","depth"})
                {
                    Action<GameObject,RectTransform> build=(root,canvas)=>
                    {
                        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                        if(type=="depth")
                        {
                            var host=new GameObject("DepthFixture");host.transform.SetParent(root.transform,false);
                            var controller=host.AddComponent<MapController>();var def=Maps.Get("verdant_vault");
                            typeof(MapController).GetField("_def",flags).SetValue(controller,def);
                            typeof(MapController).GetField("_hudCanvasRoot",flags).SetValue(controller,canvas);
                            typeof(MapController).GetField("_voice",flags).SetValue(controller,host.AddComponent<Voice>());
                            ((IEnumerator)typeof(MapController).GetMethod("DepthMechanismRoutine",flags).Invoke(controller,new object[]{def.Discoveries[0]})).MoveNext();
                        }
                        else
                        {
                            var puzzles=new PuzzleUi(null,canvas,new Rng(17),_=>{});
                            if(type=="legacy"){puzzles.RunPattern(_=>{},2).MoveNext();return;}
                            var settings=new MathSettings{Grade=type=="clock"?3:1};
                            var q=MathCurriculum.Generate(settings,new Rng(7),type=="clock"?LearningKind.Time:LearningKind.WordProblem,8);
                            ((IEnumerator)typeof(PuzzleUi).GetMethod("RunLearningQuestion",flags).Invoke(puzzles,new object[]{q,new Action<bool>(_=>{})})).MoveNext();
                        }
                    };
                    scene.Invoke(null,new object[]{"replay-"+type+"-"+size.x,size,build});
                }
            }
            finally{Voice.Enabled=voice;Sfx.Enabled=sfx;}
        }
    }
}
