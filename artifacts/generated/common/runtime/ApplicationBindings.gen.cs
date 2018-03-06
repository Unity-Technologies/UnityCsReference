// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngineInternal;

namespace UnityEngine
{
public enum NetworkReachability
{
    
    NotReachable = 0,
    
    ReachableViaCarrierDataNetwork = 1,
    
    ReachableViaLocalAreaNetwork = 2
}

public sealed partial class Application
{
    
            public delegate void AdvertisingIdentifierCallback(string advertisingId, bool trackingEnabled, string errorMsg);
    
            public delegate void LowMemoryCallback();
            public static event LowMemoryCallback lowMemory;
    [RequiredByNativeCode]
    private static void CallLowMemory()
        {
            var handler = lowMemory;
            if (handler != null)
                handler();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Quit () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CancelQuit () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Unload () ;

    [System.Obsolete ("This property is deprecated, please use LoadLevelAsync to detect if a specific scene is currently loading.")]
    public extern static bool isLoadingLevel
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  float GetStreamProgressForLevelByName (string levelName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetStreamProgressForLevel (int levelIndex) ;

    public static float GetStreamProgressForLevel(string levelName) { return GetStreamProgressForLevelByName(levelName); }
    
    
    public extern static int streamedBytes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool CanStreamedLevelBeLoadedByName (string levelName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool CanStreamedLevelBeLoaded (int levelIndex) ;

    public static bool CanStreamedLevelBeLoaded(string levelName) { return CanStreamedLevelBeLoadedByName(levelName); }
    
    
    public extern static bool isPlaying
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isFocused
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool isEditor
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("This property is deprecated and will be removed in a future version of Unity, Webplayer support has been removed since Unity 5.4", true)]
    public extern static bool isWebPlayer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [ThreadAndSerializationSafe ()]
    public extern static RuntimePlatform platform
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string[] GetBuildTags () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetBuildTags (string[] buildTags) ;

    public extern static string buildGUID
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public static bool isMobilePlatform
    {
        get
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.Android:
                case RuntimePlatform.TizenPlayer:
                    return true;
                case RuntimePlatform.WSAPlayerX86:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerARM:
                    return SystemInfo.deviceType == DeviceType.Handheld;
                default:
                    return false;
            }
        }
    }
    
    
    public static bool isConsolePlatform
    {
        get
        {
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.PS4
                || platform == RuntimePlatform.XboxOne;
        }
    }
    
    
    
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
    
    
    public extern static bool runInBackground
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [System.Obsolete ("use Application.isEditor instead")]
    public static bool isPlayer
    {
        get
        {
            return !isEditor;
        }
    }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasProLicense () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasAdvancedLicense () ;

    internal extern static bool isBatchmode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern static bool isTestRun
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern static bool isHumanControllingUs
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasARGV (string name) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetValueForARGV (string name) ;

    [System.Obsolete ("Use Object.DontDestroyOnLoad instead")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DontDestroyOnLoad (Object mono) ;

    public extern static string dataPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string streamingAssetsPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Security.SecurityCritical]
    public extern static string persistentDataPath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string temporaryCachePath
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("Application.srcValue is obsolete and has no effect. It will be removed in a subsequent Unity release.", true)]
    public extern static string srcValue
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string absoluteURL
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    private static string ObjectToJSString(object o)
    {
        if (o == null)
        {
            return "null";
        }
        else if (o is string)
        {
            string s = o.ToString().Replace("\\", "\\\\");   
            s = s.Replace("\"", "\\\"");
            s = s.Replace("\n", "\\n");
            s = s.Replace("\r", "\\r");
            s = s.Replace("\u0000", "");  
            s = s.Replace("\u2028", "");  
            s = s.Replace("\u2029", "");  
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
                return "\"\\\"\""; 
            else
                return '"' + o.ToString() + '"';
        }
        else if (o is System.Collections.IList)
        {
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
            return ObjectToJSString(o.ToString());
        }
    }
    
    
    [System.Obsolete ("Application.ExternalCall is deprecated. See https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html for alternatives.")]
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
    
    
    [System.Obsolete ("Application.ExternalEval is deprecated. See https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html for alternatives.")]
public static void ExternalEval(string script)
    {
        if (script.Length > 0 && script[script.Length - 1] != ';')
            script += ';';
        Internal_ExternalCall(script);
    }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_ExternalCall (string script) ;

    public extern static string unityVersion
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string version
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string installerName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string identifier
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static ApplicationInstallMode installMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static ApplicationSandboxType sandboxType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string productName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string companyName
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static string cloudProjectId
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal static AdvertisingIdentifierCallback OnAdvertisingIdentifierCallback;
    
    
    internal static void InvokeOnAdvertisingIdentifierCallback(string advertisingId, bool trackingEnabled)
    {
        if (OnAdvertisingIdentifierCallback != null)
            OnAdvertisingIdentifierCallback(advertisingId, trackingEnabled, string.Empty);
    }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool RequestAdvertisingIdentifierAsync (AdvertisingIdentifierCallback delegateMethod) ;

    [System.Obsolete ("Application.webSecurityEnabled is no longer supported, since the Unity Web Player is no longer supported by Unity.", true)]
    [ThreadAndSerializationSafe ()]
    public extern static bool webSecurityEnabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("Application.webSecurityHostUrl is no longer supported, since the Unity Web Player is no longer supported by Unity.", true)]
    [ThreadAndSerializationSafe ()]
    public extern static string webSecurityHostUrl
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void OpenURL (string url) ;

    [System.Obsolete ("For internal use only")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ForceCrash (int mode) ;

    public extern static int targetFrameRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static SystemLanguage systemLanguage
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

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
        if (invokedOnMainThread)
        {
            var handler = s_LogCallbackHandler;
            if (handler != null)
                handler(logString, stackTrace, type);
        }

        var threadedHandler = s_LogCallbackHandlerThreaded;
        if (threadedHandler != null)
            threadedHandler(logString, stackTrace, type);
    }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetLogCallbackDefined (bool defined) ;

    [System.Obsolete ("Use SetStackTraceLogType/GetStackTraceLogType instead")]
    public extern static StackTraceLogType stackTraceLogType
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  StackTraceLogType GetStackTraceLogType (LogType logType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetStackTraceLogType (LogType logType, StackTraceLogType stackTraceType) ;

    public extern static ThreadPriority backgroundLoadingPriority
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static NetworkReachability internetReachability
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool genuine
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool genuineCheckAvailable
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AsyncOperation RequestUserAuthorization (UserAuthorization mode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool HasUserAuthorization (UserAuthorization mode) ;

    internal extern static bool submitAnalytics
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [System.Obsolete ("This property is deprecated, please use SplashScreen.isFinished instead")]
    public static bool isShowingSplashScreen
    {
        get
        {
            return !UnityEngine.Rendering.SplashScreen.isFinished;
        }
    }
}

public enum UserAuthorization
{
    
    WebCam = 1,
    
    Microphone = 2
}

public enum ApplicationInstallMode
{
    Unknown = 0,
    
    Store = 1,
    
    DeveloperBuild = 2,
    
    Adhoc = 3,
    
    Enterprise = 4,
    
    Editor = 5
}

public enum ApplicationSandboxType
{
    Unknown = 0,
    
    NotSandboxed = 1,
    
    Sandboxed = 2,
    
    SandboxBroken = 3
}

}
