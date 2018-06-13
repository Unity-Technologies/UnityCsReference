// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Bindings;
using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode
    {
        Single = 0,
        Additive
    };

    [Flags]
    public enum LocalPhysicsMode
    {
        None = 0,
        Physics2D = 1,
        Physics3D = 2
    }

    [Serializable]
    public struct LoadSceneParameters
    {
        [SerializeField]
        private LoadSceneMode m_LoadSceneMode;

        [SerializeField]
        private LocalPhysicsMode m_LocalPhysicsMode;

        public LoadSceneMode loadSceneMode
        {
            get {return m_LoadSceneMode; }
            set {m_LoadSceneMode = value; }
        }

        public LocalPhysicsMode localPhysicsMode
        {
            get { return m_LocalPhysicsMode; }
            set { m_LocalPhysicsMode = value; }
        }

        public LoadSceneParameters(LoadSceneMode mode)
        {
            m_LoadSceneMode = mode;
            m_LocalPhysicsMode = LocalPhysicsMode.None;
        }

        public LoadSceneParameters(LoadSceneMode mode, LocalPhysicsMode physicsMode)
        {
            m_LoadSceneMode = mode;
            m_LocalPhysicsMode = physicsMode;
        }
    };

    [Serializable]
    public struct CreateSceneParameters
    {
        [SerializeField]
        private LocalPhysicsMode m_LocalPhysicsMode;

        public LocalPhysicsMode localPhysicsMode
        {
            get { return m_LocalPhysicsMode; }
            set { m_LocalPhysicsMode = value; }
        }

        public CreateSceneParameters(LocalPhysicsMode physicsMode)
        {
            m_LocalPhysicsMode = physicsMode;
        }
    }

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

        public static Scene CreateScene(string sceneName)
        {
            CreateSceneParameters parameters = new CreateSceneParameters(LocalPhysicsMode.None);
            return CreateScene(sceneName, parameters);
        }

        public static void LoadScene(string sceneName, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            var parameters = new LoadSceneParameters(mode);
            LoadScene(sceneName, parameters);
        }

        [uei.ExcludeFromDocs]
        public static void LoadScene(string sceneName)
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Single);
            LoadScene(sceneName, parameters);
        }

        public static Scene LoadScene(string sceneName, LoadSceneParameters parameters)
        {
            LoadSceneAsyncNameIndexInternal(sceneName, -1, parameters, true);
            return GetSceneAt(sceneCount - 1);
        }

        public static void LoadScene(int sceneBuildIndex, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            var parameters = new LoadSceneParameters(mode);
            LoadScene(sceneBuildIndex, parameters);
        }

        [uei.ExcludeFromDocs]
        public static void LoadScene(int sceneBuildIndex)
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Single);
            LoadScene(sceneBuildIndex, parameters);
        }

        public static Scene LoadScene(int sceneBuildIndex, LoadSceneParameters parameters)
        {
            LoadSceneAsyncNameIndexInternal(null, sceneBuildIndex, parameters, true);
            return GetSceneAt(sceneCount - 1);
        }

        public static AsyncOperation LoadSceneAsync(int sceneBuildIndex, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            var parameters = new LoadSceneParameters(mode);
            return LoadSceneAsync(sceneBuildIndex, parameters);
        }

        [uei.ExcludeFromDocs]
        public static AsyncOperation LoadSceneAsync(int sceneBuildIndex)
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Single);
            return LoadSceneAsync(sceneBuildIndex, parameters);
        }

        public static AsyncOperation LoadSceneAsync(int sceneBuildIndex, LoadSceneParameters parameters)
        {
            return LoadSceneAsyncNameIndexInternal(null, sceneBuildIndex, parameters, false);
        }

        public static AsyncOperation LoadSceneAsync(string sceneName, [uei.DefaultValue("LoadSceneMode.Single")] LoadSceneMode mode)
        {
            var parameters = new LoadSceneParameters(mode);
            return LoadSceneAsync(sceneName, parameters);
        }

        [uei.ExcludeFromDocs]
        public static AsyncOperation LoadSceneAsync(string sceneName)
        {
            var parameters = new LoadSceneParameters(LoadSceneMode.Single);
            return LoadSceneAsync(sceneName, parameters);
        }

        public static AsyncOperation LoadSceneAsync(string sceneName, LoadSceneParameters parameters)
        {
            return LoadSceneAsyncNameIndexInternal(sceneName, -1, parameters, false);
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
