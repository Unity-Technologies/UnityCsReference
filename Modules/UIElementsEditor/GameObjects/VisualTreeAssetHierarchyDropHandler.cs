// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

#nullable enable
namespace UnityEditor.UIElements
{
    internal static class VisualTreeAssetHierarchyDropHandler
    {
        const string k_UndoCreatePanelRenderer = "Create Panel Renderer";

        /// <summary>GenericData key used when entityIds do not resolve (e.g. test with CreateInstance assets). Caller may set this to an Object[] of VisualTreeAssets.</summary>
        internal const string k_GenericDataVisualTreeAssets = "VisualTreeAssetHierarchyDropHandler.VisualTreeAssets";

        /// <summary>GenericData key for tests: when the hierarchy passes null for parentForDraggedObjects, the handler may use this Transform as the parent for new objects.</summary>
        internal const string k_GenericDataParentForNewObjects = "VisualTreeAssetHierarchyDropHandler.ParentForNewObjects";

        static bool s_Registered;

        internal static void Register()
        {
            if (s_Registered)
                return;
            DragAndDrop.AddDropHandlerV2(OnHierarchyDrop);
            s_Registered = true;
        }

        static DragAndDropVisualMode OnHierarchyDrop(EntityId dropTargetEntityId, HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects, bool perform)
        {
            // Do not handle when in visual element integrated authoring; the dedicated callback will handle it.
            var currentStage = StageUtility.GetCurrentStage();
            if (currentStage != null && currentStage.GetType().Name == "VisualElementEditingStage")
                return DragAndDropVisualMode.None;

            if (!TryGetVisualTreeAssetsFromDrag(out var visualTreeAssets) || visualTreeAssets== null || visualTreeAssets?.Count == 0)
                return DragAndDropVisualMode.None;

            if (!perform)
                return DragAndDropVisualMode.Copy;

            return PerformDrop(dropTargetEntityId, dropMode, parentForDraggedObjects, visualTreeAssets!);
        }

        static bool TryGetVisualTreeAssetsFromDrag(out List<VisualTreeAsset>? visualTreeAssets)
        {
            visualTreeAssets = null;
            var refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length == 0)
            {
                // objectReferences is derived from entityIds via GetObjectFromEntityId; in some contexts
                // (e.g. tests with CreateInstance, multi-scene) that can return null. Resolve from entityIds first.
                var ids = DragAndDrop.entityIds;
                if (ids != null && ids.Length > 0)
                {
                    foreach (var id in ids)
                    {
                        var o = EditorUtility.EntityIdToObject(id);
                        if (o is VisualTreeAsset vta)
                        {
                            visualTreeAssets ??= new();
                            visualTreeAssets.Add(vta);
                        }
                        else if (o != null)
                            return false;
                    }
                }
                // When entityIds do not resolve (e.g. in-memory assets in tests), allow caller to pass VTAs via GenericData.
                if (visualTreeAssets == null || visualTreeAssets.Count == 0)
                {
                    var generic = DragAndDrop.GetGenericData(k_GenericDataVisualTreeAssets) as Object[];
                    if (generic != null && generic.Length > 0)
                    {
                        foreach (var o in generic)
                        {
                            if (o is VisualTreeAsset vta)
                            {
                                visualTreeAssets ??= new();
                                visualTreeAssets.Add(vta);
                            }
                            else if (o != null)
                                return false;
                        }
                    }
                }
                return visualTreeAssets?.Count > 0;
            }

            foreach (var o in refs)
            {
                if (o is VisualTreeAsset vta)
                {
                    visualTreeAssets ??= new();
                    visualTreeAssets.Add(vta);
                }
                else
                    return false;
            }

            return true;
        }

