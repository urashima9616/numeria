using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class ForestSliceTests
    {
        [Test]
        public void ClosedBridgeLeavesAllObjectivesReachableAndRestorationOpensShortcut()
        {
            var def = Maps.Forest();
            var map = GridMap.Parse(def.Rows);
            var progress = new Progress();
            ForestScene.ApplyPassages(map, progress);
            Assert.False(map.Walkable(15, 5));
            foreach (var discovery in def.Discoveries)
                Assert.IsNotEmpty(map.FindPath(map.Spawn, (discovery.X, discovery.Y)), discovery.Id);
            foreach (string id in def.ChestIds()) Assert.IsTrue(id.StartsWith("forest-cache-"), id);
            progress.CollectDiscovery(ForestJourney.Bridge);
            ForestScene.ApplyPassages(map, progress);
            Assert.True(map.Walkable(15, 5));
            Assert.AreEqual(4, map.FindPath((13, 5), (17, 5)).Count);
        }

        [Test]
        public void ForestAtlasAndGroundArePresentWithValidSpriteRegions()
        {
            Assert.NotNull(Resources.Load<Sprite>("generated/Exploration/forest_ground"));
            Assert.NotNull(Resources.Load<Sprite>("generated/Exploration/vine_bridge"));
            Assert.NotNull(Resources.Load<Texture2D>("generated/Exploration/spell_materials"));
            for (int i = 0; i < 4; i++)
            {
                var prop = ForestScene.Prop(i);
                Assert.NotNull(prop);
                Assert.Greater(prop.rect.width, 100);
                Assert.LessOrEqual(prop.rect.xMax, prop.texture.width);
                Assert.LessOrEqual(prop.rect.yMax, prop.texture.height);
            }
        }

        [TestCase(SkillVisualKind.EquationFlame)]
        [TestCase(SkillVisualKind.MakeTenWave)]
        [TestCase(SkillVisualKind.SymmetryBeam)]
        public void SpellSamplingAndCleanupPreserveActorTransforms(SkillVisualKind kind)
        {
            var root = new GameObject("TestCanvas", typeof(RectTransform));
            try
            {
                var canvas = (RectTransform)root.transform;
                canvas.sizeDelta = new Vector2(1440, 1080);
                var caster = Ui.Node(canvas, "Caster");
                var target = Ui.Node(canvas, "Target");
                caster.localScale = new Vector3(2, 2, 1);
                target.localPosition = new Vector3(300, 150, 0);
                var sequence = SpellSequence.Create(canvas, caster, target, kind,
                    new SpellTrace { A = 4, B = 6, Total = 10 }, true);
                sequence.Sample(1.5f);
                Assert.AreNotEqual(new Vector3(300, 150, 0), target.localPosition);
                sequence.Sample(0);
                Assert.AreEqual(new Vector3(300, 150, 0), target.localPosition);
                sequence.Sample(.3f);
                sequence.Cancel();
                Assert.AreEqual(new Vector3(2, 2, 1), caster.localScale);
                Assert.AreEqual(new Vector3(300, 150, 0), target.localPosition);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
