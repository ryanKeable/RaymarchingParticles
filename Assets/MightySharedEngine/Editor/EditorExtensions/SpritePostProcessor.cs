using UnityEngine;
using UnityEditor;
using System.IO;

public sealed class SpritePostProcessor : AssetPostprocessor
{
    void OnPostprocessTexture(Texture2D texture)
    {
        if (!assetImporter.assetPath.Contains("sprite")) return;

        MDebug.Log("[SpritePostProcessor] OnPostprocessTexture " + assetImporter.assetPath);
        MDebug.Log("[SpritePostProcessor] Importing " + assetImporter.assetPath);

        string[] tokens = assetImporter.assetPath.Split(Path.DirectorySeparatorChar);
        if (tokens.Length < 2) return; // Should not be possible, but hey...

        string directoryName = tokens[tokens.Length - 2];

        TextureImporter textureImporter = assetImporter as TextureImporter;
//        textureImporter.maxTextureSize = 512;
        textureImporter.mipmapEnabled = false;
        textureImporter.filterMode = FilterMode.Point;
        textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spritePackingTag = directoryName;
        textureImporter.spritePixelsPerUnit = 128;

        MDebug.Log("[SpritePostProcessor] Importing image as sprite: " + tokens[tokens.Length - 1]);
        MDebug.Log("[SpritePostProcessor] Setting packing tag: " + textureImporter.spritePackingTag);
        MDebug.Log("[SpritePostProcessor] Setting spritePixelsToUnits: " + textureImporter.spritePixelsPerUnit);
    }
}