        static DragAndDropVisualMode PerformDrop(EntityId dropTargetEntityId, HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects, List<VisualTreeAsset> visualTreeAssets)
        {
            if (!GetNewParentAndDropUpon(dropTargetEntityId, dropMode, parentForDraggedObjects,
                    out var newParent, out var dropUponTransform, out var destinationScene))
                return DragAndDropVisualMode.None;

            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            var effectiveParentForNaming = newParent ?? (prefabStage != null ? prefabStage.prefabContentsRoot.transform : null);
            var panelSettings = PlayModeMenuItems.GetPanelSettingsFromProjectOrCreate();

            // Prefer GenericData as the source when present so we use the caller's array directly (avoids
            // list references becoming Unity-null e.g. for CreateInstance VTAs in tests).
            var genericVtas = DragAndDrop.GetGenericData(k_GenericDataVisualTreeAssets) as Object[];
            var vtaSourceCount = 0;
            if (genericVtas != null && genericVtas.Length > 0)
            {
                vtaSourceCount = genericVtas.Length;
            }
            else if (visualTreeAssets.Count > 0)
            {
                vtaSourceCount = visualTreeAssets.Count;
            }
            if (vtaSourceCount == 0)
                return DragAndDropVisualMode.None;

            var created = new List<GameObject>(vtaSourceCount);
            Scene previousActiveScene = default;
            for (var i = 0; i < vtaSourceCount; i++)
            {
                var vta = genericVtas != null && i < genericVtas.Length
                    ? genericVtas[i] as VisualTreeAsset
                    : visualTreeAssets[i];
                if (vta == null)
                    continue;

                var baseName = string.IsNullOrEmpty(vta.name) ? "Panel Renderer" : vta.name;
                var uniqueName = GameObjectUtility.GetUniqueNameForSibling(effectiveParentForNaming, baseName);

                var go = new GameObject(uniqueName);
                var panelRenderer = go.AddComponent<PanelRenderer>();

                if (panelRenderer != null)
                {
                    panelRenderer.visualTreeAsset = vta;
                    panelRenderer.panelSettings = panelSettings;
                }

                if (newParent != null)
                {
                    // Defer MoveGameObjectToScene and SetParent until after the loop so we don't switch
                    // the active scene before all VTAs are read (see comment above).
                    created.Add(go);
                }
                else
                {
                    if (prefabStage != null)
                    {
                        StageUtility.PlaceGameObjectInCurrentStage(go);
                        go.transform.SetParent(prefabStage.prefabContentsRoot.transform);
                    }
                    else if (destinationScene.IsValid())
                    {
                        SceneManager.MoveGameObjectToScene(go, destinationScene);
                        if (dropUponTransform == null)
                            go.transform.SetAsLastSibling();
                    }
                    else
                    {
                        StageUtility.PlaceGameObjectInCurrentStage(go);
                    }
                    created.Add(go);
                }
            }

            // Now switch scene and parent when newParent is set (after VTA references have been used).
            if (newParent != null && created.Count > 0)
            {
                var targetScene = newParent.gameObject.scene;
                var inPrefabStage = prefabStage != null && targetScene == prefabStage.scene;
                if (!inPrefabStage)
                {
                    previousActiveScene = SceneManager.GetActiveScene();
                    if (previousActiveScene != targetScene)
                        SceneManager.SetActiveScene(targetScene);
                }
                foreach (var go in created)
                {
                    SceneManager.MoveGameObjectToScene(go, targetScene);
                    go.transform.SetParent(newParent);
                }
            }

            if (newParent != null && previousActiveScene.IsValid())
            {
                var targetScene = newParent.gameObject.scene;
                var inPrefabStage = prefabStage != null && targetScene == prefabStage.scene;
                if (!inPrefabStage && previousActiveScene != targetScene)
                    SceneManager.SetActiveScene(previousActiveScene);
            }

            if (dropUponTransform != null && created.Count > 0)
                SortHierarchyForNewObjects(dropUponTransform, created, dropMode);

            if (created.Count > 0)
            {
                for (var i = 0; i < created.Count; i++)
                    Undo.RegisterCreatedObjectUndo(created[i], k_UndoCreatePanelRenderer);
                Selection.objects = created.ToArray();
            }

            return DragAndDropVisualMode.Copy;
        }

