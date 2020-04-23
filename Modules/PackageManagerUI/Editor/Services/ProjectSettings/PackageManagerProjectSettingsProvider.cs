// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageManagerProjectSettingsProvider
    {
        private static readonly string k_Message =
            "Preview packages are in the early stage of development and not yet ready for production.\n" +
            "We recommend using these only for testing purpose and to give us direct feedback.";

        private static class Styles
        {
            public static readonly GUIContent EnablePreviewPackagesLabel = new GUIContent(
                L10n.Tr("Enable Preview Packages"));

            public static readonly GUIStyle verticalStyle;
            public static readonly GUIStyle linkLabel;
            public static readonly GUIStyle textLabel;

            static Styles()
            {
                verticalStyle  = new GUIStyle(EditorStyles.inspectorFullWidthMargins);
                verticalStyle.margin = new RectOffset(10, 10, 10, 10);

                linkLabel = new GUIStyle(EditorStyles.linkLabel);
                linkLabel.fontSize = EditorStyles.miniLabel.fontSize;
                linkLabel.wordWrap = true;

                textLabel = new GUIStyle(EditorStyles.miniLabel);
                textLabel.wordWrap = true;
            }
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Package Manager", SettingsScope.Project)
            {
                guiHandler = searchContext =>
                {
                    var settings = ServicesContainer.instance.Resolve<PackageManagerProjectSettingsProxy>();
                    using (new EditorGUILayout.VerticalScope(Styles.verticalStyle))
                    {
                        var newValue = EditorGUILayout.Toggle(Styles.EnablePreviewPackagesLabel, settings.enablePreviewPackages);
                        if (newValue != settings.enablePreviewPackages)
                        {
                            var saveIt = true;
                            if (newValue && !settings.oneTimeWarningShown)
                            {
                                if (EditorUtility.DisplayDialog(L10n.Tr("Package Manager"), L10n.Tr(k_Message), L10n.Tr("I understand"), L10n.Tr("Cancel")))
                                    settings.oneTimeWarningShown = true;
                                else
                                    saveIt = false;
                            }

                            if (saveIt)
                            {
                                settings.enablePreviewPackages = newValue;
                                settings.Save();
                            }
                        }

                        GUILayout.Space(15);
                        HelpBox(L10n.Tr(k_Message), L10n.Tr("Read more"), () =>
                        {
                            var applicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
                            applicationProxy.OpenURL($"https://docs.unity3d.com/{applicationProxy.shortUnityVersion}/Documentation/Manual/pack-preview.html");
                        });
                    }
                },

                keywords = new List<string>(new[] { "enable", "package", "preview" })
            };

            return provider;
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
