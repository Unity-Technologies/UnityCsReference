// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public static partial class StageUtility
    {
        internal enum ContextRenderMode
        {
            Normal,
            GreyedOut,
            Hidden
        }

        [Shortcut("Stage/Go Back")]
        static void GoBackShortcut()
        {
            StageUtility.GoBackToPreviousStage();
        }

        public static bool IsGameObjectRenderedByCamera(GameObject gameObject, Camera camera)
        {
            return IsGameObjectRenderedByCameraInternal(gameObject, camera);
        }

        public static bool IsGameObjectRenderedByCameraAndPartOfEditableScene(GameObject gameObject, Camera camera)
        {
            if (!IsGameObjectRenderedByCamera(gameObject, camera))
                return false;

            var scene = GetFocusedScene();
            if (scene.handle == 0)
                return true;

            return gameObject.scene == scene;
        }

        internal static void SetSceneToRenderInStage(Scene scene, StageHandle stageHandle)
        {
            if (!stageHandle.IsValid())
                throw new System.ArgumentException("Stage is not valid.", nameof(stageHandle));
            if (stageHandle.isMainStage)
                SetSceneToRenderInMainStageInternal(scene.handle);
            else
                SetSceneToRenderInSameStageAsOtherSceneInternal(scene.handle, stageHandle.customScene.handle);
        }

        public static Stage GetCurrentStage()
        {
            return StageNavigationManager.instance.currentStage;
        }

        public static MainStage GetMainStage()
        {
            return StageNavigationManager.instance.mainStage;
        }

        public static Stage GetStage(GameObject gameObject)
        {
            return GetStage(gameObject.scene);
        }

        public static Stage GetStage(Scene scene)
        {
            return StageNavigationManager.instance.GetStage(scene);
        }

        public static StageHandle GetCurrentStageHandle()
        {
            return StageHandle.GetCurrentStageHandle();
        }

        public static StageHandle GetMainStageHandle()
        {
            return StageHandle.GetMainStageHandle();
        }

        public static StageHandle GetStageHandle(GameObject gameObject)
        {
            return StageHandle.GetStageHandle(gameObject.scene);
        }

        public static StageHandle GetStageHandle(Scene scene)
        {
            return StageHandle.GetStageHandle(scene);
        }

        public static void GoToMainStage()
        {
            StageNavigationManager.instance.GoToMainStage(StageNavigationManager.Analytics.ChangeType.GoToMainViaUnknown);
        }

        public static void GoBackToPreviousStage()
        {
            StageNavigationManager.instance.NavigateBack(StageNavigationManager.Analytics.ChangeType.NavigateBackViaUnknown);
        }

        public static void GoToStage(Stage stage, bool setAsFirstItemAfterMainStage)
        {
            StageNavigationManager.instance.SwitchToStage(stage, setAsFirstItemAfterMainStage, StageNavigationManager.Analytics.ChangeType.Unknown);
        }

        public static void PlaceGameObjectInCurrentStage(GameObject gameObject)
        {
            StageNavigationManager.instance.PlaceGameObjectInCurrentStage(gameObject);
        }

        internal static Hash128 CreateWindowAndStageIdentifier(string windowGUID, Stage stage)
        {
            Hash128 hash = stage.GetHashForStateStorage();
            hash.Append(windowGUID);
            hash.Append(stage.GetType().FullName);
            return hash;
        }

        internal static void SetPrefabInstanceHiddenForInContextEditing(GameObject gameObject, bool hide)
        {
            SetPrefabInstanceHiddenForInContextEditingInternal(gameObject, hide);
        }

        internal static bool IsPrefabInstanceHiddenForInContextEditing(GameObject gameObject)
        {
            return IsPrefabInstanceHiddenForInContextEditingInternal(gameObject);
        }

        internal static void EnableHidingForInContextEditingInSceneView(bool enable)
        {
            EnableHidingForInContextEditingInSceneViewInternal(enable);
        }

        internal static void SetFocusedScene(Scene scene)
        {
            SetFocusedSceneInternal(scene.IsValid() ? scene.handle : 0);
        }

        internal static Scene GetFocusedScene()
        {
            return GetFocusedSceneInternal();
        }

        internal static void SetFocusedSceneContextRenderMode(ContextRenderMode contextRenderMode)
        {
            SetFocusedSceneContextRenderModeInternal(contextRenderMode);
        }

        internal static void CallAwakeFromLoadOnSubHierarchy(GameObject prefabInstanceRoot)
        {
            CallAwakeFromLoadOnSubHierarchyInternal(prefabInstanceRoot);
        }
    }
}
