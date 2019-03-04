// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using System;


namespace UnityEditor.SceneManagement
{
    public static class SceneHierarchyHooks
    {
        public struct SubSceneInfo
        {
            public Transform transform;
            public Scene scene;
            public SceneAsset sceneAsset;
            public string sceneName;
            public Color32 color;


            public bool isValid
            {
                get { return transform != null; }
            }
        }

        public static Func<SubSceneInfo[]> provideSubScenes;
        public static event Action<GenericMenu, GameObject> addItemsToGameObjectContextMenu;
        public static event Action<GenericMenu, Scene> addItemsToSceneHeaderContextMenu;

        public static void ReloadAllSceneHierarchies()
        {
            foreach (var window in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
                window.sceneHierarchy.ReloadData();
        }

        internal static void AddCustomGameObjectContextMenuItems(GenericMenu menu, GameObject gameObject)
        {
            addItemsToGameObjectContextMenu?.Invoke(menu, gameObject);
        }

        internal static void AddCustomSceneHeaderContextMenuItems(GenericMenu menu, Scene scene)
        {
            addItemsToSceneHeaderContextMenu?.Invoke(menu, scene);
        }
    }
}
