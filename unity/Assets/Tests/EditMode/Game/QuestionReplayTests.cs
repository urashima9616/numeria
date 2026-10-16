using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Numeria.Core;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Numeria.Game.Tests
{
    public class QuestionReplayTests
    {
        private GameObject _root;
        private bool _sfx,_voice;
        [SetUp] public void Setup()
        {_root=new GameObject("ReplayTest",typeof(RectTransform),typeof(Canvas));_sfx=Sfx.Enabled;_voice=Voice.Enabled;Sfx.Enabled=false;Voice.Enabled=false;}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(_root);Sfx.Enabled=_sfx;Voice.Enabled=_voice;}
        private Button Replay()=>_root.GetComponentsInChildren<Button>().Single(b=>b.name=="ReplayQuestion");
        private IEnumerator Learning(PuzzleUi puzzles,LearningQuestion q)=>
            (IEnumerator)typeof(PuzzleUi).GetMethod("RunLearningQuestion",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(puzzles,new object[]{q,new Action<bool>(_=>{})});
        private void Clear(){foreach(Transform child in _root.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);}
        private static void StartQuestion(IEnumerator routine)
        {
            while(routine.MoveNext())
            {
                if(routine.Current==null||routine.Current is YieldInstruction||routine.Current is CustomYieldInstruction)return;
                if(routine.Current is IEnumerator nested){StartQuestion(nested);return;}
            }
        }

        [Test] public void EveryGradeAndQuestionKindCanReplayItsOriginalStatementWithoutChangingState()
        {
            for(int grade=0;grade<6;grade++)foreach(var kind in MathCurriculum.Available(grade))
            {
                Clear();var settings=new MathSettings{Grade=grade};settings.Mode=MathMode.Challenging;
                var q=MathCurriculum.Generate(settings,new Rng((uint)(181+grade*31+(int)kind)),kind,8);
                string[] automatic=null,manual=null;int completions=0;
                var puzzles=new PuzzleUi(null,(RectTransform)_root.transform,new Rng(1),lines=>automatic=lines,
                    _=>completions++,settings,8,lines=>manual=lines);
                var routine=Learning(puzzles,q);Assert.True(routine.MoveNext());
                var replay=Replay();var before=q.Display;var options=q.Options.ToArray();
                for(int repeat=0;repeat<3;repeat++)replay.onClick.Invoke();
                CollectionAssert.AreEqual(LearningSpeech.Lines(q),manual,$"grade {grade} {kind}");
                CollectionAssert.AreEqual(automatic,manual);Assert.AreEqual(before,q.Display);
                CollectionAssert.AreEqual(options,q.Options);Assert.AreEqual(0,completions);
                Assert.False(Voice.Enabled,"Manual Listen must not change the automatic narration setting.");
                Assert.NotNull(replay.GetComponentInChildren<QuestionSpeakerGraphic>().canvasRenderer);
            }
        }

        [Test] public void ReplayAfterWrongAnswerRestoresQuestionRatherThanFeedbackAndLocksAfterCompletion()
        {
            var settings=new MathSettings{Grade=1};var q=MathCurriculum.Generate(settings,new Rng(8),LearningKind.WordProblem,3);
            string[] spoken=null;var puzzles=new PuzzleUi(null,(RectTransform)_root.transform,new Rng(1),lines=>spoken=lines,null,settings);
            var routine=Learning(puzzles,q);routine.MoveNext();
            Button Answer(int index)=>_root.GetComponentsInChildren<Button>().Single(b=>b.name=="Answer"+index);
            Answer((q.CorrectIndex+1)%q.Options.Length).onClick.Invoke();
            Assert.AreEqual("Let's try again!",spoken[0]);
            Replay().onClick.Invoke();CollectionAssert.AreEqual(LearningSpeech.Lines(q),spoken);
            Answer(q.CorrectIndex).onClick.Invoke();Assert.False(Replay().interactable);
            var feedback=spoken;Replay().onClick.Invoke();Assert.AreSame(feedback,spoken);
        }

        [Test] public void ListeningDoesNotClearPartiallySelectedNumbers()
        {
            var settings=new MathSettings();var q=MathCurriculum.Generate(settings,new Rng(9),LearningKind.Bonds,4);
            var puzzles=new PuzzleUi(null,(RectTransform)_root.transform,new Rng(1),_=>{},null,settings);
            Learning(puzzles,q).MoveNext();
            var first=_root.GetComponentsInChildren<Button>().Single(b=>b.name=="Answer0");first.onClick.Invoke();
            var selection=_root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Selection");string before=selection.text;
            Replay().onClick.Invoke();Assert.AreEqual(before,selection.text);Assert.False(first.interactable);
        }

        [Test] public void EveryLegacyMapQuestionAlsoHasReplay()
        {
            foreach(MapPuzzleKind kind in Enum.GetValues(typeof(MapPuzzleKind)))
            {
                Clear();string[] spoken=null;var puzzles=new PuzzleUi(null,(RectTransform)_root.transform,new Rng(42),lines=>spoken=lines);
                StartQuestion(puzzles.RunPuzzleKind(kind,_=>{},1));var original=spoken;
                Assert.NotNull(original,kind.ToString());Replay().onClick.Invoke();CollectionAssert.AreEqual(original,spoken,kind.ToString());
                foreach(string line in spoken)Assert.NotNull(Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line)),line);
            }
        }

        [Test] public void GeneratedQuestionStatementsHaveOfflineAudioAcrossGradesModesAndChapters()
        {
            var lines=new HashSet<string>();var rng=new Rng(98113);
            for(int grade=0;grade<6;grade++)for(int mode=0;mode<3;mode++)foreach(int chapter in new[]{1,4,8})
            foreach(var kind in MathCurriculum.Available(grade))for(int sample=0;sample<8;sample++)
            {
                var settings=new MathSettings{Grade=grade};settings.Mode=(MathMode)mode;
                foreach(var line in LearningSpeech.Lines(MathCurriculum.Generate(settings,rng,kind,chapter)))lines.Add(line);
            }
            foreach(string line in lines)
            {var clip=Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line));Assert.NotNull(clip,line);Assert.Greater(clip.length,.05f,line);}
        }

        [Test] public void EveryDepthStageNarratesVisibleTargetsWithoutChangingThePuzzle()
        {
            for(int kind=0;kind<8;kind++)for(int stage=0;stage<3;stage++)
            {
                var p=new DepthMechanism(kind,stage);var cells=p.Cells.ToArray();var goals=p.Goal.ToArray();
                var lines=p.Narration();Assert.AreEqual(p.Instruction,lines[0]);
                CollectionAssert.AreEqual(cells,p.Cells);CollectionAssert.AreEqual(goals,p.Goal);Assert.False(p.Solved);
                foreach(string line in lines)
                {var clip=Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line));Assert.NotNull(clip,line);Assert.Greater(clip.length,.05f,line);}
                if(kind==0)CollectionAssert.AreEqual(goals.Take(3).Select(i=>new[]{"up","right","down","left"}[i]),lines.Skip(2));
                if(kind==1)CollectionAssert.AreEqual(goals.Take(4+stage).SelectMany(i=>LearningSpeech.Number(i+1)),lines.Skip(2));
            }
        }

        [Test] public void AllDepthOverlaysExposeTheSharedSpeakerButton()
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var world in DepthCatalog.Worlds)
            {
                Clear();var host=new GameObject("DepthFixture");host.transform.SetParent(_root.transform,false);
                var controller=host.AddComponent<MapController>();var def=Maps.Get(world.Id);
                typeof(MapController).GetField("_def",flags).SetValue(controller,def);
                typeof(MapController).GetField("_hudCanvasRoot",flags).SetValue(controller,(RectTransform)_root.transform);
                typeof(MapController).GetField("_voice",flags).SetValue(controller,host.AddComponent<Voice>());
                var routine=(IEnumerator)typeof(MapController).GetMethod("DepthMechanismRoutine",flags).Invoke(controller,new object[]{def.Discoveries[0]});
                Assert.True(routine.MoveNext());Assert.True(Replay().interactable,world.Id);
                Assert.NotNull(Replay().GetComponentInChildren<QuestionSpeakerGraphic>());
            }
        }

        [Test] public void ReplayNarrationDoesNotAddTheAnswerForVisualQuestions()
        {
            foreach(var diagram in new[]{"fraction","polygon","shape","dots","array","compose","sequence","mirror","fractionline"})
            {
                var q=new LearningQuestion{Prompt="Look carefully.",Display="hidden answer 777",Answer="777",Diagram=diagram};
                CollectionAssert.AreEqual(new[]{q.Prompt},LearningSpeech.Lines(q),diagram);
            }
        }

        [Test] public void EverySeasonalOverlayHasReplayAndResetWithoutReopeningOrSpendingProgress()
        {
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(string id in HalloweenWorlds.Ids)foreach(var discovery in Maps.Get(id).Discoveries)
            {
                Clear();var host=new GameObject("SeasonalFixture");host.transform.SetParent(_root.transform,false);
                var controller=host.AddComponent<MapController>();var def=Maps.Get(id);var p=new Progress{Coins=3278};p.ApplyMigrations();
                void Field(string name,object value)=>typeof(MapController).GetField(name,flags).SetValue(controller,value);
                Field("_def",def);Field("_hudCanvasRoot",(RectTransform)_root.transform);Field("_voice",host.AddComponent<Voice>());
                Field("_progress",p);Field("_rng",new Rng(42));
                var routine=(IEnumerator)typeof(MapController).GetMethod("HalloweenMechanismRoutine",flags).Invoke(controller,new object[]{discovery});
                Assert.True(routine.MoveNext());Assert.True(Replay().interactable);Assert.NotNull(Replay().GetComponentInChildren<QuestionSpeakerGraphic>());
                var buttons=_root.GetComponentsInChildren<Button>();
                foreach(string name in new[]{"Undo","Reset","Back"})Assert.NotNull(buttons.Single(b=>b.name==name),discovery.Name);
                buttons.Single(b=>b.name=="Reset").onClick.Invoke();
                Assert.AreEqual(3278,p.Coins);Assert.False(p.CollectedDiscoveries.Contains(discovery.Id));
                var clips=_root.GetComponentInChildren<HalloweenPuzzleAudio>().Clips.ToArray();
                Clear();Assert.True(clips.All(c=>c==null),"Closing an overlay must release all generated audio clips.");
            }
        }
    }
}
