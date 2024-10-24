// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager.UI.Internal;
using System.Linq;

namespace UnityEditor
{
    class SafeModeToolbarWindow : EditorWindow
    {
        const string k_SafeModeManualPage = "https://docs.unity3d.com/Manual/SafeMode.html";

        bool m_IsProSkin = false;
        const float k_Space = 10;

        private bool m_IsPreviewPackagesInUse;

        [NonSerialized]
        private IApplicationProxy m_ApplicationProxy;
        [NonSerialized]
        private IUpmClient m_UpmClient;
        [NonSerialized]
        private IProjectSettingsProxy m_SettingsProxy;

        private static class Styles
        {
            public static readonly GUIStyle appToolbar = "AppToolbar";
            public static readonly GUIStyle forceExitButton = new GUIStyle("LargeButton") { fixedWidth = 110 };
            public static readonly GUIStyle title = new GUIStyle("LargeLabel") { fontSize = 15, alignment = TextAnchor.MiddleCenter, normal = EditorStyles.wordWrappedLabel.normal };
            public static readonly GUIStyle message = new GUIStyle("WordWrappedLabel") { alignment = TextAnchor.MiddleLeft };
            public static readonly GUIStyle helpBox = new GUIStyle("HelpBox") { alignment = TextAnchor.MiddleLeft, margin = new RectOffset(5, 5, 5, 5) };
            public static readonly GUIContent messageContent = EditorGUIUtility.TrTextContent("Unity is in Safe Mode because we detected scripts with compilation errors upon opening this project. Use this mode to fix script errors listed in the Console before exiting, which will import any remaining assets.");
            public static readonly GUIStyle previewPackageInUseDropdown = "PreviewPackageInUse";
            public static readonly GUIContent previewPackageContent = EditorGUIUtility.TrTextContent("Experimental Packages in Use");
            public static readonly GUIContent previewPackageIcon = EditorGUIUtility.TrIconContent("PreviewPackageInUse", "Experimental Packages in Use");
            public static readonly GUIContent learnModeContent = EditorGUIUtility.TrTextContent("Learn More");
            public static readonly GUIContent safeModeExitContent = EditorGUIUtility.TrTextContent("Exit Safe Mode");
            public static readonly GUIContent csScriptIcon = EditorGUIUtility.IconContent("cs Script Icon");

            public static float helpBoxWidth = 450;

            static Styles()
            {
                forceExitButton.margin.left = 10;
                forceExitButton.margin.right = 10;
            }
        }

        [CommandHandler("SafeMode/Exit", CommandHint.Menu)]
        internal static void TryForceExitSafeModeMenu(CommandExecuteContext context)
        {
            TryForceExitSafeMode();
        }

        static void TryForceExitSafeMode()
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Exit Safe Mode blocked"), L10n.Tr("Cannot exit Safe Mode while compiling scripts"), L10n.Tr("OK"));
                return;
            }

