// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Diagnostics;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class AboutWindow : EditorWindow
    {
        static readonly Vector2 WindowSize = new Vector2(640, 265);
        internal static void ShowAboutWindow()
        {
            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var aboutRect = EditorGUIUtility.GetCenteredWindowPosition(mainWindowRect, WindowSize);

            AboutWindow w = GetWindowWithRect<AboutWindow>(aboutRect, true, "About Unity");
            w.position = aboutRect;
            w.minSize = w.maxSize = WindowSize;
            w.m_Parent.window.m_DontSaveToLayout = true;
        }

        bool m_ShowDetailedVersion = false;
        private int m_InternalCodeProgress;

        static class Styles
        {
            public static GUIContent thanksContent = new GUIContent("Special thanks to our beta users");
            public static Uri thanksUri = new Uri($"https://unity.com/releases/{GetVersion()}/thanks");

            public static readonly GUIStyle mainLayout = new GUIStyle() { margin = new RectOffset(40, 40, 30, 20) };
            public static readonly GUIStyle versionLayout = new GUIStyle() { margin = new RectOffset(0, 0, 10, 15) };
            public static readonly GUIStyle poweredLayout = new GUIStyle() { margin = new RectOffset(5, 5, 0, 15) };
            public static readonly GUIStyle poweredSectionLayout = new GUIStyle() { margin = new RectOffset(2, 2, 2, 2) };

            public static readonly GUIStyle aboutWindowLicenseLabel = new GUIStyle("AboutWindowLicenseLabel");

            public static GUIStyle versionStyle = EditorStyles.FromUSS(aboutWindowLicenseLabel, "About-Version-Label");
            public static GUIStyle thanksStyle = EditorStyles.FromUSS(aboutWindowLicenseLabel, "About-Thanks-Label");

            public static readonly GUIContent HeaderLogo = EditorGUIUtility.IconContent("AboutWindow.MainHeader");
            public static readonly GUIContent MonoLogo = EditorGUIUtility.IconContent("MonoLogo");
            public static readonly GUIContent AgeiaLogo = EditorGUIUtility.IconContent("AgeiaLogo");

            static readonly RectOffset NoRect = new RectOffset(0, 0, 0, 0);
            static readonly RectOffset LogoMargin = new RectOffset(2, 0, 15, 15);
            public static readonly GUIStyle LogoLayout = new GUIStyle() { fixedWidth = 0f, fixedHeight = 0, margin = LogoMargin, padding = NoRect, stretchHeight = false, stretchWidth = false };
            public static readonly GUIStyle HeaderLayout = new GUIStyle(LogoLayout) { margin = NoRect };
            public static readonly GUIStyle MonoLogoLayout = new GUIStyle(LogoLayout) { fixedHeight = 36.901f };
            public static readonly GUIStyle AgeiaLogoLayout = new GUIStyle(LogoLayout) { fixedHeight = 22.807f };

            private static string GetVersion()
            {
                var version = InternalEditorUtility.GetUnityVersion();
                return $"{version.Major}-{version.Minor}";
            }
        }

        public void OnGUI()
        {
            var evt = Event.current;
            var mainLayoutWidth = position.width - Styles.mainLayout.margin.horizontal;
            using (new GUILayout.VerticalScope(Styles.mainLayout))
            {
                GUILayout.Label(Styles.HeaderLogo, Styles.HeaderLayout);

                using (new GUILayout.HorizontalScope(Styles.versionLayout))
                {
                    ListenForSecretCodes();

                    string extensionVersion = FormatExtensionVersionString();

                    m_ShowDetailedVersion |= evt.alt;
                    if (m_ShowDetailedVersion)
                    {
                        int t = InternalEditorUtility.GetUnityVersionDate();
                        DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        string branch = InternalEditorUtility.GetUnityBuildBranch();
                        EditorGUILayout.SelectableLabel(
                            string.Format("{0}{1}\nRevision: {2} {3}\nBuilt: {4:r}",
                                InternalEditorUtility.GetUnityDisplayVersionVerbose(), extensionVersion,
                                branch, InternalEditorUtility.GetUnityBuildHash(), dt.AddSeconds(t)),
                            Styles.versionStyle, GUILayout.MaxWidth(mainLayoutWidth), GUILayout.Height(38f));
                    }
                    else
                    {
                        GUILayout.Label(string.Format("{0}{1}", InternalEditorUtility.GetUnityDisplayVersion(), extensionVersion), Styles.versionStyle);
                    }

                    if (evt.type == EventType.ValidateCommand)
                        return;
                }

                using (new GUILayout.HorizontalScope(Styles.poweredLayout))
                {
                    var poweredBySectionMaxWidth = (mainLayoutWidth - Styles.poweredSectionLayout.margin.horizontal * 2) / 2f;
                    using (new GUILayout.VerticalScope(Styles.poweredSectionLayout, GUILayout.MaxWidth(poweredBySectionMaxWidth)))
                    {
                        GUILayout.Label("Scripting powered by The Mono Project.\n\u00A9 2011 Novell, Inc.", Styles.aboutWindowLicenseLabel);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(Styles.MonoLogo, Styles.MonoLogoLayout);
                        GUILayout.FlexibleSpace();
                        if (!InternalEditorUtility.IsUnityBeta())
                        {
                            var specialThanksRect = GUILayoutUtility.GetRect(Styles.thanksContent, Styles.thanksStyle);
                            if (GUI.Button(specialThanksRect, Styles.thanksContent, Styles.thanksStyle))
                                Process.Start(Styles.thanksUri.AbsoluteUri);
                            EditorGUIUtility.AddCursorRect(specialThanksRect, MouseCursor.Link);
                        }
                        GUILayout.Label(InternalEditorUtility.GetUnityCopyright().Replace("(c)", "\u00A9"), Styles.aboutWindowLicenseLabel);
                    }

                    GUILayout.FlexibleSpace();

                    using (new GUILayout.VerticalScope(Styles.poweredSectionLayout, GUILayout.MaxWidth(poweredBySectionMaxWidth)))
                    {
                        GUILayout.Label("Physics powered by PhysX.\n\u00A9 2019 NVIDIA Corporation.", Styles.aboutWindowLicenseLabel);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(Styles.AgeiaLogo, Styles.AgeiaLogoLayout);
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(InternalEditorUtility.GetLicenseInfo().Replace("(c)", "\u00A9"), Styles.aboutWindowLicenseLabel);
                    }
                }
            }
        }

        private void ListenForSecretCodes()
        {
            if (Event.current.type != EventType.KeyDown || (int)Event.current.character == 0)
                return;

            if (SecretCodeHasBeenTyped("internal", ref m_InternalCodeProgress))
            {
                bool enabled = !EditorPrefs.GetBool("DeveloperMode", false);
                EditorPrefs.SetBool("DeveloperMode", enabled);
                ShowNotification(new GUIContent(string.Format(L10n.Tr("Developer Mode {0}"), (enabled ? L10n.Tr("On") : L10n.Tr("Off")))));
                EditorUtility.RequestScriptReload();

                // Repaint all views to show/hide debug repaint indicator
                InternalEditorUtility.RepaintAllViews();
            }
        }

        private bool SecretCodeHasBeenTyped(string code, ref int characterProgress)
        {
            if (characterProgress < 0 || characterProgress >= code.Length || code[characterProgress] != Event.current.character)
                characterProgress = 0;

            // Don't use else here. Even if key was mismatch, it should still be recognized as first key of sequence if it matches.
            if (code[characterProgress] == Event.current.character)
            {
                characterProgress++;

                if (characterProgress >= code.Length)
                {
                    characterProgress = 0;
                    return true;
                }
            }
            return false;
        }

        private string FormatExtensionVersionString()
        {
            string extStr = EditorUserBuildSettings.selectedBuildTargetGroup.ToString();
            string ext = Modules.ModuleManager.GetExtensionVersion(extStr);

            if (!string.IsNullOrEmpty(ext))
                return " [" + extStr + ": " + ext + "]";

            return "";
        }
    }
}
