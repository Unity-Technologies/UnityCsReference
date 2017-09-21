// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public enum StackTraceLogType
    {
        None,
        ScriptOnly,
        Full
    }

    partial class Application
    {
        public static event UnityAction onBeforeRender
        {
            add
            {
                BeforeRenderHelper.RegisterCallback(value);
            }
            remove
            {
                BeforeRenderHelper.UnregisterCallback(value);
            }
        }

        [RequiredByNativeCode]
        internal static void InvokeOnBeforeRender()
        {
            BeforeRenderHelper.Invoke();
        }

        [System.Obsolete("Application.RegisterLogCallback is deprecated. Use Application.logMessageReceived instead.")]
        public static void RegisterLogCallback(LogCallback handler)
        {
            RegisterLogCallback(handler, false);
        }

        [System.Obsolete("Application.RegisterLogCallbackThreaded is deprecated. Use Application.logMessageReceivedThreaded instead.")]
        public static void RegisterLogCallbackThreaded(Application.LogCallback handler)
        {
            RegisterLogCallback(handler, true);
        }

        private static void RegisterLogCallback(LogCallback handler, bool threaded)
        {
            if (s_RegisterLogCallbackDeprecated != null)
            {
                logMessageReceived -= s_RegisterLogCallbackDeprecated;
                logMessageReceivedThreaded -= s_RegisterLogCallbackDeprecated;
            }

            s_RegisterLogCallbackDeprecated = handler;

            if (handler != null)
            {
                if (threaded)
                    logMessageReceivedThreaded += handler;
                else
                    logMessageReceived += handler;
            }
        }

        private static volatile LogCallback s_RegisterLogCallbackDeprecated;

        [System.Obsolete("Use SceneManager.sceneCountInBuildSettings")]
        public static int levelCount { get { return SceneManager.sceneCountInBuildSettings; } }

        [System.Obsolete("Use SceneManager to determine what scenes have been loaded")]
        public static int loadedLevel { get { return SceneManager.GetActiveScene().buildIndex; } }

        [System.Obsolete("Use SceneManager to determine what scenes have been loaded")]
        public static string loadedLevelName { get { return SceneManager.GetActiveScene().name; } }

        [System.Obsolete("Use SceneManager.LoadScene")]
        static public void LoadLevel(int index) { SceneManager.LoadScene(index, LoadSceneMode.Single); }

        [System.Obsolete("Use SceneManager.LoadScene")]
        static public void LoadLevel(string name) { SceneManager.LoadScene(name, LoadSceneMode.Single); }

        [System.Obsolete("Use SceneManager.LoadScene")]
        static public void LoadLevelAdditive(int index) { SceneManager.LoadScene(index, LoadSceneMode.Additive); }

        [System.Obsolete("Use SceneManager.LoadScene")]
        static public void LoadLevelAdditive(string name) { SceneManager.LoadScene(name, LoadSceneMode.Additive); }

        [System.Obsolete("Use SceneManager.LoadSceneAsync")]
        static public AsyncOperation LoadLevelAsync(int index) { return SceneManager.LoadSceneAsync(index, LoadSceneMode.Single); }

        [System.Obsolete("Use SceneManager.LoadSceneAsync")]
        static public AsyncOperation LoadLevelAsync(string levelName) { return SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single); }

        [System.Obsolete("Use SceneManager.LoadSceneAsync")]
        static public AsyncOperation LoadLevelAdditiveAsync(int index) { return SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive); }

        [System.Obsolete("Use SceneManager.LoadSceneAsync")]
        static public AsyncOperation LoadLevelAdditiveAsync(string levelName) { return SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive); }

        [System.Obsolete("Use SceneManager.UnloadScene")]
        static public bool UnloadLevel(int index) { return SceneManager.UnloadScene(index); }

        [System.Obsolete("Use SceneManager.UnloadScene")]
        static public bool UnloadLevel(string scenePath) { return SceneManager.UnloadScene(scenePath); }
    }
}
