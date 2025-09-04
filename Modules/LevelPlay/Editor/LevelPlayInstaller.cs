// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.LevelPlay
{
    internal class LevelPlayInstaller : SettingsProvider
    {
        internal static string s_ManagementPackageId = "com.unity.services.levelplay";

        private static string s_ManagementPackagePath = $"Packages/{s_ManagementPackageId}/package.json";
        private PackageManager.Requests.AddRequest m_AddManagementRequest = null;
        private static string s_ProjectSettingsInstallationMethod = "projectSettings";

        private static class Content
        {
            internal const string s_InstallationHelpText = "Drive higher revenue with the leading ads mediation platform from Unity. LevelPlay gives you access to a unified auction of all the leading SDK networks and bidders to maximize competition and increase your potential earnings. Serve a diverse mix of ad formats, get real time monetization data, A/B test adjustments to your ad strategy and more.";
            internal static readonly string s_SettingsRootTitle = "Project/Services/Ads Mediation (LevelPlay)";
            internal static readonly Uri s_DocUri = new Uri("https://developers.is.com/ironsource-mobile/unity/unity-plugin/");
            internal static readonly GUIContent s_AddLevelPlay = EditorGUIUtility.TrTextContent(L10n.Tr("Install LevelPlay"));
            internal static readonly GUIContent s_Subtitle = new GUIContent(L10n.Tr("Monetize your game with Unity LevelPlay"));
            internal static readonly GUIContent s_DownloadingText = new GUIContent(L10n.Tr("Downloading LevelPlay ..."));
            internal static readonly GUIContent s_InstallingText = new GUIContent(L10n.Tr("Installing LevelPlay ..."));
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
                linkLabel.wordWrap = true;

                textLabel = new GUIStyle(EditorStyles.wordWrappedLabel);
            }
        }

        private LevelPlayInstaller(string path, SettingsScope scopes = SettingsScope.Project) : base(path, scopes)
        {
        }

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
                EditorGUILayout.LabelField(Content.s_Subtitle, Styles.textLabel);

                DrawSeparator();

                EditorGUILayout.LabelField(L10n.Tr(Content.s_InstallationHelpText), Styles.textLabel);

                if (GUILayout.Button("Documentation", Styles.linkLabel))
                {
                    Application.OpenURL(Content.s_DocUri.AbsoluteUri); // Open the documentation URL
                }

                GUILayout.Space(15);
                if (GUILayout.Button(Content.s_AddLevelPlay))
                {
                    m_AddManagementRequest = PackageManager.Client.Add(s_ManagementPackageId);
                    LevelPlayQuickInstallAnalytic.SendEvent(s_ProjectSettingsInstallationMethod);
                }
            }
        }

        private void DrawSeparator()
        {
            GUIStyle seperator = "sv_iconselector_sep";
            GUILayout.Space(10);
            GUILayout.Label("", seperator);
            GUILayout.Space(10);
        }

        static public bool PackageIsInstalled()
        {
            var packagePath = Path.GetFullPath(s_ManagementPackagePath);
            return !string.IsNullOrEmpty(packagePath) && File.Exists(packagePath) && string.Compare(s_ManagementPackagePath, packagePath, true) != 0;
        }

        [SettingsProvider]
        internal static SettingsProvider CreateLevelPlayInstallerProvider()
        {
            try
            {
                if (!PackageIsInstalled())
                    return new LevelPlayInstaller(Content.s_SettingsRootTitle);
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to create LevelPlay installer provider. The package path '{s_ManagementPackagePath}' is invalid or the file does not exist.");
            }

            return null;
        }
    }
}
