// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Events;
using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive };

    public partial class SceneManager
    {
        public static event UnityAction<Scene, LoadSceneMode> sceneLoaded;

        public static event UnityAction<Scene> sceneUnloaded;

        public static event UnityAction<Scene, Scene> activeSceneChanged;

        [Obsolete("Use SceneManager.sceneCount and SceneManager.GetSceneAt(int index) to loop the all scenes instead.")]
        static public Scene[] GetAllScenes()
        {
            var scenes = new Scene[sceneCount];
            for (int index = 0; index < sceneCount; ++index)
            {
                scenes[index] = GetSceneAt(index);
            }
            return scenes;
        }

        public static void LoadScene(string sceneName, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            LoadSceneAsyncNameIndexInternal(sceneName, -1, mode == LoadSceneMode.Additive ? true : false, true);
        }

        [uei.ExcludeFromDocs]
        public static void LoadScene(string sceneName)
        {
            LoadSceneMode mode = LoadSceneMode.Single;
            LoadScene(sceneName, mode);
        }

        public static void LoadScene(int sceneBuildIndex, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            LoadSceneAsyncNameIndexInternal(null, sceneBuildIndex, mode == LoadSceneMode.Additive ? true : false, true);
        }

        [uei.ExcludeFromDocs]
        public static void LoadScene(int sceneBuildIndex)
        {
            LoadSceneMode mode = LoadSceneMode.Single;
            LoadScene(sceneBuildIndex, mode);
        }

        public static AsyncOperation LoadSceneAsync(int sceneBuildIndex, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            return LoadSceneAsyncNameIndexInternal(null, sceneBuildIndex, mode == LoadSceneMode.Additive ? true : false, false);
        }

        [uei.ExcludeFromDocs]
        public static AsyncOperation LoadSceneAsync(int sceneBuildIndex)
        {
            LoadSceneMode mode = LoadSceneMode.Single;
            return LoadSceneAsync(sceneBuildIndex, mode);
        }

        public static AsyncOperation LoadSceneAsync(string sceneName, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            return LoadSceneAsyncNameIndexInternal(sceneName, -1, mode == LoadSceneMode.Additive ? true : false, false);
        }

        [uei.ExcludeFromDocs]
        public static AsyncOperation LoadSceneAsync(string sceneName)
        {
            LoadSceneMode mode = LoadSceneMode.Single;
            return LoadSceneAsync(sceneName, mode);
        }

        [Obsolete("Use SceneManager.UnloadSceneAsync. This function is not safe to use during triggers and under other circumstances. See Scripting reference for more details.")]
        public static bool UnloadScene(Scene scene)
        {
            return UnloadSceneInternal(scene);
        }

        [Obsolete("Use SceneManager.UnloadSceneAsync. This function is not safe to use during triggers and under other circumstances. See Scripting reference for more details.")]
        static public bool UnloadScene(int sceneBuildIndex)
        {
            bool success;
            UnloadSceneNameIndexInternal("", sceneBuildIndex, true, out success);
            return success;
        }

        [Obsolete("Use SceneManager.UnloadSceneAsync. This function is not safe to use during triggers and under other circumstances. See Scripting reference for more details.")]
        static public bool UnloadScene(string sceneName)
        {
            bool success;
            UnloadSceneNameIndexInternal(sceneName, -1, true, out success);
            return success;
        }

        static public AsyncOperation UnloadSceneAsync(int sceneBuildIndex)
        {
            bool success;
            return UnloadSceneNameIndexInternal("", sceneBuildIndex, false, out success);
        }

        static public AsyncOperation UnloadSceneAsync(string sceneName)
        {
            bool success;
            return UnloadSceneNameIndexInternal(sceneName, -1, false, out success);
        }

        static public AsyncOperation UnloadSceneAsync(Scene scene)
        {
            return UnloadSceneAsyncInternal(scene);
        }

        [RequiredByNativeCode]
        private static void Internal_SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (sceneLoaded != null)
            {
                sceneLoaded(scene, mode);
            }
        }

        [RequiredByNativeCode]
        private static void Internal_SceneUnloaded(Scene scene)
        {
            if (sceneUnloaded != null)
            {
                sceneUnloaded(scene);
            }
        }

        [RequiredByNativeCode]
        private static void Internal_ActiveSceneChanged(Scene previousActiveScene, Scene newActiveScene)
        {
            if (activeSceneChanged != null)
            {
                activeSceneChanged(previousActiveScene, newActiveScene);
            }
        }
    }
}
