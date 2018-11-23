// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngineInternal;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    // Access to application run-time data.

    [NativeHeader("Runtime/Application/AdsIdHandler.h")]
    [NativeHeader("Runtime/Application/ApplicationInfo.h")]
    [NativeHeader("Runtime/BaseClasses/IsPlaying.h")]
    [NativeHeader("Runtime/Export/Application.bindings.h")]
    [NativeHeader("Runtime/File/ApplicationSpecificPersistentDataPath.h")]
    [NativeHeader("Runtime/Input/GetInput.h")]
    [NativeHeader("Runtime/Input/InputManager.h")]
    [NativeHeader("Runtime/Logging/LogSystem.h")]
    [NativeHeader("Runtime/Misc/BuildSettings.h")]
    [NativeHeader("Runtime/Misc/Player.h")]
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    [NativeHeader("Runtime/Misc/SystemInfo.h")]
    [NativeHeader("Runtime/Network/NetworkUtility.h")]
    [NativeHeader("Runtime/PreloadManager/LoadSceneOperation.h")]
    [NativeHeader("Runtime/PreloadManager/PreloadManager.h")]
    [NativeHeader("Runtime/Utilities/Argv.h")]
    [NativeHeader("Runtime/Utilities/URLUtility.h")]
    public partial class Application
    {
        // Quits the player application. Quit is ignored in the editor or in a WebGL player.
        [FreeFunction("GetInputManager().QuitApplication")]
        extern public static void Quit(int exitCode);

        // Quits the player application with the default exit code, 0
        public static void Quit()
        {
            Quit(0);
        }

        // Cancels quitting the application. This is useful for showing a splash screen at the end of a game.
        [Obsolete("CancelQuit is deprecated. Use the wantsToQuit event instead.")]
        [FreeFunction("GetInputManager().CancelQuitApplication")]
        extern public static void CancelQuit();

        // Unloads Unity from the application. Performs all the same actions as Application.Quit but the application does not exit.
        [FreeFunction("Application_Bindings::Unload")]
        extern public static void Unload();

        [Obsolete("This property is deprecated, please use LoadLevelAsync to detect if a specific scene is currently loading.")]
        extern public static bool isLoadingLevel
        {
            [FreeFunction("GetPreloadManager().IsLoadingOrQueued")]
            get;
        }

        [Obsolete("Streaming was a Unity Web Player feature, and is removed. This function is deprecated and always returns 1.0 for valid level indices.")]
        public static float GetStreamProgressForLevel(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < SceneManager.sceneCountInBuildSettings)
                return 1.0F;
            else
                return 0.0F;
        }

        // How far has the download progressed? [0...1]
        [Obsolete("Streaming was a Unity Web Player feature, and is removed. This function is deprecated and always returns 1.0.")]
        public static float GetStreamProgressForLevel(string levelName) { return 1.0f; }

        // How many bytes have we downloaded from the main unity web stream (RO).
        [Obsolete("Streaming was a Unity Web Player feature, and is removed. This property is deprecated and always returns 0.")]
        public static int streamedBytes
        {
            get
            {
                return 0;
            }
        }

        // We cannot currently remove this obsolete API, as it is referenced in SyntaxTree.VisualStudio.Unity.Bridge.dll, which is shipped by
        // Microsoft as part of Visual Studio, in a location we cannot API update.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Application.webSecurityEnabled is no longer supported, since the Unity Web Player is no longer supported by Unity", true)]
        static public bool webSecurityEnabled
        {
            get
            {
                return false;
            }
        }

        public static bool CanStreamedLevelBeLoaded(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < SceneManager.sceneCountInBuildSettings;
        }

        // Can the streamed level be loaded?
        [FreeFunction("Application_Bindings::CanStreamedLevelBeLoaded")]
        public extern static bool CanStreamedLevelBeLoaded(string levelName);

        // Returns true when in any kind of player (RO).
        public extern static bool isPlaying
        {
            [FreeFunction("IsWorldPlaying")]
            get;
        }

        [FreeFunction]
        public extern static bool IsPlaying(UnityEngine.Object obj);

        public extern static bool isFocused
        {
            [FreeFunction("IsPlayerFocused")]
            get;
        }

        // Returns the platform the game is running (RO).
        extern public static RuntimePlatform platform
        {
            [FreeFunction("systeminfo::GetRuntimePlatform", IsThreadSafe = true)]
            get;
        }

        [FreeFunction("GetBuildSettings().GetBuildTags")]
        extern public static string[] GetBuildTags();

        [FreeFunction("GetBuildSettings().SetBuildTags")]
        extern public static void SetBuildTags(string[] buildTags);

        extern public static string buildGUID
        {
            [FreeFunction("Application_Bindings::GetBuildGUID")]
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

        // Should the player be running when the application is in the background?
        extern public static bool runInBackground
        {
            [FreeFunction("GetPlayerSettingsRunInBackground")]
            get;
            [FreeFunction("SetPlayerSettingsRunInBackground")]
            set;
        }

        /// Is Unity activated with the Pro License?
        [FreeFunction("GetBuildSettings().GetHasPROVersion")]
        extern public static bool HasProLicense();

        extern public static bool isBatchMode
        {
            [FreeFunction("::IsBatchmode")]
            get;
        }

        extern static internal bool isTestRun
        {
            [FreeFunction("::IsTestRun")]
            get;
        }

        extern static internal bool isHumanControllingUs
        {
            [FreeFunction("::IsHumanControllingUs")]
            get;
        }

        [FreeFunction("HasARGV")]
        extern static internal bool HasARGV(string name);

        [FreeFunction("GetFirstValueForARGV")]
        extern static internal string GetValueForARGV(string name);

        // Contains the path to the game data folder (RO).
        extern public static string dataPath
        {
            [FreeFunction("GetAppDataPath")]
            get;
        }

        // Contains the path to the StreamingAssets folder (RO).
        extern public static string streamingAssetsPath
        {
            [FreeFunction("GetStreamingAssetsPath", IsThreadSafe = true)]
            get;
        }

        // Contains the path to a persistent data directory (RO).
        [System.Security.SecurityCritical]
        extern public static string persistentDataPath
        {
            [FreeFunction("GetPersistentDataPathApplicationSpecific")]
            get;
        }

        // Contains the path to a temporary data / cache directory (RO).
        extern public static string temporaryCachePath
        {
            [FreeFunction("GetTemporaryCachePathApplicationSpecific")]
            get;
        }

        // The URL of the document (what is shown in a browser's address bar) for WebGL
        extern public static string absoluteURL
        {
            [FreeFunction("GetPlayerSettings().GetAbsoluteURL")]
            get;
        }

        // Evaluates script snippet in the containing web page __(Web Player only)__.
        [Obsolete("Application.ExternalEval is deprecated. See https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html for alternatives.")]
        public static void ExternalEval(string script)
        {
            if (script.Length > 0 && script[script.Length - 1] != ';')
                script += ';';
            Internal_ExternalCall(script);
        }

        [FreeFunction("Application_Bindings::ExternalCall")]
        extern private static void Internal_ExternalCall(string script);

        // The version of the Unity runtime used to play the content.
        extern public static string unityVersion
        {
            [FreeFunction("Application_Bindings::GetUnityVersion")]
            get;
        }


        // Runtime application version.
        extern public static string version
        {
            [FreeFunction("GetApplicationInfo().GetVersion")]
            get;
        }

        // What is the name of the installer (primarily Android)
        extern public static string installerName
        {
            [FreeFunction("GetApplicationInfo().GetInstallerName")]
            get;
        }

        extern public static string identifier
        {
            [FreeFunction("GetApplicationInfo().GetApplicationIdentifier")]
            get;
        }

        // How was the application install (developer build, from the store, adhoc)
        extern public static ApplicationInstallMode installMode
        {
            [FreeFunction("GetApplicationInfo().GetInstallMode")]
            get;
        }

        // Is the appilication sandbox return sandbox type.
        extern public static ApplicationSandboxType sandboxType
        {
            [FreeFunction("GetApplicationInfo().GetSandboxType")]
            get;
        }

        extern public static string productName
        {
            [FreeFunction("GetPlayerSettings().GetProductName")]
            get;
        }

        extern public static string companyName
        {
            [FreeFunction("GetPlayerSettings().GetCompanyName")]
            get;
        }

        extern public static string cloudProjectId
        {
            [FreeFunction("GetPlayerSettings().GetCloudProjectId")]
            get;
        }

        [FreeFunction("GetAdsIdHandler().RequestAdsIdAsync")]
        extern public static bool RequestAdvertisingIdentifierAsync(AdvertisingIdentifierCallback delegateMethod);

        // Opens the /url/ in a browser.
        [FreeFunction("OpenURL")]
        extern public static void OpenURL(string url);

        [Obsolete("Use UnityEngine.Diagnostics.Utils.ForceCrash")]
        public static void ForceCrash(int mode)
        {
            UnityEngine.Diagnostics.Utils.ForceCrash((UnityEngine.Diagnostics.ForcedCrashCategory)mode);
        }

        // Instructs game to try to render at a specified frame rate.
        extern public static int targetFrameRate
        {
            [FreeFunction("GetTargetFrameRate")]
            get;
            [FreeFunction("SetTargetFrameRate")]
            set;
        }

        // The language the user's operating system is running in.
        extern public static SystemLanguage systemLanguage
        {
            [FreeFunction("(SystemLanguage)systeminfo::GetSystemLanguage")]
            get;
        }

        [FreeFunction("Application_Bindings::SetLogCallbackDefined")]
        extern private static void SetLogCallbackDefined(bool defined);

        [Obsolete("Use SetStackTraceLogType/GetStackTraceLogType instead")]
        extern public static StackTraceLogType stackTraceLogType
        {
            [FreeFunction("Application_Bindings::GetStackTraceLogType")]
            get;
            [FreeFunction("Application_Bindings::SetStackTraceLogType")]
            set;
        }

        [FreeFunction("GetStackTraceLogType")]
        extern public static StackTraceLogType GetStackTraceLogType(LogType logType);

        [FreeFunction("SetStackTraceLogType")]
        extern public static void SetStackTraceLogType(LogType logType, StackTraceLogType stackTraceType);

        extern public static string consoleLogPath
        {
            [FreeFunction("GetConsoleLogPath")]
            get;
        }

        // Priority of background loading thread.
        extern public static ThreadPriority backgroundLoadingPriority
        {
            [FreeFunction("GetPreloadManager().GetThreadPriority")]
            get;
            [FreeFunction("GetPreloadManager().SetThreadPriority")]
            set;
        }

        // Returns the type of Internet reachability currently possible on the device.
        extern public static NetworkReachability internetReachability
        {
            [FreeFunction("GetInternetReachability")]
            get;
        }


        // Returns false if application is altered in any way after it was built.
        extern public static bool genuine
        {
            [FreeFunction("IsApplicationGenuine")]
            get;
        }

        // Returns true if application integrity can be confirmed.
        extern public static bool genuineCheckAvailable
        {
            [FreeFunction("IsApplicationGenuineAvailable")]
            get;
        }


        // Request authorization to use the webcam or microphone.
        [FreeFunction("Application_Bindings::RequestUserAuthorization")]
        extern public static AsyncOperation RequestUserAuthorization(UserAuthorization mode);

        // Check if the user has authorized use of the webcam or microphone.
        [FreeFunction("Application_Bindings::HasUserAuthorization")]
        extern public  static bool HasUserAuthorization(UserAuthorization mode);

        extern internal static bool submitAnalytics
        {
            [FreeFunction("GetPlayerSettings().GetSubmitAnalytics")]
            get;
        }

        [Obsolete("This property is deprecated, please use SplashScreen.isFinished instead")]
        public static bool isShowingSplashScreen
        {
            get
            {
                return !UnityEngine.Rendering.SplashScreen.isFinished;
            }
        }
    }
}
