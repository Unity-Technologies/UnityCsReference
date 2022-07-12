// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEngine.Events;

namespace UnityEngine.Device
{
    public static class Application
    {
        public static string absoluteURL => ShimManager.applicationShim.absoluteURL;
        public static UnityEngine.ThreadPriority backgroundLoadingPriority
        {
            get => ShimManager.applicationShim.backgroundLoadingPriority;
            set => ShimManager.applicationShim.backgroundLoadingPriority = value;
        }
        public static string buildGUID => ShimManager.applicationShim.buildGUID;
        public static string cloudProjectId => ShimManager.applicationShim.cloudProjectId;
        public static string companyName => ShimManager.applicationShim.companyName;
        public static string consoleLogPath => ShimManager.applicationShim.consoleLogPath;
        public static string dataPath => ShimManager.applicationShim.dataPath;
        public static bool genuine => ShimManager.applicationShim.genuine;
        public static bool genuineCheckAvailable => ShimManager.applicationShim.genuineCheckAvailable;
        public static string identifier => ShimManager.applicationShim.identifier;
        public static string installerName => ShimManager.applicationShim.installerName;
        public static UnityEngine.ApplicationInstallMode installMode => ShimManager.applicationShim.installMode;
        public static UnityEngine.NetworkReachability internetReachability => ShimManager.applicationShim.internetReachability;
        public static bool isBatchMode => ShimManager.applicationShim.isBatchMode;
        public static bool isConsolePlatform => ShimManager.applicationShim.isConsolePlatform;
        public static bool isEditor => ShimManager.applicationShim.isEditor;
        public static bool isFocused => ShimManager.applicationShim.isFocused;
        public static bool isMobilePlatform => ShimManager.applicationShim.isMobilePlatform;
        public static bool isPlaying => ShimManager.applicationShim.isPlaying;
        public static string persistentDataPath => ShimManager.applicationShim.persistentDataPath;
        public static UnityEngine.RuntimePlatform platform => ShimManager.applicationShim.platform;
        public static string productName => ShimManager.applicationShim.productName;
        public static bool runInBackground
        {
            get => ShimManager.applicationShim.runInBackground;
            set => ShimManager.applicationShim.runInBackground = value;
        }
        public static UnityEngine.ApplicationSandboxType sandboxType => ShimManager.applicationShim.sandboxType;
        public static string streamingAssetsPath => ShimManager.applicationShim.streamingAssetsPath;
        public static UnityEngine.SystemLanguage systemLanguage => ShimManager.applicationShim.systemLanguage;
        public static int targetFrameRate
        {
            get => ShimManager.applicationShim.targetFrameRate;
            set => ShimManager.applicationShim.targetFrameRate = value;
        }
        public static string temporaryCachePath => ShimManager.applicationShim.temporaryCachePath;
        public static string unityVersion => ShimManager.applicationShim.unityVersion;
        public static string version => ShimManager.applicationShim.version;

        public static event Action<string> deepLinkActivated
        {
            add => ShimManager.applicationShim.deepLinkActivated += value;
            remove => ShimManager.applicationShim.deepLinkActivated -= value;
        }

        public static event Action<bool> focusChanged
        {
            add => ShimManager.applicationShim.focusChanged += value;
            remove => ShimManager.applicationShim.focusChanged -= value;
        }

        public static event UnityEngine.Application.LogCallback logMessageReceived
        {
            add => ShimManager.applicationShim.logMessageReceived += value;
            remove => ShimManager.applicationShim.logMessageReceived -= value;
        }

        public static event UnityEngine.Application.LogCallback logMessageReceivedThreaded
        {
            add => ShimManager.applicationShim.logMessageReceivedThreaded += value;
            remove => ShimManager.applicationShim.logMessageReceivedThreaded -= value;
        }

        public static event UnityEngine.Application.LowMemoryCallback lowMemory
        {
            add => ShimManager.applicationShim.lowMemory += value;
            remove => ShimManager.applicationShim.lowMemory -= value;
        }

        public static event UnityEngine.Application.MemoryUsageChangedCallback memoryUsageChanged
        {
            add => ShimManager.applicationShim.memoryUsageChanged += value;
            remove => ShimManager.applicationShim.memoryUsageChanged -= value;
        }

        public static event UnityAction onBeforeRender
        {
            add => ShimManager.applicationShim.onBeforeRender += value;
            remove => ShimManager.applicationShim.onBeforeRender -= value;
        }

        public static event Action quitting
        {
            add => ShimManager.applicationShim.quitting += value;
            remove => ShimManager.applicationShim.quitting -= value;
        }

        public static event Func<bool> wantsToQuit
        {
            add => ShimManager.applicationShim.wantsToQuit += value;
            remove => ShimManager.applicationShim.wantsToQuit -= value;
        }

        public static event Action unloading
        {
            add => ShimManager.applicationShim.unloading += value;
            remove => ShimManager.applicationShim.unloading -= value;
        }

        public static bool CanStreamedLevelBeLoaded(int levelIndex)
        {
            return ShimManager.applicationShim.CanStreamedLevelBeLoaded(levelIndex);
        }

        public static bool CanStreamedLevelBeLoaded(string levelName)
        {
            return ShimManager.applicationShim.CanStreamedLevelBeLoaded(levelName);
        }

        public static string[] GetBuildTags()
        {
            return ShimManager.applicationShim.GetBuildTags();
        }

        public static UnityEngine.StackTraceLogType GetStackTraceLogType(UnityEngine.LogType logType)
        {
            return ShimManager.applicationShim.GetStackTraceLogType(logType);
        }

        public static bool HasProLicense()
        {
            return ShimManager.applicationShim.HasProLicense();
        }

        public static bool HasUserAuthorization(UnityEngine.UserAuthorization mode)
        {
            return ShimManager.applicationShim.HasUserAuthorization(mode);
        }

        public static bool IsPlaying(UnityEngine.Object obj)
        {
            return ShimManager.applicationShim.IsPlaying(obj);
        }

        public static void OpenURL(string url)
        {
            ShimManager.applicationShim.OpenURL(url);
        }

        public static void Quit()
        {
            ShimManager.applicationShim.Quit();
        }

        public static void Quit(int exitCode)
        {
            ShimManager.applicationShim.Quit(exitCode);
        }

        public static bool RequestAdvertisingIdentifierAsync(UnityEngine.Application.AdvertisingIdentifierCallback delegateMethod)
        {
            return ShimManager.applicationShim.RequestAdvertisingIdentifierAsync(delegateMethod);
        }

        public static UnityEngine.AsyncOperation RequestUserAuthorization(UnityEngine.UserAuthorization mode)
        {
            return ShimManager.applicationShim.RequestUserAuthorization(mode);
        }

        public static void SetBuildTags(string[] buildTags)
        {
            ShimManager.applicationShim.SetBuildTags(buildTags);
        }

        public static void SetStackTraceLogType(UnityEngine.LogType logType, UnityEngine.StackTraceLogType stackTraceType)
        {
            ShimManager.applicationShim.SetStackTraceLogType(logType, stackTraceType);
        }

        public static void Unload()
        {
            ShimManager.applicationShim.Unload();
        }

        public static CancellationToken exitCancellationToken => ShimManager.applicationShim.exitCancellationToken;

    }
}
