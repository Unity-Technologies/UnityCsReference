// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public sealed partial class EditorSceneManager
    {
        public delegate void NewSceneCreatedCallback(Scene scene, NewSceneSetup setup, NewSceneMode mode);
        public delegate void SceneOpeningCallback(string path, OpenSceneMode mode);
        public delegate void SceneOpenedCallback(Scene scene, OpenSceneMode mode);
        public delegate void SceneClosingCallback(Scene scene, bool removingScene);
        public delegate void SceneClosedCallback(Scene scene);
        public delegate void SceneSavingCallback(Scene scene, string path);
        public delegate void SceneSavedCallback(Scene scene);

        public static event NewSceneCreatedCallback newSceneCreated;
        public static event SceneOpeningCallback sceneOpening;
        public static event SceneOpenedCallback sceneOpened;
        public static event SceneClosingCallback sceneClosing;
        public static event SceneClosedCallback sceneClosed;
        public static event SceneSavingCallback sceneSaving;
        public static event SceneSavedCallback sceneSaved;

        [RequiredByNativeCode]
        private static void Internal_NewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            if (newSceneCreated != null)
                newSceneCreated(scene, setup, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneOpening(string path, OpenSceneMode mode)
        {
            if (sceneOpening != null)
                sceneOpening(path, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (sceneOpened != null)
                sceneOpened(scene, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneClosing(Scene scene, bool removingScene)
        {
            if (sceneClosing != null)
                sceneClosing(scene, removingScene);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneClosed(Scene scene)
        {
            if (sceneClosed != null)
                sceneClosed(scene);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneSaving(Scene scene, string path)
        {
            if (sceneSaving != null)
                sceneSaving(scene, path);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneSaved(Scene scene)
        {
            if (sceneSaved != null)
                sceneSaved(scene);
        }
    }
}
