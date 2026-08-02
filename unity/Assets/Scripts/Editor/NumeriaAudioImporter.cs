using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Numeria.EditorTools
{
    /// <summary>
    /// 将用户本地导入的 Dynamic Music Asset Store 曲目同步到 Resources。
    /// 目标 WAV 与 meta 被 gitignore，避免在公开仓库重新分发商店源资产。
    /// </summary>
    public static class NumeriaAudioInstaller
    {
        private const string SourceRoot = "Assets/Dynamic Music/Audio Files";
        private const string DestinationRoot = "Assets/Resources/Music/LocalStore";

        private static readonly Dictionary<string, string> Tracks = new Dictionary<string, string>
        {
            { "Stealth/Parts/Stealth Menu Loop.wav", "forest.wav" },
            { "Tibet/Parts/Tibet Menu Loop.wav", "mountains.wav" },
            { "Centurion/Parts/Centurion Menu Loop.wav", "sky.wav" },
            { "Battlefield/Parts/Battlefield Part 1.wav", "battle.wav" },
            { "Battlefield/Parts/Battlefield Part 3.wav", "boss.wav" },
            { "Tension/Parts/Tension Part 1 Loop.wav", "evolution.wav" }
        };

        [InitializeOnLoadMethod]
        private static void QueueAutomaticSync()
        {
            EditorApplication.delayCall += SyncIfAvailable;
        }

        [MenuItem("Numeria/Audio/Sync Dynamic Music")]
        public static void SyncIfAvailable()
        {
            if (!Directory.Exists(SourceRoot)) return;
            Directory.CreateDirectory(DestinationRoot);
            bool changed = false;

            foreach (var track in Tracks)
            {
                string source = Path.Combine(SourceRoot, track.Key);
                string destination = Path.Combine(DestinationRoot, track.Value);
                if (!File.Exists(source))
                {
                    Debug.LogWarning($"Dynamic Music source is missing: {source}");
                    continue;
                }

                var sourceInfo = new FileInfo(source);
                var destinationInfo = new FileInfo(destination);
                if (destinationInfo.Exists && destinationInfo.Length == sourceInfo.Length) continue;
                File.Copy(source, destination, true);
                changed = true;
            }

            if (!changed) return;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Numeria Dynamic Music synced from the local Asset Store package.");
        }
    }

    /// <summary>短音效常驻内存；长音乐流式解码，控制 iOS 内存峰值和包体。</summary>
    public sealed class NumeriaAudioImporter : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;

            if (assetPath.StartsWith("Assets/Resources/Sfx/"))
            {
                importer.forceToMono = true;
                importer.loadInBackground = false;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.ADPCM;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
            }
            else if (assetPath.StartsWith("Assets/Resources/Music/LocalStore/"))
            {
                importer.forceToMono = false;
                importer.loadInBackground = true;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.48f;
                settings.preloadAudioData = false;
                importer.defaultSampleSettings = settings;
            }
        }
    }
}
