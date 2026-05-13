// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Assemblies;

namespace UnityEditor.QuickInstall
{
    /// <summary>
    /// Entry point for packages that want to surface install hooks in the Editor. Packages create instances of this
    /// class with a <see cref="QuickInstallConfig"/> that declares which UI surfaces (menu items, Project Settings
    /// pages) and analytics should be enabled. The QuickInstaller then automatically manages install detection,
    /// UI visibility, and event tracking on behalf of the package.
    /// </summary>
    [InitializeOnLoad]
    internal class QuickInstaller
    {
        enum InitializeState
        {
            NotStarted,
            InProgress,
            Completed,
            Failed
        }

        static InitializeState s_Initialized = InitializeState.NotStarted;
        static readonly Dictionary<string, QuickInstaller> s_InstallersByPackageName = new();
        static readonly Dictionary<string, QuickInstaller> s_InstallersByAssemblyName = new();
        static IEnumerable<QuickInstaller> s_Installers => s_InstallersByPackageName.Values;

        readonly QuickInstallConfig m_Config;
        QuickInstallMenuItem m_MenuItem;
        QuickInstallSettingsProvider m_SettingsProvider;
        QuickInstallAnalytic m_Analytic;
        InstallMethod m_InstallMethod;
        string m_InstallVersion;
        bool m_InstallDetected;
        bool m_InstallRecorded
        {
            get => EditorUserSettings.GetConfigValue($"QuickInstaller_{m_Config.PackageName}_installRecorded") == "True";
            set => EditorUserSettings.SetConfigValue($"QuickInstaller_{m_Config.PackageName}_installRecorded", value.ToString());
        }

        static QuickInstaller()
        {
            if (s_Initialized != InitializeState.NotStarted)
                return;
            
            s_Initialized = InitializeState.InProgress;
            EditorApplication.update += CreatePackageListHandler();
            PackageManager.Events.registeredPackages += OnPackagesRegistered;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveMenuItemsForAllInstallers;
        }

        internal static AddRequest InstallPackage(string packageName, InstallMethod installationMethod)
        {
            Debug.Assert(s_InstallersByPackageName.ContainsKey(packageName), $"No QuickInstaller registered for package {packageName}");
            s_InstallersByPackageName[packageName].DeferInstallMethod(installationMethod);
            return PackageManager.Client.Add(packageName);
        }

        [SettingsProviderGroup]
        static SettingsProvider[] GetSettingsProviders()
        {
            var providers = new List<SettingsProvider>();
            if (s_Initialized != InitializeState.Completed)
                return providers.ToArray();
            
            foreach (var installer in s_Installers)
            {
                if (!installer.m_InstallDetected && installer.m_SettingsProvider != null)
                    providers.Add(installer.m_SettingsProvider);
            }
            return providers.ToArray();
        }

        static void ProcessInstallStateForAllInstallers()
        {
            foreach (var installer in s_Installers)
            {
                installer.SendAnalytics();
                installer.UpdateMenuItems();
                installer.RecordState();
            }
        }

        static void RemoveMenuItemsForAllInstallers()
        {
            foreach (var installer in s_Installers)
                installer.m_MenuItem?.RemoveMenuItem();
        }

        static EditorApplication.CallbackFunction CreatePackageListHandler()
        {
            var request = Client.List(true);
            EditorApplication.CallbackFunction callback = null;
            callback = () => PackageListHandler(request, callback);
            return callback;
        }

        static void PackageListHandler(ListRequest request, EditorApplication.CallbackFunction registeredCallback)
        {
            if (!request.IsCompleted)
            {
                return;
            }
            EditorApplication.update -= registeredCallback;
            if (request.Status == StatusCode.Success)
            {
                DetectPackagesFromLoadedAssemblies();
                DetectPackagesFromPackageManager(request);
                ProcessInstallStateForAllInstallers();
                s_Initialized = InitializeState.Completed;
            }
            else
            {
                RemoveMenuItemsForAllInstallers();
                s_Initialized = InitializeState.Failed;
            }
            SettingsService.NotifySettingsProviderChanged();
        }

