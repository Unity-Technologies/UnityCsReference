// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;

namespace UnityEditor.Connect
{
    /// <summary>
    /// Singleton class used to expose common services configurations
    /// </summary>
    [InitializeOnLoad]
    internal sealed class ServicesConfiguration
    {
        static readonly ServicesConfiguration k_Instance;

        string m_CurrentUserApiUrl;
        string m_ProjectsApiUrl;
        string m_ProjectCoppaApiUrl;
        string m_ProjectUsersApiUrl;
        string m_ProjectDashboardUrl;
        string m_CloudUsageDashboardUrl;
        string m_CloudDiagCrashesDashboardUrl;
        string m_UnityTeamUrl;
        string m_CloudBuildTutorialUrl;
        string m_CloudBuildProjectUrl;
        string m_CloudBuildAddTargetUrl;
        string m_CloudBuildUploadUrl;
        string m_CloudBuildTargetUrl;
        string m_CloudBuildApiUrl;
        string m_CloudBuildApiProjectUrl;
        string m_CloudBuildApiStatusUrl;

        string m_CloudDiagnosticsUrl;
        string m_CloudDiagUserReportingSdkUrl;
        string m_CollabDashboardUrl;
        string m_PurchasingDashboardUrl;
        string m_AnalyticsDashboardUrl;

        public const string cdnConfigUri = "public-cdn.cloud.unity3d.com";
        const string k_CdnConfigUrl = "https://" + cdnConfigUri + "/config/{0}";

        Dictionary<string, string> m_ServicesUrlsConfig = new Dictionary<string, string>();

        public enum ServerEnvironment
        {
            Production,
            Development,
            Staging,
            Custom,
        }

        static ServicesConfiguration()
        {
            if (k_Instance == null)
            {
                k_Instance = new ServicesConfiguration();
            }
        }

        ServicesConfiguration()
        {
            //We load configurations from most fallback to most relevant.
            //So we always have some fallback entries if a config only overwrite some of the URLs.
            LoadDefaultConfigurations();
            LoadConfigurationsFromLocalConfigFile();
            LoadConfigurationsFromCdn();
        }

        void BuildPaths()
        {
            var apiUrl = m_ServicesUrlsConfig["core"] + "/api";
            m_CurrentUserApiUrl = apiUrl + "/users/me";
            m_ProjectsApiUrl = apiUrl + "/orgs/{0}/projects";
            var projectApiUrl = m_ProjectsApiUrl + "/{1}";
            m_ProjectCoppaApiUrl = projectApiUrl + "/coppa";
            m_ProjectUsersApiUrl = projectApiUrl + "/users";
            m_ProjectDashboardUrl = m_ServicesUrlsConfig["core"] + "/orgs/{0}/projects/{1}";
            cloudHubServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/hub";
            m_CloudUsageDashboardUrl = "/usage";
            m_UnityTeamUrl = L10n.Tr("https://unity3d.com/teams"); // Should be https://unity3d.com/fr/teams in French !

            m_CloudBuildTutorialUrl = "https://unity3d.com/learn/tutorials/topics/cloud-build-0";
            m_CloudBuildProjectUrl = m_ServicesUrlsConfig["build"] + "/build/orgs/{0}/projects/{1}";
            m_CloudBuildAddTargetUrl = "/setup/platform";

            m_CloudBuildUploadUrl = m_CloudBuildProjectUrl + "/upload/?page=1";
            m_CloudBuildTargetUrl = m_CloudBuildProjectUrl + "/buildtargets";
            m_CloudBuildApiUrl = m_ServicesUrlsConfig["build-api"];
            m_CloudBuildApiProjectUrl = m_CloudBuildApiUrl + "/api/v1/projects/{0}";
            m_CloudBuildApiStatusUrl = m_CloudBuildApiUrl + "/api/v1/status";

            m_CloudDiagnosticsUrl = "https://unitytech.github.io/clouddiagnostics/";
            m_CloudDiagUserReportingSdkUrl = "https://userreporting.cloud.unity3d.com/api/userreporting/sdk";
            m_CloudDiagCrashesDashboardUrl = m_ServicesUrlsConfig["build"] + "/diagnostics/orgs/{0}/projects/{1}/crashes";
            m_CollabDashboardUrl = m_ServicesUrlsConfig["build"] + "/collab/orgs/{0}/projects/{1}/assets/";
            m_PurchasingDashboardUrl = m_ServicesUrlsConfig["analytics"] + "/projects/{0}/purchasing/";
            m_AnalyticsDashboardUrl = m_ServicesUrlsConfig["analytics"] + "/events/{0}/";
            PrepareAdsEnvironment(ConvertStringToServerEnvironment(UnityConnect.instance.GetEnvironment()));

            pathsReady = true;
        }

