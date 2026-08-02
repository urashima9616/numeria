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
        }

        [Test]
        public void PlayerMon_KitsHaveFormulaSkill()
        {
            foreach (string id in new[] { "countipillar", "doublit", "mirrowl", "tenfin", "shapling" })
            {
                var def = GameData.PlayerMon(id, evolved: false);
                Assert.AreEqual(id, def.Id);
                Assert.AreEqual(10, def.MaxHp);
                Assert.NotNull(System.Array.Find(def.Skills, s => s.Id == "tackle"));
                var formula = System.Array.Find(def.Skills, s => s.Id == "flame-formula");
                Assert.NotNull(formula);
                Assert.AreEqual(SkillType.Formula, formula.Type);
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
        public void LaunchRoster_HasFifteenUniqueSpeciesAcrossSixLines()
        {
            Assert.AreEqual(15, GameData.Roster.Count);
            Assert.AreEqual(6, GameData.Lines.Count);

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
            Assert.AreEqual(15, stageCount);
        }

        [Test]
        public void AllRosterForms_HaveAPlayableThemeSkill()
        {
            foreach (var species in GameData.Roster)
            {
                var player = GameData.PlayerMon(species.Id, GameData.StageIndex(species.Id));
                Assert.AreEqual(species.Id, player.Id);
                Assert.NotNull(System.Array.Find(player.Skills, skill => skill.Id == "tackle"));
                Assert.NotNull(System.Array.Find(player.Skills, skill => skill.Id == "flame-formula"));
            }
        }
    }
}
