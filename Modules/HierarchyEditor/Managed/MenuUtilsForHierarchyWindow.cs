// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Hierarchy.Editor
{
    static class MenuUtilsForHierarchyWindow
    {
        const int kInvalidSceneHandle = 0;

        internal static void AddCreateGameObjectItemsToSubSceneMenu(DropdownMenu menu, Scene scene)
        {
            AddCreateGameObjectItemsToMenu( menu,
                                            Array.Empty<Object>(),
                                            false,
                                            true,
                                            true,
                                            scene.handle,
                                            MenuUtils.ContextMenuOrigin.Subscene);
        }

        internal static void AddCreateGameObjectItemsToSceneMenu(DropdownMenu menu, Scene scene)
        {
            var count = Selection.transforms.Length;
            GameObject[] gameObjects = new GameObject[count];

            for (var i = 0; i < count; i++)
            {
                gameObjects[i] = Selection.transforms[i].gameObject;
            }

            AddCreateGameObjectItemsToMenu( menu,
                                            gameObjects,
                                            false,
                                            false,
                                            true,
                                            scene.handle,
                                            MenuUtils.ContextMenuOrigin.Scene);
        }

        internal static void AddCreateGameObjectItemsToMenu(   DropdownMenu menu,
                                                               UnityEngine.Object[] context,
                                                               bool includeCreateEmptyChild,
                                                               bool useCreateEmptyParentMenuItem,
                                                               bool includeGameObjectInPath,
                                                               SceneHandle targetSceneHandle,
                                                               MenuUtils.ContextMenuOrigin origin)
        {

            using var poolHandle = GenericMenu.Pool.Get(out var genericMenu);
            MenuUtils.AddCreateGameObjectItemsToMenu(genericMenu, context, includeCreateEmptyChild,
                useCreateEmptyParentMenuItem, includeGameObjectInPath, targetSceneHandle, origin,
                BeforeCreateGameObjectMenuItemWasExecuted, AfterCreateGameObjectMenuItemWasExecuted);

            menu.AppendFromGenericMenu(genericMenu);
        }

        static void BeforeCreateGameObjectMenuItemWasExecuted(  string menuPath,
                                                                UnityEngine.Object[] contextObjects,
                                                                MenuUtils.ContextMenuOrigin origin,
                                                                SceneHandle userData)
        {
            if (origin == MenuUtils.ContextMenuOrigin.Scene || origin == MenuUtils.ContextMenuOrigin.Subscene)
                GOCreationCommands.forcePlaceObjectsAtWorldOrigin = true;
            EditorSceneManager.SetTargetSceneForNewGameObjects(userData);
        }

        static void AfterCreateGameObjectMenuItemWasExecuted(   string menuPath,
                                                                UnityEngine.Object[] contextObjects,
                                                                MenuUtils.ContextMenuOrigin origin,
                                                                SceneHandle userData)
        {
            EditorSceneManager.SetTargetSceneForNewGameObjects(SceneHandle.None);
            GOCreationCommands.forcePlaceObjectsAtWorldOrigin = false;
            // Ensure framing when creating game objects even if we are locked
            // if (isLocked)
            //     m_FrameOnSelectionSync = true;
        }
    }
}
