// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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

    // Describes network reachability options.
    public enum NetworkReachability
    {
        // Network is not reachable
        NotReachable = 0,
        // Network is reachable via carrier data network
        ReachableViaCarrierDataNetwork = 1,
        // Network is reachable via WiFi or cable
        ReachableViaLocalAreaNetwork = 2
    }

    // Constants to pass to [[Application.RequestUserAuthorization]].
    public enum UserAuthorization
    {
        // Request permission to use any video input sources attached to the computer.
        WebCam = 1,
        // Request permission to use any audio input sources attached to the computer.
        Microphone = 2
    }

    // Application install mode. Returned by Application.installMode.
    public enum ApplicationInstallMode
    {
        //
        Unknown = 0,
        // Application was installed via store.
        Store = 1,
        // Application was installed via developer env.
        DeveloperBuild = 2,
        // Application was installed via adhoc mode.
        Adhoc = 3,
        // Application was installed in enterprise mode.
        Enterprise = 4,
        // Application running in editor.
        Editor = 5
    }

    // Application sandbox type. Returned by Application.sandboxType.
    public enum ApplicationSandboxType
    {
        //
        Unknown = 0,
        // Application is not sandboxed
        NotSandboxed = 1,
        // Application is sandboxed.
        Sandboxed = 2,
        // Application sandbox is broken.
        SandboxBroken = 3
    }

    partial class Application
    {
        public delegate void AdvertisingIdentifierCallback(string advertisingId, bool trackingEnabled, string errorMsg);

        public delegate void LowMemoryCallback();
        public static event LowMemoryCallback lowMemory;

        [RequiredByNativeCode]
        internal static void CallLowMemory()
        {
            var handler = lowMemory;
            if (handler != null)
                handler();
        }

        // Use this delegate type with RegisterLogCallback to monitor what gets logged.
        public delegate void LogCallback(string condition, string stackTrace, LogType type);

        private static LogCallback s_LogCallbackHandler;
        private static LogCallback s_LogCallbackHandlerThreaded;

        public static event LogCallback logMessageReceived
        {
            add
            {
                s_LogCallbackHandler += value;
                SetLogCallbackDefined(true);
            }

            remove
            {
                s_LogCallbackHandler -= value;
            }
        }

        public static event LogCallback logMessageReceivedThreaded
        {
            add
            {
                s_LogCallbackHandlerThreaded += value;
                SetLogCallbackDefined(true);
            }
            remove
            {
                s_LogCallbackHandlerThreaded -= value;
            }
        }

        [RequiredByNativeCode]
        private static void CallLogCallback(string logString, string stackTrace, LogType type, bool invokedOnMainThread)
        {
            // Run non-thread-safe handler only on main thread.
            if (invokedOnMainThread)
            {
                var handler = s_LogCallbackHandler;
                if (handler != null)
                    handler(logString, stackTrace, type);
            }

            // Run thread-safe handlers always.
            var threadedHandler = s_LogCallbackHandlerThreaded;
            if (threadedHandler != null)
                threadedHandler(logString, stackTrace, type);
        }

        internal static AdvertisingIdentifierCallback OnAdvertisingIdentifierCallback;

        internal static void InvokeOnAdvertisingIdentifierCallback(string advertisingId, bool trackingEnabled)
        {
            if (OnAdvertisingIdentifierCallback != null)
                OnAdvertisingIdentifierCallback(advertisingId, trackingEnabled, string.Empty);
        }

        // Converts an object to a JavaScript text representation.
        private static string ObjectToJSString(object o)
        {
            if (o == null)
            {
                return "null";
            }
            else if (o is string)
            {
                string s = o.ToString().Replace("\\", "\\\\");   // escape \ into \\, JS vulnerability.
                s = s.Replace("\"", "\\\"");
                s = s.Replace("\n", "\\n");
                s = s.Replace("\r", "\\r");
                s = s.Replace("\u0000", "");  // String-terminator. JS vulnerability.
                s = s.Replace("\u2028", "");  // Line-terminator via ecma-262-7.3 JS vulnerability.
                s = s.Replace("\u2029", "");  // Same as above
                return '"' + s + '"';
            }
            else if (o is Int32 || o is Int16 || o is UInt32 || o is UInt16 || o is Byte)
            {
                return o.ToString();
            }
            else if (o is Single)
            {
                System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
                return ((Single)o).ToString(nf);
            }
            else if (o is Double)
            {
                System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
                return ((Double)o).ToString(nf);
            }
            else if (o is Char)
            {
                if ((Char)o == '"')
                    return "\"\\\"\""; // escape the '"' character
                else
                    return '"' + o.ToString() + '"';
            }
            else if (o is System.Collections.IList)
            {
                // Any IList object is dumped as JS Array
                System.Collections.IList list = (System.Collections.IList)o;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("new Array(");
                int size = list.Count;
                for (int i = 0; i < size; ++i)
                {
                    if (i != 0)
                        sb.Append(", ");
                    sb.Append(ObjectToJSString(list[i]));
                }
                sb.Append(")");
                return sb.ToString();
            }
            else
            {
                // Unrecognized objects are dumped as strings
                return ObjectToJSString(o.ToString());
            }
        }

        // Calls a function in the containing web page __(Web Player only)__.
        [Obsolete("Application.ExternalCall is deprecated. See https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html for alternatives.")]
        public static void ExternalCall(string functionName, params object[] args)
        {
            Internal_ExternalCall(BuildInvocationForArguments(functionName, args));
        }

        private static string BuildInvocationForArguments(string functionName, params object[] args)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(functionName);
            sb.Append('(');
            int size = args.Length;
            for (int i = 0; i < size; ++i)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(ObjectToJSString(args[i]));
            }
            sb.Append(')');
            sb.Append(';');
            return sb.ToString();
        }

        [Obsolete("use Application.isEditor instead")]
        public static bool isPlayer
        {
            get
            {
                return !isEditor;
            }
        }

        [Obsolete("Use Object.DontDestroyOnLoad instead")]
        public static void DontDestroyOnLoad(Object o)
        {
            if (o != null)
                Object.DontDestroyOnLoad(o);
        }

        // Captures a screenshot at path /filename/ as a PNG file.
        [System.Obsolete("Application.CaptureScreenshot is obsolete. Use ScreenCapture.CaptureScreenshot instead (UnityUpgradable) -> [UnityEngine] UnityEngine.ScreenCapture.CaptureScreenshot(*)", true)]
        static public void CaptureScreenshot(string filename, int superSize)
        {
            throw new NotSupportedException("Application.CaptureScreenshot is obsolete. Use ScreenCapture.CaptureScreenshot instead.");
        }

        [System.Obsolete("Application.CaptureScreenshot is obsolete. Use ScreenCapture.CaptureScreenshot instead (UnityUpgradable) -> [UnityEngine] UnityEngine.ScreenCapture.CaptureScreenshot(*)", true)]
        static public void CaptureScreenshot(string filename)
        {
            throw new NotSupportedException("Application.CaptureScreenshot is obsolete. Use ScreenCapture.CaptureScreenshot instead.");
        }

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

        public static event Action<bool> focusChanged;

        public static event Action<string> deepLinkActivated;

        public static event Func<bool> wantsToQuit;

        public static event Action quitting;

        public static event Action unloading;

        [RequiredByNativeCode]
        static bool Internal_ApplicationWantsToQuit()
        {
            if (wantsToQuit != null)
            {
                foreach (Func<bool> continueQuit in wantsToQuit.GetInvocationList())
                {
                    try
                    {
                        if (!continueQuit())
                            return false;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }
            return true;
        }

        [RequiredByNativeCode]
        static void Internal_ApplicationQuit()
        {
            if (quitting != null)
                quitting();
        }

        [RequiredByNativeCode]
        static void Internal_ApplicationUnload()
        {
            if (unloading != null)
                unloading();
        }

        [RequiredByNativeCode]
        internal static void InvokeOnBeforeRender()
        {
            BeforeRenderHelper.Invoke();
        }

        [RequiredByNativeCode]
        internal static void InvokeFocusChanged(bool focus)
        {
            if (focusChanged != null)
                focusChanged.Invoke(focus);
        }

        [RequiredByNativeCode]
        internal static void InvokeDeepLinkActivated(string url)
        {
            if (deepLinkActivated != null)
                deepLinkActivated.Invoke(url);
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

    internal partial class ApplicationEditor
    {
        // Are we running inside the Unity editor? (RO)
        public static bool isEditor
        {
            get
            {
                return true;
            }
        }
    }

    public partial class Application
    {
        public static bool isEditor => ShimManager.applicationShim.isEditor;
    }
}
