using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.U2D.Sprites;

public class AutoSpriteSlicer : EditorWindow
{
    private enum ProcessResult
    {
        ConvertedAndSliced,
        SlicedOnly,
        Skipped,
        Failed
    }

    private DefaultAsset folderAsset;

    [MenuItem("Tools/Auto Sprite Slicer")]
    private static void OpenWindow()
    {
        var window = GetWindow<AutoSpriteSlicer>("Auto Sprite Slicer");
        window.minSize = new Vector2(420f, 170f);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("批量自动切图工具", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("选择 Assets 下的文件夹。执行后会将其中所有 Texture2D 设置为 Sprite/Multiple，并按默认 Auto Slice 自动切割。", MessageType.Info);

        folderAsset = (DefaultAsset)EditorGUILayout.ObjectField("目标文件夹", folderAsset, typeof(DefaultAsset), false);

        using (new EditorGUI.DisabledScope(folderAsset == null))
        {
            if (GUILayout.Button("开始批量切割", GUILayout.Height(32f)))
            {
                ExecuteSlice();
            }
        }
    }

    private void ExecuteSlice()
    {
        string folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Auto Sprite Slicer", "请选择有效的工程内文件夹（Assets 下）。", "确定");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Auto Sprite Slicer", "所选文件夹下没有 Texture2D 资源。", "确定");
            return;
        }

        int convertedAndSlicedCount = 0;
        int slicedOnlyCount = 0;
        int skippedCount = 0;
        int failCount = 0;
        List<string> failedAssets = new List<string>();

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Auto Sprite Slicer", $"处理中 ({i + 1}/{guids.Length}): {assetPath}", (float)(i + 1) / guids.Length);

                ProcessResult result = ProcessTexture(assetPath);
                switch (result)
                {
                    case ProcessResult.ConvertedAndSliced:
                        convertedAndSlicedCount++;
                        break;
                    case ProcessResult.SlicedOnly:
                        slicedOnlyCount++;
                        break;
                    case ProcessResult.Skipped:
                        skippedCount++;
                        break;
                    default:
                        failCount++;
                        failedAssets.Add(assetPath);
                        break;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
        }

