using UnityEditor;
using UnityEngine;

namespace Numeria.Editor
{
    /// <summary>
    /// Resources/Art 下所有贴图自动套用像素风导入规范:
    /// 点采样、不压缩、无 mipmap、16 PPU。
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
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
            importer.spritePixelsPerUnit = 16;
        }
    }
}
