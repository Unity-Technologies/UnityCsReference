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

        readonly string m_PackageName;
        readonly SettingsPageConfig m_Config;
        PackageManager.Requests.AddRequest m_AddManagementRequest;

        internal QuickInstallSettingsProvider
        (
            string packageName,
            SettingsPageConfig config,
            SettingsScope scopes = SettingsScope.Project
        ) : base(config.SettingsPath, scopes)
        {
            m_PackageName = packageName;
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

        void DrawPendingInstallation()
        {
            EditorGUILayout.LabelField(new GUIContent(L10n.Tr(m_Config.Installing)));
        }

        void DrawInstallWindow()
        {
            if (!string.IsNullOrEmpty(m_Config.Subtitle))
            {
                EditorGUILayout.LabelField(new GUIContent(L10n.Tr(m_Config.Subtitle)), Styles.textLabel);
                if (m_Config.ShowSeparator)
                    DrawSeparator();
            }

            EditorGUILayout.BeginVertical(Styles.verticalStyle);
            EditorGUILayout.LabelField(new GUIContent(L10n.Tr(m_Config.Body)), Styles.textLabel);
            if (!string.IsNullOrEmpty(m_Config.DocumentationUrl) && GUILayout.Button(L10n.Tr("Read more"), Styles.linkLabel))
                Application.OpenURL(m_Config.DocumentationUrl);
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(15);
            if (GUILayout.Button(new GUIContent(L10n.Tr(m_Config.InstallButton))))
            {
                m_AddManagementRequest = QuickInstaller.InstallPackage(m_PackageName, InstallMethod.ProjectSettings);
            }
        }

        void DrawSeparator()
        {
            GUIStyle separator = "sv_iconselector_sep";
            GUILayout.Space(10);
            GUILayout.Label("", separator);
            GUILayout.Space(10);
        }
    }
}