        void LoadDefaultConfigurations()
        {
            //Snapshot of https://public-cdn.cloud.unity3d.com/config/production taken on 2019-11-13, update periodically.
            //The fallback for CDN configs is the productionUrls.json file in default editor resources.
            //This hardcoded string is the fallback for the file.  So not very likely to ever be used.
            const string hardCodedConfigs = @"{""activation"":""https://activation.unity3d.com"",
                ""ads"":""https://unityads.unity3d.com/admin"",
                ""analytics"":""https://analytics.cloud.unity3d.com"",
                ""build"":""https://developer.cloud.unity3d.com"",
                ""build-api"":""https://build-api.cloud.unity3d.com"",
                ""cdp-analytics"":""https://prd-lender.cdp.internal.unity3d.com"",
                ""clouddata"":""https://data.cloud.unity3d.com"",
                ""collab"":""https://collab.cloud.unity3d.com"",
                ""collab-accelerator"":""https://collab-accelerator.cloud.unity3d.com"",
                ""collab-max-files-per-commit"":10000,
                ""commenting"":""https://commenting.cloud.unity3d.com"",
                ""core"":""https://core.cloud.unity3d.com"",
                ""coreembedded"":""https://embedded.cloud.unity3d.com"",
                ""coreui"":""https://developer.cloud.unity3d.com"",
                ""genesis_api_url"":""https://api.unity.com"",
                ""genesis_service_url"":""https://id.unity.com"",
                ""identity"":""https://api.unity.com"",
                ""portal"":""https://id.unity.com"",
                ""jump"":""https://jump.cloud.unity3d.com"",
                ""license"":""https://license.unity3d.com"",
                ""sso"":true,
                ""activity-feed"":false,
                ""perf"":""https://perf.cloud.unity3d.com"",
                ""perf-events"":""https://a:b@perf-events.cloud.unity3d.com"",
                ""perf-max-batch-size"":""1"",
                ""perf-max-events"":""100"",
                ""perf-seconds-per-batch"":""3"",
                ""unauthenticatedurl"":""/landing"",
                ""unet"":""https://multiplayer.unity3d.com"",
                ""waitlist"":""https://developer.cloud.unity3d.com"",
                ""waitlist_api"":""https://collab-waiting-list.cloud.unity3d.com"",
                ""webauth"":""https://accounts.unity3d.com"",
                ""socialdashboard"":""https://dashboard.heyplayapp.com"",
                ""seat_required"":true,
                ""seatinfourl"":""/teams/learn-more"",
                ""build_upload_api_url"":""https://build-artifact-api.cloud.unity3d.com"",
                ""hub_installer_location"":""https://public-cdn.cloud.unity3d.com/hub/prod/"",
                ""hub-disable-marketing-tips"":false,
                ""asset_store_api"":""https://packages-v2.unity.com"",
                ""asset_store_url"":""https://assetstore.unity.com"",
                ""packman_key"":""6357C523886E813D1500408F05B0D7A6""}";
            LoadJsonConfiguration(hardCodedConfigs);
        }

        void LoadConfigurationsFromLocalConfigFile()
        {
            //Localfile is a snapshot of https://public-cdn.cloud.unity3d.com/config/production taken on 2019-11-13, update periodically.
            //This file is the first fallback for CDN configs is the productionUrls.json file in default editor resources.
            try
            {
                var jsonTextAsset = EditorResources.Load<TextAsset>("Configurations/ServicesWindow/productionUrls.json");
                LoadJsonConfiguration(jsonTextAsset.text);
            }
            catch (Exception)
            {
                //We fallback to hardcoded config
            }
        }

        void LoadConfigurationsFromCdn()
        {
            try
            {
                var getServicesUrlsRequest = new UnityWebRequest(string.Format(k_CdnConfigUrl, UnityConnect.instance.GetEnvironment()),
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                var operation = getServicesUrlsRequest.SendWebRequest();
                operation.completed += asyncOperation =>
                {
                    try
                    {
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(getServicesUrlsRequest))
                        {
                            LoadJsonConfiguration(getServicesUrlsRequest.downloadHandler.text);
                            BuildPaths();
                        }
                    }
                    catch (Exception)
                    {
                        //We fallback to local file config
                        BuildPaths();
                    }
                    finally
                    {
                        getServicesUrlsRequest.Dispose();
                    }
                };
            }
            catch (Exception)
            {
                //We fallback to local file config
                BuildPaths();
            }
        }

