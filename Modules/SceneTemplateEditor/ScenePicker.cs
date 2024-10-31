// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define ENABLE_SCENE_PREVIEW_OVERLAY
using System;
using System.Collections.Generic;
using UnityEditor.Experimental;
using UnityEditor.Overlays;
using UnityEditor.SceneTemplate;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Search;

namespace UnityEditor.Search
{
    static class ScenePicker
    {
        [MenuItem("Window/Search/Scene", priority = 1269, secondaryPriority = 1)]
        internal static void OpenScenePicker()
        {
            var searchContext = SearchService.CreateContext(CreateOpenSceneProviders(), string.Empty);
            var state = SearchViewState.CreatePickerState(L10n.Tr("Scenes"), searchContext, OnSceneSelected);
            state.excludeClearItem = true;
            SearchService.ShowPicker(state);
        }

        internal static IEnumerable<SearchProvider> CreateOpenSceneProviders()
        {
            yield return new SearchProvider("stemplates", L10n.Tr("Templates"), FetchTemplates)
            {
                priority = 2998,
                fetchPreview = FetchTemplatePreview,
                fetchThumbnail = FetchTemplateThumbnail
            };

            yield return new SearchProvider("sassets", L10n.Tr("Scenes"), FetchScenes)
            {
                priority = 2999,
                fetchLabel = FetchSceneLabel,
                fetchDescription = FetchSceneDescription,
                fetchPreview = FetchScenePreview,
                fetchThumbnail = FetchSceneThumbnail
            };
        }

        static Texture2D FetchTemplateThumbnail(SearchItem item, SearchContext context)
        {
            if (item.thumbnail)
                return item.thumbnail;

            var sceneTemplateInfo = item.data as SceneTemplateInfo;
            if (sceneTemplateInfo == null)
                return null;

            if (sceneTemplateInfo.thumbnail)
                return (item.thumbnail = sceneTemplateInfo.thumbnail);

            if (!string.IsNullOrEmpty(sceneTemplateInfo.thumbnailPath))
                item.thumbnail = sceneTemplateInfo.thumbnail = EditorResources.Load<Texture2D>(sceneTemplateInfo.thumbnailPath);

            return item.thumbnail;
        }

        static Texture2D FetchTemplatePreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            var sceneTemplateInfo = item.data as SceneTemplateInfo;
            if (sceneTemplateInfo == null)
                return null;

            return sceneTemplateInfo.sceneTemplate?.preview;
        }

        static IEnumerable<SearchItem> FetchTemplates(SearchContext context, SearchProvider provider)
        {
            long score = 0;
            var templates = SceneTemplateUtils.GetSceneTemplateInfos();
            List<int> matches = new List<int>();
            foreach (var t in templates)
            {
                var id = t.sceneTemplate?.GetInstanceID().ToString() ?? t.name;
                var description = t.description?.Replace("\n", " ");
                if (string.IsNullOrEmpty(context.searchQuery) || FuzzySearch.FuzzyMatch(context.searchQuery, $"{t.name} {description}", ref score, matches))
                    yield return provider.CreateItem(context, id, ~(int)score, t.name, description, t.thumbnail, t);
                score++;
            }
        }

        static string FetchSceneDescription(SearchItem item, SearchContext context)
        {
            if (TryGetScenePath(item, out var scenePath))
                return scenePath;
            return item.id;
        }

        static string FetchSceneLabel(SearchItem item, SearchContext context)
        {
            if (TryGetScenePath(item, out var scenePath))
                return System.IO.Path.GetFileNameWithoutExtension(scenePath);
            return item.id;
        }

        static Texture2D FetchSceneThumbnail(SearchItem item, SearchContext context)
        {
            if (!TryGetScenePath(item, out var scenePath))
                return null;
            return AssetDatabase.GetCachedIcon(scenePath) as Texture2D;
        }

        static Texture2D FetchScenePreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            if (!TryGetScenePath(item, out var scenePath))
                return null;
            var dirPath = System.IO.Path.GetDirectoryName(scenePath);
            var filename = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            var previewScenePath = $"{dirPath}/{filename}.preview.png";
            if (!System.IO.File.Exists(previewScenePath))
                return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(previewScenePath);
        }

        static IEnumerable<SearchItem> FetchScenes(SearchContext context, SearchProvider provider)
        {
            using (var findContext = SearchService.CreateContext("find", $"\\.unity$ {context.searchQuery}"))
            using (var request = SearchService.Request(findContext))
            {
                foreach (var r in request)
                {
                    if (r == null)
                        yield return null;
                    else
                    {
                        r.provider = provider;
                        r.data = null;
                        yield return r;
                    }
                }
            }
        }

        static bool TryGetScenePath(SearchItem item, out string path)
        {
            path = item.data as string;
            if (!string.IsNullOrEmpty(path))
                return true;

            if (!GlobalObjectId.TryParse(item.id, out var gid))
                return false;

            var scenePath = AssetDatabase.GUIDToAssetPath(gid.assetGUID);
            if (string.IsNullOrEmpty(scenePath))
                return false;

            item.data = path = scenePath;
            return true;
        }

        static void OnSceneSelected(SearchItem item, bool canceled)
        {
            if (canceled)
                return;

            if (item.data is SceneTemplateInfo sti)
            {
                const bool additive = false;
                sti.onCreateCallback?.Invoke(additive);
            }
            else
            {
                if (!TryGetScenePath(item, out var scenePath))
                    throw new Exception($"Failed to parse scene id of {item}");
                EditorApplication.OpenFileGeneric(scenePath);
            }
        }
    }

    sealed class ScenePickerScreenshotToolbarOverlay : ToolbarOverlay
    {
        const string k_Id = "unity-scene-screenshot-toolbar";
        const string k_ElementName = "Tools/Screenshot";

        public ScenePickerScreenshotToolbarOverlay() : base(k_ElementName) { }

        sealed class SceneScreenshotToolsStrip : EditorToolbarButton
        {
            const int resWidth = 256;
            const int resHeight = 256;

            public SceneScreenshotToolsStrip() : base(TakeScenePreview)
            {
                name = "ScenePreviewScreenshot";
                icon = EditorGUIUtility.LoadIcon("CameraPreview");
                tooltip = L10n.Tr("Take scene preview screenshot");
            }

            static string GetScenePreviewImagePath()
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene == null)
                    return null;
                string scenePath = scene.path;
                if (string.IsNullOrEmpty(scenePath))
                    return null;
                var dirPath = System.IO.Path.GetDirectoryName(scenePath);
                var sceneFilename = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                return $"{dirPath}/{sceneFilename}.preview.png";
            }

            static void TakeScenePreview()
            {
                var previewScenePath = GetScenePreviewImagePath();
                if (string.IsNullOrEmpty(previewScenePath))
                    return;

                var rt = new RenderTexture(resWidth, resHeight, 32);
                var camera = SceneView.lastActiveSceneView.camera;
                var screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGBA32, true);

                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
                camera.targetTexture = null;
                RenderTexture.active = null;

                var bytes = screenShot.EncodeToPNG();
                System.IO.File.WriteAllBytes(previewScenePath, bytes);
                AssetDatabase.ImportAsset(previewScenePath);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
