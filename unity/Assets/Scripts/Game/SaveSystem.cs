using System.IO;
using Numeria.Core;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>JSON 本地存档(persistentDataPath),存 Progress 全量。</summary>
    public static class SaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "numeria-save.json");

        public static Progress Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var loaded = JsonUtility.FromJson<Progress>(File.ReadAllText(SavePath));
                    if (loaded != null)
                    {
                        loaded.ApplyMigrations();
                        return loaded;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Save load failed, starting fresh: {e.Message}");
            }
            return new Progress();
        }

        public static void Save(Progress progress)
        {
            progress.ApplyMigrations();
            File.WriteAllText(SavePath, JsonUtility.ToJson(progress));
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
