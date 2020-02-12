// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Text;
using System.Threading;
using UnityEditor.Advertisements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class AdsService : SingleService
    {
        const string k_GameIdApiUrl = "/unity/v1/games";
        const string k_JsonAppleGameId = "iOSGameKey";
        const string k_JsonAndroidGameId = "androidGameKey";
        const int k_GameIdsRequestMaxIteration = 10;
        const int k_GameIdsIterationDelay = 1000;

        int m_GameIdsRequestIteration = 0;
        UnityWebRequest m_CurrentWebRequest;

        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(AdsProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.AdsService;
        public override bool requiresCoppaCompliance => true;
        public override string packageName { get; }
        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        static readonly AdsService k_Instance;

        public static AdsService instance => k_Instance;

        static AdsService()
        {
            k_Instance = new AdsService();
        }

        AdsService()
        {
            name = "Unity Ads";
            title = L10n.Tr("Ads");
            description = L10n.Tr("Monetize your games");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Ads.png";
            projectSettingsPath = "Project/Services/Ads";
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

                if (enable)
                {
                    RefreshGameIds();
                }
                else
                {
                    SetGameIds(appleGameId: null, androidGameId: null);
                }
                EditorAnalytics.SendEventServiceInfo(new AdsServiceState() { ads = enable });
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }

        void RefreshGameIds()
        {
            //Workaround because the project my not be available right away, thus doing retries on the request
            m_GameIdsRequestIteration = 0;
            RequestGameIds();
        }

        void RequestGameIds()
        {
            if (IsServiceEnabled() && m_CurrentWebRequest == null &&
                !string.IsNullOrEmpty(UnityConnect.instance.projectInfo.projectGUID))
            {
                var bodyContent = "{\"projectGUID\": \"" + UnityConnect.instance.projectInfo.projectGUID + "\",\"projectName\":\"" + UnityConnect.instance.projectInfo.projectName + "\",\"token\":\"" + UnityConnect.instance.GetUserInfo().accessToken + "\"}";
                var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyContent));

                m_CurrentWebRequest = new UnityWebRequest(ServicesConfiguration.instance.adsOperateApiUrl + k_GameIdApiUrl, UnityWebRequest.kHttpVerbPOST) { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler };
                m_CurrentWebRequest.suppressErrorsToConsole = true;
                m_CurrentWebRequest.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");

                var operation = m_CurrentWebRequest.SendWebRequest();
                operation.completed += RequestGameIdsOnCompleted;
            }
        }

        void RequestGameIdsOnCompleted(AsyncOperation asyncOperation)
        {
            if (asyncOperation.isDone)
            {
                if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_CurrentWebRequest))
                {
                    if (m_CurrentWebRequest.downloadHandler.text.Length != 0)
                    {
                        var jsonParser = new JSONParser(m_CurrentWebRequest.downloadHandler.text);
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
                    }
                }
                else if (m_CurrentWebRequest?.result == UnityWebRequest.Result.ProtocolError && m_GameIdsRequestIteration < k_GameIdsRequestMaxIteration)
                {
                    CancelCurrentWebRequest();
                    m_GameIdsRequestIteration++;

                    //Adding a delay between retries as we may be waiting for the project to be created
                    var context = SynchronizationContext.Current;
                    var unused = new Timer((obj) =>
                    {
                        context?.Post((o) => this?.RequestGameIds(), null);
                    }, null, k_GameIdsIterationDelay, 0);

                    return;
                }
                else
                {
                    SetGameIds(appleGameId: null, androidGameId: null);
                }
                CancelCurrentWebRequest();
            }
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
