using NUnit.Framework;
using Numeria.Core;

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
            foreach (string id in new[] { "countipillar", "doublit" })
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
    }
}
