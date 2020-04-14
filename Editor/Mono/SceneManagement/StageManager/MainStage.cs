// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public sealed class MainStage : Stage
    {
        static StateCache<MainStageHierarchyState> s_StateCache = new StateCache<MainStageHierarchyState>("Library/StateCache/MainStageHierarchy/");

        internal static MainStage CreateMainStage()
        {
            var mainStage = CreateInstance<MainStage>();
            mainStage.name = "MainStage";
            return mainStage;
        }

        internal override int sceneCount { get { return SceneManager.sceneCount; } }

        internal override Scene GetSceneAt(int index)
        {
            return SceneManager.GetSceneAt(index);
        }

        protected internal override bool OnOpenStage()
        {
            // do nothing as the main stage is always in memory
            return true;
        }

        protected override void OnCloseStage()
        {
            Debug.LogError("The MainStage should not be destroyed. This is not supported.");
        }

        protected internal override GUIContent CreateHeaderContent()
        {
            return new GUIContent(
                "Scenes",
                EditorGUIUtility.IconContent("SceneAsset Icon").image);
        }

        internal override BreadcrumbBar.Item CreateBreadcrumbItem()
        {
            BreadcrumbBar.Item item = base.CreateBreadcrumbItem();
            item.separatorstyle = BreadcrumbBar.SeparatorStyle.None;
            return item;
        }

        internal override ulong GetSceneCullingMask()
        {
            return SceneCullingMasks.MainStageSceneViewObjects;
        }

        internal override void SyncSceneViewToStage(SceneView sceneView)
        {
            sceneView.customScene = new Scene();
            sceneView.customParentForNewGameObjects = null;

            // NOTE: We always set overrideSceneCullingMask to ensure the gizmo handling in the Entities package works (in dots search for 'gizmo hack').
            // This ensures normal picking works with the livelink since the SceneCullingMasks.MainStageSceneViewObjects is part of the mask while the gizmo bit is also set.
            // When the gizmo hack is removed in dots we can set 'sceneView.overrideSceneCullingMask = 0'.
            sceneView.overrideSceneCullingMask = GetSceneCullingMask();
        }

        internal override void SyncSceneHierarchyToStage(SceneHierarchyWindow sceneHierarchyWindow)
        {
            var sceneHierarchy = sceneHierarchyWindow.sceneHierarchy;
            sceneHierarchy.customScenes = null;
            sceneHierarchy.customParentForNewGameObjects = null;
            sceneHierarchy.SetCustomDragHandler(null);
        }

        internal override void PlaceGameObjectInStage(GameObject rootGameObject)
        {
            if (rootGameObject.transform.parent != null)
                throw new ArgumentException("GameObject has a transform parent, only root GameObjects are valid", "rootGameObject");

            if (StageUtility.GetStageHandle(rootGameObject) != StageUtility.GetMainStageHandle())
                SceneManager.MoveGameObjectToScene(rootGameObject, SceneManager.GetActiveScene());
        }

        internal override void SaveHierarchyState(SceneHierarchyWindow hierarchyWindow)
        {
            if (!isValid)
                return;

            Hash128 key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, this);
            var state = s_StateCache.GetState(key);
            if (state == null)
                state = new MainStageHierarchyState();
            state.SaveStateFromHierarchy(hierarchyWindow, this);
            s_StateCache.SetState(key, state);
        }

        MainStageHierarchyState GetStoredHierarchyState(SceneHierarchyWindow hierarchyWindow)
        {
            Hash128 key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, this);
            return s_StateCache.GetState(key);
        }

        internal override void LoadHierarchyState(SceneHierarchyWindow hierarchy)
        {
            if (!isValid)
                return;

            var state = GetStoredHierarchyState(hierarchy);
            if (state != null)
                state.LoadStateIntoHierarchy(hierarchy, this);
            else
                OnFirstTimeOpenStageInSceneHierachyWindow(hierarchy);
        }
    }
}
