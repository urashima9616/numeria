using System;
using System.Linq;
using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class HalloweenContentTests
    {
        [Test] public void SeasonalMechanismsTeachGradeSpecificConceptsWithinTheirBounds()
        {
            for(int grade=0;grade<=5;grade++)foreach(MathMode mode in Enum.GetValues(typeof(MathMode)))for(uint seed=0;seed<100;seed++)
            {
                var settings=new MathSettings{Grade=grade};settings.Mode=mode;
                var shares=new HalloweenMechanism(1,settings,new Rng(seed));
                Assert.AreEqual(shares.Total,shares.BasketTargets.Sum());
                Assert.That(shares.Total,Is.InRange(4,18));
                Assert.AreEqual(grade==4,shares.FractionShares);Assert.AreEqual(grade==5,shares.WeightShares);
                foreach(int value in shares.BasketTargets)Assert.Greater(value,0);
                if(grade==4)
                {
                    Assert.AreEqual(shares.Total/2,shares.BasketTargets[shares.Count==2?0:1]);
                    if(shares.Count==3)Assert.AreEqual(shares.Total/4,shares.BasketTargets[0]);
                    Assert.True(shares.Narration().Contains("over"));
                }
                if(grade==5)
                {
                    Assert.That(shares.PumpkinWeight,Is.InRange(.2m,.5m));
                    Assert.True(shares.Narration().Contains("point"));
                    Assert.True(shares.BasketTargetLabel(0).EndsWith(" kg"));
                }
                var pattern=new HalloweenMechanism(2,settings,new Rng(seed));
                Assert.AreEqual(3,pattern.PatternChoices.Distinct().Count());
                Assert.Contains(pattern.Initial[pattern.PatternGap],pattern.PatternChoices);
                if(grade>0)
                {
                    int delta=pattern.Initial[1]-pattern.Initial[0];Assert.Greater(delta,0);
                    for(int i=1;i<8;i++)Assert.AreEqual(delta,pattern.Initial[i]-pattern.Initial[i-1]);
                    if(grade<=2)
                    {
                        int cap=MathCurriculum.ChapterLimit(grade,mode==MathMode.Random?MathMode.Challenging:mode,8);
                        Assert.LessOrEqual(pattern.Initial.Take(8).Max(),cap);
                        Assert.LessOrEqual(pattern.PatternChoices.Max(),cap);Assert.GreaterOrEqual(pattern.PatternChoices.Min(),0);
                    }
                    Assert.Contains("what",pattern.Narration());
                    if(grade==4)Assert.True(pattern.PatternLabel(pattern.Initial[0]).Contains("/"));
                    if(grade==5)Assert.True(pattern.Narration().Contains("point"));
                }
                var mirror=new HalloweenMechanism(3,settings,new Rng(seed));Assert.AreEqual(grade==5,mirror.CoordinateMirror);
                if(grade==5)Assert.Contains("The mirror is halfway between x equals two and x equals three.",mirror.Narration());
            }
        }
        [Test] public void DepthEvolutionScalesTheWholeSpeciesCurveButNeverCapturedOffsets()
        {
            foreach(var world in DepthCatalog.Worlds)foreach(var mon in world.Species)
            foreach(int level in new[]{world.Level,Math.Min(99,world.Level+10),99})for(int stage=1;stage<=2;stage++)
            {
                var baseline=GameData.PlayerMon(mon.Id,0,level);var evolved=GameData.PlayerMon(mon.Id,stage,level);
                int hp=mon.Giant?(stage==1?106:112):(stage==1?112:125);
                int stat=mon.Giant?(stage==1?104:108):(stage==1?110:120);
                int Expected(int value,int percent)=>Math.Max(value+1,(value*percent+50)/100);
                Assert.AreEqual(Expected(baseline.MaxHp,hp),evolved.MaxHp,mon.Id);
                Assert.AreEqual(Expected(baseline.AttackPower,stat),evolved.AttackPower,mon.Id);
                Assert.AreEqual(Expected(baseline.DefensePower,stat),evolved.DefensePower,mon.Id);
                var p=new Progress();p.ApplyMigrations();p.AddCaught(mon.Id,level);var g=p.FindGrowth(mon.Id);
                g.Stage=stage;g.CapturedHpOffset=7;g.CapturedAttackOffset=3;g.CapturedDefenseOffset=2;
                var actual=p.PlayerCombatant(mon.Id);
                Assert.AreEqual(evolved.MaxHp+7,actual.MaxHp);Assert.AreEqual(evolved.AttackPower+3,actual.AttackPower);
                Assert.AreEqual(evolved.DefensePower+2,actual.DefensePower);
            }
        }
        [Test] public void ThirtyNewFamiliesAndAllEightyEightLegacyDepthFamiliesHaveThreeStages()
        {
            Assert.AreEqual(30,ExpansionCatalog.Families.Length);
            Assert.AreEqual(15,ExpansionCatalog.Families.Count(f=>f.Element=="PUMPKIN"));
            Assert.AreEqual(15,ExpansionCatalog.Families.Count(f=>f.Element=="GHOST"));
            var bases=DepthCatalog.Worlds.SelectMany(w=>w.Species.Select(s=>s.Id)).Concat(ExpansionCatalog.Families.Select(f=>f.BaseId));
            foreach(string id in bases)
            {
                var line=GameData.LineFor(id);Assert.AreEqual(3,line.StageIds.Length,id);
                Assert.AreEqual(2,line.EvolutionLevels.Length,id);Assert.Less(line.EvolutionLevels[0],line.EvolutionLevels[1]);
                var p=new Progress{EvolutionStones=3};p.ApplyMigrations();p.AddCaught(id,99);
                for(int stage=0;stage<2;stage++)
                {
                    var before=p.PlayerCombatant(id);Assert.True(p.CanEvolve(id),id);Assert.True(p.AdvanceEvolution(id));
                    var after=p.PlayerCombatant(id);Assert.GreaterOrEqual(after.MaxHp,before.MaxHp,id);
                    Assert.GreaterOrEqual(after.AttackPower,before.AttackPower,id);Assert.GreaterOrEqual(after.DefensePower,before.DefensePower,id);
                    Assert.True(p.HasCollected(line.StageIds[stage+1]));
                }
                Assert.False(p.CanEvolve(id));Assert.AreEqual(2,p.CollectionCount);
            }
        }
        [Test] public void VersionThirteenDepthBasesRetainTheirExactStatsGrowthAndCollectionHistory()
        {
            int i=0;
            foreach(var world in DepthCatalog.Worlds)foreach(var mon in world.Species)
            {
                var s=GameData.SpeciesById(mon.Id);
                Assert.AreEqual(mon.Giant?22:12+i%5,s.MaxHp,mon.Id);
                Assert.AreEqual(mon.Giant?5:3+i%3,s.AttackPower);Assert.AreEqual(mon.Giant?4:2+i%3,s.DefensePower);
                Assert.AreEqual(mon.Giant?13:10+i%4,s.HpGrowth);Assert.AreEqual(5,s.AttackGrowth);Assert.AreEqual(4,s.DefenseGrowth);i++;
                var p=new Progress{SaveVersion=13,Coins=3278,CurrentMap=world.Id};p.AddCaught(mon.Id,87);
                var g=p.FindGrowth(mon.Id);g.CapturedHpOffset=7;g.CapturedAttackOffset=3;g.CapturedDefenseOffset=2;g.Xp=16;
                p.OpenedChests.Add("forest-chest-7-3");p.CollectedDiscoveries.Add(world.Id+"-task-0");
                var before=p.PlayerCombatant(mon.Id);p.ApplyMigrations();p.ApplyMigrations();var after=p.PlayerCombatant(mon.Id);
                Assert.AreEqual(before.MaxHp,after.MaxHp);Assert.AreEqual(before.AttackPower,after.AttackPower);Assert.AreEqual(before.DefensePower,after.DefensePower);
                Assert.AreEqual(0,g.Stage);Assert.AreEqual(87,g.Level);Assert.AreEqual(16,g.Xp);Assert.AreEqual(3278,p.Coins);
                Assert.AreEqual(world.Id,p.CurrentMap);Assert.Contains("forest-chest-7-3",p.OpenedChests);
                Assert.False(p.HasCollected(GameData.LineFor(mon.Id).StageIds[1]));
            }
        }
        [Test] public void EverySeasonalPuzzleIsSolvableAcrossGradesAndModesWithoutResourceCosts()
        {
            for(int grade=0;grade<=5;grade++)foreach(MathMode mode in Enum.GetValues(typeof(MathMode)))for(uint seed=1;seed<=12;seed++)for(int kind=0;kind<6;kind++)
            {
                var settings=new MathSettings{Grade=grade};settings.Mode=mode;
                var p=new HalloweenMechanism(kind,settings,new Rng(seed));Assert.False(p.Solved,$"initial {kind}");
                var initial=p.Cells.ToArray();p.Tap(kind==3?2:0);p.Reset();CollectionAssert.AreEqual(initial,p.Cells);
                CollectionAssert.AreEqual(new HalloweenMechanism(kind,settings,new Rng(seed)).Melody,p.Melody);
                if(kind==0)
                {
                    int[] solution={14,10,10,10,5,0,0,0,5,0,0,0,3,10,10,10};
                    for(int n=0;n<16;n++)for(int turns=0;turns<4&&p.Cells[n]!=solution[n]&&!p.Solved;turns++)p.Tap(n);
                }
                if(kind==1)
                {
                    Assert.LessOrEqual(p.Total,grade==0?10:20);
                    for(int n=0;n<p.Count;n++)for(int j=0;j<p.BasketTargets[n];j++)p.Tap(n);
                }
                if(kind==2)p.Tap(12+Array.IndexOf(p.PatternChoices,p.Initial[p.PatternGap]));
                if(kind==3)for(int y=0;y<4;y++)for(int x=0;x<2;x++)if(p.Cells[y*4+x]==1)p.Tap(y*4+3-x);
                if(kind==4)
                {
                    Assert.LessOrEqual(p.Initial.Max(),MathCurriculum.ChapterLimit(grade,mode==MathMode.Random?MathMode.Challenging:mode,8));
                    for(int n=0;n<p.Count;n++)
                    {int next=Array.IndexOf(p.Cells,p.PortraitOrder[n]);if(next!=n){p.Tap(n);p.Tap(next);}}
                }
                if(kind==5)foreach(int note in p.Melody)p.Tap(note);
                Assert.True(p.Solved,$"kind {kind} grade {grade} mode {mode} seed {seed}");
            }
        }
        [Test] public void IncorrectSharingIsUndoableAndListeningDoesNotChangeAnswers()
        {
            var p=new HalloweenMechanism(1,new MathSettings(),new Rng(7));
            p.Tap(0);p.Tap(0);p.Undo();Assert.AreEqual(1,p.Cells[0]);Assert.AreEqual(p.Total-1,p.Remaining);
            var before=p.Cells.ToArray();var lines=p.Narration();Assert.IsNotEmpty(lines);CollectionAssert.AreEqual(before,p.Cells);
            var bells=new HalloweenMechanism(5,new MathSettings(),new Rng(4));bells.Tap(bells.Melody[0]);
            bells.Narration();Assert.AreEqual(1,bells.Step);bells.Reset();Assert.AreEqual(0,bells.Step);
        }
    }
}
