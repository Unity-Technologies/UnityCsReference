// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.SceneManagement;

namespace UnityEditor.SceneManagement
{
    public sealed partial class EditorSceneManager
    {
        public delegate void SceneManagerSetupRestoredCallback(Scene[] scenes);
        public delegate void NewSceneCreatedCallback(Scene scene, NewSceneSetup setup, NewSceneMode mode);
        public delegate void SceneOpeningCallback(string path, OpenSceneMode mode);
        public delegate void SceneOpenedCallback(Scene scene, OpenSceneMode mode);
        public delegate void SceneClosingCallback(Scene scene, bool removingScene);
        public delegate void SceneClosedCallback(Scene scene);
        public delegate void SceneSavingCallback(Scene scene, string path);
        public delegate void SceneSavedCallback(Scene scene);
        public delegate void SceneDirtiedCallback(Scene scene);

        public static event SceneManagerSetupRestoredCallback sceneManagerSetupRestored
        {
            add => m_SceneManagerSetupRestoredEvent.Add(value);
            remove => m_SceneManagerSetupRestoredEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneManagerSetupRestoredCallback> m_SceneManagerSetupRestoredEvent = new EventWithPerformanceTracker<SceneManagerSetupRestoredCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneManagerSetupRestored)}");

        public static event NewSceneCreatedCallback newSceneCreated
        {
            add => m_NewSceneCreatedEvent.Add(value);
            remove => m_NewSceneCreatedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<NewSceneCreatedCallback> m_NewSceneCreatedEvent = new EventWithPerformanceTracker<NewSceneCreatedCallback>($"{nameof(EditorSceneManager)}.{nameof(newSceneCreated)}");

        public static event SceneOpeningCallback sceneOpening
        {
            add => m_SceneOpeningEvent.Add(value);
            remove => m_SceneOpeningEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneOpeningCallback> m_SceneOpeningEvent = new EventWithPerformanceTracker<SceneOpeningCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneOpening)}");

        public static event SceneOpenedCallback sceneOpened
        {
            add => m_SceneOpenedEvent.Add(value);
            remove => m_SceneOpenedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneOpenedCallback> m_SceneOpenedEvent = new EventWithPerformanceTracker<SceneOpenedCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneOpened)}");

        public static event SceneClosingCallback sceneClosing
        {
            add => m_SceneClosingEvent.Add(value);
            remove => m_SceneClosingEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneClosingCallback> m_SceneClosingEvent = new EventWithPerformanceTracker<SceneClosingCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneClosing)}");

        public static event SceneClosedCallback sceneClosed
        {
            add => m_SceneClosedEvent.Add(value);
            remove => m_SceneClosedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneClosedCallback> m_SceneClosedEvent = new EventWithPerformanceTracker<SceneClosedCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneClosed)}");
        public static event SceneSavingCallback sceneSaving
        {
            add => m_SceneSavingEvent.Add(value);
            remove => m_SceneSavingEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneSavingCallback> m_SceneSavingEvent = new EventWithPerformanceTracker<SceneSavingCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneSaving)}");
        public static event SceneSavedCallback sceneSaved
        {
            add => m_SceneSavedEvent.Add(value);
            remove => m_SceneSavedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneSavedCallback> m_SceneSavedEvent = new EventWithPerformanceTracker<SceneSavedCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneSaved)}");
        public static event SceneDirtiedCallback sceneDirtied
        {
            add => m_SceneDirtiedEvent.Add(value);
            remove => m_SceneDirtiedEvent.Remove(value);
        }
        private static EventWithPerformanceTracker<SceneDirtiedCallback> m_SceneDirtiedEvent = new EventWithPerformanceTracker<SceneDirtiedCallback>($"{nameof(EditorSceneManager)}.{nameof(sceneDirtied)}");

        [RequiredByNativeCode]
        private static void Internal_SceneManagerSetupRestored(Scene[] scenes)
        {
            foreach (var evt in m_SceneManagerSetupRestoredEvent)
                evt(scenes);
        }

        [RequiredByNativeCode]
        private static void Internal_NewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            foreach (var evt in m_NewSceneCreatedEvent)
                evt(scene, setup, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneOpening(string path, OpenSceneMode mode)
        {
            foreach (var evt in m_SceneOpeningEvent)
                evt(path, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneOpened(Scene scene, OpenSceneMode mode)
        {
            foreach (var evt in m_SceneOpenedEvent)
                evt(scene, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneClosing(Scene scene, bool removingScene)
        {
            foreach (var evt in m_SceneClosingEvent)
                evt(scene, removingScene);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneClosed(Scene scene)
        {
            foreach (var evt in m_SceneClosedEvent)
                evt(scene);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneSaving(Scene scene, string path)
        {
            foreach (var evt in m_SceneSavingEvent)
                evt(scene, path);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneSaved(Scene scene)
        {
            foreach (var evt in m_SceneSavedEvent)
                evt(scene);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneDirtied(Scene scene)
        {
            foreach (var evt in m_SceneDirtiedEvent)
                evt(scene);
        }

        [RequiredByNativeCode]
        private static Transform Internal_GetParentTransformForNewGameObjects()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
                return prefabStage.prefabContentsRoot.transform;

            return null;
        }
    }
}
