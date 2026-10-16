using System;
using System.Linq;
using Numeria.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Numeria.Game.Tests
{
    public class DepthEvolutionSpellTests
    {
        [Test] public void EveryDepthFamilyHasTwoExplicitDistinctStagesAndImportedMotifs()
        {
            Assert.AreEqual(88,DepthEvolutionSpell.Profiles.Length);
            CollectionAssert.AreEquivalent(DepthCatalog.Worlds.SelectMany(w=>w.Species).Select(s=>s.Id),
                DepthEvolutionSpell.Profiles.Select(p=>p.BaseId));
            foreach(var profile in DepthEvolutionSpell.Profiles)
            {
                Assert.AreNotEqual(profile.Second,profile.Third,profile.BaseId);
                Assert.NotNull(Resources.Load<Sprite>(profile.MotifPath),profile.BaseId);
                Assert.NotNull(Resources.Load<Sprite>(profile.AccentPath),profile.WorldId);
                Assert.LessOrEqual(Resources.Load<Sprite>(profile.MotifPath).texture.width,256,profile.BaseId+" particle memory budget");
                foreach(string id in ExpansionCatalog.DepthStages(profile.BaseId))
                    Assert.AreSame(profile,DepthEvolutionSpell.For(id),id);
            }
            Assert.AreEqual(88,DepthEvolutionSpell.Profiles.Select(p=>p.MotifPath).Distinct().Count());
        }

        [Test] public void LegacyBaseFormsAndTackleDoNotAcquireEvolutionEffects()
        {
            foreach(var profile in DepthEvolutionSpell.Profiles)
            {
                Assert.False(DepthEvolutionSpell.Applies(profile.BaseId,SkillVisualKind.EquationFlame));
                foreach(string id in ExpansionCatalog.DepthStages(profile.BaseId))
                    Assert.False(DepthEvolutionSpell.Applies(id,SkillVisualKind.Physical));
            }
            Assert.False(DepthEvolutionSpell.Applies("addmander",SkillVisualKind.EquationFlame));
            Assert.False(DepthEvolutionSpell.Applies("",SkillVisualKind.Physical));
        }

        [Test] public void All176EvolutionsBothFacingsHaveFiniteFramesSingleImpactAndRestoreTransforms()
        {
            var root=new GameObject("DepthSpellTest",typeof(RectTransform));
            try
            {
                var caster=Ui.Node(root.transform,"Caster");var target=Ui.Node(root.transform,"Target");
                foreach(var profile in DepthEvolutionSpell.Profiles)
                foreach(string id in ExpansionCatalog.DepthStages(profile.BaseId).Skip(1))
                foreach(int sign in new[]{-1,1})foreach(bool powered in new[]{false,true})
                {
                    caster.anchoredPosition=new Vector2(-240*sign,-80);target.anchoredPosition=new Vector2(240*sign,100);
                    Vector3 home=caster.localPosition,enemyHome=target.localPosition,scale=caster.localScale;
                    var kind=GameData.PlayerMon(id,GameData.StageIndex(id),80).Skills.Last().Visual;
                    Assert.True(DepthEvolutionSpell.Applies(id,kind),id);
                    var sequence=SpellSequence.Create((RectTransform)root.transform,caster,target,kind,
                        new SpellTrace{A=3,B=4,Total=7},powered,id);
                    Assert.AreEqual(36,sequence.GetComponentsInChildren<Image>().Length,id);
                    int impacts=0;bool visible=false;
                    for(int frame=0;frame<=40;frame++)
                    {
                        sequence.Advance(frame*SpellSequence.Duration/40,()=>impacts++);
                        foreach(var image in sequence.GetComponentsInChildren<Image>())
                        {
                            Assert.NotNull(image.sprite,id+" "+image.name);
                            Assert.False(image.raycastTarget,id+" blocks input");
                            var p=image.rectTransform.anchoredPosition;var s=image.rectTransform.sizeDelta;
                            Assert.False(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),id);
                            Assert.Greater(s.x,0,id);Assert.Greater(s.y,0,id);Assert.That(image.color.a,Is.InRange(0f,1f));
                            visible|=image.color.a>.1f;
                        }
                    }
                    sequence.Advance(SpellSequence.Duration,()=>impacts++);
                    Assert.AreEqual(1,impacts,id);Assert.True(visible,id);
                    Assert.AreEqual(0,sequence.GetComponent<CanvasGroup>().alpha,.0001f,id);
                    sequence.Cancel();Assert.AreEqual(home,caster.localPosition,id);Assert.AreEqual(scale,caster.localScale,id);
                    Assert.AreEqual(enemyHome,target.localPosition,id);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
