using System;
using System.IO;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    public sealed class SaveSlotSummary
    {
        public int Slot;
        public bool Exists;
        public string MathmonName = "EMPTY";
        public int Level;
        public string MapName = "";
        public string UpdatedAt = "";
    }

    /// <summary>十槽 JSON 本地存档；当前槽自动保存，也可在菜单中手动覆盖或读取其他槽。</summary>
    public static class SaveSystem
    {
        public const int SlotCount = 10;

        private static string _storageRootOverride;
        private static int _activeSlot;
        private static string Root => string.IsNullOrEmpty(_storageRootOverride)
            ? Application.persistentDataPath : _storageRootOverride;
        private static string LegacyPath => Path.Combine(Root, "numeria-save.json");
        private static string ActiveSlotPath => Path.Combine(Root, "numeria-active-slot.txt");
        private static string SlotPath(int slot) => Path.Combine(Root, $"numeria-save-slot-{slot}.json");

        public static int ActiveSlot
        {
            get
            {
                if (_activeSlot == 0) _activeSlot = ReadActiveSlot();
                return _activeSlot;
            }
        }

        public static bool IsValidSlot(int slot) => slot >= 1 && slot <= SlotCount;

        public static Progress Load()
        {
            var loaded = LoadFromSlot(ActiveSlot);
            if (loaded != null) return loaded;

            // v1–v6 单文件存档只复制到槽 1，不删除原文件，迁移可逆且不会丢档。
            if (ActiveSlot == 1)
            {
                loaded = ReadProgress(LegacyPath);
                if (loaded != null)
                {
                    SaveToSlot(loaded, 1);
                    return loaded;
                }
            }
            return new Progress();
        }

        public static void Save(Progress progress) => SaveToSlot(progress, ActiveSlot);

        public static void SaveToSlot(Progress progress, int slot)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            if (!IsValidSlot(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
            Directory.CreateDirectory(Root);
            progress.ApplyMigrations();
            string path = SlotPath(slot);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(progress));
            if (File.Exists(path))
            {
                try { File.Replace(temp, path, null); }
                catch
                {
                    File.Copy(temp, path, true);
                    File.Delete(temp);
                }
            }
            else File.Move(temp, path);
            SetActiveSlot(slot);
        }

        public static Progress LoadFromSlot(int slot)
        {
            if (!IsValidSlot(slot)) return null;
            var loaded = ReadProgress(SlotPath(slot));
            if (loaded == null) return null;
            SetActiveSlot(slot);
            return loaded;
        }

        public static bool SlotExists(int slot) => IsValidSlot(slot) && File.Exists(SlotPath(slot));

        public static SaveSlotSummary GetSlotSummary(int slot)
        {
            var summary = new SaveSlotSummary { Slot = slot };
            if (!IsValidSlot(slot)) return summary;
            string path = SlotPath(slot);
            var progress = ReadProgress(path);
            if (progress == null) return summary;
            var growth = progress.ActiveGrowth;
            var species = GameData.SpeciesById(progress.CurrentFormId(progress.ActiveMonId));
            summary.Exists = true;
            summary.MathmonName = species?.Name ?? "MATHMON";
            summary.Level = growth.Level;
            summary.MapName = progress.CurrentMap;
            summary.UpdatedAt = File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
            return summary;
        }

        public static void DeleteSlot(int slot)
        {
            if (!IsValidSlot(slot)) return;
            string path = SlotPath(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        public static void Delete() => DeleteSlot(ActiveSlot);

        private static Progress ReadProgress(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var loaded = JsonUtility.FromJson<Progress>(File.ReadAllText(path));
                if (loaded == null) return null;
                loaded.ApplyMigrations();
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save load failed for {path}: {e.Message}");
                return null;
            }
        }

        private static int ReadActiveSlot()
        {
            try
            {
                if (File.Exists(ActiveSlotPath) && int.TryParse(File.ReadAllText(ActiveSlotPath), out int slot) &&
                    IsValidSlot(slot)) return slot;
            }
            catch (Exception e) { Debug.LogWarning($"Active save slot read failed: {e.Message}"); }
            return 1;
        }

        private static void SetActiveSlot(int slot)
        {
            _activeSlot = slot;
            Directory.CreateDirectory(Root);
            File.WriteAllText(ActiveSlotPath, slot.ToString());
        }

#if UNITY_EDITOR
        /// <summary>仅供 EditMode 测试隔离 persistentDataPath。</summary>
        public static void SetStorageRootForTests(string path)
        {
            _storageRootOverride = path;
            _activeSlot = 0;
        }

        public static void ResetStorageRootForTests()
        {
            _storageRootOverride = null;
            _activeSlot = 0;
        }
#endif
    }
}
