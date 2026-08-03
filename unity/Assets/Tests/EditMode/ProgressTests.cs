using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Core.Tests
{
    public class ProgressTests
    {
        [Test]
        public void GainXp_LevelsUp_AndGrantsAttackBonus()
        {
            var p = new Progress();
            Assert.AreEqual(16, p.XpToNext);

            Assert.AreEqual(0, p.GainXp(8));
            Assert.AreEqual(8, p.Xp);
            Assert.AreEqual(1, p.Level);

            Assert.AreEqual(1, p.GainXp(8));
            Assert.AreEqual(0, p.Xp);
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(0, p.AttackBonus); // 升级成长由物种曲线计算，不伪装成装备加成
            Assert.AreEqual(0, p.DefenseBonus);
        }

        [Test]
        public void GainXp_MultipleLevelsAtOnce()
        {
            var p = new Progress();
            int gained = p.GainXp(43); // 16(→2) + 22(→3) = 38,剩 5
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, p.Level);
            Assert.AreEqual(5, p.Xp);
            Assert.AreEqual(0, p.AttackBonus);
            Assert.AreEqual(0, p.DefenseBonus);
        }

        [Test]
        public void Catch_And_Chest_AreIdempotent()
        {
            var p = new Progress();
            Assert.True(p.Catch("countipillar"));
            Assert.False(p.Catch("countipillar"));
            Assert.AreEqual(1, p.CaughtIds.Count);

            Assert.True(p.OpenChest("chest-3-4"));
            Assert.False(p.OpenChest("chest-3-4"));
        }

        [Test]
        public void AttackBonus_IncreasesSkillDamage()
        {
            var s = new BattleState(GameData.Addmander(), GameData.Countipillar());
            s.PlayerAttackBonus = 2;
            var r = s.UseSkill("tackle");
            Assert.That(r.Damage, Is.InRange(4, 6));
        }

        [Test]
        public void Countipillar_IsCatchable_Duplirock_IsNot()
        {
            Assert.True(GameData.Countipillar().Catchable);
            Assert.False(GameData.Duplirock().Catchable);
        }

        [Test]
        public void AudioSettings_DefaultOn_AndRemainOnForLegacySaves()
        {
            var fresh = new Progress();
            Assert.True(fresh.VoiceEnabled);
            Assert.True(fresh.SfxEnabled);
            Assert.True(fresh.MusicEnabled);

            // 旧存档没有新增字段；字段初始化值必须保留，避免升级后突然静音。
            var legacy = JsonUtility.FromJson<Progress>("{\"Level\":3,\"VoiceEnabled\":true}");
            legacy.ApplyMigrations();
            Assert.True(legacy.SfxEnabled);
            Assert.True(legacy.MusicEnabled);
            Assert.AreEqual(Progress.CurrentSaveVersion, legacy.SaveVersion);
        }

        [Test]
        public void Growth_IsIndependentForEachCaughtFamily()
        {
            var progress = new Progress();
            progress.Catch("countipillar");
            progress.ActiveMonId = "countipillar";
            Assert.AreEqual(1, progress.ActiveGrowth.Level);
            Assert.AreEqual(1, progress.GainXp(16));
            Assert.AreEqual(2, progress.EnsureGrowth("countipillar").Level);
            Assert.AreEqual(1, progress.EnsureGrowth("addmander").Level);
        }

        [Test]
        public void LevelCapIsNinetyNine()
        {
            var growth = new MonGrowth { Level = 98 };
            Assert.AreEqual(1, growth.GainXp(100000));
            Assert.AreEqual(99, growth.Level);
            Assert.AreEqual(0, growth.Xp);
            Assert.AreEqual(0, growth.XpToNext);
        }

        [Test]
        public void ConsumablesAreCountedAndUsedIdempotently()
        {
            var p = new Progress();
            p.AddConsumable(ConsumableType.HealthPotion, 2);
            p.AddConsumable(ConsumableType.GemSnack);
            Assert.True(p.UseConsumable(ConsumableType.HealthPotion));
            Assert.AreEqual(1, p.HealthPotions);
            Assert.AreEqual(1, p.GemSnacks);
            Assert.AreEqual(1, p.Records.ConsumablesUsed);
        }

        [Test]
        public void VictoryXpRewardsSpeciesLevelDifferenceAndBossDifficulty()
        {
            int ordinary = GrowthSystem.VictoryXp(7, 10, 10, false);
            int rarer = GrowthSystem.VictoryXp(12, 10, 10, false);
            int overLevel = GrowthSystem.VictoryXp(7, 15, 10, false);
            int boss = GrowthSystem.VictoryXp(7, 10, 10, true);
            Assert.Greater(rarer, ordinary);
            Assert.Greater(overLevel, ordinary);
            Assert.Greater(boss, ordinary * 2);
        }

        [Test]
        public void EvolutionStones_AreMilestonesForSecondAndThirdForms()
        {
            var progress = new Progress();
            var growth = progress.ActiveGrowth;
            growth.Level = 8;
            progress.AddEvolutionStone();

            Assert.True(progress.CanEvolve("addmander"));
            Assert.True(progress.AdvanceEvolution("addmander"));
            Assert.AreEqual("sumdrake", progress.CurrentFormId("addmander"));
            Assert.AreEqual(1, progress.EvolutionStones); // key item 不消耗
            Assert.False(progress.CanEvolve("addmander"));

            growth.Level = 15;
            Assert.False(progress.CanEvolve("addmander")); // 三段需要天空城第二颗石头
            progress.AddEvolutionStone();
            Assert.True(progress.CanEvolve("addmander"));
            Assert.True(progress.AdvanceEvolution("addmander"));
            Assert.AreEqual("equadragon", progress.CurrentFormId("addmander"));
        }

        [Test]
        public void LegacyEvolutionFields_MigrateIntoPerFamilyGrowth()
        {
            var legacy = JsonUtility.FromJson<Progress>(
                "{\"SaveVersion\":2,\"Level\":6,\"Xp\":4,\"AttackBonus\":3," +
                "\"Evolved\":true,\"HasEvoStone\":true,\"ActiveMonId\":\"addmander\"}");
            legacy.ApplyMigrations();

            var growth = legacy.EnsureGrowth("addmander");
            Assert.AreEqual(6, growth.Level);
            Assert.AreEqual(4, growth.Xp);
            Assert.AreEqual(3, growth.AttackBonus);
            Assert.AreEqual(3, growth.DefenseBonus); // Lv.6 旧存档按每两级 +1 补齐
            Assert.AreEqual(1, growth.Stage);
            Assert.AreEqual(1, legacy.EvolutionStones);
        }
    }
}
