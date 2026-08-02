using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>
    /// 自愈守卫:资源可能在导入器脚本编译完成之前被导入(Unity 同批刷新的时序坑),
    /// 导致素材包 UI 精灵拿不到正确的 PPU/9-slice。每次域重载后检查一次,
    /// 发现不对就强制重导入。
    /// </summary>
    public static class PackReimportGuard
    {
        private const string UiRoot = "Assets/Resources/generated/NUMERIA_Unity_Battle_Assets/UI";
        private const string Probe = UiRoot + "/Panels/Status_Panel.png";

        [InitializeOnLoadMethod]
        private static void EnsurePackImport()
        {
            EditorApplication.delayCall += () =>
            {
                var probe = AssetImporter.GetAtPath(Probe) as TextureImporter;
                if (probe == null) return; // 素材包不存在
                bool healthy = Mathf.Approximately(probe.spritePixelsPerUnit, 100f)
                               && probe.spriteBorder != Vector4.zero;
                if (healthy) return;

                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiRoot });
                foreach (string guid in guids)
                    AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
                Debug.Log($"Numeria: 重导入 {guids.Length} 张素材包 UI 贴图(修正 PPU/9-slice)");
            };
        }
    }
}
