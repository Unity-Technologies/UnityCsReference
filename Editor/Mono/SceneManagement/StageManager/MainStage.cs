// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    internal class MainStage : Stage
    {
        static StateCache<MainStageHierarchyState> s_StateCache = new StateCache<MainStageHierarchyState>("Library/StateCache/MainStageHierarchy/");

        internal override int sceneCount { get { return SceneManager.sceneCount; } }

        internal override Scene GetSceneAt(int index)
        {
            return SceneManager.GetSceneAt(index);
        }

        internal override string GetStageID(int maxCharacters)
        {
            var id = "mainStage";
            if (id.Length > maxCharacters)
                id = id.Substring(0, maxCharacters);
            return id;
        }

        internal override bool ActivateStage(Stage previousStage)
        {
            // do nothing as the main stage is always in memory
            return true;
        }

        internal override BreadcrumbBar.Item CreateBreadCrumbItem()
        {
            var history = StageNavigationManager.instance.stageHistory;
            bool isLastCrumb = this == history.Last();
            var label = "Scenes";
            var icon = EditorGUIUtility.FindTexture("UnityEditor/SceneAsset Icon");
            var style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.label;
            var tooltip = "";
            return new BreadcrumbBar.Item
            {
                content = new GUIContent(label, icon, tooltip),
                guistyle = style,
                userdata = this,
                separatorstyle = BreadcrumbBar.SeparatorStyle.None
            };
        }

        internal override ulong GetSceneCullingMask(SceneView sceneView)
        {
            return SceneCullingMasks.MainStageSceneViewObjects;
        }

        internal override void SyncSceneViewToStage(SceneView sceneView)
        {
            sceneView.customScene = new Scene();
            sceneView.customParentForDraggedObjects = null;
            ulong mask = GetSceneCullingMask(sceneView);
            sceneView.overrideSceneCullingMask = mask == EditorSceneManager.DefaultSceneCullingMask ? 0 : mask;
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

            string key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, this);
            var state = s_StateCache.GetState(key);
            if (state == null)
                state = new MainStageHierarchyState();
            state.SaveStateFromHierarchy(hierarchyWindow, this);
            s_StateCache.SetState(key, state);
        }

        MainStageHierarchyState GetStoredHierarchyState(SceneHierarchyWindow hierarchyWindow)
        {
            string key = StageUtility.CreateWindowAndStageIdentifier(hierarchyWindow.windowGUID, this);
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
