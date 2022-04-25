// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class EditorGameServiceExtension : IWindowCreatedHandler, IPackageSelectionChangedHandler
    {
        public interface ICloudProjectSettings
        {
            string organizationKey { get; }
            string projectId { get; }
        }

        internal class CloudProjectSettings : ICloudProjectSettings
        {
            public string organizationKey => UnityEditor.CloudProjectSettings.organizationKey;
            public string projectId => UnityEditor.CloudProjectSettings.projectId;
        }

        public const string defaultServiceGroupingName = "Others";
        const string k_GroupIdField = "groupId";
        const string k_GameService = "gameService";
        const string k_ConfigurePath = "configurePath";
        const string k_UseCasesUrl = "useCasesUrl";
        const string k_GenericDashboardUrl = "genericDashboardUrl";
        const string k_ProjectDashboardUrl = "projectDashboardUrl";
        const string k_ProjectDashboardUrlType = "projectDashboardUrlType";
        const string k_OrganizationKeyAndProjectGuid = "OrganizationKeyAndProjectGuid";
        const string k_OrganizationKey = "OrganizationKey";
        const string k_ProjectGuid = "ProjectGuid";
        const string k_PageContentType = "packages and features";
        internal const string k_ServicesConfigPath = "Resources/services.json";
        internal const int k_ServicesPriority = 200;

        internal static Dictionary<string, int> groupIndexes = new Dictionary<string, int>();
        internal static Dictionary<string, string> groupNames = new Dictionary<string, string>();
        static Dictionary<string, string> s_GroupMap = new Dictionary<string, string>();
        internal static ICloudProjectSettings cloudProjectSettings = new CloudProjectSettings();

        internal static bool FilterServicesPackage(IPackage package)
        {
            return !string.IsNullOrWhiteSpace(GetServicesPackageGroupName(package));
        }

        internal static string GetServicesPackageGroupName(IPackage package)
        {
            var packageGameServiceField = GetLatestEditorGameServiceField(package);
            var hasServiceGroupingId = HasDynamicServiceGroupId(packageGameServiceField);
            if (hasServiceGroupingId)
            {
                return GetDynamicServiceGroupId(packageGameServiceField);
            }

            if (s_GroupMap == null || string.IsNullOrWhiteSpace(package.name) || !s_GroupMap.ContainsKey(package.name))
            {
                return string.Empty;
            }

            return s_GroupMap[package.name];
        }

        internal static bool HasDynamicServiceGroupId(Dictionary<string, object> packageGameServiceField)
        {
            return packageGameServiceField?.ContainsKey(k_GroupIdField) ?? false;
        }

        internal static string GetDynamicServiceGroupId(Dictionary<string, object> packageGameServiceField)
        {
            var key = (string)packageGameServiceField[k_GroupIdField];
            if (!groupNames.ContainsKey(key))
            {
                return defaultServiceGroupingName;
            }

            return groupNames[key];
        }

        internal static Dictionary<string, object> GetLatestEditorGameServiceField(IPackage package)
        {
            return GetEditorGameServiceField(package?.versions?.latest?.packageInfo);
        }

        internal static Dictionary<string, object> GetEditorGameServiceField(PackageInfo packageInfo)
        {
            var upmCache = ServicesContainer.instance.Resolve<UpmCache>();
            var upmReserved = upmCache.ParseUpmReserved(packageInfo);
            if (upmReserved == null)
                return null;
            if (!upmReserved.ContainsKey(k_GameService))
                return null;
            return upmReserved[k_GameService] as Dictionary<string, object>;
        }

        internal static int CompareGroup(string groupA, string groupB)
        {
            groupIndexes.TryGetValue(groupA, out var indexA);
            groupIndexes.TryGetValue(groupB, out var indexB);
            return indexA - indexB;
        }

        IPackageActionButton m_ConfigureButton;

        public void OnWindowCreated(WindowCreatedArgs args)
        {
            var configPath = Path.Combine(EditorApplication.applicationContentsPath, k_ServicesConfigPath);
            LoadServicesConfig(configPath);

            var pageManager = ServicesContainer.instance.Resolve<PageManager>();

            pageManager.AddSubPage(PackageFilterTab.UnityRegistry, "services", L10n.Tr("Services"), L10n.Tr(k_PageContentType), k_ServicesPriority,  FilterServicesPackage, GetServicesPackageGroupName, CompareGroup);
            pageManager.AddSubPage(PackageFilterTab.InProject, "services", L10n.Tr("Services"), L10n.Tr(k_PageContentType), k_ServicesPriority, FilterServicesPackage, GetServicesPackageGroupName, CompareGroup);

            m_ConfigureButton = args.window.AddPackageActionButton();
            m_ConfigureButton.text = L10n.Tr("Configure");
            m_ConfigureButton.action += OnConfigureClicked;
        }

        internal bool LoadServicesConfig(string configPath)
        {
            var configLoaded = false;

            if (File.Exists(configPath))
            {
                var jsonString = File.ReadAllText(configPath);
                try
                {
                    var configuration = JsonUtility.FromJson<ServicesTabConfiguration>(jsonString);
                    if (configuration != null)
                    {
                        configLoaded = true;
                        foreach (var serviceGrouping in configuration.serviceGroupings)
                        {
                            groupIndexes[serviceGrouping.name] = serviceGrouping.index;
                            groupNames[serviceGrouping.id] = serviceGrouping.name;
                            foreach (var package in serviceGrouping.packages)
                            {
                                s_GroupMap[package] = serviceGrouping.name;
                            }
                        }
                    }

                    SetDefaultGroupsAsLargestIndex();
                }
                catch (Exception)
                {
                    configLoaded = false;
                }
            }

            return configLoaded;
        }

        internal void SetDefaultGroupsAsLargestIndex()
        {
            if (!groupIndexes.ContainsKey(defaultServiceGroupingName))
            {
                var largestIndex = groupIndexes.Values.Max();
                groupIndexes[defaultServiceGroupingName] = largestIndex + 1;
            }
        }

        public void OnPackageSelectionChanged(PackageSelectionArgs args)
        {
            var packageInfo = args.packageVersion?.packageInfo;
            var editorGameService = GetEditorGameServiceField(packageInfo);
            var configurePath = GetConfigurePathField(editorGameService);
            m_ConfigureButton.visible = !string.IsNullOrEmpty(configurePath);
            m_ConfigureButton.enabled = args.packageVersion?.isInstalled ?? false;
        }

        private static void OnConfigureClicked(PackageSelectionArgs args)
        {
            var database = ServicesContainer.instance.Resolve<PackageDatabase>();
            var package = database.GetPackage(args.package.uniqueId);
            var installedVersion = package?.versions?.installed;
            if (installedVersion == null)
                return;

            var editorGameService = GetEditorGameServiceField(installedVersion.packageInfo);
            var configurePath = GetConfigurePathField(editorGameService);
            if (!string.IsNullOrEmpty(configurePath))
                PackageManagerWindowAnalytics.SendEvent("configureService", args.package.uniqueId);
            SettingsService.OpenProjectSettings(configurePath);
        }

        internal static string GetConfigurePathField(Dictionary<string, object> editorGameService)
        {
            if (editorGameService == null || !editorGameService.ContainsKey(k_ConfigurePath))
                return string.Empty;
            return editorGameService[k_ConfigurePath] as string;
        }

        public static string GetDashboardUrl(IPackageVersion version)
        {
            var editorGameService = GetEditorGameServiceField(version?.packageInfo);
            if (editorGameService == null)
                return string.Empty;

            if (editorGameService.ContainsKey(k_ProjectDashboardUrl))
            {
                var dashboardUrl = editorGameService[k_ProjectDashboardUrl] as string;

                if (!string.IsNullOrEmpty(dashboardUrl))
                {
                    if (editorGameService.ContainsKey(k_ProjectDashboardUrlType))
                    {
                        var dashboardUrlType = editorGameService[k_ProjectDashboardUrlType] as string;
                        switch (dashboardUrlType)
                        {
                            case k_OrganizationKeyAndProjectGuid:
                                if (!string.IsNullOrEmpty(cloudProjectSettings.projectId) &&
                                    !string.IsNullOrEmpty(cloudProjectSettings.organizationKey))
                                {
                                    return string.Format(dashboardUrl, cloudProjectSettings.organizationKey, cloudProjectSettings.projectId);
                                }
                                break;
                            case k_OrganizationKey:
                                if (!string.IsNullOrEmpty(cloudProjectSettings.organizationKey))
                                {
                                    return string.Format(dashboardUrl, cloudProjectSettings.organizationKey);
                                }
                                break;
                            case k_ProjectGuid:
                                if (!string.IsNullOrEmpty(cloudProjectSettings.projectId))
                                {
                                    return string.Format(dashboardUrl, cloudProjectSettings.projectId);
                                }
                                break;
                        }
                    }
                }
            }

            if (editorGameService.ContainsKey(k_GenericDashboardUrl))
                return editorGameService[k_GenericDashboardUrl] as string;

            return string.Empty;
        }

        public static string GetUseCasesUrl(IPackageVersion version)
        {
            var editorGameService = GetEditorGameServiceField(version?.packageInfo);
            if (editorGameService == null || !editorGameService.ContainsKey(k_UseCasesUrl))
                return string.Empty;
            return editorGameService[k_UseCasesUrl] as string;
        }
    }
}
