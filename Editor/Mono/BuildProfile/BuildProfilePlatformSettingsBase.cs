// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Base class for platform module specific build settings.
    /// Implementation fetched from BuildProfileExtension, <see cref="ModuleManager.GetBuildProfileExtension"/>.
    /// </summary>
    [Serializable]
    [VisibleToOtherModules]
    internal abstract class BuildProfilePlatformSettingsBase
    {
        internal const string k_SettingDevelopment = "development";
        internal const string k_SettingConnectProfiler = "connectProfiler";
        internal const string k_SettingBuildWithDeepProfilingSupport = "buildWithDeepProfilingSupport";
        internal const string k_SettingAllowDebugging = "allowDebugging";
        internal const string k_SettingWaitForManagedDebugger = "WaitForManagedDebugger";
        internal const string k_SettingManagedDebuggerFixedPort = "ManagedDebuggerFixedPort";
        internal const string k_SettingCompressionType = "compressionType";
        internal const string k_SettingExplicitNullChecks = "explicitNullChecks";
        internal const string k_SettingExplicitDivideByZeroChecks = "explicitDivideByZeroChecks";
        internal const string k_SettingExplicitArrayBoundsChecks = "explicitArrayBoundsChecks";
        internal const string k_SettingInstallInBuildFolder = "installInBuildFolder";

        internal const int k_InvalidCompressionIdx = -1;
        internal const int k_MaxPortNumber = 65535;

        [SerializeField] bool m_Development = false;
        [SerializeField] bool m_ConnectProfiler = false;
        [SerializeField] bool m_BuildWithDeepProfilingSupport = false;
        [SerializeField] bool m_AllowDebugging = false;
        [SerializeField] bool m_WaitForManagedDebugger = false;
        [SerializeField] int m_ManagedDebuggerFixedPort = 0;
        [SerializeField] bool m_ExplicitNullChecks = false;
        [SerializeField] bool m_ExplicitDivideByZeroChecks = false;
        [SerializeField] bool m_ExplicitArrayBoundsChecks = false;
        [SerializeField] Compression m_CompressionType = (Compression)k_InvalidCompressionIdx;
        [SerializeField] bool m_InstallInBuildFolder = false;

        internal protected virtual bool development
        {
            get => m_Development;
            set => m_Development = value;
        }

        internal protected virtual bool connectProfiler
        {
            get => m_ConnectProfiler;
            set => m_ConnectProfiler = value;
        }

        internal protected virtual bool buildWithDeepProfilingSupport
        {
            get => m_BuildWithDeepProfilingSupport;
            set => m_BuildWithDeepProfilingSupport = value;
        }

        internal protected virtual bool allowDebugging
        {
            get => m_AllowDebugging;
            set => m_AllowDebugging = value;
        }

        internal protected virtual bool waitForManagedDebugger
        {
            get => m_WaitForManagedDebugger;
            set => m_WaitForManagedDebugger = value;
        }

        internal protected virtual int managedDebuggerFixedPort
        {
            get
            {
                if (0 < m_ManagedDebuggerFixedPort && m_ManagedDebuggerFixedPort <= k_MaxPortNumber)
                {
                    return m_ManagedDebuggerFixedPort;
                }
                return 0;
            }
            set => m_ManagedDebuggerFixedPort = value;
        }

        internal protected virtual bool explicitNullChecks
        {
            get => m_ExplicitNullChecks;
            set => m_ExplicitNullChecks = value;
        }

        internal protected virtual bool explicitDivideByZeroChecks
        {
            get => m_ExplicitDivideByZeroChecks;
            set => m_ExplicitDivideByZeroChecks = value;
        }

        internal protected virtual bool explicitArrayBoundsChecks
        {
            get => m_ExplicitArrayBoundsChecks;
            set => m_ExplicitArrayBoundsChecks = value;
        }

        internal virtual Compression compressionType
        {
            get => m_CompressionType;
            set => m_CompressionType = value;
        }

        internal protected virtual bool installInBuildFolder
        {
            get => m_InstallInBuildFolder;
            set => m_InstallInBuildFolder = value;
        }

        /// <summary>
        /// Set platform setting based on strings for name and value. Native
        /// calls this to keep build profiles and EditorUserBuildSettings
        /// PlatformSettings dictionary in sync for backward compatibility.
        /// </summary>
        public virtual void SetRawPlatformSetting(string name, string value)
        {
            switch (name)
            {
                case k_SettingWaitForManagedDebugger:
                    waitForManagedDebugger = value.ToString().ToLower() == "true";
                    break;
                case k_SettingManagedDebuggerFixedPort:
                {
                    if (Int32.TryParse(value, out int intValue))
                    {
                        if (0 < intValue && intValue <= k_MaxPortNumber)
                        {
                            managedDebuggerFixedPort = intValue;
                            break;
                        }
                    }
                    managedDebuggerFixedPort = 0;
                    break;
                }
            }
        }

        /// <summary>
        /// Get platform setting value based on its name. Native calls this to
        /// keep build profiles and EditorUserBuildSettings PlatformSettings
        /// dictionary settings in sync for backward compatibility.
        /// </summary>
        public virtual string GetRawPlatformSetting(string name)
        {
            if (name.Equals(k_SettingWaitForManagedDebugger))
            {
                return waitForManagedDebugger.ToString().ToLower();
            }
            else if (name.Equals(k_SettingManagedDebuggerFixedPort))
            {
                return managedDebuggerFixedPort.ToString();
            }

            return null;
        }

        /// <summary>
        /// Specify if a shared setting for a platform is enabled to determine
        /// if native should fetch the value from the active profile or the shared
        /// profile.
        /// </summary>
        public virtual bool IsSharedSettingEnabled(string name)
        {
            return name switch
            {
                k_SettingDevelopment => true,
                k_SettingConnectProfiler => true,
                k_SettingBuildWithDeepProfilingSupport => true,
                k_SettingAllowDebugging => true,
                k_SettingWaitForManagedDebugger => true,
                k_SettingManagedDebuggerFixedPort => false,
                k_SettingExplicitNullChecks => false,
                k_SettingExplicitDivideByZeroChecks => false,
                k_SettingExplicitArrayBoundsChecks => false,
                k_SettingInstallInBuildFolder => true,
                _ => false,
            };
        }

        /// <summary>
        /// Set platform-specific shared settings based on strings for name and value.
        /// The shared profile calls this to sync platform-specific shared settings
        /// across relevant platforms.
        /// </summary>
        public virtual void SetSharedSetting(string name, string value)
        {
        }

        /// <summary>
        /// Get platform-specific shared settings based on strings for name. Most shared
        /// settings don't need to be in it. Use it only in rare cases where a platform-specific
        /// shared setting need to be fetched outside of its platform module.
        /// </summary>
        public virtual string GetSharedSetting(string name)
        {
            return null;
        }

        /// <summary>
        /// Get last path of a runnable build for this build profile
        /// </summary>
        public virtual string GetLastRunnableBuildPathKey()
        {
            return string.Empty;
        }
    }
}
