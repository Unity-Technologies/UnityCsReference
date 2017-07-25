// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor.Purchasing;
using UnityEngine.Networking;
using UnityEditor.Connect;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class PurchasingAccess : CloudServiceAccess
    {
        private const string kServiceName = "Purchasing";
        private const string kServiceDisplayName = "In App Purchasing";
        private const string kServicePackageName = "com.unity.purchasing";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/purchasing";
        private const string kETagPath = "Assets/Plugins/UnityPurchasing/ETag";
        private const string kUnknownPackageETag = "unknown";
        private static readonly Uri kPackageUri = new Uri("https://public-cdn.cloud.unity3d.com/UnityEngine.Cloud.Purchasing.unitypackage");
        private bool m_InstallInProgress;

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        public override string GetPackageName()
        {
            return kServicePackageName;
        }

        override public bool IsServiceEnabled()
        {
            return PurchasingSettings.enabled;
        }

        public struct PurchasingServiceState { public bool iap; }
        override public void EnableService(bool enabled)
        {
            if (PurchasingSettings.enabled != enabled)
            {
                PurchasingSettings.enabled = enabled;
                EditorAnalytics.SendEventServiceInfo(new PurchasingServiceState() {iap = enabled});
            }
        }

        static PurchasingAccess()
        {
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, new PurchasingAccess(), "unity/project/cloud/purchasing");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }

        /// <summary>
        /// Download and install the Unity IAP Package.
        /// </summary>
        public void InstallUnityPackage()
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

            var client = new WebClient();
            client.DownloadFileCompleted += (sender, args) =>
                {
                    // Installation must be done on the main thread.
                    EditorApplication.CallbackFunction handler = null;
                    handler = () =>
                    {
                        ServicePointManager.ServerCertificateValidationCallback = originalCallback;
                        EditorApplication.update -= handler;
                        m_InstallInProgress = false;
                        if (args.Error == null)
                        {
                            SaveETag(client);
                            AssetDatabase.ImportPackage(location, false);
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Failed to download IAP package. Please check connectivity and retry.");
                            UnityEngine.Debug.LogException(args.Error);
                        }
                    };
                    EditorApplication.update += handler;
                };

            client.DownloadFileAsync(kPackageUri, location);
        }

        /// <summary>
        /// Get the ETag of the currently installed IAP package, if any.
        /// Called by the Editor IAP window when doing update checks.
        /// </summary>
        public string GetInstalledETag()
        {
            if (File.Exists(kETagPath))
            {
                return File.ReadAllText(kETagPath);
            }
            else if (Directory.Exists(Path.GetDirectoryName(kETagPath)))
            {
                // The plugin was installed pre ETag version tracking.
                return kUnknownPackageETag;
            }

            return null; // No plugin.
        }

        private void SaveETag(WebClient client)
        {
            string etag = client.ResponseHeaders.Get("ETag");
            if (null != etag)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(kETagPath));
                File.WriteAllText(kETagPath, etag);
            }
        }
    }
}