        void LoadJsonConfiguration(string json)
        {
            var jsonParser = new JSONParser(json);
            var parsedJson = jsonParser.Parse();
            var jsonConfigs = parsedJson.AsDict();
            foreach (var key in jsonConfigs.Keys)
            {
                if (!m_ServicesUrlsConfig.ContainsKey(key))
                {
                    m_ServicesUrlsConfig.Add(key, jsonConfigs[key].AsObject().ToString());
                }
                else
                {
                    m_ServicesUrlsConfig[key] = jsonConfigs[key].AsObject().ToString();
                }
            }
        }

        public void PrepareAdsEnvironment(ServerEnvironment environmentType)
        {
            adsGettingStartedUrl = "https://unityads.unity3d.com/help/index";
            adsLearnMoreUrl = "https://unityads.unity3d.com/help/monetization/getting-started";
            adsDashboardUrl = "https://operate.dashboard.unity3d.com/organizations/{0}/overview/revenue";

            switch (environmentType)
            {
                case ServerEnvironment.Production:
                    adsOperateApiUrl = "https://operate.dashboard.unity3d.com";
                    break;
                case ServerEnvironment.Development:
                    adsOperateApiUrl = "https://ads-selfserve.staging.unityads.unity3d.com";
                    break;
                case ServerEnvironment.Staging:
                    adsOperateApiUrl = "https://operate.staging.dashboard.unity3d.com";
                    break;
                case ServerEnvironment.Custom:
                    // Do something here !
                    break;
            }
        }

        public ServerEnvironment ConvertStringToServerEnvironment(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(value);
            }

