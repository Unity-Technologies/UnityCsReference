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
    }
}
