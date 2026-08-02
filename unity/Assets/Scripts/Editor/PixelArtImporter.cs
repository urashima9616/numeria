using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>
    /// Resources/Art 与 Resources/generated 下所有贴图自动套用像素风导入规范:
    /// Sprite 类型、点采样、不压缩、无 mipmap、Alpha 透明;
    /// 素材包 UI 面板/按钮按 Unity_Import_Settings.json 的 9-Slice Border 切边。
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        // 与 NUMERIA_Unity_Battle_Assets/Unity_Import_Settings.json 保持一致
        private static readonly Dictionary<string, Vector4> NineSliceBorders = new Dictionary<string, Vector4>
        {
            { "Status_Panel", new Vector4(32, 32, 32, 32) },
            { "Turn_Banner", new Vector4(28, 28, 28, 28) },
            { "Command_Dock", new Vector4(32, 32, 32, 32) },
            { "Generic_Panel", new Vector4(32, 32, 32, 32) },
            { "Button_Normal", new Vector4(32, 32, 32, 32) },
            { "Button_Selected", new Vector4(32, 32, 32, 32) },
            { "Button_Pressed", new Vector4(32, 32, 32, 32) },
        };

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/") &&
                !assetPath.StartsWith("Assets/Resources/generated/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            // UI 素材必须 100 PPU:UGUI 切片边框按 Canvas 参考 PPU(100)/精灵 PPU 缩放,
            // 16 PPU 会把 9-slice 边角放大 6.25 倍,面板直接崩坏
            importer.spritePixelsPerUnit = assetPath.Contains("/UI/") ? 100 : 16;

            string name = Path.GetFileNameWithoutExtension(assetPath);
            if (NineSliceBorders.TryGetValue(name, out var border))
                importer.spriteBorder = border;
        }
    }
}
