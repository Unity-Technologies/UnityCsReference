// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    /// <summary>
    /// Interface for hierarchy window implementations to provide common functionality across different hierarchy window types.
    /// This interface is used to abstract hierarchy window operations since the <see cref="HierarchyWindow" /> type is not available in the core module.
    /// </summary>
    internal interface IHierarchyWindow
    {
        IHierarchyWindow LastInteractedHierarchyWindow { get; }

        void SetExpanded(EntityId entityId, bool expanded);

        static IHierarchyWindow GetLastInteractedHierarchyWindow()
        {
            if (HierarchyPreferences.UseNewHierarchy)
            {
                var windows = Resources.FindObjectsOfTypeAll(HierarchyPreferences.HierarchyV2WindowType);
                if (windows == null || windows.Length == 0 || windows[0] is not IHierarchyWindow wnd)
                    return null;
                return wnd.LastInteractedHierarchyWindow;
            }

            return SceneHierarchyWindow.lastInteractedHierarchyWindow;
        }

        static void GetAllHierarchyWindows(List<IHierarchyWindow> windows)
        {
            windows.Clear();

            if (HierarchyPreferences.UseNewHierarchy)
            {
                var objs = Resources.FindObjectsOfTypeAll(HierarchyPreferences.HierarchyV2WindowType);
                if (objs == null || objs.Length == 0)
                    return;

                foreach (var window in objs)
                {
                    if (window && window is IHierarchyWindow hierarchyWindow)
                        windows.Add(hierarchyWindow);
                }
            }
            else
                windows.AddRange(SceneHierarchyWindow.GetAllSceneHierarchyWindows());
        }

        static void GetSelectedScenes(List<Scene> scenes)
        {
            scenes.Clear();
            var selectedEntityIds = Selection.GetEntityIdsUnsafe();
            foreach (ref readonly var entityId in selectedEntityIds)
            {
                var sceneHandle = SceneHandle.From(entityId);
                if (sceneHandle == SceneHandle.None)
                    continue;

                var scene = EditorSceneManager.GetSceneByHandle(sceneHandle);
                if (!scene.IsValid())
                    continue;

                scenes.Add(scene);
            }
        }
    }
}
