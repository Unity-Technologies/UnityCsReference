// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.Connect.Fallback
{
    class PackageInstallationHandler : IDisposable
    {
        SingleService m_ServiceInstance;
        string m_InstallVersion;

        bool m_IsDisposed;
        bool m_InstallingNewPackage;
        AddRequest m_AddRequest;
        SearchRequest m_SearchRequest;

        public event Action<bool> packageSearchComplete;
        public event Action packageInstallationComplete;

        public PackageInstallationHandler(SingleService serviceInstance)
        {
            m_ServiceInstance = serviceInstance;
        }

        ~PackageInstallationHandler()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_IsDisposed)
                return;

            packageSearchComplete = null;
            packageInstallationComplete = null;
            EditorApplication.update -= SearchPackageProgress;
            EditorApplication.update -= AddPackageProgress;

            m_IsDisposed = true;
        }

        public void StartPackageSearch()
        {
            m_SearchRequest = Client.Search(m_ServiceInstance.editorGamePackageName);
            EditorApplication.update += SearchPackageProgress;
        }

        void SearchPackageProgress()
        {
            if (!m_SearchRequest.IsCompleted)
                return;

            EditorApplication.update -= SearchPackageProgress;
            if (m_SearchRequest.Status == StatusCode.Success)
            {
                foreach (var package in m_SearchRequest.Result)
                {
                    if (package.name.Equals(m_ServiceInstance.editorGamePackageName))
                    {
                        OnSearchPackageFound(package);
                        break;
                    }
                }
            }
            else if (m_SearchRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError(m_SearchRequest.Error.message);
                packageSearchComplete?.Invoke(false);
            }
        }

        void OnSearchPackageFound(PackageManager.PackageInfo packageInfo)
        {
            m_InstallVersion = packageInfo.version;
            packageSearchComplete?.Invoke(true);
        }

        public void StartInstallation()
        {
            if (AcceptedInstallingNewPackage())
            {
                m_InstallingNewPackage = true;
                m_AddRequest = Client.Add(m_ServiceInstance.editorGamePackageName + "@" + m_InstallVersion);
                EditorApplication.update += AddPackageProgress;
            }
        }

        public bool AcceptedInstallingNewPackage()
        {
            return !m_InstallingNewPackage && EditorUtility.DisplayDialog(GetInstallDialogTitle(), GetInstallDialogMessage(),
                L10n.Tr(PackageInstallationText.Yes), L10n.Tr(PackageInstallationText.No));
        }

        string GetInstallDialogTitle()
        {
            var translatedPackageInstallationText = L10n.Tr(PackageInstallationText.Title);
            var translatedServiceTitle = L10n.Tr(m_ServiceInstance.title);
            return string.Format(translatedPackageInstallationText, translatedServiceTitle);
        }

        string GetInstallDialogMessage()
        {
            var translatedPackageInstallationText = L10n.Tr(PackageInstallationText.Message);
            var translatedServiceTitle = L10n.Tr(m_ServiceInstance.title);
            return string.Format(translatedPackageInstallationText, translatedServiceTitle);
        }

        void AddPackageProgress()
        {
            if (!m_AddRequest.IsCompleted)
                return;
            EditorApplication.update -= AddPackageProgress;
            if (m_AddRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError(m_AddRequest.Error.message);
            }
            else
            {
                packageInstallationComplete?.Invoke();
                EditorAnalytics.SendImportServicePackageEvent(new ServicesProjectSettings.ImportPackageInfo() { packageName = m_ServiceInstance.packageName, version = m_InstallVersion });
            }
            m_InstallingNewPackage = false;
        }

        static class PackageInstallationText
        {
            public const string Title = "{0} Installation";
            public const string Message = "You are about to install the latest {0}.\nDo you want to continue?";
            public const string Yes = "Ok";
            public const string No = "Cancel";
        }
    }
}