            if (string.Equals(ServerEnvironment.Custom.ToString(), value, StringComparison.CurrentCultureIgnoreCase))
            {
                return ServerEnvironment.Custom;
            }
            if (string.Equals(ServerEnvironment.Staging.ToString(), value, StringComparison.CurrentCultureIgnoreCase))
            {
                return ServerEnvironment.Staging;
            }
            if (string.Equals(ServerEnvironment.Development.ToString(), value, StringComparison.CurrentCultureIgnoreCase))
            {
                return ServerEnvironment.Development;
            }
            //Always return prod as default environment
            return ServerEnvironment.Production;
        }

        public static ServicesConfiguration instance => k_Instance;

        public string cloudHubServiceUrl { get; internal set; }

        public bool pathsReady { get; internal set; }

        /// <summary>
        /// Gets the current user api url
        /// </summary>
        /// <returns>A url to query the current user data</returns>
        public string GetCurrentUserApiUrl()
        {
            return m_CurrentUserApiUrl;
        }

        /// <summary>
        /// Get the endpoint to retrieve the projects of the specified organization, create a new one, etc.
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <returns>A url endpoint to request projects</returns>
        public string GetOrganizationProjectsApiUrl(string organizationId)
        {
            return string.Format(m_ProjectsApiUrl, organizationId);
        }

        /// <summary>
        /// Gets the current native code project coppa url
        /// </summary>
        /// <returns>A url to reach the project coppa info</returns>
        public string GetCurrentProjectCoppaApiUrl()
        {
            return GetProjectCoppaApiUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        /// <summary>
        /// Gets a specified project coppa url
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <param name="projectId">A project's ID</param>
        /// <returns>A url to reach the project coppa info</returns>
        public string GetProjectCoppaApiUrl(string organizationId, string projectId)
        {
            return string.Format(m_ProjectCoppaApiUrl, organizationId, projectId);
        }

        /// <summary>
        /// Gets the current project users api url
        /// </summary>
        /// <returns>A url to reach the project users info</returns>
        public string GetCurrentProjectUsersApiUrl()
        {
            return GetProjectUsersApiUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        /// <summary>
        /// Gets a specified project users api url
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <param name="projectId">A project's ID</param>
        /// <returns>A url to reach the project users info</returns>
        public string GetProjectUsersApiUrl(string organizationId, string projectId)
        {
            return string.Format(m_ProjectUsersApiUrl, organizationId, projectId);
        }

        /// <summary>
        /// Gets the current project dashboard url
        /// </summary>
        /// <returns></returns>
        public string GetCurrentProjectDashboardUrl()
        {
            return GetProjectDashboardUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        /// <summary>
        /// Gets a specified project dashboard url
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <param name="projectId">A project's ID</param>
        /// <returns></returns>
        public string GetProjectDashboardUrl(string organizationId, string projectId)
        {
            return string.Format(m_ProjectDashboardUrl, organizationId, projectId);
        }

        // Return the specific Cloud Usage URL for the Collab service
        public string GetCloudUsageDashboardUrl()
        {
            return GetCurrentProjectDashboardUrl() + m_CloudUsageDashboardUrl;
        }

        // Return the specific Unity Teams information URL for the Collab service
        public string GetUnityTeamInfoUrl()
        {
            return m_UnityTeamUrl;
        }

        // Return the specific cloud build tutorial URL for the cloud build service
        public string GetCloudBuildTutorialUrl()
        {
            return m_CloudBuildTutorialUrl;
        }

        public string GetCloudBuildAddTargetUrl()
        {
            return GetCloudBuildCurrentProjectUrl() + m_CloudBuildAddTargetUrl;
        }

        public string GetCloudBuildCurrentProjectUrl()
        {
            return GetCloudBuildProjectsUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        public string GetCloudBuildProjectsUrl(string organizationId, string projectId)
        {
            return string.Format(m_CloudBuildProjectUrl, organizationId, projectId);
        }

        public string GetCurrentCloudBuildProjectUploadUrl()
        {
            return GetCloudBuildProjectUploadUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        public string GetCloudBuildProjectUploadUrl(string organizationId, string projectId)
        {
            return string.Format(m_CloudBuildUploadUrl, organizationId, projectId);
        }

        public string GetCurrentCloudBuildProjectTargetUrl()
        {
            return GetCloudBuildProjectTargetUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        public string GetCloudBuildProjectTargetUrl(string organizationId, string projectId)
        {
            return string.Format(m_CloudBuildTargetUrl, organizationId, projectId);
        }

        public string GetCurrentCloudBuildProjectHistoryUrl()
        {
            return GetCloudBuildProjectHistoryUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        public string GetCloudBuildProjectHistoryUrl(string organizationId, string projectId)
        {
            return string.Format(m_CloudBuildProjectUrl, organizationId, projectId);
        }

        public string GetCloudBuildApiCurrentProjectUrl()
        {
            return GetCloudBuildApiProjectUrl(UnityConnect.instance.projectInfo.projectId);
        }

        public string GetCloudBuildApiProjectUrl(string projectId)
        {
            return string.Format(m_CloudBuildApiProjectUrl, projectId);
        }

        public string GetCloudBuildApiUrl()
        {
            return m_CloudBuildApiUrl;
        }

        public string GetCloudBuildApiStatusUrl()
        {
            return m_CloudBuildApiStatusUrl;
        }

        // Return the cloud diagnostic URL
        public string GetUnityCloudDiagnosticInfoUrl()
        {
            return m_CloudDiagnosticsUrl;
        }

        public string GetUnityCloudDiagnosticUserReportingSdkUrl()
        {
            return m_CloudDiagUserReportingSdkUrl;
        }

        internal string baseDashboardUrl { get { return m_ProjectDashboardUrl; } }
        public string baseAnalyticsDashboardUrl { get { return m_AnalyticsDashboardUrl; } }
        public string baseCloudBuildDashboardUrl { get { return m_CloudBuildProjectUrl; } }
        public string baseCloudUsageDashboardUrl { get { return baseDashboardUrl + m_CloudUsageDashboardUrl; } }
        public string baseCloudDiagCrashesDashboardUrl { get { return m_CloudDiagCrashesDashboardUrl; } }
        public string baseCollabDashboardUrl { get { return m_CollabDashboardUrl; } }
        public string basePurchasingDashboardUrl { get { return m_PurchasingDashboardUrl; } }

        public string adsGettingStartedUrl { get; private set; }
        public string adsLearnMoreUrl { get; private set; }
        public string adsDashboardUrl { get; private set; }
        public string adsOperateApiUrl { get; private set; }
        public string cloudDiagCrashesDashboardUrl => string.Format(m_CloudDiagCrashesDashboardUrl, UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        public string collabDashboardUrl => string.Format(m_CollabDashboardUrl, UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        public string purchasingDashboardUrl => string.Format(m_PurchasingDashboardUrl, UnityConnect.instance.projectInfo.projectGUID);
        public string analyticsDashboardUrl => string.Format(m_AnalyticsDashboardUrl, UnityConnect.instance.projectInfo.projectGUID);
    }
}
