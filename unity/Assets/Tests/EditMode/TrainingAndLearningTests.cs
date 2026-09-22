using System;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class TrainingAndLearningTests
    {
        [Test]
        public void GuardHalvesOneEnemyTurnAndNeverStacks()
        {
            for (uint seed = 1; seed < 100; seed++)
            {
                var enemy = GameData.CreateWild("pebblit", 15, new Rng(seed));
                var normal = new BattleState(GameData.PlayerMon("addmander", 2, 50), enemy, new Rng(seed));
                var guarded = new BattleState(GameData.PlayerMon("addmander", 2, 50), enemy, new Rng(seed));
                Assert.True(guarded.Defend()); Assert.False(guarded.Defend());
                Assert.AreEqual((normal.EnemyTurn() + 1) / 2, guarded.EnemyTurn());
                Assert.False(guarded.Defending);
                normal.StartPlayerTurn(); guarded.StartPlayerTurn();
                Assert.AreEqual(normal.EnemyTurn(), guarded.EnemyTurn());
                guarded.Defend(); guarded.StartPlayerTurn(); Assert.False(guarded.Defending);
            }
        }

        [Test]
        public void MastersCloneWildStatsWithoutChangingBossOrCaptureRules()
        {
            Assert.AreEqual(20, TrainerSystem.Ids.Distinct().Count());
            var wild = GameData.CreateWild("countipillar", 20, new Rng(6));
            int hp = wild.MaxHp;
            foreach (string id in TrainerSystem.Ids)
            for (int percent = 20; percent <= 40; percent++)
            {
                var master = TrainerSystem.Boost(wild, id, percent);
                Assert.AreNotSame(wild, master); Assert.AreEqual(wild.Level, master.Level);
                Assert.AreEqual((int)Math.Round(hp * (1 + percent / 100d), MidpointRounding.AwayFromZero), master.MaxHp);
                Assert.False(master.Catchable); Assert.False(master.IsBoss); Assert.AreEqual(id, master.TrainerId);
            }
            Assert.AreEqual(hp, wild.MaxHp); Assert.True(wild.Catchable);
            var rng = new Rng(71); int encounters = 0;
            for (int i = 0; i < 10000; i++) if (TrainerSystem.Roll(wild,rng).TrainerId != null) encounters++;
            Assert.That(encounters, Is.InRange(1800,2200));
            wild.IsBoss = true; Assert.AreSame(wild, TrainerSystem.Roll(wild,rng));
        }

        [Test]
        public void SortingNeverRewritesCatchOrderAndUsesFullStats()
        {
            var p = new Progress(); p.Catch("countipillar"); p.Catch("pebblit");
            var original = p.CaughtIds.ToArray();
            p.TeamSortOrder = TeamSort.Newest; Assert.AreEqual("pebblit",p.SortedTeam()[0]);
            p.EnsureGrowth("countipillar").Level = 50;
            p.TeamSortOrder = TeamSort.Level; Assert.AreEqual("countipillar",p.SortedTeam()[0]);
            p.AddAccessory("s","Strong",100,100); p.EquipAccessory("s","pebblit");
            p.TeamSortOrder = TeamSort.Attack; Assert.AreEqual("pebblit",p.SortedTeam()[0]);
            p.TeamSortOrder = TeamSort.Defense; Assert.AreEqual("pebblit",p.SortedTeam()[0]);
            p.TeamSortOrder = TeamSort.Species; Assert.AreEqual(3,p.SortedTeam().Distinct().Count());
            CollectionAssert.AreEqual(original,p.CaughtIds);
        }

        [Test]
        public void TransferIsAtomicForFullSlotsAndStaleOwnership()
        {
            var p = new Progress(); p.Catch("countipillar");
            foreach (string id in new[] { "a","b","c" }) p.AddAccessory(id,id,2,1);
            p.EquipAccessory("a","addmander"); p.EquipAccessory("b","addmander"); p.EquipAccessory("c","countipillar");
            Assert.False(p.TransferAccessory("c","addmander","countipillar"));
            Assert.False(p.TransferAccessory("c","addmander","wrong","a"));
            Assert.False(p.TransferAccessory("c","unknown","countipillar","a"));
            Assert.AreEqual(2,p.EquippedAccessories("addmander").Count);
            Assert.True(p.TransferAccessory("c","addmander","countipillar","a"));
            Assert.IsEmpty(p.EquippedAccessories("countipillar"));
            Assert.AreEqual("",p.Accessories.Find(a=>a.InstanceId=="a").EquippedToBaseId);
            Assert.AreEqual(2,p.EquippedAccessories("addmander").Count);
            Assert.False(p.TransferAccessory("c","addmander","addmander"));
        }

        [Test]
        public void EachGradeRemembersItsOwnModeAndMalformedSettingsRecover()
        {
            var settings = new MathSettings(); settings.Grade=2; settings.Mode=MathMode.Challenging;
            settings.Grade=0; Assert.AreEqual(MathMode.Normal,settings.Mode);
            settings.Normalize(); settings.Grade=2; Assert.AreEqual(MathMode.Challenging,settings.Mode);
            settings.Grade=99; settings.Modes=null; settings.Normalize(); Assert.AreEqual(5,settings.Grade);
            Assert.AreEqual(MathMode.Normal,settings.Mode);
        }

        [Test]
        public void AllGradesModesChaptersHaveUniqueAnswersAndStayWithinGrade()
        {
            var rng = new Rng(31);
            for(int grade=0;grade<=5;grade++)
            for(int mode=0;mode<4;mode++)
            for(int chapter=1;chapter<=8;chapter++)
            {
                var settings=new MathSettings {Grade=grade}; settings.Mode=(MathMode)mode;
                foreach(var kind in MathCurriculum.Available(grade))
                for(int i=0;i<40;i++)
                {
                    var q=MathCurriculum.Generate(settings,rng,kind,chapter);
                    Assert.AreEqual(grade,q.Grade); Assert.AreEqual(kind,q.Kind);
                    Assert.AreEqual(q.Options.Length,q.Options.Distinct().Count());
                    Assert.That(q.Options.Length,Is.InRange(2,4));
                    Assert.AreEqual(q.Answer,q.Options[q.CorrectIndex]);
                    Assert.IsNotEmpty(q.Hint); Assert.IsNotEmpty(LearningSpeech.Lines(q));
                    Assert.AreNotEqual(MathMode.Random,q.Difficulty);
                    if(grade==0) Assert.False(kind==LearningKind.Fractions||kind==LearningKind.Time);
                    if(grade<=2 && (kind==LearningKind.Arithmetic||kind==LearningKind.MissingNumber||kind==LearningKind.Pattern||kind==LearningKind.Data))
                        Assert.LessOrEqual(int.Parse(q.Answer),MathCurriculum.ChapterLimit(grade,q.Difficulty,chapter),q.Display);
                }
            }
        }

        [Test]
        public void ChapterProgressionIsMonotonicLinearAndClamped()
        {
            for(int grade=0;grade<=5;grade++) foreach(var mode in new[] {MathMode.FeelGood,MathMode.Normal,MathMode.Challenging})
            {
                int start=MathCurriculum.ChapterLimit(grade,mode,1), end=MathCurriculum.ChapterLimit(grade,mode,8);
                for(int chapter=1;chapter<=8;chapter++)
                {
                    int current=MathCurriculum.ChapterLimit(grade,mode,chapter);
                    Assert.AreEqual((int)Math.Round(start+(end-start)*(chapter-1)/7d),current);
                    Assert.That(current,Is.InRange(start,end));
                }
                Assert.AreEqual(end,MathCurriculum.ChapterLimit(grade,mode,99));
                Assert.AreEqual(start,MathCurriculum.ChapterLimit(grade,mode,-3));
            }
        }

        [Test]
        public void GeneratedEquationsAndPatternsAreIndependentlyCorrect()
        {
            decimal Evaluate(string expression)
            {
                var tokens=Regex.Matches(expression,@"\d+(?:\.\d+)?|[+\-x/]").Cast<Match>().Select(m=>m.Value).ToArray();
                decimal value=decimal.Parse(tokens[0],CultureInfo.InvariantCulture);
                for(int i=1;i<tokens.Length;i+=2)
                {
                    decimal n=decimal.Parse(tokens[i+1],CultureInfo.InvariantCulture);
                    switch(tokens[i]) {case "+": value+=n;break; case "-":value-=n;break;case "x":value*=n;break;case "/":value/=n;break;}
                }
                return value;
            }
            var rng=new Rng(831);
            for(int grade=0;grade<6;grade++) for(int chapter=1;chapter<=8;chapter++) for(int mode=0;mode<4;mode++) for(int i=0;i<100;i++)
            {
                var settings=new MathSettings {Grade=grade};settings.Mode=(MathMode)mode;
                foreach(var kind in new[]{LearningKind.Arithmetic,LearningKind.MissingNumber,LearningKind.Pattern})
                {
                    var q=MathCurriculum.Generate(settings,rng,kind,chapter);
                    string solved=q.Display.Replace("?",q.Answer);
                    if(kind==LearningKind.Pattern)
                    {
                        var numbers=solved.Split(',').Select(int.Parse).ToArray();
                        for(int j=2;j<numbers.Length;j++)Assert.AreEqual(numbers[1]-numbers[0],numbers[j]-numbers[j-1],solved);
                    }
                    else { var sides=solved.Split('='); Assert.AreEqual(Evaluate(sides[0]),Evaluate(sides[1]),solved); }
                }
            }
        }
    }
}
