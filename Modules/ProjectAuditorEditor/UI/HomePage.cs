// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define PA_DRAW_LOGO

using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

using AnalysisState = Unity.ProjectAuditor.Editor.UI.ProjectAuditorWindow.AnalysisState;

namespace Unity.ProjectAuditor.Editor.UI
{
    [System.Serializable]
    internal partial class HomePage
    {
        [AutoStaticsCleanupOnCodeReload]
        static AddRequest RulesPackageInstallRequest;

        enum Tab
        {
            Optimization,
            Upgrade,
            MigrateToURP,
            MigrateToCoreCLR,
        }

        const int k_TabViewWidth = 580;
        const int k_TabViewHeight = 380;
        internal const int k_SpacingHeight = 24;
        internal const int k_ButtonWidth = 160;
        internal const int k_ButtonHeight = 30;

        [SerializeField]
        Tab m_SelectedTab;
        int m_TabButtonControlID;

        MigrationWorkflowView m_MigrationToURP = new MigrationWorkflowView();

        public HomePage()
        {
        }

        public void OnGUI()
        {

            // Darkish grey box filling the window
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

            // Draw centered in the window, with equal space to the left and right
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                // Begin drawing top to bottom
                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(k_TabViewWidth), GUILayout.ExpandWidth(true)))
                {
                    GUILayout.FlexibleSpace();


                    DrawTabs();
                    DrawRules();

                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawRules()
        {
            if (ProjectAuditorRulesPackage.IsLatest)
                return;

            EditorGUILayout.Space(k_SpacingHeight);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var window = ProjectAuditorWindow.Instance;
                var analysisState = window.m_AnalysisState;

                using (new EditorGUI.DisabledScope((analysisState == AnalysisState.InProgress) || (RulesPackageInstallRequest != null)))
                {
                    GUIContent button;
                    string msg;
                    MessageType messageType;

                    if (RulesPackageInstallRequest != null)
                    {
                        int frame = Utility.GetStatusWheelFrame();
                        button = Contents.UpdateRulesButtonInProgress[frame];
                        msg = ProjectAuditorRulesPackage.IsInstalled ? Contents.UpdateRulesText : Contents.InstallRulesText;
                        messageType = ProjectAuditorRulesPackage.IsInstalled ? MessageType.Warning : MessageType.Error;
                    }
                    else if (!ProjectAuditorRulesPackage.IsInstalled)
                    {
                        button = Contents.InstallRulesButton;
                        msg = Contents.InstallRulesText;
                        messageType = MessageType.Error;
                    }
                    else
                    {
                        button = Contents.UpdateRulesButton;
                        msg = Contents.UpdateRulesText;
                        messageType = MessageType.Warning;
                    }

                    Utility.DrawHelpBoxWithButton(
                        msg,
                        button,
                        messageType,
                        () =>
                        {
                            RulesPackageInstallRequest = Client.Add(ProjectAuditorRulesPackage.Name);
                            EditorApplication.update += RulesPackageInstallProgressCallback;
                        });
                }

                GUILayout.FlexibleSpace();
            }
        }

