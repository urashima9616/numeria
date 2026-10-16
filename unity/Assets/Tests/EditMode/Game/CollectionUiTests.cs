using System;
using System.IO;
using System.Linq;
using Numeria.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Numeria.Game.Tests
{
    public class CollectionUiTests
    {
        [Test] public void MechanismSymbolsAlwaysHaveTheirCanvasRenderer()
        {
            var root=new GameObject("SymbolTest",typeof(RectTransform));
            try
            {
                var symbol=root.AddComponent<DepthMechanismGraphic>();
                Assert.IsNotNull(root.GetComponent<CanvasRenderer>());
                Assert.IsNotNull(symbol.canvasRenderer);
            }
            finally {Object.DestroyImmediate(root);}
        }

        [Test] public void SelectionKeepsItsPageAndUnknownEntriesHideTheirIdentity()
        {
            var root=new GameObject("DexTest",typeof(RectTransform),typeof(Canvas));
            bool sound=Sfx.Enabled;Sfx.Enabled=false;
            try
            {
                var p=new Progress();p.ApplyMigrations();
                MenuUi.Open((RectTransform)root.transform,p,()=>{},_=>{},_=>{},_=>{});
                Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                TMP_Text Text(string name)=>root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name==name);
                Find("Tab-collection").onClick.Invoke();
                Assert.AreEqual("MATHMON FIELD GUIDE   1 / 172",Text("DexProgress").text);
                Find("DexNext").onClick.Invoke();Find("DexNext").onClick.Invoke();
                string before=Text("DexPage").text;
                var card=root.GetComponentsInChildren<Button>().First(b=>b.name.StartsWith("Dex-"));
                card.onClick.Invoke();Assert.AreEqual(before,Text("DexPage").text);
                Assert.True(Text("DexTitle").text.EndsWith("???"));
                Assert.False(root.GetComponentsInChildren<Image>().Any(i=>i.name=="DexArt"));
                Assert.False(root.GetComponentsInChildren<Button>().Any(b=>b.name=="DexListen"));
                Find("DexFilter1").onClick.Invoke();Assert.AreEqual("PAGE 1 / 1",Text("DexPage").text);
                Assert.True(Text("DexTitle").text.Contains("Addmander"));
            }
            finally{Object.DestroyImmediate(root);Sfx.Enabled=sound;}
        }

        [Test] public void EvolutionPagesStayInsideOneFamilyWithoutChangingListPageOrCollectionCount()
        {
            var root=new GameObject("DexFormsTest",typeof(RectTransform),typeof(Canvas));
            bool sound=Sfx.Enabled;Sfx.Enabled=false;
            try
            {
                var p=new Progress();p.ApplyMigrations();p.RecordCollected("sumdrake");p.RecordCollected("numberfly");
                MenuUi.Open((RectTransform)root.transform,p,()=>{},_=>{},_=>{},_=>{});
                Button Find(string name)=>root.GetComponentsInChildren<Button>().Single(b=>b.name==name);
                TMP_Text Text(string name)=>root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name==name);
                Find("Tab-collection").onClick.Invoke();
                Assert.AreEqual("MATHMON FIELD GUIDE   2 / 172",Text("DexProgress").text);
                Assert.False(root.GetComponentsInChildren<Button>().Any(b=>b.name=="Dex-sumdrake"));
                string page=Text("DexPage").text,title=Text("DexTitle").text;
                Find("DexFormNext").onClick.Invoke();
                Assert.AreEqual("FORM 2 / 3",Text("DexFormPage").text);
                Assert.AreEqual("Sumdrake",Text("DexFormName").text);
                Assert.AreEqual(title,Text("DexTitle").text);Assert.AreEqual(page,Text("DexPage").text);
                Assert.NotNull(Find("DexListen"));
                Find("DexFormNext").onClick.Invoke();
                Assert.AreEqual("FORM 3 / 3",Text("DexFormPage").text);
                Assert.AreEqual("???",Text("DexFormName").text);
                Assert.False(Find("DexFormNext").interactable);
                Assert.False(root.GetComponentsInChildren<Image>().Any(i=>i.name=="DexArt"));
                Assert.False(root.GetComponentsInChildren<Button>().Any(b=>b.name=="DexListen"));
                Find("DexFilter1").onClick.Invoke();
                Assert.AreEqual(2,root.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Dex-")));
                Find("Dex-countipillar").onClick.Invoke();
                Assert.AreEqual("Numberfly",Text("DexFormName").text);
                Assert.AreEqual(CollectionBook.FirstCollectedForm(p,"countipillar")+1,
                    int.Parse(Text("DexFormPage").text.Split(' ')[1]));
                Find("DexFilter2").onClick.Invoke();
                Assert.False(root.GetComponentsInChildren<Button>().Any(b=>b.name=="Dex-countipillar"));
                Assert.False(root.GetComponentsInChildren<Button>().Any(b=>b.name=="Dex-addmander"));
            }
            finally{Object.DestroyImmediate(root);Sfx.Enabled=sound;}
        }

        [Test] public void CollectionAndPendingGiantSurviveSaveRoundTripWithoutTouchingOldRecords()
        {
            string folder=Path.Combine(Path.GetTempPath(),"numeria-dex-test-"+Guid.NewGuid().ToString("N"));
            SaveSystem.SetStorageRootForTests(folder);
            try
            {
                var p=new Progress {Coins=2614,CurrentMap="mountains"};p.ApplyMigrations();
                p.OpenedChests.Add("forest-chest-test");p.RecordCollected("numberfly");
                p.DefeatedGiants.Add("arboracle");p.PendingGiants.Add("arboracle");p.RecordCollected("arboracle");
                SaveSystem.Save(p);var loaded=SaveSystem.Load();
                Assert.AreEqual(2614,loaded.Coins);Assert.AreEqual("mountains",loaded.CurrentMap);
                CollectionAssert.Contains(loaded.OpenedChests,"forest-chest-test");
                Assert.True(loaded.HasCollected("numberfly"));Assert.True(loaded.PendingGiants.Contains("arboracle"));
                Assert.True(loaded.ClaimGiant("arboracle"));Assert.AreEqual(12,loaded.FindGrowth("arboracle").Level);
            }
            finally {SaveSystem.ResetStorageRootForTests();if(Directory.Exists(folder))Directory.Delete(folder,true);}
        }
    }
}
