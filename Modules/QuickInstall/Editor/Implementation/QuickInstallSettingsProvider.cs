// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.QuickInstall
{
    internal class QuickInstallSettingsProvider : SettingsProvider
    {
        static class Styles
        {
            public static readonly GUIStyle verticalStyle = new(EditorStyles.inspectorFullWidthMargins);
            public static readonly GUIStyle linkLabel = new(EditorStyles.linkLabel);
            public static readonly GUIStyle textLabel = new(EditorStyles.wordWrappedLabel);

            static Styles()
            {
                verticalStyle.margin = new RectOffset(10, 10, 10, 10);

                linkLabel.fontSize = EditorStyles.miniLabel.fontSize;
                linkLabel.wordWrap = true;

                textLabel.wordWrap = true;
            }
        }

        readonly string m_PackageId;
        readonly SettingsProviderConfig m_Config;
        PackageManager.Requests.AddRequest m_AddManagementRequest;

        internal QuickInstallSettingsProvider
        (
            string packageId,
            SettingsProviderConfig config,
            SettingsScope scopes = SettingsScope.Project
        ) : base(config.settingsRootTitle, scopes)
        {
            m_PackageId = packageId;
            m_Config = config;
        }

        public override void OnGUI(string _)
        {
            GUILayout.Space(15);

            if (m_AddManagementRequest is null)
                DrawInstallWindow();
            else
                DrawPendingInstallation();
        }

        protected void DrawPendingInstallation()
        {
            if (m_AddManagementRequest.IsCompleted)
                EditorGUILayout.LabelField(new GUIContent(L10n.Tr(m_Config.downloadingText)));
            else
                EditorGUILayout.LabelField(new GUIContent(L10n.Tr(m_Config.installingText)));
        }

        protected void DrawInstallWindow()
        {
            if (m_Config.showSubtitle && !string.IsNullOrEmpty(m_Config.subtitle))
                EditorGUILayout.LabelField(new GUIContent(L10n.Tr(m_Config.subtitle)), Styles.textLabel);

            if (m_Config.showSeparator)
                DrawSeparator();

            if (string.IsNullOrEmpty(m_Config.subtitle))
            {
                // Use HelpBox for InAppPurchasing style
                var onClick = () => Application.OpenURL(m_Config.documentationUri.AbsoluteUri);
                HelpBox(L10n.Tr(m_Config.installationHelpText), L10n.Tr("Read more"), onClick);
            }
            else
            {
                // Use regular label for LevelPlay style
                EditorGUILayout.LabelField(L10n.Tr(m_Config.installationHelpText), Styles.textLabel);
            }

            if (m_Config.showDocumentationButton)
            {
                if (GUILayout.Button("Documentation", Styles.linkLabel))
                    Application.OpenURL(m_Config.documentationUri.AbsoluteUri);
                
            }

            GUILayout.Space(15);
            if (GUILayout.Button(EditorGUIUtility.TrTextContent(L10n.Tr(m_Config.installButtonText))))
            {
                QuickInstaller.InstallPackage(m_PackageId, InstallMethod.ProjectSettings);
                m_AddManagementRequest = PackageManager.Client.Add(m_PackageId);
            }
        }

        protected void DrawSeparator()
        {
            GUIStyle separator = "sv_iconselector_sep";
            GUILayout.Space(10);
            GUILayout.Label("", separator);
            GUILayout.Space(10);
        }

        protected void HelpBox(string text, string linkText, Action linkAction)
        {
            EditorGUILayout.BeginVertical(Styles.verticalStyle);
            EditorGUILayout.LabelField(text, Styles.textLabel);
            if (GUILayout.Button(linkText, Styles.linkLabel))
                linkAction?.Invoke();
            
            EditorGUILayout.EndVertical();
        }
    }
}
