// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class GameObjectModule : ModuleWithAnalyzers<GameObjectModuleAnalyzer>
    {
        enum GameObjectProperty
        {
            Asset,
            Num
        }

        internal static readonly IssueLayout k_IssueLayout = new IssueLayout
        {
            Category = IssueCategory.GameObject,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description", MaxAutoWidth = 800 },
                new PropertyDefinition { Type = PropertyType.Severity, Format = PropertyFormat.String, Name = "Severity"},
                new PropertyDefinition { Type = PropertyType.Areas, Format = PropertyFormat.String, Name = "Areas", LongName = "Impacted Areas" },
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(GameObjectProperty.Asset), Name = "Asset", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true},
                new PropertyDefinition { Type = PropertyType.IsIgnored, Name = "Ignored"},
            ]
        };

        public override string Name => "Game Objects";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => [k_IssueLayout];

        public override AnalysisResult Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length == 0)
                return AnalysisResult.Success;

            var gameObjectTracker = new HashSet<EntityId>(); // Only analyze each GameObject once

            AuditScenes(analyzers, gameObjectTracker, analysisParams, progress);
            AuditPrefabs(analyzers, gameObjectTracker, analysisParams, progress);

            if (progress != null)
            {
                progress.Clear();
                if (progress.IsCancelled)
                    return AnalysisResult.Cancelled;
            }

            return AnalysisResult.Success;
        }

        void AuditScenes(GameObjectModuleAnalyzer[] analyzers, HashSet<EntityId> gameObjectTracker, AnalysisParams analysisParams, IProgress progress)
        {
            var context = new AnalysisContext { Params = analysisParams };
            var scenePaths = GetAssetPathsByFilter("t:Scene", context);
            
            progress?.Start("Finding Scene Game Objects", "Search in Progress...", scenePaths.Length);
            
            foreach (string scenePath in scenePaths)
            {
                var scene = SceneManager.GetSceneByPath(scenePath);

                // If scene is not open, open a preview scene for it
                bool closeScene = false;
                if (scene == null || !scene.IsValid() || !scene.isLoaded)
                {
                    if (!scenePath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                    {
                        scene = EditorSceneManager.OpenPreviewScene(scenePath);
                        closeScene = true;
                    }
                }

                // Iterate all GameObjects
                if (scene != null && scene.IsValid())
                {
                    var roots = scene.GetRootGameObjects();
                    foreach (var go in roots)
                        IterateGameObjectHierarchy(analyzers, gameObjectTracker, analysisParams, go, scene.path);
                }

                // Unload preview
                if (closeScene)
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                    EditorUtility.UnloadUnusedAssetsImmediate();
                }

                // Progress
                if (progress != null)
                {
                    if (progress.IsCancelled)
                        return;
                    progress.Advance();
                }
            }
        }

        void AuditPrefabs(GameObjectModuleAnalyzer[] analyzers, HashSet<EntityId> gameObjectTracker, AnalysisParams analysisParams, IProgress progress)
        {
            var context = new AnalysisContext { Params = analysisParams };
            var allAssetPaths = GetAssetPathsByFilter("t:prefab", context);

            progress?.Start("Finding Prefab Game Objects", "Search in Progress...", allAssetPaths.Length);

            foreach (var assetPath in allAssetPaths)
            {
                // Iterate GameObjects
                using (var editingScope = new ViewPrefabContentsScope(assetPath))
                {
                    var loadedPrefabRoot = editingScope.prefabContentsRoot;
                    IterateGameObjectHierarchy(analyzers, gameObjectTracker, analysisParams, loadedPrefabRoot, assetPath);
                }

                // Progress
                if (progress != null)
                {
                    if (progress.IsCancelled)
                        break;
                    progress.Advance();
                }
            }
        }

        // Traverse a GameObject hierarchy and run the analyzers
        void IterateGameObjectHierarchy(GameObjectModuleAnalyzer[] analyzers, HashSet<EntityId> gameObjectTracker, AnalysisParams analysisParams, GameObject gameObject, string issuePath)
        {
            // Only visit each GameObject once
            if (gameObjectTracker.Add(gameObject.GetEntityId()) == false)
                return;

            var gameObjectAnalysisContext = new GameObjectAnalysisContext
            {
                Params = analysisParams,
                GameObject = gameObject
            };

            // Analyze
            foreach (var analyzer in analyzers)
            {
                var reportItemBuilders = analyzer.Analyze(gameObjectAnalysisContext);
                var reportItems = new List<ReportItem>();
                foreach (var item in reportItemBuilders)
                    reportItems.Add(item.WithCustomProperties([issuePath]));
                analysisParams.OnIncomingIssues(reportItems);
            }

            // Itearate children
            var transform = gameObject.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i).gameObject;
                IterateGameObjectHierarchy(analyzers, gameObjectTracker, analysisParams, child, issuePath);
            }
        }

        struct ViewPrefabContentsScope : IDisposable
        {
            public readonly GameObject prefabContentsRoot;

            public ViewPrefabContentsScope(string assetPath)
            {
                prefabContentsRoot = PrefabUtility.LoadPrefabContents(assetPath);
            }

            public void Dispose()
            {
                PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
            }
        }
    }
}