        static bool GetNewParentAndDropUpon(EntityId dropTargetEntityId, HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects, out Transform? newParent, out Transform? dropUponTransform,
            out Scene destinationScene)
        {
            newParent = null;
            dropUponTransform = null;
            destinationScene = default;

            var genericParent = DragAndDrop.GetGenericData(k_GenericDataParentForNewObjects) as Transform;
            var effectiveParent = parentForDraggedObjects ?? genericParent;
            if (effectiveParent != null)
            {
                newParent = effectiveParent;
                return true;
            }

            var dropUpon = EditorUtility.EntityIdToObject(dropTargetEntityId) as GameObject;
            if (dropUpon == null)
            {
                // EntityIdToObject can return null for objects in a non-active scene. Set that scene active and retry
                // so we can resolve the drop target and parent (e.g. drop between children in second scene).
                var sceneForEntity = GameObject.GetScene(dropTargetEntityId);
                if (sceneForEntity.IsValid())
                {
                    var prev = SceneManager.GetActiveScene();
                    if (prev != sceneForEntity)
                        SceneManager.SetActiveScene(sceneForEntity);
                    dropUpon = EditorUtility.EntityIdToObject(dropTargetEntityId) as GameObject;
                    if (prev.IsValid() && prev != sceneForEntity)
                        SceneManager.SetActiveScene(prev);
                }
            }

            if (dropUpon != null)
            {
                dropUponTransform = dropUpon.transform;
                var dropUponParent = dropUponTransform.parent;

                if ((dropMode & HierarchyDropFlags.DropBetween) != 0 || (dropMode & HierarchyDropFlags.DropAbove) != 0)
                    newParent = dropUponParent;
                else
                    newParent = dropUponTransform;

                // When dropping between roots (or above first root), newParent is null; we still need the scene
                // so the new object is created in the correct scene, not the active one.
                if (newParent == null)
                    destinationScene = dropUpon.gameObject.scene;

                return true;
            }

            // EntityIdToObject can return null for objects in a non-active scene (e.g. second scene in multi-scene).
            // Resolve the scene containing this entity so we at least create in the correct scene.
            var sceneForEntity2 = GameObject.GetScene(dropTargetEntityId);
            if (sceneForEntity2.IsValid())
            {
                destinationScene = sceneForEntity2;
                return true;
            }

            // Drop target may be a Scene header (e.g. in multi-scene setup); EntityIdToObject returns null for Scene.
            var sceneHandle = SceneHandle.FromRawData(EntityId.ToULong(dropTargetEntityId));
            var sceneByHandle = EditorSceneManager.GetSceneByHandle(sceneHandle);
            if (sceneByHandle.IsValid())
            {
                destinationScene = sceneByHandle;
                return true;
            }

            var lastScene = GetLastLoadedScene();
            if (!lastScene.IsValid())
                return false;

            destinationScene = lastScene;
            return true;
        }

        static void SortHierarchyForNewObjects(Transform dropUpon, List<GameObject> created, HierarchyDropFlags dropMode)
        {
            if (dropUpon == null || created.Count == 0)
                return;

            var k = dropUpon.GetSiblingIndex();
            var transforms = created.ConvertAll(go => (Object)go.transform).ToArray();
            Undo.RecordObjects(transforms, k_UndoCreatePanelRenderer);


            int startSibingIndex = 0;

            if ((dropMode & HierarchyDropFlags.DropBetween) != 0)
            {
                if ((dropMode & HierarchyDropFlags.DropAfterParent) != 0)
                    startSibingIndex = 0;
                else
                    startSibingIndex = k + 1;
            }
            else if ((dropMode & HierarchyDropFlags.DropAbove) != 0)
            {
                startSibingIndex = k;
            }
            else
            {
                // Let's assume they have been created in the right order
                return;
            }
                
            for (var i = 0; i < created.Count; i++)
                created[i].transform.SetSiblingIndex(startSibingIndex + i);
        }

        static Scene GetLastLoadedScene()
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !scene.isSubScene)
                    return scene;
            }

            return default;
        }

    }
}