        if (failCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Auto Sprite Slicer",
                $"完成：\n已转 Multiple 并切割 {convertedAndSlicedCount}\n仅自动切割 {slicedOnlyCount}\n已跳过（已是 Multiple 且已切割）{skippedCount}\n失败 {failCount}",
                "确定");
        }
        else
        {
            string failedSummary = string.Join("\n", failedAssets.ToArray());
            EditorUtility.DisplayDialog(
                "Auto Sprite Slicer",
                $"完成：\n已转 Multiple 并切割 {convertedAndSlicedCount}\n仅自动切割 {slicedOnlyCount}\n已跳过（已是 Multiple 且已切割）{skippedCount}\n失败 {failCount}\n\n失败资源：\n{failedSummary}",
                "确定");
        }
    }

    private static ProcessResult ProcessTexture(string assetPath)
    {
        try
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return ProcessResult.Failed;
            }

            bool isMultiple = importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Multiple;
            bool hasSlices = HasAnySpriteRect(importer);

            if (isMultiple && hasSlices)
            {
                return ProcessResult.Skipped;
            }

            bool needsConvertToMultiple = !isMultiple;
            bool changedImporterSettings = false;

            if (needsConvertToMultiple)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                changedImporterSettings = true;
            }

            // InternalSpriteUtility 自动切割依赖可读贴图，避免出现“统计成功但无实际切割”。
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changedImporterSettings = true;
            }

            if (changedImporterSettings)
            {
                importer.SaveAndReimport();
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return ProcessResult.Failed;
            }

            Texture2D temporaryTexture = null;
            Texture2D textureToSlice = null;
            try
            {
                textureToSlice = GetTextureToSlice(importer, texture, out temporaryTexture);

                Rect[] rects = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(textureToSlice, 4, 0);
                if (rects == null || rects.Length == 0)
                {
                    // 首次自动切割为空时，降低最小尺寸再尝试一次，兼容小图元与细线资源。
                    rects = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(textureToSlice, 1, 0);
                }

                if (rects == null || rects.Length == 0)
                {
                    // 若默认自动切割仍无结果，兜底为整图单切片，避免资源遗漏。
                    rects = new[] { new Rect(0f, 0f, textureToSlice.width, textureToSlice.height) };
                    Debug.LogWarning($"[AutoSpriteSlicer] 自动切割未检测到可切区域，已使用整图兜底切片: {assetPath}");
                }

                if (!ApplySpriteRects(importer, texture.name, rects))
                {
                    return ProcessResult.Failed;
                }
            }
            finally
            {
                if (temporaryTexture != null)
                {
                    UnityEngine.Object.DestroyImmediate(temporaryTexture);
                }
            }

            return needsConvertToMultiple ? ProcessResult.ConvertedAndSliced : ProcessResult.SlicedOnly;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoSpriteSlicer] 处理失败: {assetPath}\n{ex}");
            return ProcessResult.Failed;
        }
    }

    private static Texture2D GetTextureToSlice(TextureImporter importer, Texture2D importedTexture, out Texture2D temporaryTexture)
    {
        temporaryTexture = null;

        if (importedTexture == null)
        {
            return null;
        }

        int actualWidth = importedTexture.width;
        int actualHeight = importedTexture.height;
        TryGetImporterTextureSize(importer, ref actualWidth, ref actualHeight);

        if (actualWidth <= 0 || actualHeight <= 0)
        {
            return importedTexture;
        }

        if (importedTexture.width == actualWidth && importedTexture.height == actualHeight)
        {
            return importedTexture;
        }

        temporaryTexture = CreateScaledTextureCopy(importedTexture, actualWidth, actualHeight);
        return temporaryTexture ?? importedTexture;
    }

    private static void TryGetImporterTextureSize(TextureImporter importer, ref int width, ref int height)
    {
        if (importer == null)
        {
            return;
        }

        try
        {
            MethodInfo sourceSizeMethod = typeof(TextureImporter).GetMethod(
                "GetSourceTextureWidthAndHeight",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType() },
                null);

            if (sourceSizeMethod != null)
            {
                object[] args = { width, height };
                sourceSizeMethod.Invoke(importer, args);

                int resolvedWidth = (int)args[0];
                int resolvedHeight = (int)args[1];
                if (resolvedWidth > 0 && resolvedHeight > 0)
                {
                    width = resolvedWidth;
                    height = resolvedHeight;
                    return;
                }
            }

            MethodInfo widthHeightMethod = typeof(TextureImporter).GetMethod(
                "GetWidthAndHeight",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType() },
                null);

            if (widthHeightMethod != null)
            {
                object[] args = { width, height };
                widthHeightMethod.Invoke(importer, args);

                int resolvedWidth = (int)args[0];
                int resolvedHeight = (int)args[1];
                if (resolvedWidth > 0 && resolvedHeight > 0)
                {
                    width = resolvedWidth;
                    height = resolvedHeight;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoSpriteSlicer] 获取导入纹理尺寸失败，回退为当前纹理尺寸: {importer.assetPath}\n{ex.Message}");
        }
    }

    private static Texture2D CreateScaledTextureCopy(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;

        try
        {
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D copy = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0);
            copy.Apply();
            return copy;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private static bool HasAnySpriteRect(TextureImporter importer)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            return false;
        }

        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
        return spriteRects != null && spriteRects.Length > 0;
    }

    private static bool ApplySpriteRects(TextureImporter importer, string textureName, Rect[] rects)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (dataProvider == null)
        {
            return false;
        }

        dataProvider.InitSpriteEditorDataProvider();

        var spriteRects = new List<SpriteRect>(rects.Length);
        for (int i = 0; i < rects.Length; i++)
        {
            SpriteRect spriteRect = new SpriteRect
            {
                name = $"{textureName}_{i}",
                rect = rects[i],
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };

            spriteRect.spriteID = GUID.Generate();
            spriteRects.Add(spriteRect);
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());

        ISpriteNameFileIdDataProvider nameFileIdDataProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdDataProvider != null)
        {
            var nameFileIdPairs = new List<SpriteNameFileIdPair>(spriteRects.Count);
            for (int i = 0; i < spriteRects.Count; i++)
            {
                nameFileIdPairs.Add(new SpriteNameFileIdPair(spriteRects[i].name, spriteRects[i].spriteID));
            }

            nameFileIdDataProvider.SetNameFileIdPairs(nameFileIdPairs);
        }

        dataProvider.Apply();
        importer.SaveAndReimport();
        return true;
    }
}
