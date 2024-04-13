// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using TargetAttributes = UnityEditor.BuildTargetDiscovery.TargetAttributes;

namespace UnityEditor.Build.Profile
{
    [Serializable]
    internal sealed class SharedPlatformSettings: BuildProfilePlatformSettingsBase
    {
        internal const string k_SettingWindowsDevicePortalAddress = "windowsDevicePortalAddress";
        internal const string k_SettingWindowsDevicePortalUsername = "windowsDevicePortalUsername";
        internal const string k_SettingWindowsDevicePortalPassword = "windowsDevicePortalPassword";
        internal const string k_SettingForceInstallation = "forceInstallation";
        internal const string k_SettingiOSXcodeBuildConfig = "iOSXcodeBuildConfig";
        internal const string k_SettingSymlinkSources = "symlinkSources";
        internal const string k_SettingPreferredXcode = "preferredXcode";
        internal const string k_SettingSymlinkTrampoline = "symlinkTrampoline";
        internal const string k_SettingRemoteDeviceInfo = "remoteDeviceInfo";
        internal const string k_SettingRemoteDeviceAddress = "remoteDeviceAddress";
        internal const string k_SettingRemoteDeviceUsername = "remoteDeviceUsername";
        internal const string k_SettingRemoteDeviceExports = "remoteDeviceExports";
        internal const string k_SettingPathOnRemoteDevice = "pathOnRemoteDevice";

        [SerializeField] string m_WindowsDevicePortalAddress = string.Empty;
        [SerializeField] string m_WindowsDevicePortalUsername = string.Empty;
        string m_WindowsDevicePortalPassword = string.Empty;
        [SerializeField] bool m_ForceInstallation = false;
        [SerializeField] XcodeBuildConfig m_iOSXcodeBuildConfig = XcodeBuildConfig.Release;
        [SerializeField] bool m_SymlinkSources = false;
        [SerializeField] string m_PreferredXcode = string.Empty;
        [SerializeField] bool m_SymlinkTrampoline = false;
        [SerializeField] bool m_RemoteDeviceInfo = false;
        [SerializeField] string m_RemoteDeviceAddress = string.Empty;
        [SerializeField] string m_RemoteDeviceUsername = string.Empty;
        [SerializeField] string m_RemoteDeviceExports = string.Empty;
        [SerializeField] string m_PathOnRemoteDevice = string.Empty;

        internal protected override bool development
        {
            get => base.development;
            set
            {
                if (base.development != value)
                {
                    base.development = value;
                    SyncSharedSettings(k_SettingDevelopment);
                }
            }
        }

        internal protected override bool connectProfiler
        {
            get => base.connectProfiler;
            set
            {
                if (base.connectProfiler != value)
                {
                    base.connectProfiler = value;
                    SyncSharedSettings(k_SettingConnectProfiler);
                }
            }
        }

        internal protected override bool buildWithDeepProfilingSupport
        {
            get => base.buildWithDeepProfilingSupport;
            set
            {
                if (base.buildWithDeepProfilingSupport != value)
                {
                    base.buildWithDeepProfilingSupport = value;
                    SyncSharedSettings(k_SettingBuildWithDeepProfilingSupport);
                }
            }
        }

        internal protected override bool allowDebugging
        {
            get => base.allowDebugging;
            set
            {
                if (base.allowDebugging != value)
                {
                    base.allowDebugging = value;
                    SyncSharedSettings(k_SettingAllowDebugging);
                }
            }
        }

        internal protected override bool waitForManagedDebugger
        {
            get => base.waitForManagedDebugger;
            set
            {
                if (base.waitForManagedDebugger != value)
                {
                    base.waitForManagedDebugger = value;
                    SyncSharedSettings(k_SettingWaitForManagedDebugger);
                }
            }
        }

        internal protected override int managedDebuggerFixedPort
        {
            get => base.managedDebuggerFixedPort;
            set
            {
                if (base.managedDebuggerFixedPort != value)
                {
                    base.managedDebuggerFixedPort = value;
                    SyncSharedSettings(k_SettingManagedDebuggerFixedPort);
                }
            }
        }

        internal protected override bool explicitNullChecks
        {
            get => base.explicitNullChecks;
            set
            {
                if (base.explicitNullChecks != value)
                {
                    base.explicitNullChecks = value;
                    SyncSharedSettings(k_SettingExplicitNullChecks);
                }
            }
        }

