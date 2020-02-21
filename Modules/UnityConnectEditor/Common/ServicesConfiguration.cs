// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    internal sealed class ServicesConfiguration
    {
        static readonly ServicesConfiguration k_Instance;
        const string k_CloudHubServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/hub";
        const string k_CloudUsageDashboardUrl = "/usage";
        const string k_CloudBuildAddTargetUrl = "/setup/platform";
        const string k_CloudBuildTutorialUrl = "https://unity3d.com/learn/tutorials/topics/cloud-build-0";
        const string k_CloudDiagnosticsUrl = "https://unitytech.github.io/clouddiagnostics/";
        const string k_CloudDiagUserReportingSdkUrl = "https://userreporting.cloud.unity3d.com/api/userreporting/sdk";
        const int k_NoProgressId = -1;
        const int k_NoProgressAmount = -1;
        const string k_ProgressTitle = "Connecting to services url";
        const string k_ConfigJsonSessionStateKey = "UnityServiceConfig::ConfigJson";

        string m_CurrentUserApiUrl;
        string m_ProjectsApiUrl;
        string m_ProjectApiUrl;
        string m_ProjectCoppaApiUrl;
        string m_ProjectUsersApiUrl;
        string m_ProjectDashboardUrl;
        string m_ProjectServiceFlagsApiUrl;
        string m_CloudDiagCrashesDashboardUrl;
        string m_UnityTeamUrl;

        string m_CloudBuildProjectUrl;
        string m_CloudBuildUploadUrl;
        string m_CloudBuildTargetUrl;
        string m_CloudBuildApiUrl;
        string m_CloudBuildApiProjectUrl;
        string m_CloudBuildApiStatusUrl;

        string m_CollabDashboardUrl;
        string m_PurchasingDashboardUrl;
        string m_AnalyticsDashboardUrl;

        UnityWebRequest m_GetServicesUrlsRequest;

        public const string cdnConfigUri = "public-cdn.cloud.unity3d.com";
        const string k_CdnConfigUrl = "https://" + cdnConfigUri + "/config/{0}";

        Dictionary<string, string> m_ServicesUrlsConfig = new Dictionary<string, string>();


        public static ServicesConfiguration instance => k_Instance;

        public string cloudHubServiceUrl => k_CloudHubServiceUrl;

        public bool pathsReady { get; internal set; }
        public bool loadingConfigurations  { get; private set; }
        int m_ProgressId = k_NoProgressId;
        Queue<AsyncUrlCallback> m_AsyncUrlCallbacks = new Queue<AsyncUrlCallback>();

        public enum ServerEnvironment
        {
            Production,
            Development,
            Staging,
            Custom,
        }

        enum AsyncUrlId
        {
            CurrentUserApiUrl,
            ProjectsApiUrl,
            ProjectApiUrl,
            ProjectCoppaApiUrl,
            ProjectUsersApiUrl,
            ProjectDashboardUrl,
            ProjectServiceFlagsApiUrl,
            CloudBuildProjectUrl,
            CloudBuildUploadUrl,
            CloudBuildTargetUrl,
            CloudBuildApiUrl,
            CloudBuildApiProjectUrl,
            CloudBuildApiStatusUrl,
            CloudDiagCrashesDashboardUrl,
            CollabDashboardUrl,
            PurchasingDashboardUrl,
            AnalyticsDashboardUrl,
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
            m_UnityTeamUrl = L10n.Tr("https://unity3d.com/teams"); // Should be https://unity3d.com/fr/teams in French !
            PrepareAdsEnvironment(ConvertStringToServerEnvironment(UnityConnect.instance.GetEnvironment()));
            if (!string.IsNullOrEmpty(SessionState.GetString(k_ConfigJsonSessionStateKey, null)))
            {
                LoadConfigurationsFromSessionState();
            }
        }

        internal void LoadConfigurations(bool forceLoad = false)
        {
            if ((!pathsReady && !loadingConfigurations) || forceLoad)
            {
                loadingConfigurations = true;
                pathsReady = false;
                StartProgress();
                //We load configurations from most fallback to most relevant.
                //So we always have some fallback entries if a config only overwrite some of the URLs.
                LoadDefaultConfigurations();

                if (!forceLoad && !string.IsNullOrEmpty(SessionState.GetString(k_ConfigJsonSessionStateKey, null)))
                {
                    LoadConfigurationsFromSessionState();
                }
                else
                {
                    LoadConfigurationsFromCdn();
                }
            }
        }

        void BuildPaths()
        {
            var apiUrl = m_ServicesUrlsConfig["core"] + "/api";
            m_CurrentUserApiUrl = apiUrl + "/users/me";
            m_ProjectsApiUrl = apiUrl + "/orgs/{0}/projects";
            m_ProjectApiUrl = m_ProjectsApiUrl + "/{1}";
            m_ProjectCoppaApiUrl = m_ProjectApiUrl + "/coppa";
            m_ProjectUsersApiUrl = m_ProjectApiUrl + "/users";
            m_ProjectDashboardUrl = m_ServicesUrlsConfig["core"] + "/orgs/{0}/projects/{1}";
            m_ProjectServiceFlagsApiUrl = apiUrl + "/projects/{0}/service_flags"; //no org to specify

            m_CloudBuildProjectUrl = m_ServicesUrlsConfig["build"] + "/build/orgs/{0}/projects/{1}";

            m_CloudBuildUploadUrl = m_CloudBuildProjectUrl + "/upload/?page=1";
            m_CloudBuildTargetUrl = m_CloudBuildProjectUrl + "/buildtargets";
            m_CloudBuildApiUrl = m_ServicesUrlsConfig["build-api"];
            m_CloudBuildApiProjectUrl = m_CloudBuildApiUrl + "/api/v1/projects/{0}";
            m_CloudBuildApiStatusUrl = m_CloudBuildApiUrl + "/api/v1/status";

            m_CloudDiagCrashesDashboardUrl = m_ServicesUrlsConfig["build"] + "/diagnostics/orgs/{0}/projects/{1}/crashes";
            m_CollabDashboardUrl = m_ServicesUrlsConfig["build"] + "/collab/orgs/{0}/projects/{1}/assets/";
            m_PurchasingDashboardUrl = m_ServicesUrlsConfig["analytics"] + "/projects/{0}/purchasing/";
            m_AnalyticsDashboardUrl = m_ServicesUrlsConfig["analytics"] + "/events/{0}/";

            pathsReady = true;
            loadingConfigurations = false;

            while (m_AsyncUrlCallbacks.Count > 0)
            {
                InvokeAsyncUrlCallback(m_AsyncUrlCallbacks.Dequeue());
            }

            StopProgress();
        }

        void StartProgress()
        {
            if (m_ProgressId != k_NoProgressId)
            {
                Progress.Cancel(m_ProgressId);
                Progress.Remove(m_ProgressId);
            }

            m_ProgressId = Progress.Start(L10n.Tr(k_ProgressTitle), options: Progress.Options.Indefinite);
        }

        void StopProgress()
        {
            Progress.Finish(m_ProgressId);
            m_ProgressId = k_NoProgressId;
        }

        internal void UpdateProgress()
        {
            if (m_ProgressId != k_NoProgressId)
            {
                Progress.Report(m_ProgressId, k_NoProgressAmount);
            }
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

        void LoadConfigurationsFromSessionState()
        {
            var sessionStateConfigJson = SessionState.GetString(k_ConfigJsonSessionStateKey, null);
            if (sessionStateConfigJson != null)
            {
                LoadJsonConfiguration(sessionStateConfigJson);
                BuildPaths();
            }
        }

        void LoadConfigurationsFromCdn()
        {
            try
            {
                if (m_GetServicesUrlsRequest != null)
                {
                    try
                    {
                        m_GetServicesUrlsRequest.Abort();
                    }
                    catch (Exception)
                    {
                        // ignored, we try to abort (best effort) but no need to panic if it fails
                    }
                    m_GetServicesUrlsRequest.Dispose();
                    m_GetServicesUrlsRequest = null;
                }

                m_GetServicesUrlsRequest = new UnityWebRequest(string.Format(k_CdnConfigUrl, UnityConnect.instance.GetEnvironment()),
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                m_GetServicesUrlsRequest.suppressErrorsToConsole = true;
                var operation = m_GetServicesUrlsRequest.SendWebRequest();
                operation.completed += asyncOperation =>
                {
                    try
                    {
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_GetServicesUrlsRequest))
                        {
                            LoadJsonConfiguration(m_GetServicesUrlsRequest.downloadHandler.text);
                            SessionState.SetString(k_ConfigJsonSessionStateKey, m_GetServicesUrlsRequest.downloadHandler.text);
                            BuildPaths();
                        }
                    }
                    catch (Exception)
                    {
                        //We fallback to hardcoded config
                        BuildPaths();
                    }
                    finally
                    {
                        m_GetServicesUrlsRequest?.Dispose();
                        m_GetServicesUrlsRequest = null;
                    }
                };
            }
            catch (Exception)
            {
                //We fallback to hardcoded config
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
            adsDashboardUrl = "https://operate.dashboard.unity3d.com/organizations/{0}/projects/{1}/overview/revenue";

            switch (environmentType)
            {
                case ServerEnvironment.Production:
                    adsOperateApiUrl = "https://legacy-editor-integration.dashboard.unity3d.com";
                    break;
                case ServerEnvironment.Development:
                    adsOperateApiUrl = "https://ads-selfserve.staging.unityads.unity3d.com";
                    break;
                case ServerEnvironment.Staging:
                    adsOperateApiUrl = "https://legacy-editor-integration.staging.dashboard.unity3d.com";
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

        class AsyncUrlCallback
        {
            internal AsyncUrlId asyncUrlId;
            internal Action<string> callback;
        }

        void RequestAsyncUrl(AsyncUrlId asyncUrlId, Action<string> callback)
        {
            var asyncUrlCallback = new AsyncUrlCallback()
            {
                asyncUrlId = asyncUrlId,
                callback = callback
            };
            if (pathsReady)
            {
                InvokeAsyncUrlCallback(asyncUrlCallback);
            }
            else
            {
                m_AsyncUrlCallbacks.Enqueue(asyncUrlCallback);
                LoadConfigurations();
            }
        }

        void InvokeAsyncUrlCallback(AsyncUrlCallback asyncUrlCallback)
        {
            switch (asyncUrlCallback.asyncUrlId)
            {
                case AsyncUrlId.CurrentUserApiUrl:
                    asyncUrlCallback.callback(m_CurrentUserApiUrl);
                    break;
                case AsyncUrlId.ProjectsApiUrl:
                    asyncUrlCallback.callback(m_ProjectsApiUrl);
                    break;
                case AsyncUrlId.ProjectApiUrl:
                    asyncUrlCallback.callback(m_ProjectApiUrl);
                    break;
                case AsyncUrlId.ProjectCoppaApiUrl:
                    asyncUrlCallback.callback(m_ProjectCoppaApiUrl);
                    break;
                case AsyncUrlId.ProjectUsersApiUrl:
                    asyncUrlCallback.callback(m_ProjectUsersApiUrl);
                    break;
                case AsyncUrlId.ProjectDashboardUrl:
                    asyncUrlCallback.callback(m_ProjectDashboardUrl);
                    break;
                case AsyncUrlId.ProjectServiceFlagsApiUrl:
                    asyncUrlCallback.callback(m_ProjectServiceFlagsApiUrl);
                    break;
                case AsyncUrlId.CloudBuildProjectUrl:
                    asyncUrlCallback.callback(m_CloudBuildProjectUrl);
                    break;
                case AsyncUrlId.CloudBuildUploadUrl:
                    asyncUrlCallback.callback(m_CloudBuildUploadUrl);
                    break;
                case AsyncUrlId.CloudBuildTargetUrl:
                    asyncUrlCallback.callback(m_CloudBuildTargetUrl);
                    break;
                case AsyncUrlId.CloudBuildApiUrl:
                    asyncUrlCallback.callback(m_CloudBuildApiUrl);
                    break;
                case AsyncUrlId.CloudBuildApiProjectUrl:
                    asyncUrlCallback.callback(m_CloudBuildApiProjectUrl);
                    break;
                case AsyncUrlId.CloudBuildApiStatusUrl:
                    asyncUrlCallback.callback(m_CloudBuildApiStatusUrl);
                    break;
                case AsyncUrlId.CloudDiagCrashesDashboardUrl:
                    asyncUrlCallback.callback(m_CloudDiagCrashesDashboardUrl);
                    break;
                case AsyncUrlId.CollabDashboardUrl:
                    asyncUrlCallback.callback(m_CollabDashboardUrl);
                    break;
                case AsyncUrlId.PurchasingDashboardUrl:
                    asyncUrlCallback.callback(m_PurchasingDashboardUrl);
                    break;
                case AsyncUrlId.AnalyticsDashboardUrl:
                    asyncUrlCallback.callback(m_AnalyticsDashboardUrl);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Requests the current user api url
        /// </summary>
        /// <param name="callback"></param>
        /// <returns>A url to query the current user data</returns>
        public void RequestCurrentUserApiUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CurrentUserApiUrl, callback);
        }

        /// <summary>
        /// Request the endpoint to retrieve the projects of the specified organization, create a new one, etc.
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <param name="callback"></param>
        /// <returns>A url endpoint to request projects</returns>
        public void RequestOrganizationProjectsApiUrl(string organizationId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectsApiUrl, projectsApiUrl =>
            {
                callback(string.Format(projectsApiUrl, organizationId));
            });
        }

        public void RequestCurrentProjectApiUrl(Action<string> callback)
        {
            RequestProjectApiUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestProjectApiUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectApiUrl, projectApiUrl =>
            {
                callback(string.Format(projectApiUrl, organizationId, projectId));
            });
        }

        public void RequestCurrentProjectCoppaApiUrl(Action<string> callback)
        {
            RequestProjectCoppaApiUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestProjectCoppaApiUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectCoppaApiUrl, projectCoppaApiUrl =>
            {
                callback(string.Format(projectCoppaApiUrl, organizationId, projectId));
            });
        }

        /// <summary>
        /// Requests the current project users api url
        /// </summary>
        /// <param name="callback"></param>
        /// <returns>A url to reach the project users info</returns>
        public void RequestCurrentProjectUsersApiUrl(Action<string> callback)
        {
            RequestProjectUsersApiUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        /// <summary>
        /// Requests a specified project users api url
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <param name="projectId">A project's ID</param>
        /// <param name="callback"></param>
        /// <returns>A url to reach the project users info</returns>
        public void RequestProjectUsersApiUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectUsersApiUrl, projectUsersApiUrl =>
            {
                callback(string.Format(projectUsersApiUrl, organizationId, projectId));
            });
        }

        /// <summary>
        /// Requests the current project dashboard url
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public void RequestCurrentProjectDashboardUrl(Action<string> callback)
        {
            RequestProjectDashboardUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        /// <summary>
        /// Requests a specified project dashboard url
        /// </summary>
        /// <param name="organizationId">Identical to organization name at the time of this comment</param>
        /// <param name="projectId">A project's ID</param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public void RequestProjectDashboardUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectDashboardUrl, projectDashboardUrl =>
            {
                callback(string.Format(projectDashboardUrl, organizationId, projectId));
            });
        }

        public void RequestCurrentProjectServiceFlagsApiUrl(Action<string> callback)
        {
            RequestProjectServiceFlagsApiUrl(UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestProjectServiceFlagsApiUrl(string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectServiceFlagsApiUrl, projectServiceFlagsApiUrl =>
            {
                callback(string.Format(projectServiceFlagsApiUrl, projectId));
            });
        }

        // Return the specific Cloud Usage URL for the Collab service
        public void RequestCloudUsageDashboardUrl(Action<string> callback)
        {
            RequestCurrentProjectDashboardUrl(cloudUsageDashboardUrl =>
            {
                callback(cloudUsageDashboardUrl + k_CloudUsageDashboardUrl);
            });
        }

        // Return the specific Unity Teams information URL for the Collab service
        public string GetUnityTeamInfoUrl()
        {
            return m_UnityTeamUrl;
        }

        // Return the specific cloud build tutorial URL for the cloud build service
        public string GetCloudBuildTutorialUrl()
        {
            return k_CloudBuildTutorialUrl;
        }

        public void RequestCloudBuildAddTargetUrl(Action<string> callback)
        {
            RequestCloudBuildCurrentProjectUrl(cloudBuildCurrentProjectUrl =>
            {
                callback(cloudBuildCurrentProjectUrl + k_CloudBuildAddTargetUrl);
            });
        }

        public void RequestCloudBuildCurrentProjectUrl(Action<string> callback)
        {
            RequestCloudBuildProjectsUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestCloudBuildProjectsUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildProjectUrl, cloudBuildProjectUrl =>
            {
                callback(string.Format(cloudBuildProjectUrl, organizationId, projectId));
            });
        }

        public void RequestCurrentCloudBuildProjectUploadUrl(Action<string> callback)
        {
            RequestCloudBuildProjectUploadUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestCloudBuildProjectUploadUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildUploadUrl, cloudBuildUploadUrl =>
            {
                callback(string.Format(cloudBuildUploadUrl, organizationId, projectId));
            });
        }

        public void RequestCurrentCloudBuildProjectTargetUrl(Action<string> callback)
        {
            RequestCloudBuildProjectTargetUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestCloudBuildProjectTargetUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildTargetUrl, cloudBuildTargetUrl =>
            {
                callback(string.Format(cloudBuildTargetUrl, organizationId, projectId));
            });
        }

        public void RequestCurrentCloudBuildProjectHistoryUrl(Action<string> callback)
        {
            RequestCloudBuildProjectHistoryUrl(UnityConnect.instance.projectInfo.organizationId, UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestCloudBuildProjectHistoryUrl(string organizationId, string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildProjectUrl, cloudBuildProjectUrl =>
            {
                callback(string.Format(cloudBuildProjectUrl, organizationId, projectId));
            });
        }

        public void RequestCloudBuildApiCurrentProjectUrl(Action<string> callback)
        {
            RequestCloudBuildApiProjectUrl(UnityConnect.instance.projectInfo.projectId, callback);
        }

        public void RequestCloudBuildApiProjectUrl(string projectId, Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildApiProjectUrl, cloudBuildApiProjectUrl =>
            {
                callback(string.Format(cloudBuildApiProjectUrl, projectId));
            });
        }

        public void RequestCloudBuildApiUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildApiUrl, callback);
        }

        public void RequestCloudBuildApiStatusUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildApiStatusUrl, callback);
        }

        // Return the cloud diagnostic URL
        public string GetUnityCloudDiagnosticInfoUrl()
        {
            return k_CloudDiagnosticsUrl;
        }

        public string GetUnityCloudDiagnosticUserReportingSdkUrl()
        {
            return k_CloudDiagUserReportingSdkUrl;
        }

        internal void RequestBaseDashboardUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.ProjectDashboardUrl, callback);
        }

        public void RequestBaseAnalyticsDashboardUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.AnalyticsDashboardUrl, callback);
        }

        public void RequestBaseCloudBuildDashboardUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudBuildProjectUrl, callback);
        }

        public void RequestBaseCloudUsageDashboardUrl(Action<string> callback)
        {
            RequestBaseDashboardUrl(baseDashboardUrl =>
            {
                callback(baseDashboardUrl + k_CloudUsageDashboardUrl);
            });
        }

        public void RequestBaseCloudDiagCrashesDashboardUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CloudDiagCrashesDashboardUrl, callback);
        }

        public void RequestBaseCollabDashboardUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.CollabDashboardUrl, callback);
        }

        public void RequestBasePurchasingDashboardUrl(Action<string> callback)
        {
            RequestAsyncUrl(AsyncUrlId.PurchasingDashboardUrl, callback);
        }

        public string adsGettingStartedUrl { get; private set; }
        public string adsLearnMoreUrl { get; private set; }
        public string adsDashboardUrl { get; private set; }
        public string adsOperateApiUrl { get; private set; }
    }
}
