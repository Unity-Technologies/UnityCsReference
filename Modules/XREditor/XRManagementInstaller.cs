// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.XR
{
    [UnityEngine.Internal.ExcludeFromDocs]
    class XRManagementInstaller : SettingsProvider
    {
        static string s_ManagementPackageId = "com.unity.xr.management";
        static string s_ManagementPackagePath = $"Packages/{s_ManagementPackageId}/package.json";


        private PackageManager.Requests.AddRequest m_AddManagementRequest = null;

        static class Content
        {
            internal const string s_InstallationHelpText = "In order to use the new XR Plugin system you need to install the XR Plugin Management package. Clicking the button below will install the latest XR Plugin Management package and allow you to configure your project for XR.";
            internal static string s_SettingsRootTitle = "Project/XR Plugin Management";
            internal static GUIContent s_AddXrManagement = EditorGUIUtility.TrTextContent("Install XR Plugin Management");
            internal static GUIContent s_DownloadingText = new GUIContent("Downloading XR Management system...");
            internal static GUIContent s_InstallingText = new GUIContent("Installing XR Management system...");
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        XRManagementInstaller(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
        {
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public override void OnGUI(string searchContext)
        {
            if (m_AddManagementRequest != null)
            {
                if (m_AddManagementRequest.IsCompleted)
                {
                    EditorGUILayout.LabelField(Content.s_DownloadingText);
                }
                else
                {
                    EditorGUILayout.LabelField(Content.s_InstallingText);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(Content.s_InstallationHelpText, MessageType.Info);
                if (GUILayout.Button(Content.s_AddXrManagement))
                {
                    m_AddManagementRequest = PackageManager.Client.Add(s_ManagementPackageId);
                }
            }
        }

        [SettingsProvider]
        static SettingsProvider CreateXRManagementInstallerProvider()
        {
            try
            {
                var packagePath = Path.GetFullPath(s_ManagementPackagePath);
                if (String.IsNullOrEmpty(packagePath) || !File.Exists(packagePath) || String.Compare(s_ManagementPackagePath, packagePath, true) == 0)
                    return new XRManagementInstaller(Content.s_SettingsRootTitle);
            }
            catch (Exception)
            {
                // DO NOTHING...
            }

            return null;
        }
    }
}