        internal protected override bool explicitDivideByZeroChecks
        {
            get => base.explicitDivideByZeroChecks;
            set
            {
                if (base.explicitDivideByZeroChecks != value)
                {
                    base.explicitDivideByZeroChecks = value;
                    SyncSharedSettings(k_SettingExplicitDivideByZeroChecks);
                }
            }
        }

        internal protected override bool explicitArrayBoundsChecks
        {
            get => base.explicitArrayBoundsChecks;
            set
            {
                if (base.explicitArrayBoundsChecks != value)
                {
                    base.explicitArrayBoundsChecks = value;
                    SyncSharedSettings(k_SettingExplicitArrayBoundsChecks);
                }
            }
        }

        internal override Compression compressionType
        {
            get => base.compressionType;
            set
            {
                if (base.compressionType != value)
                {
                    base.compressionType = value;
                    SyncSharedSettings(k_SettingCompressionType);
                }
            }
        }

        internal protected override bool installInBuildFolder
        {
            get => base.installInBuildFolder;
            set
            {
                if (base.installInBuildFolder != value)
                {
                    base.installInBuildFolder = value;
                    SyncSharedSettings(k_SettingInstallInBuildFolder);
                }
            }
        }

        public string windowsDevicePortalAddress
        {
            get => m_WindowsDevicePortalAddress;
            set
            {
                if (m_WindowsDevicePortalAddress != value)
                {
                    m_WindowsDevicePortalAddress = value;
                    SyncSharedSettings(k_SettingWindowsDevicePortalAddress);
                }
            }
        }

        public string windowsDevicePortalUsername
        {
            get =>  EditorUserBuildSettings.DecodeBase64(m_WindowsDevicePortalUsername);
            set
            {
                string encodedString = EditorUserBuildSettings.EncodeBase64(value);

                if (m_WindowsDevicePortalUsername != encodedString)
                {
                    m_WindowsDevicePortalUsername = EditorUserBuildSettings.EncodeBase64(value);
                    SyncSharedSettings(k_SettingWindowsDevicePortalUsername);
                }
            }
        }

        public string windowsDevicePortalPassword
        {
            get => EditorUserBuildSettings.DecodeBase64(m_WindowsDevicePortalPassword);
            set
            {
                string encodedString = EditorUserBuildSettings.EncodeBase64(value);

                if (m_WindowsDevicePortalPassword != encodedString)
                {
                    m_WindowsDevicePortalPassword = encodedString;
                    SyncSharedSettings(k_SettingWindowsDevicePortalPassword);
                }
            }
        }

        public bool forceInstallation
        {
            get => m_ForceInstallation;
            set
            {
                if (m_ForceInstallation != value)
                {
                    m_ForceInstallation = value;
                    SyncSharedSettings(k_SettingForceInstallation);
                }
            }
        }

        public XcodeBuildConfig iOSXcodeBuildConfig
        {
            get => m_iOSXcodeBuildConfig;
            set
            {
                if (m_iOSXcodeBuildConfig != value)
                {
                    m_iOSXcodeBuildConfig = value;
                    SyncSharedSettings(k_SettingiOSXcodeBuildConfig);
                }
            }
        }

        public bool symlinkSources
        {
            get => m_SymlinkSources;
            set
            {
                if (m_SymlinkSources != value)
                {
                    m_SymlinkSources = value;
                    SyncSharedSettings(k_SettingSymlinkSources);
                }
            }
        }

        public string preferredXcode
        {
            get => m_PreferredXcode;
            set
            {
                if (m_PreferredXcode != value)
                {
                    m_PreferredXcode = value;
                    SyncSharedSettings(k_SettingPreferredXcode);
                }
            }
        }

        public bool symlinkTrampoline
        {
            get => m_SymlinkTrampoline;
            set
            {
                if (m_SymlinkTrampoline != value)
                {
                    m_SymlinkTrampoline = value;
                    SyncSharedSettings(k_SettingSymlinkTrampoline);
                }
            }
        }

        public bool remoteDeviceInfo
        {
            get => m_RemoteDeviceInfo;
            set
            {
                if (m_RemoteDeviceInfo != value)
                {
                    m_RemoteDeviceInfo = value;
                    SyncSharedSettings(k_SettingRemoteDeviceInfo);
                }
            }
        }

