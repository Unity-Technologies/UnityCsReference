// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        enum MeshColliderProperty
        {
            Triangles,
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

        static readonly IssueLayout k_MeshColliderLayout = new IssueLayout
        {
            Category = IssueCategory.MeshCollider,
            Properties =
            [
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "MeshCollider GameObject Name", MaxAutoWidth = 500 },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(MeshColliderProperty.Triangles), Format = PropertyFormat.Integer, Name = "Triangles", LongName = "Triangle Count"},
                new PropertyDefinition { Type = PropertyType.Path, Name = "Path", MaxAutoWidth = 500, IsDefaultGroup = true }
            ]
        };

        public override string Name => "Game Objects";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => [k_IssueLayout, k_MeshColliderLayout];

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var analyzers = GetCompatibleAnalyzers(analysisParams);
            if (analyzers.Length > 0)
            {
                var gameObjectTracker = new HashSet<EntityId>(); // Only analyze each GameObject once

                foreach (var analyzer in analyzers)
                    analyzer.OnAnalysisStarted();

                yield return AuditScenes(analyzers, gameObjectTracker, analysisParams, progress);
                yield return AuditPrefabs(analyzers, gameObjectTracker, analysisParams, progress);
            }

            analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
        }

        IEnumerator AuditScenes(GameObjectModuleAnalyzer[] analyzers, HashSet<EntityId> gameObjectTracker, AnalysisParams analysisParams, IProgress progress)
        {
            var context = new AnalysisContext { Params = analysisParams };
            var scenePaths = GetAssetPathsByFilter("t:scene", context);

            AsyncProgressState progressState = progress?.Start("Analyzing Scene Game Objects", scenePaths.Length);

            yield return null;

            foreach (string scenePath in scenePaths)
            {
                // Progress
                if (AdvanceAsyncProgress(progress, progressState, Path.GetFileName(scenePath)) == false)
                    break;

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

                yield return null;
            }

            progress?.Clear(progressState);
        }

        IEnumerator AuditPrefabs(GameObjectModuleAnalyzer[] analyzers, HashSet<EntityId> gameObjectTracker, AnalysisParams analysisParams, IProgress progress)
        {
            var context = new AnalysisContext { Params = analysisParams };
            var allAssetPaths = GetAssetPathsByFilter("t:prefab", context);

            AsyncProgressState progressState = progress?.Start("Analyzing Prefab Game Objects", allAssetPaths.Length);

            yield return null;

            foreach (var assetPath in allAssetPaths)
            {
                // Progress
                if (AdvanceAsyncProgress(progress, progressState, Path.GetFileName(assetPath)) == false)
                    break;

                // Iterate GameObjects
                using (var editingScope = new ViewPrefabContentsScope(assetPath))
                {
                    var loadedPrefabRoot = editingScope.prefabContentsRoot;

                    List<ReportItem> meshColliderIssues = null;
                    var meshColliders = loadedPrefabRoot.GetComponentsInChildren<MeshCollider>(true);
                    for (var i = 0; i < meshColliders.Length; i++)
                    {
                        var meshCollider = meshColliders[i];
                        var sharedMesh = meshCollider.sharedMesh;
                        var triangleCount = sharedMesh != null ? CalculateTotalTriangleCount(sharedMesh) : 0;

                        meshColliderIssues ??= [];
                        meshColliderIssues.Add(context
                            .CreateInsight(IssueCategory.MeshCollider, meshCollider.gameObject.name)
                            .WithCustomProperties([triangleCount])
                            .WithLocation(assetPath));
                    }

                    if (meshColliderIssues is { Count: > 0 })
                        analysisParams.OnIncomingIssues(meshColliderIssues);

                    IterateGameObjectHierarchy(analyzers, gameObjectTracker, analysisParams, loadedPrefabRoot, assetPath);
                }

                yield return null;
            }

            progress?.Clear(progressState);
        }

        static int CalculateTotalTriangleCount(Mesh mesh)
        {
            var totalTriangles = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                totalTriangles += (int)(mesh.GetIndexCount(i) / 3);
            }

            return totalTriangles;
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
