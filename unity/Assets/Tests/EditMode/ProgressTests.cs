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
            Assert.AreEqual(10, p.XpToNext); // Lv1 → 10 xp

            Assert.AreEqual(0, p.GainXp(5));
            Assert.AreEqual(5, p.Xp);
            Assert.AreEqual(1, p.Level);

            Assert.AreEqual(1, p.GainXp(5)); // 10/10 → Lv2
            Assert.AreEqual(0, p.Xp);
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(1, p.AttackBonus);
        }

        [Test]
        public void GainXp_MultipleLevelsAtOnce()
        {
            var p = new Progress();
            int gained = p.GainXp(35); // 10(→2) + 20(→3) = 30,剩 5
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, p.Level);
            Assert.AreEqual(5, p.Xp);
            Assert.AreEqual(2, p.AttackBonus);
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
            var r = s.UseSkill("tackle"); // 2 + 2 = 4,无盾
            Assert.AreEqual(4, r.Damage);
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
    }
}
