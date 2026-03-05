// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.QuickInstall
{
    [InitializeOnLoad]
    internal class QuickInstaller
    {
        enum InitializeState
        {
            NotStarted,
            InProgress,
            Completed
        }

        static InitializeState s_Initialized = InitializeState.NotStarted;
        static readonly Dictionary<string, QuickInstaller> s_PackageManagerTrackedInstallers = new();
        static readonly Dictionary<string, QuickInstaller> s_AssemblyTrackedInstallers = new();

        readonly QuickInstallConfig m_Config;
        InstallMethod? m_InstallMethod;
        QuickInstallMenuItem m_MenuItem;
        bool m_InstallDetected = false;
        bool m_InstallRecorded
        {
            get => EditorUserSettings.GetConfigValue($"QuickInstaller_{m_Config.packageId}_installRecorded") == "True";
            set => EditorUserSettings.SetConfigValue($"QuickInstaller_{m_Config.packageId}_installRecorded", value.ToString());
        }

        static QuickInstaller()
        {
            if (s_Initialized != InitializeState.NotStarted)
                return;
            
            s_Initialized = InitializeState.InProgress;
            EditorApplication.update += CreatePackageListHandler();
            PackageManager.Events.registeredPackages += OnPackagesRegistered;
        }

        internal static void InstallPackage(string packageId, InstallMethod installationMethod)
        {
            s_PackageManagerTrackedInstallers[packageId].DeferInstallMethod(installationMethod);
            Client.Add(packageId);
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
                return;
            
            DetectPackagesFromPackageManager(request);
            DetectPackagesFromLoadedAssemblies();
            foreach (QuickInstaller installer in s_PackageManagerTrackedInstallers.Values)
                installer.Process();
            
            s_Initialized = InitializeState.Completed;
            EditorApplication.update -= registeredCallback;
            SettingsService.NotifySettingsProviderChanged();
        }

        static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            if (HasUnprocessedChanges(args.added, true) || HasUnprocessedChanges(args.removed, false))
                EditorApplication.update += CreatePackageListHandler();
        }

        static void DetectPackagesFromPackageManager(ListRequest request)
        {
            if (request.Status != StatusCode.Success)
                return;

            foreach (var packageInfo in request.Result)
            {
                if (s_PackageManagerTrackedInstallers.TryGetValue(packageInfo.name, out var target))
                {
                    target.m_InstallMethod = target.GetAndClearDeferredInstallMethod() ?? InstallMethod.PackageManager;
                    target.m_InstallDetected = true;
                }
            }
        }

        static void DetectPackagesFromLoadedAssemblies()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in loadedAssemblies)
            {
                if (s_AssemblyTrackedInstallers.TryGetValue(assembly.GetName().Name, out var target))
                    target.m_InstallDetected = true;
            }
        }

        static bool HasUnprocessedChanges(ReadOnlyCollection<PackageManager.PackageInfo> packageInfos, bool expectedRecordedState)
        {
            foreach (var packageInfo in packageInfos)
            {
                s_PackageManagerTrackedInstallers.TryGetValue(packageInfo.packageId, out var installer);
                if (installer?.m_InstallRecorded != expectedRecordedState)
                    return true;
            }
            return false;
        }

        public QuickInstaller(QuickInstallConfig config)
        {
            m_Config = config;
            s_PackageManagerTrackedInstallers.Add(m_Config.packageId, this);
            if (!string.IsNullOrEmpty(m_Config.alternateInstallAssembly))
                s_AssemblyTrackedInstallers.Add(m_Config.alternateInstallAssembly, this);
        }

        public void SetupMenu()
        {
            m_MenuItem = new QuickInstallMenuItem(m_Config.packageId, m_Config.menuPath);
        }

        public SettingsProvider CreateSettingsProvider()
        {
            if (s_Initialized != InitializeState.Completed || m_InstallDetected)
                return null;
            
            try
            {
                return new QuickInstallSettingsProvider(m_Config.packageId, m_Config.settingsProviderConfig);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create installer provider for {m_Config.packageId} with exception: {e}");
                return null;
            }
        }

        void Process()
        {
            // Send Analytics when we detect an install state transition
            if (m_InstallDetected && !m_InstallRecorded)
                QuickInstallAnalytic.SendEvent(m_Config.analytic, m_InstallMethod ?? InstallMethod.Assets);
            
            // Update menus
            if (m_InstallDetected)
                m_MenuItem?.RemoveMenuItem();
            else
                m_MenuItem?.AddMenuItem();
            
            // Update recorded state
            m_InstallRecorded = m_InstallDetected;
        }

        void DeferInstallMethod(InstallMethod method)
        {
            SessionState.SetString($"QuickInstaller_{m_Config.packageId}_deferredInstallMethod", method.ToString());
        }

        InstallMethod? GetAndClearDeferredInstallMethod()
        {
            string key = $"QuickInstaller_{m_Config.packageId}_deferredInstallMethod";
            string value = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(value))
                return null;
            
            bool success = Enum.TryParse<InstallMethod>(value, out var deferred);
            SessionState.EraseString(key);
            return success ? deferred : null;
        }
    }
}
