// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace UnityEditor.SceneTemplate
{
    internal static class SceneTemplateUtils
    {
        private const string k_LastSceneOperationFolder = "SceneTemplateLastOperationFolder";

        public const string TemplateScenePropertyName = nameof(SceneTemplateAsset.templateScene);
        public const string TemplatePipelineName = nameof(SceneTemplateAsset.templatePipeline);
        public const string TemplateTitlePropertyName = nameof(SceneTemplateAsset.templateName);
        public const string TemplateDescriptionPropertyName = nameof(SceneTemplateAsset.description);
        public const string TemplateAddToDefaultsPropertyName = nameof(SceneTemplateAsset.addToDefaults);
        public const string TemplateThumbnailPropertyName = nameof(SceneTemplateAsset.preview);
        public const string DependenciesPropertyName = nameof(SceneTemplateAsset.dependencies);
        public const string DependencyPropertyName = nameof(DependencyInfo.dependency);
        public const string InstantiationModePropertyName = nameof(DependencyInfo.instantiationMode);

        internal static IEnumerable<string> GetSceneTemplatePaths()
        {
            return AssetDatabase.FindAssets("t:SceneTemplateAsset").Select(AssetDatabase.GUIDToAssetPath);
        }

        internal static Rect GetMainWindowCenteredPosition(Vector2 size)
        {
            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            return EditorGUIUtility.GetCenteredWindowPosition(mainWindowRect, size);
        }

        internal static void SetLastFolder(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var lastFolder = path;
                if (File.Exists(path))
                    lastFolder = Path.GetDirectoryName(path);
                if (Path.IsPathRooted(lastFolder))
                {
                    lastFolder = FileUtil.GetProjectRelativePath(lastFolder);
                }
                if (Directory.Exists(lastFolder))
                {
                    EditorPrefs.SetString($"{k_LastSceneOperationFolder}{Path.GetExtension(path)}", lastFolder);
                }
            }
        }

        internal static string GetLastFolder(string fileExtension)
        {
            var lastFolder = EditorPrefs.GetString($"{k_LastSceneOperationFolder}.{fileExtension}", null);
            if (lastFolder != null)
            {
                if (Path.IsPathRooted(lastFolder))
                {
                    lastFolder = FileUtil.GetProjectRelativePath(lastFolder);
                }
                if (!Directory.Exists(lastFolder))
                {
                    lastFolder = null;
                }
            }

            return lastFolder ?? "Assets";
        }

        internal static string SaveFilePanelUniqueName(string title, string directory, string filename, string extension)
        {
            var initialPath = Path.Combine(directory, filename + "." + extension).Replace("\\", "/");
            if (Path.IsPathRooted(initialPath))
            {
                initialPath = FileUtil.GetProjectRelativePath(initialPath);
            }
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(initialPath);
            directory = Path.GetDirectoryName(uniqueAssetPath);
            filename = Path.GetFileName(uniqueAssetPath);
            var result = EditorUtility.SaveFilePanel(title, directory, filename, extension);
            if (!string.IsNullOrEmpty(result))
            {
                SetLastFolder(result);
                if (Path.IsPathRooted(result))
                {
                    result = FileUtil.GetProjectRelativePath(result);
                }
            }

            return result;
        }

        internal static void OpenDocumentationUrl()
        {
            const string documentationUrl = "https://docs.unity3d.com/Packages/com.unity.scene-template@latest/";
            var uri = new Uri(documentationUrl);
            Process.Start(uri.AbsoluteUri);
        }
    }
}
