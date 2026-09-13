using System;
using System.IO;
using System.Linq;
using Numeria.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>
    /// Numeria 的 iPad 构建真理源。Bundle ID 决定 iOS persistentDataPath，发布后不得更换；
    /// PostProcess 同时开放 Documents 给 Finder，供十槽备份包无云端依赖地迁移。
    /// </summary>
    public sealed class NumeriaIosBuild : IPreprocessBuildWithReport
    {
        public const string BundleIdentifier = "com.yuankunxue.numeria";
        public const string ProductName = "Numeria";
        public const string CompanyName = "Numeria";
        public const string MinimumIosVersion = "15.0";
        public const string AppIconAssetPath = "Assets/AppIcon/numeria_app_icon_1024.png";

        public int callbackOrder => -100;

        [MenuItem("Numeria/iOS/Configure Project")]
        public static void ConfigureProject()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = MinimumIosVersion;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.statusBarHidden = true;
            AssignAppIcon();
            AssetDatabase.SaveAssets();
            Debug.Log($"Numeria iOS configured: {BundleIdentifier}, iOS {MinimumIosVersion}+, landscape, universal iPad/iPhone.");
        }

        private static void AssignAppIcon()
        {
            var importer = AssetImporter.GetAtPath(AppIconAssetPath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Default ||
                                     importer.textureCompression != TextureImporterCompression.Uncompressed ||
                                     importer.mipmapEnabled || importer.alphaIsTransparency))
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconAssetPath);
            if (texture == null)
            {
                Debug.LogWarning($"Numeria app icon is missing at {AppIconAssetPath}.");
                return;
            }
            foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
            {
                PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
                foreach (PlatformIcon icon in icons) icon.SetTextures(texture);
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            ConfigureProject();
            // Runtime-created TMP fonts use Shader.Find; an Editor-only shader would abort
            // MapController.Awake before the title screen and camera can be initialized.
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/GraphicsSettings.asset")[0]);
            var included = graphics.FindProperty("m_AlwaysIncludedShaders");
            var textShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
            bool hasTextShader = textShader != null && Enumerable.Range(0, included.arraySize)
                .Any(i => included.GetArrayElementAtIndex(i).objectReferenceValue == textShader);
            if (!hasTextShader)
                throw new BuildFailedException("Numeria requires TextMeshPro/Mobile/Distance Field in Graphics Settings > Always Included Shaders. Runtime fonts otherwise fail on iPad.");
            if (EditorBuildSettings.scenes.All(scene => !scene.enabled))
                throw new BuildFailedException("Numeria needs at least one enabled scene before creating the Xcode project.");
        }

        [MenuItem("Numeria/iOS/Build Xcode Project")]
        public static void BuildXcodeProject()
        {
            ConfigureProject();
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new BuildFailedException("Unity could not switch to iOS. Verify iOS Build Support in Unity Hub.");

            string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
                .Select(scene => scene.path).ToArray();
            if (scenes.Length == 0) throw new BuildFailedException("No enabled scenes are configured.");

            string buildRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/iOS"));
            string output = NextAvailableBuildPath(buildRoot);
            Directory.CreateDirectory(output);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Numeria iOS build failed: {report.summary.result}");
            Debug.Log($"Numeria Xcode project ready: {output}");
        }

        private static string NextAvailableBuildPath(string buildRoot)
        {
            string candidate = Path.Combine(buildRoot, ProductName);
            if (!Directory.Exists(candidate)) return candidate;
            int suffix = 2;
            do candidate = Path.Combine(buildRoot, $"{ProductName}-{suffix++}");
            while (Directory.Exists(candidate));
            return candidate;
        }

        [MenuItem("Numeria/iOS/Export Current Mac Save Backup")]
        public static void ExportCurrentMacSaveBackup()
        {
            // Load 会先把旧 DefaultCompany/unity 身份中的存档无损复制到正式身份。
            var progress = SaveSystem.Load();
            SaveSystem.Save(progress);
            string path = SaveSystem.ExportBackup("ipad-transfer");
            Debug.Log($"Numeria iPad transfer backup ready: {path}");
        }

        [PostProcessBuild(100)]
        public static void EnableFinderSaveTransfer(BuildTarget target, string buildPath)
        {
            if (target != BuildTarget.iOS) return;
            string plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;
            root.SetBoolean("UIFileSharingEnabled", true);
            root.SetBoolean("LSSupportsOpeningDocumentsInPlace", true);
            root.SetString("CFBundleDisplayName", ProductName);
            plist.WriteToFile(plistPath);
            Debug.Log("Numeria iOS Documents enabled for Finder save backup transfer.");
        }
    }
}
