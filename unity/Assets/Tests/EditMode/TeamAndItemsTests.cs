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
        public void DuplicateCatch_ReportsUpgradeOnlyForAStrongerWildPartner()
        {
            var p = new Progress();
            Assert.AreEqual(CatchRosterResult.Added, p.AddCaught("doublit", 7));
            Assert.AreEqual(7, p.EnsureGrowth("doublit").Level);
            Assert.AreEqual(CatchRosterResult.Duplicate, p.AddCaught("doublit", 6));
            Assert.AreEqual(CatchRosterResult.UpgradeAvailable, p.AddCaught("doublit", 9));
            Assert.AreEqual(7, p.EnsureGrowth("doublit").Level, "The choice must not mutate the roster yet.");
            Assert.False(p.Catch("addmander")); // starter 永远已在队伍中
        }

        [Test]
        public void CatchPreservesWildLevelEvolutionStageAndExactBattleStats()
        {
            var p = new Progress();
            var wild = GameData.CreateWild("stackstone", 13, new Rng(7));
            wild.AttackPower += 2;
            wild.DefensePower += 1;

            Assert.AreEqual(CatchRosterResult.Added, p.AddCaught(wild));

            var caught = p.EnsureGrowth("pebblit");
            Assert.AreEqual(13, caught.Level);
            Assert.AreEqual(1, caught.Stage);
            Assert.AreEqual("stackstone", p.CurrentFormId("pebblit"));
            var buddy = p.PlayerCombatant("pebblit");
            Assert.AreEqual(wild.MaxHp, buddy.MaxHp);
            Assert.AreEqual(wild.AttackPower, buddy.AttackPower);
            Assert.AreEqual(wild.DefensePower, buddy.DefensePower);

            int hpOffset = caught.CapturedHpOffset;
            int attackOffset = caught.CapturedAttackOffset;
            int defenseOffset = caught.CapturedDefenseOffset;
            caught.GainXp(caught.XpToNext);
            var leveled = p.PlayerCombatant("pebblit");
            var standard = GameData.PlayerMon("pebblit", 1, 14);
            Assert.AreEqual(standard.MaxHp + hpOffset, leveled.MaxHp);
            Assert.AreEqual(standard.AttackPower + attackOffset, leveled.AttackPower);
            Assert.AreEqual(standard.DefensePower + defenseOffset, leveled.DefensePower);
        }

        [Test]
        public void StrongerDuplicateCanReplaceOwnedGrowthWithoutLosingBonuses()
        {
            var p = new Progress();
            p.AddCaught("pebblit", 8);
            p.EnsureGrowth("pebblit").AttackBonus = 2;
            var stronger = GameData.CreateWild("stackstone", 12, new Rng(11));
            stronger.AttackPower += 1;
            stronger.DefensePower += 2;

            Assert.AreEqual(CatchRosterResult.UpgradeAvailable, p.AddCaught(stronger));
            Assert.True(p.AdoptCaptured(stronger));
            Assert.AreEqual(12, p.EnsureGrowth("pebblit").Level);
            Assert.AreEqual(1, p.EnsureGrowth("pebblit").Stage);
            Assert.AreEqual(2, p.EnsureGrowth("pebblit").AttackBonus);
            var adopted = p.PlayerCombatant("pebblit");
            Assert.AreEqual(stronger.MaxHp, adopted.MaxHp);
            Assert.AreEqual(stronger.AttackPower, adopted.AttackPower);
            Assert.AreEqual(stronger.DefensePower, adopted.DefensePower);
            Assert.False(p.AdoptCaptured("pebblit", 7));
        }

        [Test]
        public void TeamCapacity_IsNinetyNine_AndFullCatchRequiresAChoice()
        {
            var p = new Progress();
            Assert.AreEqual(1, p.TeamCount);
            Assert.AreEqual(99, Progress.TeamCapacity);

            for (int i = 0; i < 98; i++)
                Assert.AreEqual(CatchRosterResult.Added, p.AddCaught($"future-family-{i}"));

            Assert.AreEqual(99, p.TeamCount);
            Assert.True(p.TeamIsFull);
            Assert.AreEqual(CatchRosterResult.Full, p.AddCaught("overflow-family"));
            Assert.AreEqual(CatchRosterResult.Duplicate, p.AddCaught("future-family-4"));
        }

        [Test]
        public void FullTeamReplacement_ReleasesGrowthAndUnequipsAccessories()
        {
            var p = new Progress();
            for (int i = 0; i < 98; i++) p.Catch($"future-family-{i}");
            p.ActiveMonId = "future-family-3";
            p.AddAccessory("keepsake", "Counting Charm", 1, 0);
            Assert.True(p.EquipAccessory("keepsake", "future-family-3"));

            var newcomer = GameData.CreateWild("stackstone", 14, new Rng(13));
            newcomer.AttackPower += 2;
            Assert.True(p.ReplaceCaught("future-family-3", newcomer));
            Assert.AreEqual(99, p.TeamCount);
            Assert.False(p.CaughtIds.Contains("future-family-3"));
            Assert.True(p.CaughtIds.Contains("pebblit"));
            Assert.AreEqual("pebblit", p.ActiveMonId);
            Assert.IsNull(p.FindGrowth("future-family-3"));
            Assert.AreEqual(14, p.FindGrowth("pebblit").Level);
            Assert.AreEqual(1, p.FindGrowth("pebblit").Stage);
            Assert.AreEqual(newcomer.MaxHp, p.PlayerCombatant("pebblit").MaxHp);
            Assert.AreEqual(newcomer.AttackPower, p.PlayerCombatant("pebblit").AttackPower);
            Assert.AreEqual(newcomer.DefensePower, p.PlayerCombatant("pebblit").DefensePower);
            Assert.AreEqual("", p.Accessories[0].EquippedToBaseId);
            Assert.False(p.ReplaceCaught("addmander", "another-family"));
        }

        [Test]
        public void ConversionRewards_IncreaseAtEveryLevel()
        {
            int previousCoins = 0;
            int previousXp = 0;
            for (int level = 1; level <= GrowthSystem.MaxLevel; level++)
            {
                int coins = MathmonConversionSystem.CoinsForLevel(level);
                int xp = MathmonConversionSystem.XpForLevel(level);
                Assert.Greater(coins, previousCoins, $"coin reward at Lv.{level}");
                Assert.Greater(xp, previousXp, $"XP reward at Lv.{level}");
                previousCoins = coins;
                previousXp = xp;
            }
            Assert.AreEqual(3, MathmonConversionSystem.CoinsForLevel(1));
            Assert.AreEqual(202, MathmonConversionSystem.XpForLevel(99));
        }

        [Test]
        public void ConvertingActiveMathmonForCoins_RemovesGrowthAndReturnsAccessory()
        {
            var p = new Progress();
            Assert.AreEqual(CatchRosterResult.Added, p.AddCaught("countipillar", 12));
            p.ActiveMonId = "countipillar";
            p.AddAccessory("keepsake", "Counting Charm", 1, 0);
            Assert.True(p.EquipAccessory("keepsake", "countipillar"));

            Assert.AreEqual(14, p.ConvertCaught("numberfly", MathmonConversionReward.Coins));
            Assert.AreEqual(14, p.Coins);
            Assert.AreEqual(14, p.Records.CoinsEarned);
            Assert.AreEqual("addmander", p.ActiveMonId);
            Assert.False(p.Owns("countipillar"));
            Assert.IsNull(p.FindGrowth("countipillar"));
            Assert.AreEqual("", p.Accessories[0].EquippedToBaseId);
        }

        [Test]
        public void ConvertingMathmonForXp_RewardsBattleBuddyAndProtectsStarter()
        {
            var p = new Progress();
            Assert.AreEqual(CatchRosterResult.Added, p.AddCaught("glimlet", 5));

            Assert.AreEqual(14, p.ConvertCaught("glimlet", MathmonConversionReward.Experience));
            Assert.AreEqual(14, p.EnsureGrowth("addmander").Xp);
            Assert.AreEqual(14, p.Records.TotalXpEarned);
            Assert.AreEqual(0, p.ConvertCaught("addmander", MathmonConversionReward.Coins));
            Assert.AreEqual(0, p.ConvertCaught("not-owned", MathmonConversionReward.Experience));
        }

        [Test]
        public void ExpandedRoster_HasNinetyThreeUniqueSpeciesAcrossThirtyTwoLines()
        {
            Assert.AreEqual(93, GameData.Roster.Count);
            Assert.AreEqual(32, GameData.Lines.Count);

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
            Assert.AreEqual(93, stageCount);
        }

        [Test]
        public void FourRequestedElementsEachAddFiveCompleteThreeStageFamilies()
        {
            var requested = new Dictionary<string, string[]>
            {
                { "FAIRY", new[] { "glimlet", "moonmote", "charmite", "wishwink", "pixipip" } },
                { "DRAGON", new[] { "addling", "dracount", "loopling", "twinsting", "shardrake" } },
                { "ELECTRIC", new[] { "voltlet", "sparkit", "chargecub", "flickerfin", "switchick" } },
                { "GRASS", new[] { "budsum", "clovercub", "sprouturn", "mossbit", "seedseq" } },
            };

            foreach (var pair in requested)
            {
                Assert.AreEqual(5, pair.Value.Length, pair.Key);
                foreach (string baseId in pair.Value)
                {
                    var line = GameData.LineFor(baseId);
                    Assert.NotNull(line, baseId);
                    Assert.AreEqual(pair.Key, line.Element, baseId);
                    Assert.AreEqual(3, line.StageIds.Length, baseId);
                    Assert.AreEqual(2, line.EvolutionLevels.Length, baseId);
                }
            }
        }

        [Test]
        public void FlyingTypeAddsOneCompleteSkipCountingFamily()
        {
            var line = GameData.LineFor("numblet");
            Assert.NotNull(line);
            Assert.AreEqual("FLYING", line.Element);
            Assert.AreEqual(PuzzleAffinity.RepeatedAddition, line.Affinity);
            CollectionAssert.AreEqual(new[] { "numblet", "tallywing", "totalon" }, line.StageIds);
            CollectionAssert.AreEqual(new[] { 12, 24 }, line.EvolutionLevels);

            var skill = System.Array.Find(GameData.PlayerMon("numblet", 0).Skills,
                candidate => candidate.Type == SkillType.Formula);
            Assert.AreEqual(SkillVisualKind.FlyingGust, skill.Visual);
            Assert.AreEqual("generated/Skills/flying_gust", skill.IconResource);
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
        public void LaunchElevenFamilies_KeepTheirDistinctThemeVisualsAndIcons()
        {
            var visuals = new HashSet<SkillVisualKind>();
            var icons = new HashSet<string>();
            var ids = new HashSet<string>();
            for (int i = 0; i < 11; i++)
            {
                var line = GameData.Lines[i];
                var player = GameData.PlayerMon(line.BaseId, 0);
                var theme = System.Array.Find(player.Skills, skill => skill.Type == SkillType.Formula);
                Assert.NotNull(theme, line.BaseId);
                Assert.True(visuals.Add(theme.Visual), $"duplicate visual: {theme.Visual}");
                Assert.True(icons.Add(theme.IconResource), $"duplicate icon: {theme.IconResource}");
                Assert.True(ids.Add(theme.Id), $"duplicate skill id: {theme.Id}");
            }
            Assert.AreEqual(11, visuals.Count);
        }

        [Test]
        public void RequestedElementsHaveDedicatedVisualLanguages()
        {
            var expected = new Dictionary<string, SkillVisualKind>
            {
                { "glimlet", SkillVisualKind.FairyGlimmer },
                { "addling", SkillVisualKind.DragonSpiral },
                { "voltlet", SkillVisualKind.ElectricBolt },
                { "budsum", SkillVisualKind.GrassBloom },
                { "numblet", SkillVisualKind.FlyingGust },
            };
            foreach (var pair in expected)
            {
                var player = GameData.PlayerMon(pair.Key, 0);
                var theme = System.Array.Find(player.Skills, skill => skill.Type == SkillType.Formula);
                Assert.AreEqual(pair.Value, theme.Visual, pair.Key);
            }
        }
    }
}