        static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            if (HasUnprocessedChanges(args.added, true) || HasUnprocessedChanges(args.removed, false))
                EditorApplication.update += CreatePackageListHandler();
        }

        static void DetectPackagesFromPackageManager(ListRequest request)
        {
            foreach (var packageInfo in request.Result)
            {
                if (s_InstallersByPackageName.TryGetValue(packageInfo.name, out var target))
                {
                    target.m_InstallDetected = true;
                    target.m_InstallMethod = target.GetAndClearDeferredInstallMethod() ?? InstallMethod.PackageManager;
                    target.m_InstallVersion = packageInfo.version;
                }
            }
        }

        static void DetectPackagesFromLoadedAssemblies()
        {
            var loadedAssemblies = CurrentAssemblies.GetLoadedAssemblies();
            foreach (var assembly in loadedAssemblies)
            {
                if (s_InstallersByAssemblyName.TryGetValue(assembly.GetName().Name, out var target))
                {
                    // Don't overwrite install state from assembly detection
                    if (target.m_InstallDetected)
                        continue;
                    
                    target.m_InstallDetected = true;
                    target.m_InstallMethod = InstallMethod.Assets;
                    target.m_InstallVersion = assembly.GetName().Version?.ToString() ?? "Unknown";
                }
            }
        }

        static bool HasUnprocessedChanges(ReadOnlyCollection<PackageManager.PackageInfo> packageInfos, bool expectedRecordedState)
        {
            foreach (var packageInfo in packageInfos)
            {
                s_InstallersByPackageName.TryGetValue(packageInfo.name, out var installer);
                if (installer != null && installer.m_InstallRecorded != expectedRecordedState)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Creates a <see cref="QuickInstaller"/> and registers it for the package specified in
        /// <paramref name="config"/>. Sub-configurations that are omitted (left null) disable the
        /// corresponding feature — for example, passing no <see cref="MenuConfig"/> means no menu item
        /// will be created for the package.
        /// </summary>
        /// <param name="config">The configuration describing the package and its UI surfaces.</param>
        internal QuickInstaller(QuickInstallConfig config)
        {
            m_Config = config;
            s_InstallersByPackageName.Add(m_Config.PackageName, this);
            s_InstallersByAssemblyName.Add(m_Config.Assembly, this);

            m_SettingsProvider = m_Config.SettingsPageConfig != null
                ? new QuickInstallSettingsProvider(m_Config.PackageName, m_Config.SettingsPageConfig)
                : null;
            m_MenuItem = m_Config.MenuConfig != null
                ? new QuickInstallMenuItem(m_Config.PackageName, m_Config.MenuConfig)
                : null;
            m_Analytic = m_Config.AnalyticConfig != null
                ? new QuickInstallAnalytic(m_Config.PackageName, m_Config.AnalyticConfig)
                : null;
        }

        void UpdateMenuItems()
        {
            if (m_InstallDetected)
                m_MenuItem?.RemoveMenuItem();
            else
                m_MenuItem?.AddMenuItem();
        }

        void SendAnalytics()
        {
            // Send Analytics when we detect an install state transition
            if (m_InstallDetected && !m_InstallRecorded)
            {
                UnityEngine.Debug.Assert(m_InstallMethod != InstallMethod.Unknown, "Installation method should have been set when install was detected");
                m_Analytic?.SendEvent(m_InstallVersion, m_InstallMethod);
            }
        }

        void RecordState()
        {
            m_InstallRecorded = m_InstallDetected;
        }

        void DeferInstallMethod(InstallMethod method)
        {
            SessionState.SetString($"QuickInstaller_{m_Config.PackageName}_deferredInstallMethod", method.ToString());
        }

        InstallMethod? GetAndClearDeferredInstallMethod()
        {
            string key = $"QuickInstaller_{m_Config.PackageName}_deferredInstallMethod";
            string value = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(value))
                return null;
            
            bool success = Enum.TryParse<InstallMethod>(value, out var deferred);
            SessionState.EraseString(key);
            return success ? deferred : null;
        }
    }
}
