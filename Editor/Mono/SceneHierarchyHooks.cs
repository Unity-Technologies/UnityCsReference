// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Pool;

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

        internal static bool CanSceneChangesBeDiscarded(Scene scene)
        {
            bool canReload = scene.isDirty && CanSceneBeReloaded(scene);
            bool canDiscardChanges = !EditorApplication.isPlaying && canReload;

            return canDiscardChanges;
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

        internal static void UnloadScenes(object userData)
        {
            var (scenes, _) = ((Scene[], bool))userData;
            using var _ = ListPool<Scene>.Get(out var unloadableScenes);

            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.path) && !scene.isSubScene && scene.isLoaded)
                    unloadableScenes.Add(scene);
            }

            CloseScenes(removeScene: false, unloadableScenes);
        }

        internal static void LoadScenes(object userData)
        {
            var scenes = (Scene[])userData;

            foreach (var scene in scenes)
            {
                if (!string.IsNullOrEmpty(scene.path) && !scene.isLoaded)
                    EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
            }

            EditorApplication.RequestRepaintAllViews();
        }

        internal static void RemoveScenes(object userData)
        {
            var scenes = (Scene[])userData;
            using var _ = ListPool<Scene>.Get(out var removableScenes);

            foreach (var scene in scenes)
                removableScenes.Add(scene);

            CloseScenes(removeScene: true, removableScenes);
        }

        static void CloseScenes(bool removeScene, List<Scene> scenes)
        {
            using var dirtyScenesSpan = scenes.Count <= 16
                ? new RentSpanUnmanaged<Scene>(stackalloc Scene[scenes.Count])
                : new RentSpanUnmanaged<Scene>(scenes.Count);

            var dirtyCount = 0;
            foreach (var scene in scenes)
            {
                if (scene.isDirty)
                    dirtyScenesSpan.Span[dirtyCount++] = scene;
            }

            var userCancelled = !EditorSceneManager.SaveModifiedScenesIfUserWantsToSpan(dirtyScenesSpan.Span.Slice(0, dirtyCount));
            if (userCancelled)
                return;

            foreach (var scene in scenes)
                EditorSceneManager.CloseScene(scene, removeScene);

            EditorApplication.RequestRepaintAllViews();
        }

        internal static void SaveScenes(object userData)
        {
            var (scenes, _, _) = ((Scene[], bool, bool))userData;

            foreach (var scene in scenes)
            {
                if (scene.isLoaded && (!EditorApplication.isPlaying || scene.isSubScene))
                    EditorSceneManager.SaveScene(scene);
            }
        }

        internal static void SaveSceneAs(object userData)
        {
            var (scene, _, _) = ((Scene, bool, bool))userData;
            if (scene.isLoaded)
                EditorSceneManager.SaveSceneAs(scene);
        }

        internal static void DiscardChanges(object userData)
        {
            var (scenes, _) = ((Scene[], bool))userData;

            using var discardableScenesSpan = scenes.Length <= 16
                ? new RentSpanUnmanaged<Scene>(stackalloc Scene[scenes.Length])
                : new RentSpanUnmanaged<Scene>(scenes.Length);

            var discardablesCount = 0;
            foreach (var scene in scenes)
            {
                if (string.IsNullOrEmpty(scene.path))
                {
                    Debug.LogWarning($"Discarding changes in a scene that have not yet been saved is not supported. Save the scene ({scene.name}) first or create a new scene.");
                    continue;
                }

                if (!CanSceneChangesBeDiscarded(scene))
                    continue;

                discardableScenesSpan.Span[discardablesCount++] = scene;
            }

            if (!SceneHierarchy.UserAllowedDiscardingChanges(discardableScenesSpan.Span.Slice(0, discardablesCount)))
                return;

            for (int i = 0; i < discardablesCount; ++i)
                EditorSceneManager.ReloadScene(discardableScenesSpan.Span[i]);

            EditorApplication.RequestRepaintAllViews();
        }
    }
}
