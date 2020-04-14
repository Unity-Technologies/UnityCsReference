// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using System;
using System.IO;
using System.Net;
using UnityEditor.Purchasing;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    /// <summary>
    /// The implementation of the In-App Purchasing service for the service window
    /// </summary>
    [InitializeOnLoad]
    internal class PurchasingService : SingleService
    {
        readonly Uri k_PackageUri;

        private const string k_ETagHeader = "ETag";
        const string k_ETagPath = "Assets/Plugins/UnityPurchasing/ETag";
        const string k_GoogleKeySubPath = "/api/v2/projects/";
        const string k_GoogleKeyGetSuffix = "/get_google_pub_key";
        const string k_GoogleKeyPostSuffix = "/set_google_pub_key";
        const string k_GoogleKeyJsonLabel = "google_pub_key";
        const string k_UnknownPackageETag = "unknown";
        const string k_WaitingOnPackageETag = "waiting";

        bool m_InstallInProgress;
        string m_LatestETag = k_WaitingOnPackageETag;
        Action m_NotifyOnGetLatestETag;

        private struct PurchasingServiceState { public bool iap; }

        private struct ImportPackageInfo
        {
            public string packageName;
            public string eTag;
        }

        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(PurchasingProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.PurchasingService;
        public override string packageName { get; }
        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        static readonly PurchasingService k_Instance;

        public static PurchasingService instance => k_Instance;

        public string googleKeyJsonLabel
        {
            get { return k_GoogleKeyJsonLabel; }
        }

        public string unknownPackage
        {
            get { return k_UnknownPackageETag; }
        }

        public string waitingOnPackage
        {
            get { return k_WaitingOnPackageETag; }
        }

        public string latestETag
        {
            get { return m_LatestETag; }
        }

        static PurchasingService()
        {
            k_Instance = new PurchasingService();
        }

        PurchasingService()
        {
            k_PackageUri = new Uri(PurchasingConfiguration.instance.purchasingPackageUrl);

            name = "Purchasing";
            title = L10n.Tr("In-App Purchasing");
            description = L10n.Tr("Simplify cross-platform IAP");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Purchasing.png";
            projectSettingsPath = "Project/Services/In-App Purchasing";
            displayToggle = true;
            packageName = "com.unity.purchasing";
            serviceFlagName = "purchasing";
            ServicesRepository.AddService(this);
        }

        public override bool IsServiceEnabled()
        {
            return PurchasingSettings.enabled;
        }

        public override bool requiresCoppaCompliance => true;

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (PurchasingSettings.enabled != enable)
            {
                PurchasingSettings.SetEnabledServiceWindow(enable);
                EditorAnalytics.SendEventServiceInfo(new PurchasingServiceState() { iap = enable });
                if (enable && !AnalyticsService.instance.IsServiceEnabled())
                {
                    AnalyticsService.instance.EnableService(true, shouldUpdateApiFlag);
                }
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }

        /// <summary>
        /// Download and install the Unity IAP Package.
        /// </summary>
        public void InstallUnityPackage(Action onImport)
        {
            if (m_InstallInProgress)
                return;

            var originalCallback = ServicePointManager.ServerCertificateValidationCallback;
            // Only OSX supports SSL certificate validation, disable checking on other platforms.
            // TODO - fix when a Unity Web framework supports SSL.
            if (Application.platform != RuntimePlatform.OSXEditor)
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;

            m_InstallInProgress = true;
            var location = FileUtil.GetUniqueTempPathInProject();
            // Extension is required for correct Windows import.
            location = Path.ChangeExtension(location, ".unitypackage");

            UnityWebRequest request = new UnityWebRequest(k_PackageUri, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(location);
            request.suppressErrorsToConsole = true;
            var operation = request.SendWebRequest();
            operation.completed += (asyncOp) =>
            {
                // Installation must be done on the main thread.
                EditorApplication.CallbackFunction handler = null;
                handler = () =>
                {
                    ServicePointManager.ServerCertificateValidationCallback = originalCallback;
                    EditorApplication.update -= handler;
                    m_InstallInProgress = false;
                    if ((request.result != UnityWebRequest.Result.ProtocolError) && (request.result != UnityWebRequest.Result.ConnectionError))
                    {
                        string etag = request.GetResponseHeaders()[k_ETagHeader];
                        SaveETag(etag);

                        AssetDatabase.ImportPackage(location, false);

                        EditorAnalytics.SendImportServicePackageEvent(new ImportPackageInfo() { packageName = Path.GetFileName(k_PackageUri.ToString()) , eTag = etag });

                        //TODO: ImportPackage is a delayeed operation with no callback. See if we can get a confirmation of successful installation
                        //      before firing our callback, or even before Saving the ETag.
                        onImport();
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(L10n.Tr("Failed to download IAP package. Please check connectivity and retry. Web request Error: ") + request.error);
                    }
                };
                EditorApplication.update += handler;
            };
        }

        private void SaveETag(string etag)
        {
            if (etag != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(k_ETagPath));
                File.WriteAllText(k_ETagPath, etag);
            }
        }

        public string GetInstalledETag()
        {
            if (File.Exists(k_ETagPath))
            {
                return File.ReadAllText(k_ETagPath);
            }
            else if (Directory.Exists(Path.GetDirectoryName(k_ETagPath)))
            {
                // The plugin was installed pre ETag version tracking.
                return k_UnknownPackageETag;
            }

            return null; // No plugin.
        }

        //checks for the most recent version available online
        internal void GetLatestETag(Action<AsyncOperation> onGet)
        {
            var request = UnityWebRequest.Head(k_PackageUri);
            request.suppressErrorsToConsole = true;
            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }

        internal void OnGetLatestETag(AsyncOperation op)
        {
            UnityWebRequestAsyncOperation webOp = (UnityWebRequestAsyncOperation)op;

            if (webOp != null)
            {
                string eTagHeader = ((webOp.webRequest.result != UnityWebRequest.Result.ProtocolError) && (webOp.webRequest.result != UnityWebRequest.Result.ConnectionError))
                    ? webOp.webRequest.GetResponseHeaders()["ETag"]
                    : null;

                m_LatestETag = (eTagHeader != null) ? eTagHeader : k_UnknownPackageETag;
            }
            else
            {
                m_LatestETag = k_UnknownPackageETag;
            }

            if (m_NotifyOnGetLatestETag != null)
            {
                m_NotifyOnGetLatestETag();
                m_NotifyOnGetLatestETag = null;
            }
        }

        public void RequestNotifyOnVersionCheck(Action onCheck)
        {
            if (m_LatestETag == k_WaitingOnPackageETag)
            {
                m_NotifyOnGetLatestETag += onCheck;
            }
            else
            {
                onCheck();
            }
        }

        //TODO: Consider moving to 'core' of services (also exists in Analytics):
        public void RequestAuthSignature(Action<AsyncOperation> onGet, out UnityWebRequest request)
        {
            request = UnityWebRequest.Get(String.Format(AnalyticsConfiguration.instance.coreProjectsUrl, UnityConnect.instance.projectInfo.projectGUID));
            request.suppressErrorsToConsole = true;
            request.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }

        public void GetGooglePlayKey(Action<AsyncOperation> onGet, string authSignature)
        {
            var request = UnityWebRequest.Get(GetGoogleKeyResource() + k_GoogleKeyGetSuffix);
            request.suppressErrorsToConsole = true;

            var encodedAuthToken = ServicesUtils.Base64Encode((UnityConnect.instance.projectInfo.projectGUID + ":" + authSignature));
            request.SetRequestHeader("Authorization", $"Basic {encodedAuthToken}");

            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }

        public void SubmitGooglePlayKey(string submittedKey, Action<AsyncOperation> onSubmit, string authSignature)
        {
            var payload = "{\"" + k_GoogleKeyJsonLabel + "\": \"" + submittedKey + "\"}";
            var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
            var request = new UnityWebRequest(GetGoogleKeyResource() + k_GoogleKeyPostSuffix,
                UnityWebRequest.kHttpVerbPOST)
            { uploadHandler = uploadHandler};
            request.suppressErrorsToConsole = true;

            var encodedAuthToken = ServicesUtils.Base64Encode((UnityConnect.instance.projectInfo.projectGUID + ":" + authSignature));
            request.SetRequestHeader("Authorization", $"Basic {encodedAuthToken}");
            request.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");
            var operation = request.SendWebRequest();

            operation.completed += onSubmit;
        }

        private string GetGoogleKeyResource()
        {
            return PurchasingConfiguration.instance.analyticsApiUrl + k_GoogleKeySubPath + UnityConnect.instance.projectInfo.projectGUID;
        }
    }
}
