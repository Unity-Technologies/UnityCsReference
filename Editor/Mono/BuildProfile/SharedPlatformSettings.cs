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
        internal const string k_ForceInstallation = "forceInstallation";

        [SerializeField] string m_WindowsDevicePortalAddress = string.Empty;
        [SerializeField] string m_WindowsDevicePortalUsername = string.Empty;
        string m_WindowsDevicePortalPassword = string.Empty;
        [SerializeField] bool m_ForceInstallation = false;

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
            get => m_WindowsDevicePortalUsername;
            set
            {
                if (m_WindowsDevicePortalUsername != value)
                {
                    m_WindowsDevicePortalUsername = value;
                    SyncSharedSettings(k_SettingWindowsDevicePortalUsername);
                }
            }
        }

        public string windowsDevicePortalPassword
        {
            get => m_WindowsDevicePortalPassword;
            set
            {
                if (m_WindowsDevicePortalPassword != value)
                {
                    m_WindowsDevicePortalPassword = value;
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
                    SyncSharedSettings(k_ForceInstallation);
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
                    return;

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
                        var isStandalone = BuildTargetDiscovery.PlatformHasFlag(profile.buildTarget, TargetAttributes.IsStandalonePlatform);
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
                    case k_ForceInstallation:
                        platformSettingsBase.SetSharedSetting(k_ForceInstallation, forceInstallation.ToString().ToLower());
                        break;
                }
            }
        }
    }
}
