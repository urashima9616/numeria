using System;
using System.IO;
using NUnit.Framework;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game.Tests
{
    public class SaveSystemTests
    {
        private string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "numeria-save-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            SaveSystem.SetStorageRootForTests(_root);
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.ResetStorageRootForTests();
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void AllTenSlotsSaveLoadAndReportIndependentSummaries()
        {
            Assert.AreEqual(10, SaveSystem.SlotCount);
            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                var progress = new Progress { CurrentMap = slot % 2 == 0 ? "mountains" : "forest" };
                progress.ActiveGrowth.Level = slot;
                SaveSystem.SaveToSlot(progress, slot);
                Assert.True(SaveSystem.SlotExists(slot));
            }

            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
            {
                var loaded = SaveSystem.LoadFromSlot(slot);
                Assert.NotNull(loaded);
                Assert.AreEqual(slot, loaded.ActiveGrowth.Level);
                Assert.AreEqual(slot, SaveSystem.ActiveSlot);
                var summary = SaveSystem.GetSlotSummary(slot);
                Assert.True(summary.Exists);
                Assert.AreEqual(slot, summary.Level);
                Assert.AreEqual("Addmander", summary.MathmonName);
            }
            Assert.IsNull(SaveSystem.LoadFromSlot(0));
            Assert.IsNull(SaveSystem.LoadFromSlot(11));
        }

        [Test]
        public void AutosaveWritesOnlyTheActiveSlotAndDeleteRemovesOnlyThatSlot()
        {
            var first = new Progress();
            first.ActiveGrowth.Level = 3;
            SaveSystem.SaveToSlot(first, 1);
            var second = new Progress();
            second.ActiveGrowth.Level = 8;
            SaveSystem.SaveToSlot(second, 2);

            second.ActiveGrowth.Level = 9;
            SaveSystem.Save(second);
            Assert.AreEqual(3, SaveSystem.LoadFromSlot(1).ActiveGrowth.Level);
            Assert.AreEqual(9, SaveSystem.LoadFromSlot(2).ActiveGrowth.Level);
            SaveSystem.Delete();
            Assert.False(SaveSystem.SlotExists(2));
            Assert.True(SaveSystem.SlotExists(1));
        }

        [Test]
        public void LegacySingleSaveMigratesNonDestructivelyIntoSlotOne()
        {
            string legacy = Path.Combine(_root, "numeria-save.json");
            File.WriteAllText(legacy, "{\"SaveVersion\":5,\"Level\":4,\"ActiveMonId\":\"addmander\"}");
            var loaded = SaveSystem.Load();
            Assert.AreEqual(4, loaded.ActiveGrowth.Level);
            Assert.True(SaveSystem.SlotExists(1));
            Assert.True(File.Exists(legacy));
        }

        [Test]
        public void StartNewGameReplacesLegacyWorldStateWithCleanProgress()
        {
            string legacy = Path.Combine(_root, "numeria-save.json");
            File.WriteAllText(legacy,
                "{\"SaveVersion\":5,\"StoryIntroSeen\":true,\"OpenedChests\":[\"forest-chest-7-3\"]," +
                "\"BossBeaten\":true,\"ActiveMonId\":\"addmander\"}");

            var migrated = SaveSystem.Load();
            Assert.True(migrated.StoryIntroSeen);
            Assert.Contains("forest-chest-7-3", migrated.OpenedChests);

            var fresh = SaveSystem.StartNewGame(1);
            Assert.False(fresh.StoryIntroSeen);
            Assert.IsEmpty(fresh.OpenedChests);
            Assert.False(fresh.BossBeaten);

            var reloaded = SaveSystem.Load();
            Assert.False(reloaded.StoryIntroSeen);
            Assert.IsEmpty(reloaded.OpenedChests);
            Assert.False(reloaded.BossBeaten);
            Assert.True(File.Exists(legacy), "Starting a new game should not destructively delete the legacy backup.");
        }

        [Test]
        public void StartNewGameOnlyOverwritesTheSelectedSlot()
        {
            var existing = new Progress();
            existing.OpenedChests.Add("mountains-chest-4-4");
            SaveSystem.SaveToSlot(existing, 2);

            var fresh = SaveSystem.StartNewGame(1);

            Assert.AreEqual(1, SaveSystem.ActiveSlot);
            Assert.IsEmpty(fresh.OpenedChests);
            Assert.Contains("mountains-chest-4-4", SaveSystem.LoadFromSlot(2).OpenedChests);
        }

        [Test]
        public void EquippedAccessoriesRoundTripInsideTheirOwnSaveSlot()
        {
            var progress = new Progress();
            progress.AddAccessory("forest-chest", "Power Acorn", 1, 0);
            Assert.True(progress.EquipAccessory("forest-chest", "addmander"));
            SaveSystem.SaveToSlot(progress, 7);

            var loaded = SaveSystem.LoadFromSlot(7);
            Assert.AreEqual(1, loaded.Accessories.Count);
            Assert.AreEqual("addmander", loaded.Accessories[0].EquippedToBaseId);
            Assert.AreEqual(1, loaded.AccessoryAttackBonus("addmander"));
            Assert.AreEqual(0, loaded.AccessoryAttackBonus("countipillar"));
        }

        [Test]
        public void MenuBuildsAccessorySlotsAndAllTenSaveRows()
        {
            var rootObject = new GameObject("TestCanvas", typeof(RectTransform));
            bool sfxWasEnabled = Sfx.Enabled;
            try
            {
                Sfx.Enabled = false; // EditMode 不允许 Sfx 创建 DontDestroyOnLoad AudioSource。
                var progress = new Progress();
                MenuUi.Open((RectTransform)rootObject.transform, progress, () => { }, _ => { },
                    _ => { }, _ => { });
                Assert.NotNull(Find(rootObject, "AccessorySlot0"));
                Assert.NotNull(Find(rootObject, "AccessorySlot1"));

                var savesTab = Find(rootObject, "Tab-saves");
                Assert.NotNull(savesTab);
                savesTab.GetComponent<Button>().onClick.Invoke();
                for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
                    Assert.NotNull(Find(rootObject, $"SaveSlot{slot}"));

                var settingsTab = Find(rootObject, "Tab-settings");
                settingsTab.GetComponent<Button>().onClick.Invoke();
                Assert.NotNull(Find(rootObject, "BtnReturnToMenu"));
                Assert.IsNull(Find(rootObject, "BtnReset"));
                Find(rootObject, "BtnReturnToMenu").GetComponent<Button>().onClick.Invoke();
                Assert.NotNull(Find(rootObject, "ReturnMenuConfirm"));
                Assert.NotNull(Find(rootObject, "BtnSaveAndReturn"));
                Assert.NotNull(Find(rootObject, "BtnReturnWithoutSaving"));
                Assert.NotNull(Find(rootObject, "BtnCancelReturn"));
            }
            finally
            {
                Sfx.Enabled = sfxWasEnabled;
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        private static GameObject Find(GameObject root, string name)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (transform.name == name) return transform.gameObject;
            return null;
        }
    }
}
