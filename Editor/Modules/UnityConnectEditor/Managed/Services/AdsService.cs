// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor.Advertisements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal partial class AdsService : SingleService
    {
        const string k_GameIdApiUrl = "/unity/v1/games";
        const string k_JsonAppleGameId = "iOSGameKey";
        const string k_JsonAndroidGameId = "androidGameKey";
        const string k_GameIdsFetchFailedMessage = "Could not get the Ads game IDs. Check your connection and try again.";
        internal const int k_GameIdsRequestMaxIteration = 10;
        const int k_GameIdsIterationDelay = 1000;

        int m_GameIdsRequestIteration = 0;
        UnityWebRequest m_CurrentWebRequest;
        Timer m_RetryTimer;
        string m_LinkedProjectGuid;

        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; } = "Project/Services/Ads";
        public override string settingsProviderClassName => nameof(AdsProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.AdsService;
        public override bool requiresCoppaCompliance => true;
        public override string packageName { get; }

        public override string editorGamePackageName { get; } = "com.unity.ads";

        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        [AutoStaticsCleanupOnCodeReload]
        static AdsService k_Instance;

        public static AdsService instance
        {
            get
            {
                EnsureInstanceInitialized();
                return k_Instance;
            }
        }

        // [OnCodeLoaded] re-registers this service with ServicesRepository after a reload;
        // the null check in `instance` covers consumers that reach us before it has run
        // (ordering across classes is not guaranteed).
        [OnCodeLoaded]
        static void EnsureInstanceInitialized()
        {
            if (k_Instance == null)
            {
                k_Instance = new AdsService();
            }
        }

        AdsService()
        {
            name = "Unity Ads";
            title = L10n.Tr("Ads", null);
            description = L10n.Tr("Monetize your games", null);
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Ads.png";
            displayToggle = true;
            packageName = "com.unity.ads";
            serviceFlagName = "ads";
            ServicesRepository.AddService(this);

            InitializeService();
        }

        void InitializeService()
        {
            var iPhoneGameId = AdvertisementSettings.GetGameId(RuntimePlatform.IPhonePlayer);
            var androidGameId = AdvertisementSettings.GetGameId(RuntimePlatform.Android);

            //Make sure that the service was enabled as expected, if not refresh the information
            if (IsServiceEnabled()
                && (string.IsNullOrEmpty(iPhoneGameId) || string.IsNullOrEmpty(androidGameId)))
            {
                RefreshGameIds();
            }
        }

        public override bool IsServiceEnabled()
        {
            return AdvertisementSettings.enabled;
        }

        private struct AdsServiceState
        {
            public bool ads;
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (AdvertisementSettings.enabled != enable)
            {
                AdvertisementSettings.SetEnabledServiceWindow(enable);
                CancelCurrentWebRequest();

                if (!enable)
                {
                    SetGameIds(appleGameId: null, androidGameId: null);
                }
                EditorAnalytics.SendEventServiceInfo(new AdsServiceState() { ads = enable });
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }

        protected override void OnServiceFlagRequestCompleted(AsyncOperation asyncOperation)
        {
            base.OnServiceFlagRequestCompleted(asyncOperation);

            RefreshGameIds();
        }

        void RefreshGameIds()
        {
            //Workaround because the project my not be available right away, thus doing retries on the request
            m_GameIdsRequestIteration = 0;
            RequestGameIds();
        }

        void RequestGameIds()
        {
            //Make sure to dispose of the retry timer if it was active for a retry
            m_RetryTimer?.Dispose();
            m_RetryTimer = null;

            if (IsServiceEnabled() && m_CurrentWebRequest == null &&
                !string.IsNullOrEmpty(UnityConnect.instance.projectInfo.projectGUID))
            {
                var bodyContent = CreateGameIdsRequestBody(UnityConnect.instance.projectInfo.projectGUID, UnityConnect.instance.projectInfo.projectName, UnityConnect.instance.GetUserInfo().accessToken);
                var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyContent));

                m_CurrentWebRequest = new UnityWebRequest(ServicesConfiguration.instance.adsOperateApiUrl + k_GameIdApiUrl, UnityWebRequest.kHttpVerbPOST) { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler };
                m_CurrentWebRequest.suppressErrorsToConsole = true;
                m_CurrentWebRequest.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");

                var request = m_CurrentWebRequest;
                var operation = request.SendWebRequest();
                operation.completed += asyncOperation => RequestGameIdsOnCompleted(request);
            }
        }

        internal static string CreateGameIdsRequestBody(string projectGuid, string projectName, string accessToken)
        {
            return Json.Serialize(new Dictionary<string, object>
            {
                { "projectGUID", projectGuid },
                { "projectName", projectName ?? string.Empty },
                { "token", accessToken ?? string.Empty },
            });
        }

        internal enum GameIdsResult
        {
            Cancelled,
            GameIds,
            Retry,
            NoGameIds,
        }

        internal static GameIdsResult GetGameIdsResult(UnityWebRequest request, UnityWebRequest currentRequest, int iteration)
        {
            // A cancelled request still completes, possibly after the next request started
            if (request != currentRequest)
                return GameIdsResult.Cancelled;
            if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(request))
                return GameIdsResult.GameIds;

            // The server refuses the same sign-in again
            if (request.result == UnityWebRequest.Result.ProtocolError && request.responseCode != (long)HttpStatusCode.Unauthorized
                && iteration < k_GameIdsRequestMaxIteration)
                return GameIdsResult.Retry;
            return GameIdsResult.NoGameIds;
        }

        void RequestGameIdsOnCompleted(UnityWebRequest request)
        {
            switch (GetGameIdsResult(request, m_CurrentWebRequest, m_GameIdsRequestIteration))
            {
                case GameIdsResult.Cancelled:
                    return;

                case GameIdsResult.GameIds:
                {
                    var jsonParser = new JSONParser(request.downloadHandler.text);
                    try
                    {
                        var json = jsonParser.Parse();
                        var key = k_JsonAppleGameId;
                        string appleGameId = null;
                        string androidGameId = null;

                        if (json.AsDict().ContainsKey(key))
                        {
                            appleGameId = json.AsDict()[key].ToString();
                        }
                        key = k_JsonAndroidGameId;
                        if (json.AsDict().ContainsKey(key))
                        {
                            androidGameId = json.AsDict()[key].ToString();
                        }
                        SetGameIds(appleGameId, androidGameId);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        NotificationManager.instance.Publish(Notification.Topic.AdsService, Notification.Severity.Error, ex.Message);
                    }
                    break;
                }

                case GameIdsResult.Retry:
                {
                    CancelCurrentWebRequest();
                    m_GameIdsRequestIteration++;

                    //Adding a delay between retries as we may be waiting for the project to be created
                    var context = SynchronizationContext.Current;
                    m_RetryTimer = new Timer((obj) =>
                    {
                        context?.Post((o) => this?.RequestGameIds(), null);
                    }, null, k_GameIdsIterationDelay, 0);

                    return;
                }

                // A failed fetch says nothing about the game IDs, so the ones already set are kept
                case GameIdsResult.NoGameIds:
                    NotificationManager.instance.Publish(Notification.Topic.AdsService, Notification.Severity.Error, L10n.Tr(k_GameIdsFetchFailedMessage, null));
                    break;
            }
            CancelCurrentWebRequest();
        }

        internal override void OnLinkedToAnotherProject(string linkedProjectGuid)
        {
            CancelCurrentWebRequest();
            SetGameIds(appleGameId: null, androidGameId: null);

            // Fetching before the link is done would ask for the previous project's game IDs
            m_LinkedProjectGuid = linkedProjectGuid;
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterLink;
            UnityConnect.instance.ProjectStateChanged += OnProjectStateChangedAfterLink;
        }

        void OnProjectStateChangedAfterLink(ProjectInfo projectInfo)
        {
            var linkedProject = UnityConnect.instance.projectInfo;
            RefreshGameIdsIfLinked(linkedProject.projectBound, linkedProject.projectGUID);
        }

        internal bool RefreshGameIdsIfLinked(bool projectBound, string projectGuid)
        {
            if (string.IsNullOrEmpty(m_LinkedProjectGuid) || !projectBound || projectGuid != m_LinkedProjectGuid)
                return false;

            m_LinkedProjectGuid = null;
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterLink;
            RefreshGameIds();
            return true;
        }

        private void SetGameIds(string appleGameId, string androidGameId)
        {
            AdvertisementSettings.SetGameId(RuntimePlatform.IPhonePlayer, appleGameId);
            AdvertisementSettings.SetGameId(RuntimePlatform.Android, androidGameId);
            gameIdsUpdatedEvent?.Invoke();
        }

        private void CancelCurrentWebRequest()
        {
            m_CurrentWebRequest?.Abort();
            m_CurrentWebRequest?.Dispose();
            m_CurrentWebRequest = null;
        }

        /// <summary>
        /// AdsService will launch this event after the gameIds are refreshed.
        /// GameIds are available via AdvertisementSettings
        /// </summary>
        internal event Action gameIdsUpdatedEvent;
    }
}