        void DrawTabs()
        {
            // Tab headings
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal(SharedStyles.TabBackground, GUILayout.Width(k_TabViewWidth));
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal(GUILayout.Width(k_TabViewWidth - 24)); // indent tabs a bit

                for (var i = 0; i < Contents.TabNames.Length; i++)
                {
                    if (DrawTabButton(Contents.TabNames[i], (int)m_SelectedTab == i))
                    {
                        m_SelectedTab = (Tab)i;
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }

            // Active tab
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(SharedStyles.TabBackground, GUILayout.Width(k_TabViewWidth), GUILayout.Height(k_TabViewHeight)))
                {
                    switch (m_SelectedTab)
                    {
                        case Tab.Optimization:
                            DrawTab(Contents.OptimizationText, PageId.Optimization, true);
                            break;
                        case Tab.Upgrade:
                            DrawTab(Contents.UpgradeText, PageId.Upgrade, true);
                            break;
                        case Tab.MigrateToURP:
                            m_MigrationToURP.DrawContent();
                            break;
                        case Tab.MigrateToCoreCLR:
                            DrawTab(Contents.MigrateToCoreCLRText, PageId.MigrationToCoreCLR, false);
                            break;
                    }
                }
                GUILayout.FlexibleSpace();
            }
        }

        bool DrawTabButton(string text, bool isActive)
        {
            EditorGUILayout.BeginVertical();

            const int k_TabButtonHeight = 27;

            var content = Utility.TempContent(text);
            bool wasButtonClicked = GUILayout.Button(content, SharedStyles.TabButton, GUILayout.Height(k_TabButtonHeight));
            int id = GUIUtility.GetControlID(content, FocusType.Passive);
            var lastRect = GUILayoutUtility.GetLastRect();
            var isHoverState = lastRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.MouseMove)
            {
                if (isHoverState)
                {
                    if (m_TabButtonControlID != id)
                    {
                        m_TabButtonControlID = id;
                        ProjectAuditorWindow.Instance.Repaint();
                    }
                }
                else
                {
                    if (m_TabButtonControlID == id)
                    {
                        m_TabButtonControlID = 0;
                        ProjectAuditorWindow.Instance.Repaint();
                    }
                }
            }

            var draw2D = ProjectAuditorWindow.Instance.Draw2D;
            var rect = EditorGUILayout.GetControlRect(false, 3, SharedStyles.TabBackground, GUILayout.Height(2));
            if ((isActive || isHoverState) && draw2D.DrawStart(rect))
            {
                var color = isActive ? SharedStyles.TabBottomActiveColor : SharedStyles.TabBottomHoverColor;

                draw2D.DrawFilledBox(0, 0, lastRect.width, 2.5f, color);
                draw2D.DrawEnd();
            }

            EditorGUILayout.EndVertical();

            return wasButtonClicked;
        }

        static void DrawTab(GUIContent title, PageId targetSummaryPage, bool includeProjectAreas)
        {
            DrawTabTitleText(title);
            DrawSharedPreferences(includeProjectAreas);

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(k_ButtonWidth)))
                {
                    DrawAnalyzeButton(targetSummaryPage);
                    DrawPreferencesButton();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(8);
        }

        internal static void DrawTabTitleText(GUIContent title)
        {
            GUILayout.Space(k_SpacingHeight);
            EditorGUILayout.LabelField(title, SharedStyles.WelcomeTextArea, GUILayout.MaxWidth(k_TabViewWidth - 32), GUILayout.Height(k_SpacingHeight * 2));
            GUILayout.Space(k_SpacingHeight - 6);
        }

        static void DrawSharedPreferences(bool includeProjectAreas)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(350), GUILayout.Height(140)))
                {
                    using (new EditorGUI.DisabledScope(RulesPackageInstallRequest != null || !ProjectAuditorRulesPackage.IsInstalled))
                    {
                        if (includeProjectAreas)
                        {
                            UserPreferences.ProjectAreasPreferencesGUI();
                            UserPreferences.SharedPreferencesGUI(UserPreferences.ProjectAreasToAnalyze);
                        }
                        else
                        {
                            UserPreferences.SharedPreferencesGUI(ProjectAreaFlags.Code);
                        }
                    }
                    GUILayout.Space(k_SpacingHeight);
                }
                GUILayout.FlexibleSpace();
            }
        }

        static void DrawAnalyzeButton(PageId targetSummaryPage)
        {
            var window = ProjectAuditorWindow.Instance;
            var analysisState = window.m_AnalysisState;

            using (new EditorGUI.DisabledScope((analysisState == AnalysisState.InProgress) || (ProjectAuditorRulesPackage.IsInstalled == false) || (RulesPackageInstallRequest != null)))
            {
                var content = ProjectAuditorRulesPackage.IsInstalled ? Contents.AnalyzeButton : Contents.AnalyzeButtonDisabled;
                if (window.m_ViewManager.HasPendingCategories())
                {
                    int frame = Utility.GetStatusWheelFrame();
                    content = Contents.AnalyzeButtonInProgress[frame];
                }

                if (GUILayout.Button(content, GUILayout.Width(k_ButtonWidth), GUILayout.Height(k_ButtonHeight)))
                {
                    bool canAnalyze = true;

                    // m_Report can be null here (e.g. after cancelling an analysis)
                    // In this case, there is nothing to save/discard.
                    if (window.m_Report != null && window.m_Report.NeedsSaving)
                    {
                        DialogResult response = DialogResult.DefaultAction;
                        if (analysisState == AnalysisState.Valid)
                            response = EditorDialog.DisplayComplexDecisionDialog(Contents.Discard, Contents.DiscardQuestion, "Discard", "Save", "Cancel");
                        else
                            response = EditorUtility.DisplayDialog(Contents.Discard, Contents.DiscardQuestion, "Discard", "Cancel") ? DialogResult.DefaultAction : DialogResult.Cancel;

                        if (response == DialogResult.AlternateAction)
                        {
                            if (!ProjectAuditorWindow.SaveReport(window.m_Report, out var _))
                                canAnalyze = false;
                        }
                        else if (response == DialogResult.Cancel)
                        {
                            canAnalyze = false;
                        }
                    }

                    ProjectAreaFlags areasToAnalyze = UserPreferences.ProjectAreasToAnalyze;
                    if (canAnalyze)
                    {
                        // CoreCLR only has code issues
                        if (targetSummaryPage == PageId.MigrationToCoreCLR)
                        {
                            areasToAnalyze = ProjectAreaFlags.Code;
                        }
                        else if (areasToAnalyze == ProjectAreaFlags.None)
                        {
                            canAnalyze = false;
                            if (EditorUtility.DisplayDialog(Contents.EnableAreas, Contents.EnableAreasQuestion, "Ok", "Cancel"))
                            {
                                UserPreferences.ProjectAreasToAnalyze.Set(ProjectAreaFlags.All);
                                areasToAnalyze = ProjectAreaFlags.All;
                                canAnalyze = true;
                            }
                        }

                        if ((areasToAnalyze & ProjectAreaFlags.Code) != 0)
                        {
                            if (canAnalyze)
                                canAnalyze = ProjectAuditorWindow.ValidateCodeAnalysisWithPopup();
                        }
                    }

                    if (canAnalyze)
                    {
                        window.Analyze(areasToAnalyze, UserPreferences.CodeAnalysisFlags, targetSummaryPage);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        static void DrawPreferencesButton()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(Contents.AllPreferences, SharedStyles.LinkLabel, GUILayout.Height(k_ButtonHeight)))
                {
                    EditorInterop.OpenProjectAuditorPreferences();
                }
                GUILayout.FlexibleSpace();
            }
        }

        static void RulesPackageInstallProgressCallback()
        {
            var wnd = EditorWindow.GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null)
                wnd.Repaint();

            if (RulesPackageInstallRequest.IsCompleted)
            {
                if (RulesPackageInstallRequest.Status == StatusCode.Success)
                {
                    Debug.Log("Installed: " + RulesPackageInstallRequest.Result.packageId);
                    Events.registeredPackages += OnRulesPackageRegistered;
                }
                else if (RulesPackageInstallRequest.Status >= StatusCode.Failure)
                {
                    Debug.Log(RulesPackageInstallRequest.Error.message);
                }

                EditorApplication.update -= RulesPackageInstallProgressCallback;
                RulesPackageInstallRequest = null;
            }
        }

        static void OnRulesPackageRegistered(PackageRegistrationEventArgs args)
        {
#pragma warning disable UAC2001
            foreach (var p in args.added.Concat(args.changedTo))
#pragma warning restore UAC2001
            {
                if (p.name == ProjectAuditorRulesPackage.Name)
                {
                    Events.registeredPackages -= OnRulesPackageRegistered;
                    ProjectAuditorRulesPackage.Initialize();
                    ProjectAuditorWindow.Instance?.m_ProjectAuditor?.InitModules();
                    return;
                }
            }
        }

        internal static class Contents
        {
            public static readonly string[] TabNames =
            {
                L10n.Tr("Optimization", null),
                L10n.Tr("Upgrade", null),
                L10n.Tr("Migrate to URP", null),
                L10n.Tr("Migrate to CoreCLR", null),
            };

            public static readonly GUIContent OptimizationText = L10n.TextContent(
@"Choose which areas of your project to analyze, then select <b>Start Analysis</b> to generate a report that focuses on optimization and correctness issues.",
null, null, null);

            public static readonly GUIContent UpgradeText = L10n.TextContent(
@"Choose which areas of your project to analyze, then select <b>Start Analysis</b> to generate a report that focuses on upgrading to a new version of Unity.",
null, null, null);

            public static readonly GUIContent MigrateToCoreCLRText = L10n.TextContent(
@"Select <b>Start Analysis</b> to generate a report that focuses on supporting CoreCLR in your project.",
null, null, null);

            public static readonly GUIContent AllPreferences = L10n.TextContent("All Preferences", null, null, null);

            public static readonly GUIContent AnalyzeButton = L10n.TextContent("Start Analysis", "Analyze Project and list all issues found.", null, null);
            public static readonly GUIContent AnalyzeButtonDisabled = L10n.TextContent("Start Analysis", $"Please install the rules package to analyze your project ({ProjectAuditorRulesPackage.Name}).", null, null);
            public static readonly GUIContent[] AnalyzeButtonInProgress;

            public static readonly GUIContent InstallRulesButton = L10n.TextContent("Install Rules", $"Please install the rules package to analyze your project ({ProjectAuditorRulesPackage.Name}).", null, null);
            public static readonly GUIContent UpdateRulesButton = L10n.TextContent("Update Rules", $"Please update your rules package to the latest version ({ProjectAuditorRulesPackage.Name}@{ProjectAuditorRulesPackage.LatestVersion}).", null, null);
            public static readonly GUIContent[] UpdateRulesButtonInProgress;

            public static readonly string InstallRulesText = L10n.Tr("Project Auditor needs the Rules Package to run an analysis.", null);
            public static readonly string UpdateRulesText = L10n.Tr($"Rules Package version {ProjectAuditorRulesPackage.LatestVersion} is available", null);

            public static readonly string Discard = L10n.Tr("Start New Analysis");
            public static readonly string DiscardQuestion = L10n.Tr("If you start a new analysis, the current report will be discarded.");
            public static readonly string EnableAreas = L10n.Tr("No Project Areas selected");
            public static readonly string EnableAreasQuestion = L10n.Tr("Enable all analysis areas and continue?\n\nAreas can be individually toggled in the Project Auditor section of Preferences.");


            static Contents()
            {
                UpdateRulesButtonInProgress = new GUIContent[12];
                AnalyzeButtonInProgress = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                {
                    UpdateRulesButtonInProgress[i] = L10n.TextContentWithIcon(" Installing Rules...", null, "WaitSpin" + i.ToString("00"), null);
                    AnalyzeButtonInProgress[i] = L10n.TextContentWithIcon(" Analyzing...", null, "WaitSpin" + i.ToString("00"), null);
                }
            }
        }
    }
}
