// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.InAppPurchasing
{
    internal class InAppPurchasingInstaller : SettingsProvider
    {
        internal static string s_ManagementPackageId = "com.unity.purchasing";

        private static string s_ManagementPackagePath = $"Packages/{s_ManagementPackageId}/package.json";
        private PackageManager.Requests.AddRequest m_AddManagementRequest = null;
        private static string s_ProjectSettingsInstallationMethod = "projectSettings";

        private static class Content
        {
            internal const string s_InstallationHelpText = "In order to use In-App Purchasing you need to install the In-App Purchasing package. Clicking the button below will install the latest In-App Purchasing package and allow you to configure your project for In-App Purchasing.";
            internal static readonly string s_SettingsRootTitle = "Project/Services/In-App Purchasing";
            internal static readonly Uri s_DocUri = new Uri("https://docs.unity3d.com/Packages/com.unity.purchasing@latest");
            internal static readonly GUIContent s_AddInAppPurchasing = EditorGUIUtility.TrTextContent(L10n.Tr("Install In-App Purchasing"));
            internal static readonly GUIContent s_DownloadingText = new GUIContent(L10n.Tr("Downloading In-App Purchasing ..."));
            internal static readonly GUIContent s_InstallingText = new GUIContent(L10n.Tr("Installing In-App Purchasing ..."));
        }

        private static class Styles
        {
            public static readonly GUIStyle verticalStyle;
            public static readonly GUIStyle linkLabel;
            public static readonly GUIStyle textLabel;

            static Styles()
            {
                verticalStyle = new GUIStyle(EditorStyles.inspectorFullWidthMargins);
                verticalStyle.margin = new RectOffset(10, 10, 10, 10);

                linkLabel = new GUIStyle(EditorStyles.linkLabel);
                linkLabel.fontSize = EditorStyles.miniLabel.fontSize;
                linkLabel.wordWrap = true;

                textLabel = new GUIStyle(EditorStyles.miniLabel);
                textLabel.wordWrap = true;
            }
        }

        private InAppPurchasingInstaller(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
        {
        }

        [UnityEngine.Internal.ExcludeFromDocs]
        public override void OnGUI(string searchContext)
        {
            GUILayout.Space(15);
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
                HelpBox(L10n.Tr(Content.s_InstallationHelpText), L10n.Tr("Read more"), () =>
                {
                    System.Diagnostics.Process.Start(Content.s_DocUri.AbsoluteUri);
                });
                GUILayout.Space(15);
                if (GUILayout.Button(Content.s_AddInAppPurchasing))
                {
                    m_AddManagementRequest = PackageManager.Client.Add(s_ManagementPackageId);
                    InAppPurchasingQuickInstallAnalytic.SendEvent(s_ProjectSettingsInstallationMethod);
                }
            }
        }

        static public bool PackageIsInstalled()
        {
            var packagePath = Path.GetFullPath(s_ManagementPackagePath);
            return !String.IsNullOrEmpty(packagePath) && File.Exists(packagePath) && String.Compare(s_ManagementPackagePath, packagePath, true) != 0;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateInAppPurchasingInstallerProvider()
        {
            try
            {
                var packagePath = Path.GetFullPath(s_ManagementPackagePath);
                if (String.IsNullOrEmpty(packagePath) || !File.Exists(packagePath) || String.Compare(s_ManagementPackagePath, packagePath, true) == 0)
                    return new InAppPurchasingInstaller(Content.s_SettingsRootTitle);
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to create In-App Purchasing installer provider. The package path '{s_ManagementPackagePath}' is invalid or the file does not exist.");
            }

            return null;
        }

        private static void HelpBox(string message, string link, Action linkClicked)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(EditorGUIUtility.GetHelpIcon(MessageType.Info), GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            GUILayout.Label(message, Styles.textLabel);

            if (GUILayout.Button(link, Styles.linkLabel))
                linkClicked?.Invoke();

            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
