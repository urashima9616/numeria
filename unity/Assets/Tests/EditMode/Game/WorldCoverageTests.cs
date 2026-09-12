using System;
using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game.Tests
{
    public class WorldCoverageTests
    {
        [Test]
        public void EverySpeciesNormalAndMegaSkillHasAuthoredPresentation()
        {
            Assert.AreEqual(141, GameData.Roster.Count);
            foreach (var species in GameData.Roster)
            {
                var player = GameData.PlayerMon(species.Id, GameData.StageIndex(species.Id), 20);
                foreach (var skill in player.Skills)
                    Assert.True(SpellSequence.Supports(skill.Visual), species.Id + "/" + skill.Name);
                Assert.True(SpellSequence.Supports(MegaSystem.For(player).Skill.Visual), species.Id + "/Mega");
            }
            foreach (SkillVisualKind kind in Enum.GetValues(typeof(SkillVisualKind)))
                Assert.True(SpellSequence.Supports(kind), kind.ToString());
            Assert.False(SpellSequence.Supports((SkillVisualKind)999));
        }

        [Test]
        public void AllBiomesHaveDistinctResourcesAndRetainReachability()
        {
            foreach (var def in Maps.All())
            {
                Assert.NotNull(Resources.Load<Sprite>("generated/Exploration/" + def.Id + "_ground"), def.Id);
                for (int i = 0; i < 4; i++) Assert.NotNull(WorldScene.Prop(def.Id, i), def.Id + "/" + i);
                Assert.NotNull(WorldScene.BridgeSprite(def.Id));
                var map = GridMap.Parse(def.Rows);
                var p = new Progress();
                if (def.Id == "forest") ForestScene.ApplyPassages(map, p);
                foreach (var discovery in def.Discoveries)
                    Assert.IsNotEmpty(map.FindPath(map.Spawn, (discovery.X, discovery.Y)), discovery.Id);
                Assert.IsNotEmpty(map.FindPath(map.Spawn, (def.Merchant.X, def.Merchant.Y)), def.Id);
            }
        }

        [Test]
        public void EveryTimelineSamplesInBothDirectionsAndCancelsWithoutMovingActors()
        {
            foreach (SkillVisualKind kind in Enum.GetValues(typeof(SkillVisualKind)))
                foreach (int direction in new[] { -1, 1 })
                {
                    var root = new GameObject("CoverageCanvas", typeof(RectTransform));
                    try
                    {
                        var canvas = (RectTransform)root.transform;
                        canvas.sizeDelta = new Vector2(1440, 1080);
                        var caster = Ui.Node(canvas, "Caster");
                        var target = Ui.Node(canvas, "Target");
                        caster.localPosition = new Vector3(-300 * direction, -100, 0);
                        target.localPosition = new Vector3(300 * direction, 150, 0);
                        var from = caster.localPosition; var to = target.localPosition;
                        var sequence = SpellSequence.Create(canvas, caster, target, kind,
                            new SpellTrace { A = 4, B = 6, Total = 10 }, true, "equadragon", true);
                        int impacts = 0;
                        sequence.Advance(SpellSequence.ImpactTime - .01f, () => impacts++);
                        Assert.AreEqual(0, impacts, kind.ToString());
                        foreach (float time in new[] { 0f, .4f, .9f, 1.23f, 1.6f, 1.9f })
                        {
                            sequence.Advance(time, () => impacts++);
                            foreach (RectTransform rt in sequence.GetComponentsInChildren<RectTransform>())
                            {
                                Assert.False(float.IsNaN(rt.localPosition.x), kind.ToString());
                                Assert.Less(rt.sizeDelta.magnitude, 4000, kind.ToString());
                            }
                        }
                        Assert.AreEqual(1, impacts, kind.ToString());
                        sequence.Cancel();
                        Assert.AreEqual(from, caster.localPosition, kind.ToString());
                        Assert.AreEqual(to, target.localPosition, kind.ToString());
                        Assert.AreEqual(Vector3.one, caster.localScale);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(root); }
                }
        }

        [Test]
        public void EveryNewVfxMaterialIsPresent()
        {
            foreach (string atlas in new[] { "nature", "arcane", "impact" })
                for (int i = 0; i < 4; i++) Assert.NotNull(SpellChoreography.Material(atlas, i), atlas + i);
            Assert.NotNull(Resources.Load<Sprite>("generated/Exploration/spell_bite"));
        }

        [Test]
        public void BuildingEveryMapPreservesGridAndSaveData()
        {
            foreach (var def in Maps.All())
            {
                var root = new GameObject("WorldTest");
                try
                {
                    var p = new Progress { Coins = 345, CurrentMap = def.Id };
                    var map = GridMap.Parse(def.Rows);
                    if (def.Id == "forest") ForestScene.ApplyPassages(map, p);
                    string before = JsonUtility.ToJson(p);
                    var cells = new Tile[map.Width, map.Height];
                    for (int y = 0; y < map.Height; y++)
                        for (int x = 0; x < map.Width; x++) cells[x, y] = map.At(x, y);
                    WorldScene.Build(root.transform, map, def, p);
                    Assert.AreEqual(before, JsonUtility.ToJson(p), def.Id);
                    for (int y = 0; y < map.Height; y++)
                        for (int x = 0; x < map.Width; x++) Assert.AreEqual(cells[x, y], map.At(x, y), def.Id);
                    Assert.Less(root.GetComponentsInChildren<SpriteRenderer>().Length, 550, def.Id);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
    }
}