            if (EditorUtility.DisplayDialog(L10n.Tr("Exiting Safe Mode"),
                L10n.Tr("Are you sure you want to exit?\n\n" +
                    "Exiting Safe Mode while you still have compilation errors may cause your project to be in an incomplete or broken state. " +
                    "For example, packages in your project may fail to load, and your assets may not import correctly."),
                L10n.Tr("Cancel"), L10n.Tr("Exit Anyway")) == false) // the default btn is Cancel, when dialog returns true. Exit Anyway returns false.
            {
                EditorUtility.RequestPartialScriptReload();
            }
        }

        internal void OnEnable()
        {
            m_IsProSkin = EditorGUIUtility.isProSkin;
            EditorApplication.updateMainWindowTitle += UpdateSafeModeTitle;

            m_UpmClient = ServicesContainer.instance.Resolve<IUpmClient>();
            m_SettingsProxy = ServicesContainer.instance.Resolve<IProjectSettingsProxy>();
            m_ApplicationProxy = ServicesContainer.instance.Resolve<IApplicationProxy>();
            RefreshIsPreviewPackagesInUse();

            PackageManager.Events.registeredPackages += RegisteredPackagesEventHandler;
        }

        internal void OnDisable()
        {
            EditorApplication.updateMainWindowTitle -= UpdateSafeModeTitle;
            PackageManager.Events.registeredPackages -= RegisteredPackagesEventHandler;
        }

        private void UpdateSafeModeTitle(ApplicationTitleDescriptor desc)
        {
            desc.title = $"{desc.projectName} - SAFE MODE - {desc.unityVersion}";
        }

        private void RegisteredPackagesEventHandler(PackageRegistrationEventArgs packageRegistrationEventArgs)
        {
            RefreshIsPreviewPackagesInUse();
        }

        private void RefreshIsPreviewPackagesInUse()
        {
            m_IsPreviewPackagesInUse = m_UpmClient.IsAnyExperimentalPackagesInUse();
        }

        internal void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
                Styles.appToolbar.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            GUILayout.BeginHorizontal();
            {
                const int k_MinWidthChangePreviewPackageInUseToIcon = 1100;
                const float k_PreviewPackagesinUseCompleteWidth = 193;
                const float k_PreviewPackagesinUseIconWidth = 45;
                var previewPackagesinUseWidth = k_PreviewPackagesinUseCompleteWidth;
                var useIcon = false;

                var showPreviewPackagesInUseDropDown = m_IsPreviewPackagesInUse && !m_SettingsProxy.dismissPreviewPackagesInUse;
                if (showPreviewPackagesInUseDropDown)
                {
                    useIcon = position.width < k_MinWidthChangePreviewPackageInUseToIcon;
                    previewPackagesinUseWidth = useIcon ? k_PreviewPackagesinUseIconWidth : k_PreviewPackagesinUseCompleteWidth;
                }

                OnHelpBoxGUI(showPreviewPackagesInUseDropDown ? position.width - previewPackagesinUseWidth - k_Space * 3 : position.width - k_Space);

                if (showPreviewPackagesInUseDropDown)
                    OnPreviewPackagesInUseGUI(useIcon, previewPackagesinUseWidth);

                // TODO Controls collab go here
            }
            GUILayout.EndHorizontal();
        }

        void OnHelpBoxGUI(float width)
        {
            GUILayout.BeginHorizontal(Styles.helpBox, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true), GUILayout.Width(width));
            {
                // Script Icon
                EditorGUIUtility.SetIconSize(Vector2.one * 32);
                GUILayout.Label(Styles.csScriptIcon, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));
                EditorGUIUtility.SetIconSize(Vector2.zero);

                // Title
                GUILayout.Label("Safe Mode", Styles.title, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(true));

                GUILayout.Space(10);

                // Message
                EditorGUILayout.LabelField(Styles.messageContent, Styles.message, GUILayout.MaxWidth(Styles.helpBoxWidth), GUILayout.ExpandHeight(true));

                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical(GUILayout.ExpandWidth(false));
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal(GUILayout.ExpandWidth(false));
                    {
                        // Learn More
                        if (EditorGUILayout.LinkButton(Styles.learnModeContent, GUILayout.ExpandWidth(false)))
                            Help.ShowHelpPage(k_SafeModeManualPage);

                        // Exit Safe mode
                        if (GUILayout.Button(Styles.safeModeExitContent, Styles.forceExitButton, GUILayout.ExpandWidth(false)))
                            TryForceExitSafeMode();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        void OnPreviewPackagesInUseGUI(bool useIcon, float width)
        {
            GUILayout.Space(k_Space);
            GUILayout.BeginVertical(GUILayout.Width(width), GUILayout.ExpandWidth(false));
            {
                GUILayout.FlexibleSpace();
                var dropDownCustomColor = new GUIStyle(Styles.previewPackageInUseDropdown);
                var content = useIcon ? Styles.previewPackageIcon : Styles.previewPackageContent;
                var rectPos = GUILayoutUtility.GetRect(content, Styles.previewPackageInUseDropdown);

                if (EditorGUI.DropdownButton(rectPos, content, FocusType.Passive, dropDownCustomColor))
                    ShowPreviewPackageInUseMenu(EditorToolGUI.GetToolbarEntryRect(rectPos));
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
        }

        void ShowPreviewPackageInUseMenu(Rect rect)
        {
            var menu = new GenericMenu();

            // Here hide the button : reappear after creating a new unity project.
            menu.AddItem(EditorGUIUtility.TrTextContent("Dismiss"), false, () =>
            {
                m_SettingsProxy.dismissPreviewPackagesInUse = true;
                m_SettingsProxy.Save();
            });
            menu.AddSeparator("");

            // Here we open the package manager, In-Project open and search field have experimental.
            menu.AddItem(EditorGUIUtility.TrTextContent("Show Experimental Packages..."), false, () =>
            {
                PackageManagerWindow.OpenAndSelectPage(InProjectPage.k_Id, "experimental");
            });
            menu.AddSeparator("");

            // Here we go to the link explaining why we see this...
            menu.AddItem(EditorGUIUtility.TrTextContent("Why am I seeing this?"), false, () =>
            {
                m_ApplicationProxy.OpenURL($"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/pack-exp.html");
            });

            menu.DropDown(rect, true);
        }
    }
}
