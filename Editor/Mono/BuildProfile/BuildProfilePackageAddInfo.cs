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

        [NonSerialized]
        string m_PackageRequestKey;

        ProgressEntry m_PackageAddProgressInfo = new();

        /// <summary>
        /// Begins package installation if not already started.
        /// </summary>
        public void RequestPackageInstallation()
        {
            if (m_PackageRequestKey != null || packagesToAdd.Length <= 0)
                return;

            m_PackageRequestKey = BuildProfilePackageInstaller.RequestPackageInstallation(
                packagesToAdd, HandlePackageAddProgress, HandlePackageAddComplete);
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
            BuildProfilePackageInstaller.RemovePackageAddCallbacks(
                m_PackageRequestKey, HandlePackageAddProgress, HandlePackageAddComplete);
            m_PackageRequestKey = null;
            OnPackageAddComplete = null;
            OnPackageAddProgress = null;
        }

        void HandlePackageAddProgress(ProgressEntry info)
        {
            m_PackageAddProgressInfo = info;
            OnPackageAddProgress?.Invoke();
        }

        void HandlePackageAddComplete()
        {
            packagesToAdd = Array.Empty<BuildTargetDiscovery.PlatformPackageIdentifier>();
            OnPackageAddComplete?.Invoke();
        }
    }
}
