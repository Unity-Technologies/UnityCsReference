// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace UnityEngine.Android
{
    // Directly matches values returned by PlayCore API
    // https://developer.android.com/reference/com/google/android/play/core/assetpacks/model/AssetPackStatus
    public enum AndroidAssetPackStatus
    {
        Unknown = 0,
        Pending = 1,
        Downloading = 2,
        Transferring = 3,
        Completed = 4,
        Failed = 5,
        Canceled = 6,
        WaitingForWifi = 7,
        NotInstalled = 8,
    }

    // Directly matches values returned by PlayCore API
    // https://developer.android.com/reference/com/google/android/play/core/assetpacks/model/AssetPackErrorCode
    public enum AndroidAssetPackError
    {
        NoError = 0,
        AppUnavailable = -1,
        PackUnavailable = -2,
        InvalidRequest = -3,
        DownloadNotFound = -4,
        ApiNotAvailable = -5,
        NetworkError = -6,
        AccessDenied = -7,
        InsufficientStorage = -10,
        PlayStoreNotFound = -11,
        NetworkUnrestricted = -12,
        AppNotOwned = -13,
        InternalError = -100,
    }

    public class AndroidAssetPackInfo
    {
        internal AndroidAssetPackInfo(string name, AndroidAssetPackStatus status, ulong size, ulong bytesDownloaded, float transferProgress, AndroidAssetPackError error)
        {
            this.name = name;
            this.status = status;
            this.size = size;
            this.bytesDownloaded = bytesDownloaded;
            this.transferProgress = transferProgress;
            this.error = error;
        }

        public string                 name { get; }
        public AndroidAssetPackStatus status { get; }
        public ulong                  size { get; }
        public ulong                  bytesDownloaded { get; }
        public float                  transferProgress { get; }
        public AndroidAssetPackError  error { get; }
    }

    public class AndroidAssetPackState
    {
        internal AndroidAssetPackState(string name, AndroidAssetPackStatus status, AndroidAssetPackError error)
        {
            this.name = name;
            this.status = status;
            this.error = error;
        }

        public string                 name { get; }
        public AndroidAssetPackStatus status { get; }
        public AndroidAssetPackError  error { get; }
    }

    public class AndroidAssetPackUseMobileDataRequestResult
    {
        internal AndroidAssetPackUseMobileDataRequestResult(bool allowed)
        {
            this.allowed = allowed;
        }

        public bool allowed { get; }
    }

    public class DownloadAssetPackAsyncOperation : CustomYieldInstruction
    {
        Dictionary<string, AndroidAssetPackInfo> m_AssetPackInfos;

        public override bool keepWaiting
        {
            get
            {
                lock (m_AssetPackInfos)
                {
                    foreach (var info in m_AssetPackInfos.Values)
                    {
                        // Continue waiting if we did not get even a single callback for some of the expected asset packs
                        // Google's PlayCore API does not call any callbacks for non-existing asset packs, but we should detect that in java and still report AndroidAssetPackStatus.Unknown
                        if (info == null)
                            return true;

                        if (info.status != AndroidAssetPackStatus.Canceled
                            && info.status != AndroidAssetPackStatus.Completed
                            && info.status != AndroidAssetPackStatus.Failed
                            && info.status != AndroidAssetPackStatus.Unknown)
                        {
                            return true;
                        }
                    }

                    // Stop waiting when all asset packs were downloaded, canceled downloading or failed the download
                    return false;
                }
            }
        }

        public bool isDone => !keepWaiting;

        public float progress
        {
            get
            {
                lock (m_AssetPackInfos)
                {
                    var downloadProgress = 0f;
                    var transferProgress = 0f;
                    foreach (var info in m_AssetPackInfos.Values)
                    {
                        if (info == null)
                            continue;
                        if (info.status == AndroidAssetPackStatus.Canceled
                            || info.status == AndroidAssetPackStatus.Completed
                            || info.status == AndroidAssetPackStatus.Failed
                            || info.status == AndroidAssetPackStatus.Unknown)
                        {
                            // We are counting the whole operation progress, so a failed subtask is "done" subtask in this case
                            downloadProgress += 1f;
                            transferProgress += 1f;
                        }
                        else
                        {
                            downloadProgress += info.bytesDownloaded / info.size;
                            transferProgress += info.transferProgress;
                        }
                    }
                    // Use 0.8 weight for download and 0.2 weight for transfer (unpacking)
                    return Mathf.Clamp((downloadProgress * 0.8f + transferProgress * 0.2f) / m_AssetPackInfos.Count, 0f, 1f);
                }
            }
        }

        public string[] downloadedAssetPacks
        {
            get
            {
                lock (m_AssetPackInfos)
                {
                    List<string> packNames = new List<string>();
                    foreach (var info in m_AssetPackInfos.Values)
                    {
                        if (info == null)
                            continue;
                        if (info.status == AndroidAssetPackStatus.Completed)
                        {
                            packNames.Add(info.name);
                        }
                    }
                    return packNames.ToArray();
                }
            }
        }

        public string[] downloadFailedAssetPacks
        {
            get
            {
                lock (m_AssetPackInfos)
                {
                    List<string> packNames = new List<string>();
                    foreach (var keyPair in m_AssetPackInfos)
                    {
                        var info = keyPair.Value;
                        if (info == null)
                        {
                            packNames.Add(keyPair.Key);
                        }
                        else if (info.status == AndroidAssetPackStatus.Canceled
                                 || info.status == AndroidAssetPackStatus.Failed
                                 || info.status == AndroidAssetPackStatus.Unknown)
                        {
                            packNames.Add(info.name);
                        }
                    }
                    return packNames.ToArray();
                }
            }
        }

        internal DownloadAssetPackAsyncOperation(string[] assetPackNames)
        {
            m_AssetPackInfos = assetPackNames.ToDictionary<string, string, AndroidAssetPackInfo>(name => name, name => null);
        }

        internal void OnUpdate(AndroidAssetPackInfo info)
        {
            lock (m_AssetPackInfos)
            {
                m_AssetPackInfos[info.name] = info;
            }
        }
    }

    public class GetAssetPackStateAsyncOperation : CustomYieldInstruction
    {
        ulong m_Size;
        AndroidAssetPackState[] m_States;
        readonly object m_OperationLock;

        public override bool keepWaiting
        {
            get
            {
                lock (m_OperationLock)
                {
                    return m_States == null;
                }
            }
        }

        public bool isDone => !keepWaiting;

        public ulong size
        {
            get
            {
                lock (m_OperationLock)
                {
                    return m_Size;
                }
            }
        }

        public AndroidAssetPackState[] states
        {
            get
            {
                lock (m_OperationLock)
                {
                    return m_States;
                }
            }
        }

        internal GetAssetPackStateAsyncOperation()
        {
            m_OperationLock = new object();
        }

        internal void OnResult(ulong size, AndroidAssetPackState[] states)
        {
            lock (m_OperationLock)
            {
                m_Size = size;
                m_States = states;
            }
        }
    }

    public class RequestToUseMobileDataAsyncOperation : CustomYieldInstruction
    {
        AndroidAssetPackUseMobileDataRequestResult m_RequestResult;
        readonly object m_OperationLock;

        public override bool keepWaiting
        {
            get
            {
                lock (m_OperationLock)
                {
                    return m_RequestResult == null;
                }
            }
        }

        public bool isDone => !keepWaiting;

        public AndroidAssetPackUseMobileDataRequestResult result
        {
            get
            {
                lock (m_OperationLock)
                {
                    return m_RequestResult;
                }
            }
        }

        internal RequestToUseMobileDataAsyncOperation()
        {
            m_OperationLock = new object();
        }

        internal void OnResult(AndroidAssetPackUseMobileDataRequestResult result)
        {
            lock (m_OperationLock)
            {
                m_RequestResult = result;
            }
        }
    }

    [NativeHeader("Modules/AndroidJNI/Public/AndroidAssetPacksBindingsHelpers.h")]
    [StaticAccessor("AndroidAssetPacksBindingsHelpers", StaticAccessorType.DoubleColon)]
    public static class AndroidAssetPacks
    {
        public static bool coreUnityAssetPacksDownloaded { get { return CoreUnityAssetPacksDownloaded(); } }

        internal static string dataPackName { get { return GetDataPackName(); } }
        internal static string streamingAssetsPackName { get { return GetStreamingAssetsPackName(); } }

        [NativeConditional("PLATFORM_ANDROID")]
        private static extern bool CoreUnityAssetPacksDownloaded();

        public static string[] GetCoreUnityAssetPackNames() { return new string[0]; }
        public static void GetAssetPackStateAsync(string[] assetPackNames, Action<ulong, AndroidAssetPackState[]> callback) {}
        public static GetAssetPackStateAsyncOperation GetAssetPackStateAsync(string[] assetPackNames) { return null; }
        public static void DownloadAssetPackAsync(string[] assetPackNames, Action<AndroidAssetPackInfo> callback) {}
        public static DownloadAssetPackAsyncOperation DownloadAssetPackAsync(string[] assetPackNames) { return null; }
        public static void RequestToUseMobileDataAsync(Action<AndroidAssetPackUseMobileDataRequestResult> callback) {}
        public static RequestToUseMobileDataAsyncOperation RequestToUseMobileDataAsync() { return null; }
        public static string GetAssetPackPath(string assetPackName) { return ""; }
        public static void CancelAssetPackDownload(string[] assetPackNames) {}
        public static void RemoveAssetPack(string assetPackName) {}

        // These values must match constants in AndroidAssetPacks.h
        // We can't directly access them since all code in PlatformDependent gets stripped when building source code delivery
        private static string GetDataPackName() { return "UnityDataAssetPack"; }
        private static string GetStreamingAssetsPackName() { return "UnityStreamingAssetsPack"; }
    }
}
