using NUnit.Framework;
using Numeria.Core;
using System.Collections.Generic;

namespace Numeria.Core.Tests
{
    public class TeamAndItemsTests
    {
        [Test]
        public void Progress_TeamAndItemDefaults()
        {
            var p = new Progress();
            Assert.AreEqual("addmander", p.ActiveMonId);
            Assert.IsEmpty(p.Items);
            Assert.IsEmpty(p.Accessories);
        }

        [Test]
        public void AccessoriesBelongToOneMathmonAndRespectEvolutionSlots()
        {
            var p = new Progress();
            p.Catch("countipillar");
            Assert.True(p.AddAccessory("a", "Power Acorn", 1, 0));
            Assert.True(p.AddAccessory("b", "Granite Guard", 0, 1));
            Assert.True(p.AddAccessory("c", "Mirror Feather", 0, 1));
            Assert.False(p.AddAccessory("a", "Duplicate", 9, 9));

            Assert.AreEqual(2, p.AccessorySlotCount("addmander"));
            Assert.True(p.EquipAccessory("a", "addmander"));
            Assert.True(p.EquipAccessory("b", "addmander"));
            Assert.False(p.EquipAccessory("c", "addmander"));
            Assert.AreEqual(1, p.AccessoryAttackBonus("addmander"));
            Assert.AreEqual(1, p.AccessoryDefenseBonus("addmander"));
            Assert.AreEqual(0, p.AccessoryAttackBonus("countipillar"));
            Assert.AreEqual(0, p.AccessoryDefenseBonus("countipillar"));

            p.EnsureGrowth("addmander").Stage = 1;
            Assert.AreEqual(3, p.AccessorySlotCount("addmander"));
            Assert.True(p.EquipAccessory("c", "addmander"));
            Assert.AreEqual(2, p.AccessoryDefenseBonus("addmander"));
            p.EnsureGrowth("addmander").Stage = 2;
            Assert.AreEqual(4, p.AccessorySlotCount("addmander"));
        }

        [Test]
        public void AccessoryCanMoveOnlyWhenTargetHasCapacity()
        {
            var p = new Progress();
            p.Catch("countipillar");
            p.AddAccessory("a", "Power Acorn", 1, 0);
            Assert.True(p.EquipAccessory("a", "addmander"));
            Assert.True(p.EquipAccessory("a", "countipillar"));
            Assert.AreEqual(0, p.EquippedAccessories("addmander").Count);
            Assert.AreEqual(1, p.EquippedAccessories("countipillar").Count);
            Assert.True(p.UnequipAccessory("a"));
            Assert.False(p.UnequipAccessory("a"));
        }

        [Test]
        public void PlayerMon_KitsHaveFamilyThemeSkill()
        {
            foreach (string id in new[] { "countipillar", "doublit", "mirrowl", "tenfin", "shapling" })
            {
                var def = GameData.PlayerMon(id, evolved: false);
                Assert.AreEqual(id, def.Id);
                Assert.GreaterOrEqual(def.MaxHp, 8);
                Assert.NotNull(System.Array.Find(def.Skills, s => s.Id == "tackle"));
                var formula = System.Array.Find(def.Skills, s => s.Type == SkillType.Formula);
                Assert.NotNull(formula);
                Assert.AreEqual(SkillType.Formula, formula.Type);
                StringAssert.StartsWith("generated/Skills/", formula.IconResource);
                Assert.AreNotEqual(SkillVisualKind.Physical, formula.Visual);
            }
        }

        [Test]
        public void PlayerMon_StarterFollowsEvolution()
        {
            Assert.AreEqual("addmander", GameData.PlayerMon("addmander", false).Id);
            Assert.AreEqual("sumdrake", GameData.PlayerMon("addmander", true).Id);
        }

        [Test]
        public void DuplicateCatch_ReturnsFalse_ForXpConversion()
        {
            var p = new Progress();
            Assert.True(p.Catch("doublit"));
            Assert.False(p.Catch("doublit")); // 调用方据此转化为经验值
        }

        [Test]
        public void LaunchRoster_HasThirtyUniqueSpeciesAcrossElevenLines()
        {
            Assert.AreEqual(30, GameData.Roster.Count);
            Assert.AreEqual(11, GameData.Lines.Count);

            var ids = new HashSet<string>();
            int stageCount = 0;
            foreach (var line in GameData.Lines)
            {
                stageCount += line.StageIds.Length;
                Assert.AreEqual(line.StageIds.Length - 1, line.EvolutionLevels.Length);
                foreach (string id in line.StageIds)
                {
                    Assert.True(ids.Add(id), $"duplicate roster id: {id}");
                    Assert.NotNull(GameData.SpeciesById(id));
                    Assert.AreEqual(line.BaseId, GameData.BaseId(id));
                }
            }
            Assert.AreEqual(30, stageCount);
        }

        [Test]
        public void StarterEvolutionLevels_MatchTheApprovedDesign()
        {
            foreach (string starter in new[] { "addmander", "tenfin", "shapling" })
                CollectionAssert.AreEqual(new[] { 8, 15 }, GameData.LineFor(starter).EvolutionLevels);
        }

        [Test]
        public void EveryEvolutionTrial_UsesItsFamiliesApprovedMathAffinity()
        {
            Assert.AreEqual(PuzzleAffinity.Formula, GameData.LineFor("addmander").Affinity);
            Assert.AreEqual(PuzzleAffinity.MakeTen, GameData.LineFor("tenfin").Affinity);
            Assert.AreEqual(PuzzleAffinity.Pattern, GameData.LineFor("shapling").Affinity);
            Assert.AreEqual(PuzzleAffinity.Counting, GameData.LineFor("countipillar").Affinity);
            Assert.AreEqual(PuzzleAffinity.RepeatedAddition, GameData.LineFor("doublit").Affinity);
            Assert.AreEqual(PuzzleAffinity.Symmetry, GameData.LineFor("mirrowl").Affinity);
        }

        [Test]
        public void AllRosterForms_HaveAPlayableThemeSkill()
        {
            foreach (var species in GameData.Roster)
            {
                var player = GameData.PlayerMon(species.Id, GameData.StageIndex(species.Id));
                Assert.AreEqual(species.Id, player.Id);
                Assert.NotNull(System.Array.Find(player.Skills, skill => skill.Id == "tackle"));
                Assert.NotNull(System.Array.Find(player.Skills, skill => skill.Type == SkillType.Formula));
                Assert.Greater(player.AttackPower, 0);
                Assert.Greater(player.DefensePower, 0);
            }
        }

        [Test]
        public void ElevenFamilies_HaveElevenDistinctThemeVisualsAndIcons()
        {
            var visuals = new HashSet<SkillVisualKind>();
            var icons = new HashSet<string>();
            var ids = new HashSet<string>();
            foreach (var line in GameData.Lines)
            {
                var player = GameData.PlayerMon(line.BaseId, 0);
                var theme = System.Array.Find(player.Skills, skill => skill.Type == SkillType.Formula);
                Assert.NotNull(theme, line.BaseId);
                Assert.True(visuals.Add(theme.Visual), $"duplicate visual: {theme.Visual}");
                Assert.True(icons.Add(theme.IconResource), $"duplicate icon: {theme.IconResource}");
                Assert.True(ids.Add(theme.Id), $"duplicate skill id: {theme.Id}");
            }
            Assert.AreEqual(11, visuals.Count);
        }
    }
}
