using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AndroidBuildSizeOptimizer
{
    public static string Execute()
    {
        var log = new List<string>();

        ConfigureAndroidPlayerSettings(log);
        int optimizedCount = OptimizeQuestionTextures(log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.Insert(0, $"Optimized question textures: {optimizedCount}");
        return string.Join("\n", log);
    }

    public static string ConfigurePlayerSettingsOnly()
    {
        var log = new List<string>();
        ConfigureAndroidPlayerSettings(log);
        AssetDatabase.SaveAssets();
        return string.Join("\n", log);
    }

    public static string OptimizeTexturesByPrefix(string prefix)
    {
        var log = new List<string>();
        int optimizedCount = OptimizeQuestionTextures(log, prefix);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.Insert(0, $"Prefix: {prefix}");
        log.Insert(0, $"Optimized question textures: {optimizedCount}");
        return string.Join("\n", log);
    }

    private static void ConfigureAndroidPlayerSettings(List<string> log)
    {
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.Android.minifyRelease = true;
        PlayerSettings.Android.minifyDebug = false;

        try
        {
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Medium);
            log.Add("Managed Stripping Level: Medium");
        }
        catch (Exception ex)
        {
            log.Add("Managed Stripping Level could not be changed: " + ex.Message);
        }

        log.Add("Strip Engine Code: Enabled");
        log.Add("Android Minify Release: Enabled");
        log.Add("Android Minify Debug: Disabled");
    }

    private static int OptimizeQuestionTextures(List<string> log, string requiredPrefix = null)
    {
        string[] searchFolders = { "Assets/Resources/Gorseller" };
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", searchFolders);
        int changedCount = 0;

        string normalizedPrefix = string.IsNullOrWhiteSpace(requiredPrefix)
            ? null
            : requiredPrefix.Trim().ToLowerInvariant();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(fileName))
                continue;

            string lowerName = fileName.ToLowerInvariant();
            if (lowerName.Contains("puzzle"))
                continue;

            if (!string.IsNullOrEmpty(normalizedPrefix) && !lowerName.StartsWith(normalizedPrefix + "_"))
                continue;

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = false;

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Compressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }

            var androidSettings = importer.GetPlatformTextureSettings("Android");
            if (androidSettings == null)
                androidSettings = new TextureImporterPlatformSettings();

            int targetSize = lowerName.Contains("_o.") ? 512 : 1024;

            if (!androidSettings.overridden)
            {
                androidSettings.overridden = true;
                changed = true;
            }

            if (androidSettings.maxTextureSize != targetSize)
            {
                androidSettings.maxTextureSize = targetSize;
                changed = true;
            }

            if (androidSettings.format != TextureImporterFormat.Automatic)
            {
                androidSettings.format = TextureImporterFormat.Automatic;
                changed = true;
            }

            if (!androidSettings.crunchedCompression)
            {
                androidSettings.crunchedCompression = true;
                changed = true;
            }

            if (androidSettings.compressionQuality != 50)
            {
                androidSettings.compressionQuality = 50;
                changed = true;
            }

            importer.SetPlatformTextureSettings(androidSettings);

            if (changed)
            {
                importer.SaveAndReimport();
                changedCount++;
            }
        }

        log.Add("Question image import settings optimized for Android.");
        return changedCount;
    }
}