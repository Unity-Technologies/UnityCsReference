// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEngine.Events;

namespace UnityEngine
{
    internal class ApplicationShimBase : IDisposable
    {
        public void Dispose()
        {
            ShimManager.RemoveShim(this);
        }

        public bool IsActive()
        {
            return ShimManager.IsShimActive(this);
        }

        public virtual string absoluteURL => UnityEngine.Application.absoluteURL;

        public virtual UnityEngine.ThreadPriority backgroundLoadingPriority
        {
            get => UnityEngine.Application.backgroundLoadingPriority;
            set => UnityEngine.Application.backgroundLoadingPriority = value;
        }
        public virtual string buildGUID => UnityEngine.Application.buildGUID;
        public virtual string cloudProjectId => UnityEngine.Application.cloudProjectId;
        public virtual string companyName => UnityEngine.Application.companyName;
        public virtual string consoleLogPath => UnityEngine.Application.consoleLogPath;
        public virtual string dataPath => UnityEngine.Application.dataPath;
        public virtual bool genuine => UnityEngine.Application.genuine;
        public virtual bool genuineCheckAvailable => UnityEngine.Application.genuineCheckAvailable;
        public virtual string identifier => UnityEngine.Application.identifier;
        public virtual string installerName => UnityEngine.Application.installerName;
        public virtual UnityEngine.ApplicationInstallMode installMode => UnityEngine.Application.installMode;
        public virtual UnityEngine.NetworkReachability internetReachability => UnityEngine.Application.internetReachability;
        public virtual bool isBatchMode => UnityEngine.Application.isBatchMode;
        public virtual bool isConsolePlatform => UnityEngine.Application.isConsolePlatform;
        public virtual bool isEditor => UnityEngine.Application.isEditor;
        public virtual bool isFocused => UnityEngine.Application.isFocused;
        public virtual bool isMobilePlatform => UnityEngine.Application.isMobilePlatform;
        public virtual bool isPlaying => UnityEngine.Application.isPlaying;
        public virtual string persistentDataPath => UnityEngine.Application.persistentDataPath;
        public virtual UnityEngine.RuntimePlatform platform => UnityEngine.Application.platform;
        public virtual string productName => UnityEngine.Application.productName;

        public virtual bool runInBackground
        {
            get => UnityEngine.Application.runInBackground;
            set => UnityEngine.Application.runInBackground = value;
        }
        public virtual UnityEngine.ApplicationSandboxType sandboxType => UnityEngine.Application.sandboxType;
        public virtual string streamingAssetsPath => UnityEngine.Application.streamingAssetsPath;
        public virtual UnityEngine.SystemLanguage systemLanguage => UnityEngine.Application.systemLanguage;

        public virtual int targetFrameRate
        {
            get => UnityEngine.Application.targetFrameRate;
            set => UnityEngine.Application.targetFrameRate = value;
        }
        public virtual string temporaryCachePath => UnityEngine.Application.temporaryCachePath;
        public virtual string unityVersion => UnityEngine.Application.unityVersion;
        public virtual string version => UnityEngine.Application.version;

        public virtual event Action<string> deepLinkActivated
        {
            add => UnityEngine.Application.deepLinkActivated += value;
            remove => UnityEngine.Application.deepLinkActivated -= value;
        }

        public virtual event Action<bool> focusChanged
        {
            add => UnityEngine.Application.focusChanged += value;
            remove => UnityEngine.Application.focusChanged -= value;
        }

        public virtual event UnityEngine.Application.LogCallback logMessageReceived
        {
            add => UnityEngine.Application.logMessageReceived += value;
            remove => UnityEngine.Application.logMessageReceived -= value;
        }

        public virtual event UnityEngine.Application.LogCallback logMessageReceivedThreaded
        {
            add => UnityEngine.Application.logMessageReceivedThreaded += value;
            remove => UnityEngine.Application.logMessageReceivedThreaded -= value;
        }

        public virtual event UnityEngine.Application.LowMemoryCallback lowMemory
        {
            add => UnityEngine.Application.lowMemory += value;
            remove => UnityEngine.Application.lowMemory -= value;
        }

        public virtual event UnityAction onBeforeRender
        {
            add => UnityEngine.Application.onBeforeRender += value;
            remove => UnityEngine.Application.onBeforeRender -= value;
        }

        public virtual event Action quitting
        {
            add => UnityEngine.Application.quitting += value;
            remove => UnityEngine.Application.quitting -= value;
        }

        public virtual event Func<bool> wantsToQuit
        {
            add => UnityEngine.Application.wantsToQuit += value;
            remove => UnityEngine.Application.wantsToQuit -= value;
        }

        public virtual event Action unloading
        {
            add => UnityEngine.Application.unloading += value;
            remove => UnityEngine.Application.unloading -= value;
        }

        public virtual bool CanStreamedLevelBeLoaded(int levelIndex)
        {
            return UnityEngine.Application.CanStreamedLevelBeLoaded(levelIndex);
        }

        public virtual bool CanStreamedLevelBeLoaded(string levelName)
        {
            return UnityEngine.Application.CanStreamedLevelBeLoaded(levelName);
        }

        public virtual string[] GetBuildTags()
        {
            return UnityEngine.Application.GetBuildTags();
        }

        public virtual UnityEngine.StackTraceLogType GetStackTraceLogType(UnityEngine.LogType logType)
        {
            return UnityEngine.Application.GetStackTraceLogType(logType);
        }

        public virtual bool HasProLicense()
        {
            return UnityEngine.Application.HasProLicense();
        }

        public virtual bool HasUserAuthorization(UnityEngine.UserAuthorization mode)
        {
            return UnityEngine.Application.HasUserAuthorization(mode);
        }

        public virtual bool IsPlaying(UnityEngine.Object obj)
        {
            return UnityEngine.Application.IsPlaying(obj);
        }

        public virtual void OpenURL(string url)
        {
            UnityEngine.Application.OpenURL(url);
        }

        public virtual void Quit()
        {
            UnityEngine.Application.Quit();
        }

        public virtual void Quit(int exitCode)
        {
            UnityEngine.Application.Quit(exitCode);
        }

        public virtual bool RequestAdvertisingIdentifierAsync(UnityEngine.Application.AdvertisingIdentifierCallback delegateMethod)
        {
            return UnityEngine.Application.RequestAdvertisingIdentifierAsync(delegateMethod);
        }

        public virtual UnityEngine.AsyncOperation RequestUserAuthorization(UnityEngine.UserAuthorization mode)
        {
            return UnityEngine.Application.RequestUserAuthorization(mode);
        }

        public virtual void SetBuildTags(string[] buildTags)
        {
            UnityEngine.Application.SetBuildTags(buildTags);
        }

        public virtual void SetStackTraceLogType(UnityEngine.LogType logType, UnityEngine.StackTraceLogType stackTraceType)
        {
            UnityEngine.Application.SetStackTraceLogType(logType, stackTraceType);
        }

        public virtual void Unload()
        {
            UnityEngine.Application.Unload();
        }

        public virtual CancellationToken exitCancellationToken => UnityEngine.Application.exitCancellationToken;
    }
}

