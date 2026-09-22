using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Numeria.Game.Tests
{
    public class TrainingUiTests
    {
        private GameObject _root;
        private string _saveRoot;
        private bool _sound, _voice;
        [SetUp] public void Setup()
        {
            _root=new GameObject("TrainingUiTest",typeof(RectTransform)); _root.AddComponent<Canvas>();
            _sound=Sfx.Enabled; _voice=Voice.Enabled; Sfx.Enabled=false; Voice.Enabled=false;
            _saveRoot=Path.Combine(Path.GetTempPath(),"numeria-training-"+Guid.NewGuid().ToString("N")); SaveSystem.SetStorageRootForTests(_saveRoot);
        }
        [TearDown] public void Cleanup()
        {
            Object.DestroyImmediate(_root); SaveSystem.ResetStorageRootForTests();
            if(Directory.Exists(_saveRoot))Directory.Delete(_saveRoot,true);
            Sfx.Enabled=_sound; Voice.Enabled=_voice;
        }
        private object Menu(Progress p)
        {
            return Activator.CreateInstance(typeof(MenuUi),BindingFlags.Instance|BindingFlags.NonPublic,null,
                new object[] {(RectTransform)_root.transform,p,new Action(()=>{}),new Action<bool>(_=>{}),new Action<string>(_=>{}),new Action<Progress>(_=>{})},null);
        }
        private static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,args);

        [Test] public void LearningDiagramsProduceVisibleMeshes()
        {
            foreach(var kind in new[] {LearningKind.Time,LearningKind.Fractions,LearningKind.Measurement,LearningKind.Data,LearningKind.Geometry,LearningKind.PlaceValue})
            {
                var q=MathCurriculum.Generate(new MathSettings {Grade=kind==LearningKind.PlaceValue?0:1},new Rng(8),kind,6);
                var node=Ui.Node(_root.transform,"Diagram"); var diagram=node.gameObject.AddComponent<LearningDiagram>(); diagram.Question=q;
                Assert.NotNull(diagram.GetComponent<CanvasRenderer>(),kind.ToString());
                using(var helper=new VertexHelper())
                {
                    typeof(LearningDiagram).GetMethod("OnPopulateMesh",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly).Invoke(diagram,new object[]{helper});
                    Assert.GreaterOrEqual(helper.currentVertCount,6,kind.ToString());
                }
            }
        }

        [Test] public void SettingsSliderPersistsPerGradeModesWithoutTouchingAdventure()
        {
            var p=new Progress {Coins=876,CurrentMap="mountains"}; p.Catch("countipillar");
            var menu=Menu(p); Call(menu,"Build"); Call(menu,"ShowSettings");
            var slider=_root.GetComponentInChildren<Slider>(); Assert.NotNull(slider); slider.value=4;
            var mode=_root.GetComponentsInChildren<Button>().First(b=>b.name=="Mode-2"); mode.onClick.Invoke();
            slider.value=1; Assert.AreEqual(MathMode.Normal,p.Learning.Mode); slider.value=4;
            Assert.AreEqual(MathMode.Challenging,p.Learning.Mode);
            var loaded=SaveSystem.Load(); Assert.AreEqual(4,loaded.Learning.Grade); Assert.AreEqual(876,loaded.Coins);
            Assert.True(loaded.Owns("countipillar")); Assert.AreEqual("mountains",loaded.CurrentMap);
        }

        [Test] public void ReleaseDoesNothingUntilTheSecondConfirmationIsArmed()
        {
            var p=new Progress(); p.Catch("countipillar"); var menu=Menu(p); Call(menu,"Build"); Call(menu,"ShowConversionPicker","countipillar");
            var coins=_root.GetComponentsInChildren<Button>().First(b=>b.name=="Coins"); coins.onClick.Invoke();
            Assert.True(p.Owns("countipillar")); Assert.AreEqual(0,p.Coins);
            var confirm=_root.GetComponentsInChildren<Button>().First(b=>b.name=="Confirm"); Assert.False(confirm.interactable);
            confirm.onClick.Invoke(); Assert.True(p.Owns("countipillar"));
            // Final mutation itself is covered by Core conversion tests; cancelling/removing this overlay is safe.
            Assert.NotNull(confirm.GetComponent<ConfirmationDelay>());
        }

        [Test] public void AccessoryPickerIncludesEquippedItemsAndOffersTransfer()
        {
            var p=new Progress(); p.Catch("countipillar"); p.AddAccessory("a","Power Acorn",2,0); p.EquipAccessory("a","countipillar");
            var menu=Menu(p); Call(menu,"Build"); Call(menu,"ShowAccessoryPicker","addmander",null);
            var button=_root.GetComponentsInChildren<Button>().First(b=>b.transform.parent.name=="Pick-a");
            StringAssert.Contains("Countipillar",button.GetComponentInChildren<TMP_Text>().text);
            button.onClick.Invoke(); Assert.AreEqual("countipillar",p.Accessories[0].EquippedToBaseId);
            Assert.True(_root.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<TMP_Text>()?.text=="TRANSFER"));
        }

        [Test] public void NewArtworkAndSpeechArePresentAndTransparent()
        {
            var resources=TrainerSystem.Ids.Select(TrainerSystem.Portrait).Concat(new[]{"generated/Heroes/lucas_battle","generated/UI/tackle"});
            foreach(string path in resources)
            {
                var sprite=Resources.Load<Sprite>(path); Assert.NotNull(sprite,path);
                Assert.LessOrEqual(sprite.texture.width,path.EndsWith("/tackle")?256:512,path);
                var texture=new Texture2D(2,2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"Resources",path+".png")));
                Assert.AreEqual(0,texture.GetPixel(0,0).a,path); Assert.AreEqual(0,texture.GetPixel(texture.width-1,texture.height-1).a,path); Object.DestroyImmediate(texture);
            }
            string manifest=Path.GetFullPath(Path.Combine(Application.dataPath,"../../tools/learning-voice-lines.txt"));
            foreach(string line in File.ReadAllLines(manifest))
            {
                var clip=Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line)); Assert.NotNull(clip,line); Assert.Greater(clip.length,.05f,line);
            }
        }

        [Test] public void OldSaveMigrationAddsOnlySettingsAndKeepsCaughtBattleStats()
        {
            var p=new Progress {SaveVersion=11,Coins=2228,CurrentMap="mountains"};
            p.AddCaught(GameData.CreateWild("countipillar",32,new Rng(9))); p.OpenedChests.Add("kept-chest");
            p.Learning=null; var loaded=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(p)); loaded.ApplyMigrations();
            Assert.AreEqual(Progress.CurrentSaveVersion,loaded.SaveVersion); Assert.NotNull(loaded.Learning);
            Assert.AreEqual(2228,loaded.Coins); CollectionAssert.Contains(loaded.OpenedChests,"kept-chest");
            Assert.AreEqual(JsonUtility.ToJson(p.PlayerCombatant("countipillar")),JsonUtility.ToJson(loaded.PlayerCombatant("countipillar")));
        }
    }
}
