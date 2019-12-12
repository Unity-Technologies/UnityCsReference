// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

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
        string m_CloudBuildDeploymentUrl;
        string m_CloudBuildTargetUrl;
        string m_CloudBuildApiUrl;
        string m_CloudBuildApiProjectUrl;
        string m_CloudBuildApiStatusUrl;

        string m_CloudDiagnosticsUrl;
        string m_CloudDiagUserReportingSdkUrl;
        string m_CollabDashboardUrl;
        string m_PurchasingDashboardUrl;
        string m_AnalyticsDashboardUrl;

        public enum ServerEnvironment
        {
            Production,
            Development,
            Staging,
            Custom,
        }

        static ServicesConfiguration()
        {
            k_Instance = new ServicesConfiguration();
        }

        ServicesConfiguration()
        {
            //todo load configurations here: from file most likely... scriptable object within asset?
            var apiUrl = "https://core.cloud.unity3d.com/api";
            var organizationApiUrl = apiUrl + "/orgs/{0}";
            var usersApiUrl = apiUrl + "/users";
            m_CurrentUserApiUrl = usersApiUrl + "/me";
            m_ProjectsApiUrl = organizationApiUrl + "/projects";
            var projectApiUrl = m_ProjectsApiUrl + "/{1}";
            m_ProjectCoppaApiUrl = projectApiUrl + "/coppa";
            m_ProjectUsersApiUrl = projectApiUrl + "/users";
            m_ProjectDashboardUrl = "https://core.cloud.unity3d.com/orgs/{0}/projects/{1}";
            cloudHubServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/hub";
            m_CloudUsageDashboardUrl = "/usage";
            m_UnityTeamUrl = L10n.Tr("https://unity3d.com/teams"); // Should be https://unity3d.com/fr/teams in French !

            m_CloudBuildTutorialUrl = "https://unity3d.com/learn/tutorials/topics/cloud-build-0";
            m_CloudBuildProjectUrl = "https://developer.cloud.unity3d.com/build/orgs/{0}/projects/{1}";
            m_CloudBuildAddTargetUrl = "/setup/platform";

            m_CloudBuildDeploymentUrl = m_CloudBuildProjectUrl + "/deployments";
            m_CloudBuildTargetUrl = m_CloudBuildProjectUrl + "/buildtargets";
            m_CloudBuildApiUrl = "https://build-api.cloud.unity3d.com";
            m_CloudBuildApiProjectUrl = m_CloudBuildApiUrl + "/api/v1/projects/{0}";
            m_CloudBuildApiStatusUrl = m_CloudBuildApiUrl + "/api/v1/status";

            m_CloudDiagnosticsUrl = "https://unitytech.github.io/clouddiagnostics/";
            m_CloudDiagUserReportingSdkUrl = "https://userreporting.cloud.unity3d.com/api/userreporting/sdk";
            m_CloudDiagCrashesDashboardUrl = "https://developer.cloud.unity3d.com/diagnostics/orgs/{0}/projects/{1}/crashes";
            m_CollabDashboardUrl = "https://developer.cloud.unity3d.com/collab/orgs/{0}/projects/{1}/assets/";
            m_PurchasingDashboardUrl = "https://analytics.cloud.unity3d.com/projects/{0}/purchasing/";
            m_AnalyticsDashboardUrl = "https://analytics.cloud.unity3d.com/events/{0}/";
            PrepareAdsEnvironment(ServerEnvironment.Production);
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

        public static ServicesConfiguration instance => k_Instance;

        public string cloudHubServiceUrl { get; }

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

        public string GetCurrentCloudBuildProjectDeploymentUrl()
        {
            return GetCloudBuildProjectDeploymentUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId);
        }

        public string GetCloudBuildProjectDeploymentUrl(string organizationId, string projectId)
        {
            return string.Format(m_CloudBuildDeploymentUrl, organizationId, projectId);
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
    }
}
