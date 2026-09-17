// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{

    /// <summary>
    /// Package installation progress tracker for build profile initialization.
    /// </summary>
    [Serializable]
    [VisibleToOtherModules("UnityEditor.BuildProfileModule")]
    internal class BuildProfilePackageAddInfo
    {
        public enum ProgressState
        {
            PackageStateUnknown,
            PackagePending,
            PackageDownloading,
            PackageInstalling,
            PackageReady,
            PackageError,
            ConfigurationPending,
            ConfigurationRunning
        }
        public record struct ProgressEntry(ProgressState state, string name, int packageCount);

        /// <summary>
        /// Packages pending installation. Each entry keeps its name and optional pinned version
        /// apart, so lookups can match on the name while the install request uses both.
        /// </summary>
        [field: SerializeField]
        public BuildTargetDiscovery.PlatformPackageIdentifier[] packagesToAdd { get; set; }
            = Array.Empty<BuildTargetDiscovery.PlatformPackageIdentifier>();

        public Action OnPackageAddProgress;
        public Action OnPackageAddComplete;

        [SerializeField]
        bool isPackageAddRequest = false;

        PackageManager.Requests.AddAndRemoveRequest m_PackageAddRequest = null;
        ProgressEntry m_PackageAddProgressInfo = new();

        bool m_CallbacksSubscribed = false;

        /// <summary>
        /// Begins package installation if not already started.
        /// </summary>
        public void RequestPackageInstallation()
        {
            if (m_PackageAddRequest != null && isPackageAddRequest)
                return;

            if (packagesToAdd.Length > 0)
            {
                var installIdentifiers = new string[packagesToAdd.Length];
                for (int i = 0; i < packagesToAdd.Length; i++)
                    installIdentifiers[i] = packagesToAdd[i].GetInstallIdentifier();

                m_PackageAddRequest = PackageManager.Client.AddAndRemove(installIdentifiers);
                m_PackageAddRequest.progressUpdated += HandlePackageAddProgress;

                EditorApplication.update += CheckCompletion;
                AssemblyReloadEvents.beforeAssemblyReload += UnsubscribeRequestCallbacks;
                m_CallbacksSubscribed = true;
                isPackageAddRequest = true;
            }
        }

        /// <summary>
        /// Check if package installation is completed, considers that multiple
        /// package add requests with similar packages could be made.
        /// </summary>
        public bool IsPackageRequestDone()
        {
            foreach (var package in packagesToAdd)
            {
                if (!PackageManager.PackageInfo.IsPackageRegistered(package.name))
                    return false;
            }

            return true;
        }

        public ProgressEntry GetPackageAddProgressInfo() => m_PackageAddProgressInfo;

        /// <summary>
        /// Cleans up event subscriptions and resources.
        /// </summary>
        public void Cleanup()
        {
            UnsubscribeRequestCallbacks();
            isPackageAddRequest = false;
            m_PackageAddRequest = null;
            OnPackageAddComplete = null;
            OnPackageAddProgress = null;
        }

        /// <summary>
        /// Detaches the native-backed callbacks (the editor update loop, the request's
        /// <c>progressUpdated</c> event, and the domain-reload hook). These must be removed
        /// before any domain reload — otherwise the native delegate lists keep GC handles to
        /// this instance from the previous domain, producing "invalid GC handle ... from a
        /// previous domain" warnings when they are later resolved or released.
        /// </summary>
        void UnsubscribeRequestCallbacks()
        {
            // Skip when the callbacks weren't subscribed in this domain. After a domain reload
            // the request object may survive (serialized) but its progress tracker does not, so
            // unsubscribing progressUpdated would dereference a null tracker and throw.
            if (!m_CallbacksSubscribed)
                return;

            m_CallbacksSubscribed = false;
            AssemblyReloadEvents.beforeAssemblyReload -= UnsubscribeRequestCallbacks;
            EditorApplication.update -= CheckCompletion;
            if (m_PackageAddRequest != null)
                m_PackageAddRequest.progressUpdated -= HandlePackageAddProgress;
        }

        bool ContainsPackage(string name)
        {
            foreach (var package in packagesToAdd)
            {
                if (package.name == name)
                    return true;
            }
            return false;
        }

        void CheckCompletion()
        {
            if (m_PackageAddRequest == null || !m_PackageAddRequest.IsCompleted)
                return;

            UnsubscribeRequestCallbacks();

            if (m_PackageAddRequest.Status >= PackageManager.StatusCode.Failure)
                Debug.LogError(m_PackageAddRequest.Error.message);

            packagesToAdd = Array.Empty<BuildTargetDiscovery.PlatformPackageIdentifier>();
            OnPackageAddComplete?.Invoke();
        }

        void HandlePackageAddProgress(PackageManager.ProgressUpdateEventArgs progress)
        {
            int readyPackageNum = 0;
            bool packageInstalling = false;
            bool packageDownloading = false;
            bool packageErr = false;
            string packageNames = "";
            int packagesDownloadingCnt = 0;
            int packagesInstallingCnt = 0;
            int packagesErrCnt = 0;

            ProgressEntry info;
            foreach (var entry in progress.entries)
            {
                if (ContainsPackage(entry.name))
                {
                    if (packageNames.Length > 0)
                        packageNames += " ";
                    switch (entry.state)
                    {
                        case PackageManager.ProgressState.Ready:
                            readyPackageNum += 1;
                            break;
                        case PackageManager.ProgressState.Error:
                            packageErr = true;
                            packagesErrCnt++;
                            break;
                        case PackageManager.ProgressState.Downloading:
                            packageDownloading = true;
                            packagesDownloadingCnt++;
                            break;
                        case PackageManager.ProgressState.Installing:
                            packageInstalling = true;
                            packagesInstallingCnt++;
                            break;
                    }
                    packageNames += entry.name;
                }
            }

            bool done = (readyPackageNum == packagesToAdd.Length);

            if (packageErr)
            {
                info = new ProgressEntry(ProgressState.PackageError, packageNames, packagesErrCnt);
            }
            else if (packageInstalling)
            {
                info = new ProgressEntry(ProgressState.PackageInstalling, packageNames, packagesInstallingCnt);
            }
            else if (packageDownloading)
            {
                info = new ProgressEntry(ProgressState.PackageDownloading, packageNames, packagesDownloadingCnt);
            }
            else
            {
                info = new ProgressEntry(done ? ProgressState.ConfigurationRunning : ProgressState.ConfigurationPending, packageNames, 0);
            }

            m_PackageAddProgressInfo = info;
            OnPackageAddProgress?.Invoke();
        }
    }
}
