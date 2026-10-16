using System.Linq;
using Numeria.Core;
using NUnit.Framework;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class HalloweenWorldTests
    {
        [Test] public void AllSixRestorationsAnimateAndReopeningRestoresTheCompletedScene()
        {
            foreach(string id in HalloweenWorlds.Ids)
            {
                var map=Maps.Get(id);var p=new Progress();foreach(var task in map.Discoveries)p.CollectDiscovery(task.Id);
                var root=new GameObject("RestorationTest");
                try
                {
                    var grid=GridMap.Parse(map.Rows);HalloweenWorlds.Apply(grid,map,p);
                    HalloweenScene.Build(root.transform,grid,map,p);var scene=root.GetComponentInChildren<HalloweenScene>();
                    var sprites=root.GetComponentsInChildren<SpriteRenderer>();
                    var completed=sprites.ToDictionary(s=>s,s=>(s.transform.localPosition,s.transform.localScale,s.color));
                    foreach(var task in map.Discoveries)
                    {
                        scene.SampleRestoration(task.Id,0);
                        Assert.True(sprites.Any(s=>completed[s]!=(s.transform.localPosition,s.transform.localScale,s.color)),task.Id+" must visibly restore");
                        foreach(float t in new[]{0f,.3f,.6f,1f})
                        {
                            scene.SampleRestoration(task.Id,t);
                            foreach(var sprite in sprites){Assert.NotNull(sprite.sprite,sprite.name);Assert.That(sprite.color.a,Is.InRange(0f,1f));}
                        }
                        foreach(var sprite in sprites)
                            Assert.AreEqual(completed[sprite],(sprite.transform.localPosition,sprite.transform.localScale,sprite.color),task.Id+" reload mismatch "+sprite.name);
                        Vector3 focus=scene.RestorationFocus(task.Id);
                        Assert.That(focus.x,Is.InRange(0,map.Rows[0].Length));Assert.That(focus.y,Is.InRange(0,map.Rows.Length));
                    }
                }
                finally{Object.DestroyImmediate(root);}
            }
        }
        [Test] public void TallSeasonalPropsFadeOnlyWhenTheyOccludeThePlayer()
        {
            var root=new GameObject("OcclusionTest");
            try
            {
                var map=Maps.Get(HalloweenWorlds.Ids[0]);
                HalloweenScene.Build(root.transform,GridMap.Parse(map.Rows),map,new Progress());
                var scene=root.GetComponentInChildren<HalloweenScene>();
                var tree=root.GetComponentsInChildren<SpriteRenderer>().First(s=>s.name=="LanternTree");
                scene.UpdateOcclusion(tree.bounds.center,tree.sortingOrder-1,1);Assert.AreEqual(.32f,tree.color.a,.001f);
                scene.UpdateOcclusion(tree.bounds.center,tree.sortingOrder+1,1);Assert.AreEqual(1,tree.color.a);
                scene.UpdateOcclusion(new Vector3(-100,-100,0),tree.sortingOrder-1,1);Assert.AreEqual(1,tree.color.a);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void SeasonalStoriesAndAllGradeCluesHaveOfflineNarration()
        {
            var lines=new System.Collections.Generic.HashSet<string>();
            foreach(var family in ExpansionCatalog.Families)foreach(string id in family.Ids)lines.UnionWith(CollectionBook.Narration(id));
            for(int grade=0;grade<=5;grade++)foreach(MathMode mode in System.Enum.GetValues(typeof(MathMode)))for(uint seed=1;seed<=30;seed++)for(int kind=0;kind<6;kind++)
            {var settings=new MathSettings{Grade=grade};settings.Mode=mode;lines.UnionWith(new HalloweenMechanism(kind,settings,new Rng(seed)).Narration());}
            foreach(string line in lines)Assert.NotNull(Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line)),line);
        }
        [Test] public void AllThirtyFamiliesHaveDistinctAuthoredSignaturesAndEveryFrameCleansUp()
        {
            Assert.AreEqual(30,ExpansionCatalog.Families.Select(f=>ExpansionCatalog.Visual(f.BaseId)).Distinct().Count());
            var root=new GameObject("SeasonalSpellTest",typeof(RectTransform));
            try
            {
                var caster=Ui.Node(root.transform,"Caster");var target=Ui.Node(root.transform,"Target");
                foreach(var family in ExpansionCatalog.Families)foreach(string id in family.Ids)foreach(int sign in new[]{-1,1})
                {
                    caster.anchoredPosition=new Vector2(-240*sign,-80);target.anchoredPosition=new Vector2(240*sign,100);
                    Vector3 home=caster.localPosition;var kind=ExpansionCatalog.Visual(id);
                    if(kind>=SkillVisualKind.RindPounce)Assert.NotNull(Resources.Load<Sprite>(SeasonalFamilySpell.MotifPath(kind)),id);
                    var sequence=SpellSequence.Create((RectTransform)root.transform,caster,target,kind,new SpellTrace{A=3,B=4,Total=7},true,id);
                    foreach(float t in new[]{0f,.35f,.65f,.95f,SpellSequence.ImpactTime,SpellSequence.Duration})
                    {
                        sequence.Sample(t);
                        foreach(var image in sequence.GetComponentsInChildren<UnityEngine.UI.Image>())
                        {
                            Assert.NotNull(image.sprite,id+" "+image.name);
                            Assert.False(image.raycastTarget,id+" blocks input");
                            Assert.False(float.IsNaN(image.rectTransform.anchoredPosition.x));
                            Assert.That(image.color.a,Is.InRange(0f,1f));
                        }
                    }
                    sequence.Cancel();Assert.AreEqual(home,caster.localPosition,id+" caster must return home");
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void NewWorldsUnlockTogetherAndEveryObjectiveChestHabitatAndExitIsReachable()
        {
            foreach(string id in HalloweenWorlds.Ids)
            {
                var map=Maps.Get(id);var p=new Progress();p.ApplyMigrations();var grid=GridMap.Parse(map.Rows);
                Assert.False(Maps.CanVisit(id,p));Maps.VeilmoonLake().ClearGate(p);Assert.True(Maps.CanVisit(id,p));
                for(int stage=0;stage<=3;stage++)
                {
                    HalloweenWorlds.Apply(grid,map,p);
                    Assert.IsNotEmpty(grid.FindPath(grid.Spawn,HalloweenWorlds.ReturnCell(id)),id+" exit");
                    foreach(var d in map.Discoveries)Assert.IsNotEmpty(grid.FindPath(grid.Spawn,(d.X,d.Y)),id+" "+d.Name);
                    for(int y=0;y<grid.Height;y++)for(int x=0;x<grid.Width;x++)if(grid.At(x,y)==Tile.Chest||grid.At(x,y)==Tile.Bush)
                        Assert.IsNotEmpty(grid.FindPath(grid.Spawn,(x,y)),id+" cell "+x+","+y);
                    Assert.AreEqual(stage==3,map.GuardianReady(p));
                    if(stage<3)p.CollectDiscovery(map.Discoveries[stage].Id);
                }
                map.ClearGate(p);int count=p.Accessories.Count;map.ClearGate(p);Assert.AreEqual(count,p.Accessories.Count);
            }
        }
        [Test] public void SeasonalEcologyCoversFifteenFamiliesAndBossNeverScalesWithThePlayer()
        {
            foreach(string id in HalloweenWorlds.Ids)
            {
                var map=Maps.Get(id);Assert.AreEqual(15,map.Encounters.Length);
                var species=Enumerable.Range(0,5).SelectMany(z=>HalloweenWorlds.Ecology(id,z)).Select(e=>e.SpeciesId).Distinct();
                CollectionAssert.AreEquivalent(map.Encounters.Select(e=>e.SpeciesId),species);
                var a=map.RollBossEncounter(1,new Rng(8));var b=map.RollBossEncounter(99,new Rng(8));
                Assert.AreEqual(84,a.Level);Assert.AreEqual(a.Level,b.Level);Assert.AreEqual(a.MaxHp,b.MaxHp);
                for(uint seed=0;seed<100;seed++){var wild=map.RollWildEncounter(99,new Rng(seed));Assert.That(wild.Level,Is.InRange(76,82));}
            }
        }
        [Test] public void EveryNewWorldAndFormHasRealArtworkAndNarration()
        {
            foreach(string id in HalloweenWorlds.Ids)
            {
                Assert.NotNull(Resources.Load<Sprite>(Maps.Get(id).BattleBg));
                Assert.NotNull(Resources.Load<Texture2D>("generated/Exploration/"+id+"_tiles"));
                for(int prop=0;prop<4;prop++)Assert.NotNull(WorldScene.Prop(id,prop));
            }
            foreach(var family in ExpansionCatalog.Families)foreach(string id in family.Ids)
            {
                Assert.NotNull(SpriteLib.LargeIcon(id),id);var skill=GameData.PlayerMon(id,GameData.StageIndex(id),84).Skills.Last();
                Assert.NotNull(Resources.Load<Sprite>(skill.IconResource),id);Assert.True(SpellSequence.Supports(skill.Visual));
            }
            for(int kind=0;kind<6;kind++)foreach(string line in new HalloweenMechanism(kind,new MathSettings(),new Rng(1)).Narration())
                Assert.NotNull(Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line)),line);
        }

        [Test] public void SeasonalWildMatchupsAvoidMinimumDamageSlowness()
        {
            foreach(int level in new[]{76,82,84,92,99})foreach(var family in ExpansionCatalog.Families)
            {
                int stage=level>=92?2:level>=84?1:0;
                var player=GameData.PlayerMon(family.BaseId,stage,level);
                foreach(var opponent in ExpansionCatalog.Families)
                {
                    var enemy=GameData.CreateWild(opponent.BaseId,System.Math.Min(82,level),new Rng(17));
                    var state=new BattleState(player,enemy,new Rng(19));
                    int total=0;for(int roll=0;roll<60;roll++)total+=state.RollDamage(player.AttackPower+player.Skills.Last().Power,enemy.DefensePower);
                    double hits=enemy.MaxHp/(total/60d);
                    Assert.That(hits,Is.InRange(2d,10d),$"{family.BaseId} vs {opponent.BaseId}, level {level}: {hits:F1} formula hits");
                }
            }
        }

        [Test] public void SeasonalBossTrainingAndDefendWorkWithEveryNewFamily()
        {
            foreach(string id in HalloweenWorlds.Ids)foreach(var family in ExpansionCatalog.Families)
            foreach(int level in new[]{76,84,92,99})foreach(int equipment in new[]{0,4,8})
            {
                int stage=level>=92?2:level>=84?1:0,wins=0,actionsTotal=0;
                for(uint seed=1;seed<=16;seed++)
                {
                    var player=GameData.PlayerMon(family.BaseId,stage,level);player.AttackPower+=equipment;player.DefensePower+=equipment;
                    var state=new BattleState(player,Maps.Get(id).RollBossEncounter(level,new Rng(seed)),new Rng(seed));
                    int actions=0;
                    while(state.Outcome==BattleOutcome.None&&actions<100)
                    {
                        state.StartPlayerTurn();
                        if(state.EnemyShielded)state.BreakShield();
                        else{var skill=state.Player.Skills.Last();state.UseSkill(state.Gems>=skill.Cost?skill.Id:"tackle");}
                        if(state.Outcome==BattleOutcome.None&&!state.ConsumeEnemySkipTurn())state.EnemyTurn();actions++;
                    }
                    if(state.Outcome==BattleOutcome.Win)wins++;actionsTotal+=actions;
                }
                if(level>=84)Assert.AreEqual(16,wins,$"{id}: trained {family.BaseId} level {level}, equipment {equipment}");
                TestContext.Out.WriteLine($"SEASONAL_BALANCE {id},{family.BaseId},{level},gear{equipment},{wins}/16,{actionsTotal/16d:F1} actions");
                var foe=Maps.Get(id).RollBossEncounter(level,new Rng(7));
                var normal=new BattleState(GameData.PlayerMon(family.BaseId,stage,level),foe,new Rng(1));
                var guarded=new BattleState(GameData.PlayerMon(family.BaseId,stage,level),foe,new Rng(1));
                normal.StartPlayerTurn();guarded.StartPlayerTurn();guarded.Defend();
                int a=normal.EnemyTurn(),b=guarded.EnemyTurn();Assert.AreEqual((a+1)/2,b);
            }
        }
    }
}
