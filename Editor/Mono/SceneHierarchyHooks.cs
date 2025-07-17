// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Runtime.InteropServices;

namespace UnityEditor.SceneManagement
{
    public static class SceneHierarchyHooks
    {
        [StructLayout(LayoutKind.Sequential)]
        [UnityEngine.Bindings.NativeType(IntermediateScriptingStructName = "SceneHierarchyHooks_SubSceneInfo")]
        public struct SubSceneInfo
        {
            public Transform transform;
            public Scene scene;
            [UnityEngine.Bindings.Ignore] public SceneAsset sceneAsset;
            [UnityEngine.Bindings.Ignore] public string sceneName;  // TODO: remove when when we have changed our examples to use 'provideSubSceneName'
            [UnityEngine.Bindings.Ignore] public Color32 color;

            public bool isValid
            {
                get { return transform != null; }
            }
        }

        public static Func<SubSceneInfo[]> provideSubScenes;
        public static Func<SubSceneInfo, string> provideSubSceneName;
        public static event Action<GenericMenu, GameObject> addItemsToGameObjectContextMenu;
        public static event Action<GenericMenu, Scene> addItemsToSceneHeaderContextMenu;
        public static event Action<GenericMenu, SubSceneInfo> addItemsToSubSceneHeaderContextMenu;
        public static event Action<GenericMenu> addItemsToCreateMenu;

        public static void ReloadAllSceneHierarchies()
        {
            foreach (var window in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
                window.sceneHierarchy.ReloadData();
        }

        static void RefreshSubSceneInfo()
        {
            if (SubSceneGUI.IsUsingSubScenes())
                SubSceneGUI.FetchSubSceneInfo();
        }

        public static bool CanSetNewParent(Transform transform, Transform newParent)
        {
            if (transform == null)
                throw new ArgumentNullException("transform");

            if (newParent == null)
                return true;

            RefreshSubSceneInfo();
            var parentIsChild = SubSceneGUI.IsChildOrSameAsOtherTransform(newParent, transform);
            return !parentIsChild;
        }

        public static bool CanMoveTransformToScene(Transform transform, Scene scene)
        {
            if (transform == null)
                throw new ArgumentNullException("transform");

            if (!scene.IsValid())
                throw new ArgumentException("The scene is not valid", "scene");

            RefreshSubSceneInfo();
            var subSceneInfo = SubSceneGUI.GetSubSceneInfo(scene);
            if (!subSceneInfo.isValid)
                return true;  // scene is a root and is always valid to move into

            if (transform == subSceneInfo.transform)
                return false;  // cannot move a SubScene's transform parent into itself

            return CanSetNewParent(transform, subSceneInfo.transform);
        }

        internal static void AddCustomGameObjectContextMenuItems(GenericMenu menu, GameObject gameObject)
        {
            addItemsToGameObjectContextMenu?.Invoke(menu, gameObject);
        }

        internal static void AddCustomSceneHeaderContextMenuItems(GenericMenu menu, Scene scene)
        {
            addItemsToSceneHeaderContextMenu?.Invoke(menu, scene);
        }

        internal static void AddCustomSubSceneHeaderContextMenuItems(GenericMenu menu, SubSceneInfo subSceneInfo)
        {
            addItemsToSubSceneHeaderContextMenu?.Invoke(menu, subSceneInfo);
        }

        internal static void AddCustomItemsToCreateMenu(GenericMenu menu)
        {
            addItemsToCreateMenu?.Invoke(menu);
        }

        internal static bool CanSceneBeReloaded(Scene scene)
        {
            var path = scene.path;
            return !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
        }

        internal static void UnloadScene(object userData)
        {
            var (scene, _) = ((Scene, bool))userData;
            CloseScene(removeScene: false, scene);
        }

        internal static void LoadScene(object userData)
        {
            var scene = (Scene)userData;
            if (!scene.isLoaded)
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);

            EditorApplication.RequestRepaintAllViews();
        }

        internal static void RemoveScene(object userData)
        {
            var scene = (Scene)userData;
            CloseScene(removeScene: true, scene);
        }

        static void CloseScene(bool removeScene, Scene scene)
        {
            if (scene.isDirty)
            {
                var userCancelled = !EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[] { scene });
                if (userCancelled)
                    return;
            }

            EditorSceneManager.CloseScene(scene, removeScene);

            EditorApplication.RequestRepaintAllViews();
        }

        internal static void SaveScene(object userData)
        {
            var (scene, _, _) = ((Scene, bool, bool))userData;
            if (scene.isLoaded)
                EditorSceneManager.SaveScene(scene);
        }

        internal static void SaveSceneAs(object userData)
        {
            var (scene, _, _) = ((Scene, bool, bool))userData;
            if (scene.isLoaded)
                EditorSceneManager.SaveSceneAs(scene);
        }

        // TODO: This needs to be able to handle multi-selections for multiple scenes.
        internal static void DiscardChanges(object userData)
        {
            var (scene, _) = ((Scene, bool))userData;

            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("Discarding changes in a scene that have not yet been saved is not supported. Save the scene first or create a new scene.");
                return;
            }

            var scenes = new Scene[1];
            scenes[0] = scene;
            if (!SceneHierarchy.UserAllowedDiscardingChanges(scenes))
                return;

            EditorSceneManager.ReloadScene(scene);
            EditorApplication.RequestRepaintAllViews();
        }
    }
}