        public string remoteDeviceAddress
        {
            get => m_RemoteDeviceAddress;
            set
            {
                if (m_RemoteDeviceAddress != value)
                {
                    m_RemoteDeviceAddress = value;
                    SyncSharedSettings(k_SettingRemoteDeviceAddress);
                }
            }
        }

        public string remoteDeviceUsername
        {
            get => m_RemoteDeviceUsername;
            set
            {
                if (m_RemoteDeviceUsername != value)
                {
                    m_RemoteDeviceUsername = value;
                    SyncSharedSettings(k_SettingRemoteDeviceUsername);
                }
            }
        }

        public string remoteDeviceExports
        {
            get => m_RemoteDeviceExports;
            set
            {
                if (m_RemoteDeviceExports != value)
                {
                    m_RemoteDeviceExports = value;
                    SyncSharedSettings(k_SettingRemoteDeviceExports);
                }
            }
        }

        public string pathOnRemoteDevice
        {
            get => m_PathOnRemoteDevice;
            set
            {
                if (m_PathOnRemoteDevice != value)
                {
                    m_PathOnRemoteDevice = value;
                    SyncSharedSettings(k_SettingPathOnRemoteDevice);
                }
            }
        }

        public override string GetSharedSetting(string name)
        {
            return name switch
            {
                k_SettingWindowsDevicePortalPassword => windowsDevicePortalPassword,
                _ => null,
            };
        }

        void SyncSharedSettings(string name)
        {
            var classicProfiles = BuildProfileContext.instance.classicPlatformProfiles;
            foreach (var profile in classicProfiles)
            {
                var platformSettingsBase = profile.platformBuildProfile;
                if (platformSettingsBase == null)
                    continue;

                switch (name)
                {
                    case k_SettingDevelopment:
                        platformSettingsBase.development = development;
                        break;
                    case k_SettingConnectProfiler:
                        platformSettingsBase.connectProfiler = connectProfiler;
                        break;
                    case k_SettingBuildWithDeepProfilingSupport:
                        platformSettingsBase.buildWithDeepProfilingSupport = buildWithDeepProfilingSupport;
                        break;
                    case k_SettingAllowDebugging:
                        platformSettingsBase.allowDebugging = allowDebugging;
                        break;
                    case k_SettingWaitForManagedDebugger:
                        platformSettingsBase.waitForManagedDebugger = waitForManagedDebugger;
                        break;
                    case k_SettingManagedDebuggerFixedPort:
                        platformSettingsBase.managedDebuggerFixedPort = managedDebuggerFixedPort;
                        break;
                    case k_SettingExplicitNullChecks:
                        platformSettingsBase.explicitNullChecks = explicitNullChecks;
                        break;
                    case k_SettingExplicitDivideByZeroChecks:
                        platformSettingsBase.explicitDivideByZeroChecks = explicitDivideByZeroChecks;
                        break;
                    case k_SettingExplicitArrayBoundsChecks:
                        platformSettingsBase.explicitArrayBoundsChecks = explicitArrayBoundsChecks;
                        break;
                    case k_SettingCompressionType:
                    {
                        var isStandalone = BuildProfileModuleUtil.IsStandalonePlatform(profile.buildTarget);
                        if (isStandalone)
                        {
                            platformSettingsBase.compressionType = compressionType;
                        }
                        break;
                    }
                    case k_SettingInstallInBuildFolder:
                        platformSettingsBase.installInBuildFolder = installInBuildFolder;
                        break;
                    case k_SettingWindowsDevicePortalAddress:
                        platformSettingsBase.SetSharedSetting(k_SettingWindowsDevicePortalAddress, windowsDevicePortalAddress);
                        break;
                    case k_SettingWindowsDevicePortalUsername:
                        platformSettingsBase.SetSharedSetting(k_SettingWindowsDevicePortalUsername, windowsDevicePortalUsername);
                        break;
                    case k_SettingWindowsDevicePortalPassword:
                        platformSettingsBase.SetSharedSetting(k_SettingWindowsDevicePortalPassword, windowsDevicePortalPassword);
                        break;
                    case k_SettingForceInstallation:
                        platformSettingsBase.SetSharedSetting(k_SettingForceInstallation, forceInstallation.ToString().ToLower());
                        break;
                    case k_SettingiOSXcodeBuildConfig:
                        platformSettingsBase.SetSharedSetting(k_SettingiOSXcodeBuildConfig, iOSXcodeBuildConfig.ToString());
                        break;
                    case k_SettingSymlinkSources:
                        platformSettingsBase.SetSharedSetting(k_SettingSymlinkSources, symlinkSources.ToString().ToLower());
                        break;
                    case k_SettingPreferredXcode:
                        platformSettingsBase.SetSharedSetting(k_SettingPreferredXcode, preferredXcode);
                        break;
                    case k_SettingSymlinkTrampoline:
                        platformSettingsBase.SetSharedSetting(k_SettingSymlinkTrampoline, symlinkTrampoline.ToString().ToLower());
                        break;
                    case k_SettingRemoteDeviceInfo:
                        platformSettingsBase.SetSharedSetting(k_SettingRemoteDeviceInfo, remoteDeviceInfo.ToString().ToLower());
                        break;
                    case k_SettingRemoteDeviceAddress:
                        platformSettingsBase.SetSharedSetting(k_SettingRemoteDeviceAddress, remoteDeviceAddress);
                        break;
                    case k_SettingRemoteDeviceUsername:
                        platformSettingsBase.SetSharedSetting(k_SettingRemoteDeviceUsername, remoteDeviceUsername);
                        break;
                    case k_SettingRemoteDeviceExports:
                        platformSettingsBase.SetSharedSetting(k_SettingRemoteDeviceExports, remoteDeviceExports);
                        break;
                    case k_SettingPathOnRemoteDevice:
                        platformSettingsBase.SetSharedSetting(k_SettingPathOnRemoteDevice, pathOnRemoteDevice);
                        break;
                }
            }
        }

