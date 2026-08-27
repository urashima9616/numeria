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
        public void BackupRoundTripRestoresEverySlotAndCreatesPreImportSafetyCopy()
        {
            var first = new Progress { CurrentMap = "forest", Coins = 41 };
            first.OpenedChests.Add("forest-chest-7-3");
            first.ActiveGrowth.Level = 6;
            SaveSystem.SaveToSlot(first, 1);
            var third = new Progress { CurrentMap = "dark_mines", Coins = 311 };
            third.ActiveGrowth.Level = 27;
            SaveSystem.SaveToSlot(third, 3);

            string exported = SaveSystem.ExportBackup("test");
            Assert.True(File.Exists(exported));

            var changed = new Progress { CurrentMap = "sky", Coins = 999 };
            SaveSystem.SaveToSlot(changed, 1);
            SaveSystem.SaveToSlot(new Progress(), 2);
            SaveSystem.DeleteSlot(3);

            var imported = SaveSystem.ImportBackup(exported);

            Assert.True(imported.Success, imported.Message);
            Assert.AreEqual(3, SaveSystem.ActiveSlot);
            Assert.AreEqual(41, SaveSystem.LoadFromSlot(1).Coins);
            Assert.Contains("forest-chest-7-3", SaveSystem.LoadFromSlot(1).OpenedChests);
            Assert.False(SaveSystem.SlotExists(2), "Import should restore the backup's exact slot set.");
            Assert.AreEqual(311, SaveSystem.LoadFromSlot(3).Coins);
            Assert.AreEqual("dark_mines", imported.Progress.CurrentMap);
            Assert.True(File.Exists(imported.SafetyBackupPath));
            StringAssert.Contains("pre-import", Path.GetFileName(imported.SafetyBackupPath));
        }

        [Test]
        public void InvalidBackupIsRejectedBeforeCurrentSaveChanges()
        {
            var current = new Progress { Coins = 73, CurrentMap = "mountains" };
            SaveSystem.SaveToSlot(current, 1);
            string invalid = Path.Combine(_root, "numeria-backup-invalid.json");
            File.WriteAllText(invalid,
                "{\"Magic\":\"NOT_NUMERIA\",\"FormatVersion\":1,\"ActiveSlot\":1,\"Slots\":[]}");

            var result = SaveSystem.ImportBackup(invalid);

            Assert.False(result.Success);
            Assert.AreEqual(73, SaveSystem.LoadFromSlot(1).Coins);
            Assert.AreEqual("mountains", SaveSystem.LoadFromSlot(1).CurrentMap);
            Assert.IsEmpty(result.SafetyBackupPath, "Validation failures must not create or overwrite anything.");
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
        public void CapturedLevelEvolutionStageAndBattleStatsRoundTrip()
        {
            var progress = new Progress();
            var wild = GameData.CreateWild("stackstone", 13, new Rng(17));
            wild.AttackPower += 2;
            wild.DefensePower += 1;
            Assert.AreEqual(CatchRosterResult.Added, progress.AddCaught(wild));
            SaveSystem.SaveToSlot(progress, 4);

            var loaded = SaveSystem.LoadFromSlot(4);
            Assert.AreEqual("stackstone", loaded.CurrentFormId("pebblit"));
            Assert.AreEqual(13, loaded.EnsureGrowth("pebblit").Level);
            Assert.AreEqual(1, loaded.EnsureGrowth("pebblit").Stage);
            var buddy = loaded.PlayerCombatant("pebblit");
            Assert.AreEqual(wild.MaxHp, buddy.MaxHp);
            Assert.AreEqual(wild.AttackPower, buddy.AttackPower);
            Assert.AreEqual(wild.DefensePower, buddy.DefensePower);
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
                Assert.NotNull(Find(rootObject, "BtnExportBackup"));
                Assert.NotNull(Find(rootObject, "BtnImportBackup"));

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
