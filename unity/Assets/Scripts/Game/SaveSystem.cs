using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [Serializable]
    public sealed class SaveBackupSlot
    {
        public int Slot;
        public string Json = "";
    }

    [Serializable]
    public sealed class SaveBackupDocument
    {
        public string Magic = SaveSystem.BackupMagic;
        public int FormatVersion = SaveSystem.BackupFormatVersion;
        public int SaveSchemaVersion = Progress.CurrentSaveVersion;
        public string CreatedAtUtc = "";
        public int ActiveSlot = 1;
        public SaveBackupSlot[] Slots = Array.Empty<SaveBackupSlot>();
        public string LegacyJson = "";
    }

    public sealed class SaveBackupImportResult
    {
        public bool Success;
        public string Message = "";
        public string SafetyBackupPath = "";
        public Progress Progress;
    }

    /// <summary>十槽 JSON 本地存档；当前槽自动保存，也可在菜单中手动覆盖或读取其他槽。</summary>
    public static class SaveSystem
    {
        public const int SlotCount = 10;
        public const string BackupMagic = "NUMERIA_SAVE_BACKUP";
        public const int BackupFormatVersion = 1;

        private const string BackupFilePattern = "numeria-backup-*.json";
        private const string BackupFolderName = "Numeria Backups";

        private static string _storageRootOverride;
        private static int _activeSlot;
        private static bool _formerMacIdentityChecked;
        private static string Root => string.IsNullOrEmpty(_storageRootOverride)
            ? Application.persistentDataPath : _storageRootOverride;
        private static string LegacyPath => Path.Combine(Root, "numeria-save.json");
        private static string ActiveSlotPath => Path.Combine(Root, "numeria-active-slot.txt");
        private static string SlotPath(int slot) => Path.Combine(Root, $"numeria-save-slot-{slot}.json");
        private static string BackupRoot => Path.Combine(Root, BackupFolderName);

        public static int ActiveSlot
        {
            get
            {
                MigrateFormerMacIdentityIfNeeded();
                if (_activeSlot == 0) _activeSlot = ReadActiveSlot();
                return _activeSlot;
            }
        }

        public static bool IsValidSlot(int slot) => slot >= 1 && slot <= SlotCount;

        /// <summary>
        /// 在指定槽创建一份完全干净的新进度。直接覆盖目标槽，而不是先删除再 Load，
        /// 因此旧版单文件存档不会被迁移回来，已开启宝箱等世界状态也不会残留。
        /// </summary>
        public static Progress StartNewGame(int slot)
        {
            if (!IsValidSlot(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
            var progress = new Progress();
            SaveToSlot(progress, slot);
            return progress;
        }

        public static Progress Load()
        {
            MigrateFormerMacIdentityIfNeeded();
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

        /// <summary>
        /// 将十个槽、当前槽指针和旧版单文件打包为一个可通过 Finder 搬到 iPad 的 JSON。
        /// 导出前会验证每一个存在的槽，拒绝把已损坏的存档包装成看似可用的备份。
        /// </summary>
        public static string ExportBackup(string purpose = "manual")
        {
            Directory.CreateDirectory(BackupRoot);
            var document = CaptureBackupDocument();
            string safePurpose = SanitizePurpose(purpose);
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
            string path = Path.Combine(BackupRoot, $"numeria-backup-{safePurpose}-{stamp}.json");
            WriteAtomic(path, JsonUtility.ToJson(document, true));
            return path;
        }

        /// <summary>返回 Finder 可见 Documents 根目录和应用备份目录中的备份，最新的排在前面。</summary>
        public static string[] GetAvailableBackups()
        {
            try
            {
                var paths = new List<string>();
                if (Directory.Exists(Root))
                    paths.AddRange(Directory.GetFiles(Root, BackupFilePattern, SearchOption.TopDirectoryOnly));
                if (Directory.Exists(BackupRoot))
                    paths.AddRange(Directory.GetFiles(BackupRoot, BackupFilePattern, SearchOption.TopDirectoryOnly));
                return paths.Distinct().OrderByDescending(File.GetLastWriteTimeUtc).ToArray();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save backup scan failed: {e.Message}");
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 完整恢复一个备份。任何写入发生前都会生成 pre-import 安全备份；输入先全部校验并
        /// 规范化，写入出错时自动恢复原数据。成功后返回导入备份的当前槽进度。
        /// </summary>
        public static SaveBackupImportResult ImportBackup(string path)
        {
            var result = new SaveBackupImportResult();
            if (!TryReadBackup(path, out var incoming, out var normalizedSlots, out string error))
            {
                result.Message = error;
                return result;
            }

            SaveBackupDocument rollback = null;
            try
            {
                rollback = CaptureBackupDocument();
                result.SafetyBackupPath = ExportBackup("pre-import");
                RestoreBackupDocument(incoming, normalizedSlots);
                var loaded = LoadFromSlot(incoming.ActiveSlot);
                if (loaded == null) throw new InvalidDataException("The imported active slot could not be loaded.");
                result.Success = true;
                result.Progress = loaded;
                result.Message = $"Imported {Path.GetFileName(path)}";
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Save backup import failed: {e.Message}");
                if (rollback != null)
                {
                    try
                    {
                        var rollbackSlots = NormalizeSlots(rollback, out string rollbackError);
                        if (rollbackSlots == null) throw new InvalidDataException(rollbackError);
                        RestoreBackupDocument(rollback, rollbackSlots);
                    }
                    catch (Exception rollbackError)
                    {
                        Debug.LogError($"Save rollback failed: {rollbackError.Message}");
                        result.Message = "Import failed and automatic rollback also failed. Keep the pre-import backup and do not start a new game.";
                        return result;
                    }
                }
                result.Message = $"Import was not applied: {e.Message}";
                return result;
            }
        }

        public static SaveSlotSummary GetSlotSummary(int slot)
        {
            MigrateFormerMacIdentityIfNeeded();
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

        private static SaveBackupDocument CaptureBackupDocument()
        {
            Directory.CreateDirectory(Root);
            var slots = new List<SaveBackupSlot>();
            for (int slot = 1; slot <= SlotCount; slot++)
            {
                string path = SlotPath(slot);
                if (!File.Exists(path)) continue;
                string json = File.ReadAllText(path);
                if (!TryNormalizeProgress(json, out _, out string error))
                    throw new InvalidDataException($"Slot {slot} is not valid: {error}");
                slots.Add(new SaveBackupSlot { Slot = slot, Json = json });
            }

            return new SaveBackupDocument
            {
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                ActiveSlot = ActiveSlot,
                Slots = slots.ToArray(),
                LegacyJson = File.Exists(LegacyPath) ? File.ReadAllText(LegacyPath) : "",
            };
        }

        private static bool TryReadBackup(string path, out SaveBackupDocument document,
            out Dictionary<int, string> normalizedSlots, out string error)
        {
            document = null;
            normalizedSlots = null;
            error = "";
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    error = "No backup file was found. Copy a Numeria backup into the app with Finder first.";
                    return false;
                }
                document = JsonUtility.FromJson<SaveBackupDocument>(File.ReadAllText(path));
                if (document == null || document.Magic != BackupMagic)
                {
                    error = "This is not a Numeria save backup.";
                    return false;
                }
                if (document.FormatVersion != BackupFormatVersion)
                {
                    error = $"Backup format {document.FormatVersion} is not supported by this version of Numeria.";
                    return false;
                }
                if (!IsValidSlot(document.ActiveSlot))
                {
                    error = "The backup has an invalid active slot.";
                    return false;
                }
                normalizedSlots = NormalizeSlots(document, out error);
                if (normalizedSlots == null) return false;
                if (!normalizedSlots.ContainsKey(document.ActiveSlot))
                {
                    error = "The backup does not contain its active save slot.";
                    normalizedSlots = null;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                error = $"The backup could not be read: {e.Message}";
                return false;
            }
        }

        private static Dictionary<int, string> NormalizeSlots(SaveBackupDocument document, out string error)
        {
            error = "";
            var normalized = new Dictionary<int, string>();
            if (document.Slots == null || document.Slots.Length == 0)
            {
                error = "The backup contains no save slots.";
                return null;
            }
            foreach (var entry in document.Slots)
            {
                if (entry == null || !IsValidSlot(entry.Slot))
                {
                    error = "The backup contains an invalid slot number.";
                    return null;
                }
                if (normalized.ContainsKey(entry.Slot))
                {
                    error = $"The backup contains Slot {entry.Slot} more than once.";
                    return null;
                }
                if (!TryNormalizeProgress(entry.Json, out string json, out string slotError))
                {
                    error = $"Slot {entry.Slot} is invalid: {slotError}";
                    return null;
                }
                normalized.Add(entry.Slot, json);
            }
            return normalized;
        }

        private static bool TryNormalizeProgress(string json, out string normalized, out string error)
        {
            normalized = "";
            error = "";
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    error = "save data is empty";
                    return false;
                }
                var progress = JsonUtility.FromJson<Progress>(json);
                if (progress == null)
                {
                    error = "save data is not valid JSON";
                    return false;
                }
                if (progress.SaveVersion > Progress.CurrentSaveVersion)
                {
                    error = $"save schema {progress.SaveVersion} is newer than supported schema {Progress.CurrentSaveVersion}";
                    return false;
                }
                progress.ApplyMigrations();
                normalized = JsonUtility.ToJson(progress);
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private static void RestoreBackupDocument(SaveBackupDocument document, Dictionary<int, string> slots)
        {
            Directory.CreateDirectory(Root);
            for (int slot = 1; slot <= SlotCount; slot++)
            {
                string path = SlotPath(slot);
                if (slots.TryGetValue(slot, out string json)) WriteAtomic(path, json);
                else if (File.Exists(path)) File.Delete(path);
            }
            if (!string.IsNullOrEmpty(document.LegacyJson)) WriteAtomic(LegacyPath, document.LegacyJson);
            else if (File.Exists(LegacyPath)) File.Delete(LegacyPath);
            WriteAtomic(ActiveSlotPath, document.ActiveSlot.ToString());
            _activeSlot = document.ActiveSlot;
        }

        private static string SanitizePurpose(string purpose)
        {
            if (string.IsNullOrWhiteSpace(purpose)) return "manual";
            var chars = purpose.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
            string value = new string(chars).Trim('-');
            return string.IsNullOrEmpty(value) ? "manual" : value;
        }

        private static void WriteAtomic(string path, string contents)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temp, contents);
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
        }

        /// <summary>
        /// iOS 准备阶段把默认 DefaultCompany/unity 改为正式 Numeria/Numeria。macOS Editor 的
        /// persistentDataPath 因此会改变；首次运行时只复制不删除旧目录，确保 Lucas 原记录继续出现。
        /// </summary>
        private static void MigrateFormerMacIdentityIfNeeded()
        {
            if (_formerMacIdentityChecked || !string.IsNullOrEmpty(_storageRootOverride)) return;
            _formerMacIdentityChecked = true;
            if (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer)
                return;

            // Unity 的 Mono 在某些 macOS 版本会把 SpecialFolder.ApplicationData 解析到产品目录本身，
            // 因此从已知的新 persistentDataPath 向上两级取得真正的 Application Support。
            var companyDirectory = Directory.GetParent(Root);
            var applicationSupportDirectory = companyDirectory?.Parent;
            string appSupport = applicationSupportDirectory?.FullName;
            if (string.IsNullOrEmpty(appSupport))
                appSupport = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string formerRoot = Path.Combine(appSupport, "DefaultCompany", "unity");
            if (!Directory.Exists(formerRoot) || string.Equals(formerRoot, Root, StringComparison.Ordinal)) return;
            bool destinationAlreadyHasSave = Enumerable.Range(1, SlotCount)
                .Any(slot => File.Exists(SlotPath(slot)));
            if (destinationAlreadyHasSave) return;

            try
            {
                Directory.CreateDirectory(Root);
                bool copiedAnySlot = false;
                for (int slot = 1; slot <= SlotCount; slot++)
                {
                    string source = Path.Combine(formerRoot, $"numeria-save-slot-{slot}.json");
                    if (!File.Exists(source)) continue;
                    string json = File.ReadAllText(source);
                    if (!TryNormalizeProgress(json, out _, out string error))
                    {
                        Debug.LogWarning($"Skipped invalid former Slot {slot}: {error}");
                        continue;
                    }
                    File.Copy(source, SlotPath(slot), false);
                    copiedAnySlot = true;
                }

                string formerLegacy = Path.Combine(formerRoot, "numeria-save.json");
                if (File.Exists(formerLegacy) && !File.Exists(LegacyPath)) File.Copy(formerLegacy, LegacyPath, false);
                string formerActive = Path.Combine(formerRoot, "numeria-active-slot.txt");
                if (copiedAnySlot && File.Exists(formerActive) && !File.Exists(ActiveSlotPath))
                    File.Copy(formerActive, ActiveSlotPath, false);
                if (copiedAnySlot)
                    Debug.Log($"Numeria copied former save data into the permanent app identity. Original kept at {formerRoot}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Former Numeria save migration was not completed: {e.Message}");
            }
        }

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
            _formerMacIdentityChecked = false;
        }

        public static void ResetStorageRootForTests()
        {
            _storageRootOverride = null;
            _activeSlot = 0;
            _formerMacIdentityChecked = false;
        }
#endif
    }
}