        // <summary>
        // Copy shared settings to a build profile.
        // </summary>
        public void CopySharedSettingsToBuildProfile(BuildProfile profile)
        {
            if (profile == null)
                return;

            var platformSettings = profile.platformBuildProfile;
            if (platformSettings == null)
                return;

            platformSettings.development = development;
            platformSettings.connectProfiler = connectProfiler;
            platformSettings.buildWithDeepProfilingSupport = buildWithDeepProfilingSupport;
            platformSettings.allowDebugging = allowDebugging;
            platformSettings.waitForManagedDebugger = waitForManagedDebugger;
            platformSettings.managedDebuggerFixedPort = managedDebuggerFixedPort;
            platformSettings.explicitNullChecks = explicitNullChecks;
            platformSettings.explicitDivideByZeroChecks = explicitDivideByZeroChecks;
            platformSettings.explicitArrayBoundsChecks = explicitArrayBoundsChecks;
            if (BuildProfileModuleUtil.IsStandalonePlatform(profile.buildTarget))
                platformSettings.compressionType = compressionType;
            platformSettings.installInBuildFolder = installInBuildFolder;
            platformSettings.SetSharedSetting(k_SettingWindowsDevicePortalAddress, windowsDevicePortalAddress);
            platformSettings.SetSharedSetting(k_SettingWindowsDevicePortalUsername, windowsDevicePortalUsername);
            platformSettings.SetSharedSetting(k_SettingWindowsDevicePortalPassword, windowsDevicePortalPassword);
            platformSettings.SetSharedSetting(k_SettingForceInstallation, forceInstallation.ToString().ToLower());
            platformSettings.SetSharedSetting(k_SettingiOSXcodeBuildConfig, iOSXcodeBuildConfig.ToString());
            platformSettings.SetSharedSetting(k_SettingSymlinkSources, symlinkSources.ToString().ToLower());
            platformSettings.SetSharedSetting(k_SettingPreferredXcode, preferredXcode);
            platformSettings.SetSharedSetting(k_SettingSymlinkTrampoline, symlinkTrampoline.ToString().ToLower());
            platformSettings.SetSharedSetting(k_SettingRemoteDeviceInfo, remoteDeviceInfo.ToString().ToLower());
            platformSettings.SetSharedSetting(k_SettingRemoteDeviceAddress, remoteDeviceAddress);
            platformSettings.SetSharedSetting(k_SettingRemoteDeviceUsername, remoteDeviceUsername);
            platformSettings.SetSharedSetting(k_SettingRemoteDeviceExports, remoteDeviceExports);
            platformSettings.SetSharedSetting(k_SettingPathOnRemoteDevice, pathOnRemoteDevice);
        }
    }
}
