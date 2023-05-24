// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEditor.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneTemplate
{
    internal enum BuiltinTemplateType
    {
        Empty,
        Default2D,
        Default2DMode3DCamera,
        Default3D
    }

    internal static class SceneTemplateUtils
    {
        private const string k_LastSceneOperationFolder = "SceneTemplateLastOperationFolder";

        public const string TemplateScenePropertyName = nameof(SceneTemplateAsset.templateScene);
        public const string TemplatePipelineName = nameof(SceneTemplateAsset.templatePipeline);
        public const string TemplateTitlePropertyName = nameof(SceneTemplateAsset.templateName);
        public const string TemplateDescriptionPropertyName = nameof(SceneTemplateAsset.description);
        public const string TemplateAddToDefaultsPropertyName = nameof(SceneTemplateAsset.addToDefaults);
        public const string TemplateThumbnailPropertyName = nameof(SceneTemplateAsset.preview);
        public const string TemplateThumbnailBadgePropertyName = nameof(SceneTemplateAsset.badge);
        public const string DependenciesPropertyName = nameof(SceneTemplateAsset.dependencies);
        public const string DependencyPropertyName = nameof(DependencyInfo.dependency);
        public const string InstantiationModePropertyName = nameof(DependencyInfo.instantiationMode);

        internal static SceneTemplateInfo emptySceneTemplateInfo = new SceneTemplateInfo
        {
            name = "Empty",
            isPinned = true,
            thumbnailPath = $"{Styles.k_IconsFolderFolder}scene-template-empty-scene.png",
            description = L10n.Tr("Just an empty scene - no Game Objects."),
            onCreateCallback = additive => CreateBuiltinScene(BuiltinTemplateType.Empty, additive)
        };
        internal static SceneTemplateInfo default2DSceneTemplateInfo = new SceneTemplateInfo
        {
            name = "Basic 2D (Built-in)",
            isPinned = true,
            thumbnailPath = $"{Styles.k_IconsFolderFolder}scene-template-2d-scene.png",
            badgePath = $"{Styles.k_IconsFolderFolder}2d-badge-scene-template.png",
            description = L10n.Tr("Contains an orthographic camera setup for 2D games. Works with built-in renderer."),
            onCreateCallback = additive => CreateBuiltinScene(BuiltinTemplateType.Default2D, additive)
        };
        internal static SceneTemplateInfo default2DMode3DSceneTemplateInfo = new SceneTemplateInfo
        {
            name = "Basic 3D (Built-in)",
            isPinned = true,
            thumbnailPath = $"{Styles.k_IconsFolderFolder}scene-template-3d-scene.png",
            badgePath = $"{Styles.k_IconsFolderFolder}3d-badge-scene-template.png",
            description = L10n.Tr("Contains a camera and directional light. Works with built-in renderer."),
            onCreateCallback = additive => CreateBuiltinScene(BuiltinTemplateType.Default2DMode3DCamera, additive)
        };
        internal static SceneTemplateInfo default3DSceneTemplateInfo = new SceneTemplateInfo
        {
            name = "Basic (Built-in)",
            isPinned = true,
            thumbnailPath = $"{Styles.k_IconsFolderFolder}scene-template-3d-scene.png",
            badgePath = $"{Styles.k_IconsFolderFolder}3d-badge-scene-template.png",
            description = L10n.Tr("Contains a camera and directional light, works with built-in renderer."),
            onCreateCallback = additive => CreateBuiltinScene(BuiltinTemplateType.Default3D, additive)
        };
        internal static SceneTemplateInfo[] builtinTemplateInfos = new[] { emptySceneTemplateInfo, default2DSceneTemplateInfo, default2DMode3DSceneTemplateInfo, default3DSceneTemplateInfo };
        internal static SceneTemplateInfo[] builtin2DTemplateInfos = new[] { emptySceneTemplateInfo, default2DSceneTemplateInfo, default2DMode3DSceneTemplateInfo };
        internal static SceneTemplateInfo[] builtin3DTemplateInfos = new[] { emptySceneTemplateInfo, default3DSceneTemplateInfo };

        internal static IEnumerable<string> GetSceneTemplatePaths()
        {
            return GetSceneTemplates().Select(asset => AssetDatabase.GetAssetPath(asset.GetInstanceID()));
        }

        internal static IEnumerable<SceneTemplateAsset> GetSceneTemplates()
        {
            using (var sceneTemplateItr = AssetDatabase.EnumerateAllAssets(CreateSceneTemplateSearchFilter()))
            {
                while (sceneTemplateItr.MoveNext())
                    yield return sceneTemplateItr.Current.pptrValue as SceneTemplateAsset;
            }
        }

        private static SearchFilter CreateSceneTemplateSearchFilter()
        {
            return new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.AllAssets,
                classNames = new[] { nameof(SceneTemplateAsset) },
                showAllHits = false
            };
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
            if (string.IsNullOrEmpty(result))
            {
                // User has cancelled.
                return null;
            }

            directory = Paths.ConvertSeparatorsToUnity(Path.GetDirectoryName(result));
            if (!Search.Utils.IsPathUnderProject(directory))
            {
                UnityEngine.Debug.LogWarning($"Not a valid folder to save an asset: {directory}.");
                return null;
            }
            if (!Paths.IsValidAssetPath(result, ".scenetemplate", out var errorMessage))
            {
                UnityEngine.Debug.LogWarning($"Save SceneTemplate has failed. {errorMessage}.");
                return null;
            }

            SetLastFolder(directory);
            return Search.Utils.GetPathUnderProject(result);
        }

        internal static void OpenDocumentationUrl()
        {
            const string documentationUrl = "https://docs.unity3d.com/2021.1/Documentation/Manual/scene-templates.html";
            var uri = new Uri(documentationUrl);
            Process.Start(uri.AbsoluteUri);
        }

        internal static List<SceneTemplateInfo> GetSceneTemplateInfos()
        {
            var sceneTemplateList = new List<SceneTemplateInfo>();
            // Add the special Empty and Basic template

            foreach (var builtinTemplateInfo in builtinTemplateInfos)
            {
                builtinTemplateInfo.isPinned = SceneTemplateProjectSettings.Get().GetPinState(builtinTemplateInfo.name);
            }

            // Check for real templateAssets:
            var sceneTemplateAssetInfos = GetSceneTemplates().Select(sceneTemplateAsset =>
            {
                var templateAssetPath = AssetDatabase.GetAssetPath(sceneTemplateAsset.GetInstanceID());
                return Tuple.Create(templateAssetPath, sceneTemplateAsset);
            })
                .Where(templateData =>
                {
                    if (templateData.Item2 == null)
                        return false;
                    if (!templateData.Item2.isValid)
                        return false;
                    var pipeline = templateData.Item2.CreatePipeline();
                    if (pipeline == null)
                        return true;
                    return pipeline.IsValidTemplateForInstantiation(templateData.Item2);
                }).
                Select(templateData =>
                {
                    var assetName = Path.GetFileNameWithoutExtension(templateData.Item1);

                    var isReadOnly = false;
                    if (templateData.Item1.StartsWith("Packages/") && AssetDatabase.TryGetAssetFolderInfo(templateData.Item1, out var isRootFolder, out var isImmutable))
                    {
                        isReadOnly = isImmutable;
                    }

                    return new SceneTemplateInfo
                    {
                        name = string.IsNullOrEmpty(templateData.Item2.templateName) ? assetName : templateData.Item2.templateName,
                        isPinned = templateData.Item2.addToDefaults,
                        isReadonly = isReadOnly,
                        assetPath = templateData.Item1,
                        description = templateData.Item2.description,
                        thumbnail = templateData.Item2.preview,
                        badge = templateData.Item2.badge,
                        sceneTemplate = templateData.Item2,
                        onCreateCallback = loadAdditively => CreateSceneFromTemplate(templateData.Item1, loadAdditively)
                    };
                }).ToList();

            sceneTemplateAssetInfos.Sort();
            sceneTemplateList.AddRange(sceneTemplateAssetInfos);

            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
            {
                sceneTemplateList.AddRange(builtin2DTemplateInfos);
            }
            else
            {
                sceneTemplateList.AddRange(builtin3DTemplateInfos);
            }

            return sceneTemplateList;
        }

        internal static bool CreateBuiltinScene(BuiltinTemplateType type, bool loadAdditively)
        {
            if (loadAdditively && HasSceneUntitled() && !EditorSceneManager.SaveOpenScenes())
            {
                return false;
            }

            if (!loadAdditively && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            var eventType = type != BuiltinTemplateType.Empty ? SceneTemplateAnalytics.SceneInstantiationType.DefaultScene : SceneTemplateAnalytics.SceneInstantiationType.EmptyScene;
            var instantiateEvent = new SceneTemplateAnalytics.SceneInstantiationEvent(eventType)
            {
                additive = loadAdditively
            };

            Scene scene;
            switch (type)
            {
                case BuiltinTemplateType.Default2DMode3DCamera:
                    // Fake 3D mode to ensure proper set of default game objects are created.
                    EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;
                    scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, loadAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
                    EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
                    break;
                case BuiltinTemplateType.Default3D:
                    scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, loadAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
                    break;
                case BuiltinTemplateType.Default2D:
                    scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, loadAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
                    break;
                default:
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, loadAdditively ? NewSceneMode.Additive : NewSceneMode.Single);
                    break;
            }
            EditorSceneManager.ClearSceneDirtiness(scene);
            SceneTemplateAnalytics.SendSceneInstantiationEvent(instantiateEvent);
            return true;
        }

        private static bool CreateSceneFromTemplate(string templateAssetPath, bool loadAdditively)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(templateAssetPath);
            if (sceneAsset == null)
                return false;
            if (!sceneAsset.isValid)
            {
                UnityEngine.Debug.LogError("Cannot instantiate scene template: scene is null or deleted.");
                return false;
            }

            return SceneTemplateService.Instantiate(sceneAsset, loadAdditively, null, SceneTemplateAnalytics.SceneInstantiationType.NewSceneMenu) != null;
        }

        internal static bool HasSceneUntitled()
        {
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(scene.path))
                    return true;
            }

            return false;
        }

        // Based on UpmPackageInfo::IsPackageReadOnly() in PackageManagerCommon.cpp
        internal static bool IsPackageReadOnly(PackageManager.PackageInfo pi)
        {
            if (pi.source == PackageSource.Embedded || pi.source == PackageSource.Local)
                return false;
            return true;
        }

        internal static bool IsAssetReadOnly(string assetPath)
        {
            var pi = PackageManager.PackageInfo.FindForAssetPath(assetPath);
            return pi != null && IsPackageReadOnly(pi);
        }

        internal static void DeleteAsset(string path, int retryCount = 5)
        {
            var retries = 0;
            while (retries < retryCount && !AssetDatabase.DeleteAsset(path))
                ++retries;
            if (retries >= retryCount)
                throw new Exception($"Failed to delete asset \"{path}\"");
        }
    }
}
