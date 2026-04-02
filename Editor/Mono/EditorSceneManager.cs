// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using uei = UnityEngine.Internal;

namespace UnityEditor.SceneManagement
{
    // Must match same enums in C++
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }
    public enum NewSceneMode { Single, Additive }
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }

    public sealed partial class EditorSceneManager : SceneManager
    {
        internal static UnityAction<Scene, NewSceneMode> sceneWasCreated;
        internal static UnityAction<Scene, OpenSceneMode> sceneWasOpened;
        public static event UnityAction<Scene, Scene> activeSceneChangedInEditMode
        {
            add => m_ActiveSceneChangedInEditModeEvent.Add(value);
            remove => m_ActiveSceneChangedInEditModeEvent.Remove(value);
        }

        private static EventWithPerformanceTracker<UnityAction<Scene, Scene>> m_ActiveSceneChangedInEditModeEvent = new EventWithPerformanceTracker<UnityAction<Scene, Scene>>($"{nameof(EditorSceneManager)}.{nameof(activeSceneChangedInEditMode)}");

        private static void PlayModeStateChangedCallback(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingPlayMode:
                    SceneManager.s_AllowLoadScene = false;
                    break;
                case PlayModeStateChange.EnteredEditMode:   // When in edit mode calling LoadScene will fail with an exception so it is "ok" to call from edit mode
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                default:
                    SceneManager.s_AllowLoadScene = true;
                    break;
            }
        }

        static EditorSceneManager()
        {
            SceneManager.s_AllowLoadScene = true;
            EditorApplication.playModeStateChanged += PlayModeStateChangedCallback;
        }

        internal static bool CreateSceneAsset(string scenePath, bool createDefaultGameObjects)
        {
            if (!Utils.Paths.CheckValidAssetPathAndThatDirectoryExists(scenePath, ".unity"))
                return false;

            return CreateSceneAssetInternal(scenePath, createDefaultGameObjects);
        }

        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => SaveCurrentModifiedScenesIfUserWantsTo(null);

        [uei.ExcludeFromDocs]
        public static Scene OpenScene(string scenePath)
        {
            Scene result;
            result = OpenScene(scenePath, OpenSceneMode.Single);
            return result;
        }

        [uei.ExcludeFromDocs]
        public static Scene NewScene(NewSceneSetup setup)
        {
            Scene result;
            result = NewScene(setup, NewSceneMode.Single);
            return result;
        }

        [uei.ExcludeFromDocs]
        public static bool SaveScene(Scene scene, string dstScenePath)
        {
            bool saveAsCopy = false;
            return SaveScene(scene, dstScenePath, saveAsCopy);
        }

        [uei.ExcludeFromDocs]
        public static bool SaveScene(Scene scene)
        {
            bool saveAsCopy = false;
            string dstScenePath = "";
            return SaveScene(scene, dstScenePath, saveAsCopy);
        }

        public static bool SaveScene(Scene scene, [uei.DefaultValue("\"\"")] string dstScenePath, [uei.DefaultValue("false")] bool saveAsCopy)
        {
            if (!string.IsNullOrEmpty(dstScenePath))
                if (!Utils.Paths.CheckValidAssetPathAndThatDirectoryExists(dstScenePath, ".unity"))
                    return false;

            return SaveSceneInternal(scene, dstScenePath, saveAsCopy);
        }

        private static void Internal_NewSceneWasCreated(Scene scene, NewSceneMode mode)
        {
            if (sceneWasCreated != null)
                sceneWasCreated(scene, mode);
        }

        private static void Internal_SceneWasOpened(Scene scene, OpenSceneMode mode)
        {
            if (sceneWasOpened != null)
                sceneWasOpened(scene, mode);
        }

        [RequiredByNativeCode]
        private static void Internal_ActiveSceneChangedInEditor(Scene previousActiveScene, Scene newActiveScene)
        {
            foreach (var evt in m_ActiveSceneChangedInEditModeEvent)
            {
                evt(previousActiveScene, newActiveScene);
            }
        }

        public static Scene LoadSceneInPlayMode(string path, LoadSceneParameters parameters)
        {
            AsyncOperation op = LoadSceneInPlayModeInternal(path, parameters, true);
            if (op != null)
                return GetSceneAt(sceneCount - 1);

            return new Scene();
        }

        public static AsyncOperation LoadSceneAsyncInPlayMode(string path, LoadSceneParameters parameters)
        {
            return LoadSceneInPlayModeInternal(path, parameters, false);
        }

        [RequiredByNativeCode]
        static void ProcessInitializeOnEnterPlayModeAttributes(EnterPlayModeOptions options)
        {
            var methods = TypeCache.GetMethodsWithAttribute<InitializeOnEnterPlayModeAttribute>();
            var parameters = new object[] { options };

            foreach (var method in methods)
            {
                if (!ValidateInitializeOnEnterPlayModeMethod(method, out var hasParameter))
                    continue;

                try
                {
                    // We currently accept no arguments for [InitializeOnEnterPlayMode] methods even though it is undocumented
                    method.Invoke(null, hasParameter ? parameters : null);
                }
                catch (TargetInvocationException e)
                {
                    Debug.LogException(e.InnerException ?? e);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        internal static bool ValidateInitializeOnEnterPlayModeMethod(MethodInfo methodInfo, out bool hasParameter)
        {
            hasParameter = false;
            if (!methodInfo.IsStatic)
            {
                return false;
            }

            if (methodInfo.IsSpecialName)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' is a property or event accessor method and cannot be marked with [InitializeOnEnterPlayMode]");
                return false;
            }

            var parameters = methodInfo.GetParameters();

            if (parameters.Length > 1)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' signature does not match expected method signature for [InitializeOnEnterPlayMode] wrong number of arguments");
                return false;
            }

            if (parameters.Length == 1)
            {
                if (parameters[0].ParameterType != typeof(EnterPlayModeOptions))
                {
                    Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' signature does not match method signature for [InitializeOnEnterPlayMode] methods, expected argument {typeof(EnterPlayModeOptions).FullName} actual argument {parameters[0].ParameterType}");
                    return false;
                }

                hasParameter = true;
            }

            if (methodInfo.DeclaringType != null && methodInfo.DeclaringType.IsGenericType)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' is in a generic type, but [InitializeOnEnterPlayMode] methods cannot be in generic types");
                return false;
            }

            if (methodInfo.IsGenericMethod)
            {
                Debug.LogError($"Method '{methodInfo.DeclaringType?.FullName ?? "Global"}.{methodInfo.Name}' is a generic method, but [InitializeOnEnterPlayMode] methods cannot be generic");
                return false;
            }

            return true;
        }
    }
}
