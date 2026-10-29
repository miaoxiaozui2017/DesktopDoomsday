#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DesktopDoomsday.EditorTools
{
    /// <summary>
    /// 像素美术导入设置：Assets/Art/Sprites 下的 PNG 自动设为 Sprite、Point 过滤、无压缩。
    /// 保证"高清像素风"在 Unity 里保持锐利、无 Mipmap 糊边。
    /// </summary>
    public class PixelArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string p = assetPath.Replace('\\', '/');
            if (!p.Contains("Assets/Art/Sprites/")) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.spritePixelsPerUnit = 100f;
            ti.maxTextureSize = 2048;
        }
    }
}
#endif
