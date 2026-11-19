// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal partial class MigrationWorkflowView
    {
        const ProjectAreaFlags k_ProjectAreaFlags =
            ProjectAreaFlags.ProjectSettings | ProjectAreaFlags.Code |
            ProjectAreaFlags.Assets | ProjectAreaFlags.GameObjects;

        const CodeAnalysisFlags k_CodeAnalysisFlags =
            CodeAnalysisFlags.Player | CodeAnalysisFlags.Packages;

        List<Step> m_Steps;

        UnityEditor.PackageManager.Requests.AddRequest m_UrpAddRequest;

        [AutoStaticsCleanupOnCodeReload]
        static bool s_ConverterRunning;

        internal static bool ConverterRunning => s_ConverterRunning;

        static MigrateToURPSummaryView GetSummaryView(ProjectAuditorWindow window)
        {
            var result = (MigrateToURPSummaryView)window.m_ViewManager.GetView(IssueCategory.MigrateToURPSummary);
            result.RefreshIfDirty();
            return result;
        }

        internal const string k_BackupAcknowledgedKey = "ProjectAuditor.MigrationWizard.BackupAcknowledged";

        internal static bool BackupAcknowledged
        {
            get => SessionState.GetBool(k_BackupAcknowledgedKey, false);
            set => SessionState.SetBool(k_BackupAcknowledgedKey, value);
        }

        const string k_AnalysisCompletedKey = "ProjectAuditor.MigrationWizard.AnalysisCompleted";

        // Keeps the Analyze step marked complete while a re-analysis is in progress, so the wizard stays on the Fix step.
        static bool AnalysisCompleted
        {
            get => SessionState.GetBool(k_AnalysisCompletedKey, false);
            set => SessionState.SetBool(k_AnalysisCompletedKey, value);
        }

        internal const string k_TargetPipelineKey ="ProjectAuditor.MigrationWizard.TargetPipeline";

        // The pending choice for the next analysis. Once a matching Report exists, SyncTargetPipelineFromReport
        // seeds this from the Report instead, so a reloaded Report is reflected rather than overridden.
        internal static string SelectedTargetPipeline
        {
            get => SessionState.GetString(k_TargetPipelineKey, MigrationToURPUtilities.DefaultTargetPipelineContainer);
            set => SessionState.SetString(k_TargetPipelineKey, value);
        }

        [AutoStaticsCleanupOnCodeReload]
        static Report s_LastSyncedReport;

        static class BirpDeprecationDialog
        {
            static class Styles
            {
                public static readonly string DialogTitle = L10n.Tr("The Built-In Render Pipeline is not supported", null);
                public static readonly string DialogMessage = L10n.Tr("The Built-In Render Pipeline is no longer supported. Migrate to a Scriptable Render Pipeline in order to render.\n\nContinue Without Rendering turns off this prompt for everyone on the project. You can migrate later from Project Auditor.", null);
                public static readonly string StartMigrationToURP = L10n.Tr("Start Migration to Universal Render Pipeline", null);
                public static readonly string ContinueWithoutRendering = L10n.Tr("Continue Without Rendering", null);
                public static readonly string RemindMeLater = L10n.Tr("Remind Me Later", null);
            }

            public static void Show()
            {
                int result = EditorUtility.DisplayDialogComplex(
                    Styles.DialogTitle,
                    Styles.DialogMessage,
                    Styles.StartMigrationToURP,
                    Styles.RemindMeLater,
                    Styles.ContinueWithoutRendering);

                switch (result)
                {
                    case 0: // Start Migration to URP
                        StartMigrationToURP();
                        break;
                    case 1: // Remind Me Later
                        break;
                    case 2: // Continue Without Rendering
                        UnityEditor.Rendering.EditorGraphicsSettings.builtInRenderPipelineUsage =
                            UnityEditor.Rendering.BuiltInRenderPipelineUsage.NotInUse;
                        break;
                }
            }
        }

        static void StartMigrationToURP()
        {
            ProjectAuditorNavigator.RequestHomePage(HomePage.Tab.MigrateToURP);
            ProjectAuditorWindow.ShowWindow();
        }

        struct StartupPromptShownState
        {
            const string k_StartupPromptShownKey = "ProjectAuditor.RenderPipelineStartupPrompt.Shown";
            public static bool Shown
            {
                get => SessionState.GetBool(k_StartupPromptShownKey, false);
                set => SessionState.SetBool(k_StartupPromptShownKey, value);
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterStartMigration()
        {
            UnityEditor.Rendering.EditorGraphicsSettings.startBuiltInRenderPipelineMigration = StartMigrationToURP;
        }

        [InitializeOnLoadMethod]
        static void ShowStartupPromptIfNeeded()
        {
            if (StartupPromptShownState.Shown)
                return;

            EditorApplication.delayCall += OnDelayCall;
        }

        static void OnDelayCall()
        {
            EditorApplication.delayCall -= OnDelayCall;

            StartupPromptShownState.Shown = true;

            if (!UnityEditor.Rendering.EditorGraphicsSettings.ShouldDisplayBuiltInRenderPipelineMigrationDialog())
                return;

            BirpDeprecationDialog.Show();
        }

        public MigrationWorkflowView()
        {
        }

        // Seeds SelectedTargetPipeline from a newly-seen Report, so opening the window on an existing Report
        // (rather than picking one via the dropdown) shows the pipeline it was actually scanned against.
        // A project already migrated to the selected pipeline keeps it, so the Report reads as stale rather than undoing the migration.
        internal static void SyncTargetPipelineFromReport(Report report)
        {
            if (ReferenceEquals(report, s_LastSyncedReport))
                return;
            s_LastSyncedReport = report;

            if (report == null || !report.IsValid() || string.IsNullOrEmpty(report.SessionInfo.MigrationTargetPipeline))
                return;

            var reportTargetPipeline = report.SessionInfo.MigrationTargetPipeline;
            if (MigrationToURPUtilities.IsProjectFullyOnUrp(SelectedTargetPipeline) &&
                !MigrationToURPUtilities.IsProjectFullyOnUrp(reportTargetPipeline))
                return;

            SelectedTargetPipeline = reportTargetPipeline;
        }

        internal sealed class Step
        {
            public string Title;
            public GUIContent Heading;
            public string Warning;
            public GUIContent[] Contents;

            public Func<ProjectAuditorWindow, bool> IsComplete;
            public Func<bool> DrawUI;
            public Action<ProjectAuditorWindow, Step, bool, bool> DrawButtonUI;
        }

        List<Step> Steps => m_Steps ??= new List<Step>
        {
            new Step
            {
                Title = L10n.Tr("Install URP", null),
                Heading = L10n.TextContent("This wizard will guide you through the migration process to URP and provide a report of the assets that need conversion.", null, null, null),
                Warning = L10n.Tr("Before you analyze your project, Unity must install the URP package and make changes to your project. You should back up your project, or make sure you are using version control, before you start.", null),
                Contents =
                [
                    L10n.TextContent("Install Package", "Install the URP package.", null, null),
                    L10n.TextContent("Install Package", "Please confirm that you have backed up your project before starting migration.", null, null),
                ],

                IsComplete = (_) => MigrationToURPUtilities.IsUrpPackageInstalled(),
                DrawUI = () =>
                {
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    DrawRenderPipelineUI();
                    GUILayout.FlexibleSpace();
                    return DrawBackupConfirmation() && (m_UrpAddRequest == null);
                },

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled, bool converterRunning) =>
                {
                    GUIContent content = enabled ? step.Contents[0] : step.Contents[1];
                    if (m_UrpAddRequest != null)
                    {
                        int frame = Utility.GetStatusWheelFrame();
                        content = Contents.InstallingPackageButtonInProgress[frame];
                    }

                    if (CenteredButton(content))
                    {
                        OnInstallUrp();
                        GUIUtility.ExitGUI();
                    }
                }
            },
            new Step
            {
                Title = L10n.Tr("Create URP asset", null),
                Heading = L10n.TextContent("Unity will now create and assign Render Pipeline assets to your project.", null, null, null),
                Contents =
                [
                    L10n.TextContent("Create URP Asset", "Create the Render Pipeline Assets and assign them to your project.", null, null),
                    L10n.TextContent("Create URP Asset", $"Please install the rules package before starting migration ({ProjectAuditorRulesPackage.Name}).", null, null),
                ],

                IsComplete = (_) => MigrationToURPUtilities.IsProjectFullyOnUrp(SelectedTargetPipeline),

                DrawUI = () =>
                {
                    DrawTargetPipelineDropdown();
                    GUILayout.Space(8);
                    return true;
                },

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled, bool converterRunning) =>
                {
                    GUILayout.FlexibleSpace();

                    if (CenteredButton(enabled ? step.Contents[0] : step.Contents[1]) && ConfirmAssignUrpAsset())
                    {
                        MigrationToURPUtilities.ApplyTargetPipelineToQualitySettings(SelectedTargetPipeline);
                        GUIUtility.ExitGUI();
                    }
                }
            },
            new Step
            {
                Title = L10n.Tr("Analyze", null),
                Heading = L10n.TextContent("Analyze which issues can be converted automatically and which need manual conversion.", null, null, null),

                IsComplete = (ProjectAuditorWindow window) =>
                {
                    if (window.m_ViewManager.HasPendingCategories())
                        return AnalysisCompleted;

                    AnalysisCompleted = IsReportValidForMigration(out var _);
                    return AnalysisCompleted;
                },

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled, bool converterRunning) =>
                {
                    // Start Analysis
                    using (new CenteredColumn())
                    {
                        GUILayout.FlexibleSpace();

                        GUIContent buttonLabel;
                        if (window.m_ViewManager.HasPendingCategories())
                        {
                            int frame = Utility.GetStatusWheelFrame();
                            buttonLabel = HomePage.Contents.AnalyzeButtonInProgress[frame];
                        }
                        else
                        {
                            buttonLabel = enabled ? HomePage.Contents.AnalyzeButton : HomePage.Contents.AnalyzeButtonDisabled;
                        }
                        
                        using (new EditorGUI.DisabledScope(window.m_ViewManager.HasPendingCategories()))
                        {
                            if (CenteredButton(buttonLabel) && HomePage.ConfirmDiscardCurrentReport())
                            {
                                Reanalyze(window, PageId.None);
                                GUIUtility.ExitGUI();
                            }
                        }
                    }
                }
            },
            new Step
            {
                Title = L10n.Tr("Fix Issues", null),
                Heading = L10n.TextContent("Fix the migration issues. Some may be converted automatically, and others may need manual conversion.", null, null, null),

                IsComplete = (ProjectAuditorWindow window) =>
                {
                    if (window.m_ViewManager.HasPendingCategories())
                        return false;
                    return GetSummaryView(window).TotalIssueCount == 0;
                },

                Contents =
                [
                    L10n.TextContent("Run Converter", "Open the Render Pipeline Converter.", null, null),
                    L10n.TextContent("Run Converter", "There are no issues to convert.", null, null)
                ],

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled, bool converterRunning) =>
                {
                    var summaryView = GetSummaryView(window);
                    bool analysisInProgress = window.m_ViewManager.HasPendingCategories();

                    // Total issues
                    var content = Utility.TempContent(string.Format(Contents.TotalIssues, summaryView.TotalIssueCount));
                    GUILayout.Label(content, SharedStyles.LargeLabel);
                    GUILayout.Space(10);

                    // Render Pipeline Converter
                    GUIContent pipelineConverterLabel = Utility.TempContent(string.Format(Contents.ConverterIssues, summaryView.RenderPipelineConverterIssueCount));
                    GUILayout.Label(pipelineConverterLabel);
                    using (new EditorGUI.DisabledScope((summaryView.RenderPipelineConverterIssueCount == 0) || converterRunning || analysisInProgress))
                    {
                        EditorGUILayout.BeginHorizontal();

                        GUIContent runContent = Contents.RunConverterButton;
                        if (converterRunning)
                        {
                            int frame = Utility.GetStatusWheelFrame();
                            runContent = Contents.RunningConverterButton[frame];
                        }

                        if (GUILayout.Button(runContent, GUILayout.Width(HomePage.k_ButtonWidth)))
                        {
                            OnRunConverter();
                            GUIUtility.ExitGUI();
                        }

                        GUILayout.Space(8);

                        if (GUILayout.Button(Contents.OpenConverterButton, GUILayout.Width(HomePage.k_ButtonWidth)))
                        {
                            MigrationToURPUtilities.OpenRenderPipelineConverter(null, null);
                            GUIUtility.ExitGUI();
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    GUILayout.Space(10);

                    // Re-analyze
                    GUIContent reanalyzeLabel;
                    if (analysisInProgress)
                    {
                        int frame = Utility.GetStatusWheelFrame();
                        reanalyzeLabel = HomePage.Contents.AnalyzeButtonInProgress[frame];
                    }
                    else
                    {
                        GUILayout.Label(Contents.RefreshReport);
                        reanalyzeLabel = enabled ? Contents.ReanalyzeButton : HomePage.Contents.AnalyzeButtonDisabled;
                    }
                    using (new EditorGUI.DisabledScope(analysisInProgress || converterRunning))
                    {
                        if (GUILayout.Button(reanalyzeLabel, GUILayout.Width(HomePage.k_ButtonWidth)) && HomePage.ConfirmDiscardCurrentReport())
                        {
                            Reanalyze(window, PageId.None);
                            GUIUtility.ExitGUI();
                        }
                    }

                    GUILayout.FlexibleSpace();

                    // Go to summary
                    using (new CenteredColumn())
                    {
                        GUIContent manualFixesLabel = Utility.TempContent(string.Format(Contents.ManualIssues, summaryView.ManualIssueCount));
                        GUILayout.Label(manualFixesLabel, SharedStyles.TextAreaCentered);

                        if (CenteredButton(Contents.GoToManualFixesButton))
                        {
                            window.GotoCategory(IssueCategory.MigrateToURPSummary);
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
        };

        enum InvalidReportReason
        {
            None,
            NoReport,
            ReportIsNotForCurrentProject,
            ReportDoesNotHaveCorrectAreas,
            ReportTargetsADifferentPipeline,
            ReportDoesNotHaveConverterScan,
            ReportScannedBeforeQualitySettingsApplied
        }

        static GUIContent GetInvalidReportMessage(InvalidReportReason reason)
        {
            switch (reason)
            {
                case InvalidReportReason.ReportIsNotForCurrentProject:
                    return Contents.InvalidReportProject;
                case InvalidReportReason.ReportDoesNotHaveCorrectAreas:
                    return Contents.InvalidReportAreas;
                case InvalidReportReason.ReportTargetsADifferentPipeline:
                    return Contents.InvalidReportTargetPipeline;
                case InvalidReportReason.ReportDoesNotHaveConverterScan:
                    return Contents.InvalidReportMigrationScan;
                case InvalidReportReason.ReportScannedBeforeQualitySettingsApplied:
                    return Contents.InvalidReportQualitySettings;
                default:
                    return null;
            }
        }

        static void DrawInvalidReportReason(InvalidReportReason reason)
        {
            var message = GetInvalidReportMessage(reason);
            if (message == null)
                return;

            using (new CenteredRow())
                GUILayout.Label(message, SharedStyles.TextAreaCentered);
            GUILayout.Space(4);
        }

        // The first two wizard steps (Install URP, Create URP asset) come before the analysis step.
        internal static bool IsWizardBeforeAnalysisStep()
        {
            return !MigrationToURPUtilities.IsUrpPackageInstalled() ||
                !MigrationToURPUtilities.IsProjectFullyOnUrp(SelectedTargetPipeline);
        }

        // False when there is no report to re-run, or the report is still valid for migration.
        internal static bool TryGetInvalidReportMessage(ProjectAuditorWindow window, out string message)
        {
            message = null;
            if (window.m_Report == null || !window.m_Report.IsValid() || IsReportValidForMigration(out var reason))
                return false;

            message = GetInvalidReportMessage(reason).text;
            return true;
        }

        internal static void Reanalyze(ProjectAuditorWindow window, PageId targetSummaryPage)
        {
            window.Analyze(k_ProjectAreaFlags, k_CodeAnalysisFlags, targetSummaryPage, SelectedTargetPipeline);
        }

        static bool IsReportValidForMigration(out InvalidReportReason reason)
        {
            reason = InvalidReportReason.None;

            var window = ProjectAuditorWindow.Instance;
            var report = window.m_Report;

            // must have a report
            if (report == null || !report.IsValid())
                reason = InvalidReportReason.NoReport;

            // must be for this project
            if (reason == InvalidReportReason.None && !report.IsForCurrentProject())
                reason = InvalidReportReason.ReportIsNotForCurrentProject;

            // must include some particular code analysis areas
            if (reason == InvalidReportReason.None && (report.SessionInfo.CodeAnalysisFlags & k_CodeAnalysisFlags) != k_CodeAnalysisFlags)
                reason = InvalidReportReason.ReportDoesNotHaveCorrectAreas;

            // must include all the migration categories
            if (reason == InvalidReportReason.None && !window.GetSelectedCategories(k_ProjectAreaFlags).TrueForAll(c => report.SessionInfo.Categories?.Contains(c) ?? false))
                reason = InvalidReportReason.ReportDoesNotHaveCorrectAreas;

            // must have been scanned against the currently selected target pipeline.
            if (reason == InvalidReportReason.None)
            {
                var reportTargetPipeline = string.IsNullOrEmpty(report.SessionInfo.MigrationTargetPipeline)
                    ? MigrationToURPUtilities.DefaultTargetPipelineContainer
                    : report.SessionInfo.MigrationTargetPipeline;

                if (reportTargetPipeline != SelectedTargetPipeline)
                    reason = InvalidReportReason.ReportTargetsADifferentPipeline;
            }

            // must have a valid converter scan
            if (reason == InvalidReportReason.None && !ReportRecordsConverterScan(report))
                reason = InvalidReportReason.ReportDoesNotHaveConverterScan;

            // must have scanned while a Universal Render Pipeline Asset was already active; several converters
            // (eg Material, Readonly Material, Animation Clip) only report items once that's true, so a report
            // from before Step 2 completed would be missing most of its issues.
            if (reason == InvalidReportReason.None)
            {
                var summaryView = GetSummaryView(window);
                var qualitySettingsIssueCount = (SelectedTargetPipeline == MigrationToURPUtilities.Urp2DContainer)
                    ? summaryView.QualitySettings2DIssueCount
                    : summaryView.QualitySettings3DIssueCount;

                if (qualitySettingsIssueCount > 0)
                    reason = InvalidReportReason.ReportScannedBeforeQualitySettingsApplied;
            }

            return (reason == InvalidReportReason.None);
        }

        static bool ReportRecordsConverterScan(Report report)
        {
            return report.TryGetModuleResult(MigrationToURPModule.k_ModuleName, out var result) &&
                result == AnalysisResult.Success;
        }

        public void DrawContent()
        {
            SyncTargetPipelineFromReport(ProjectAuditorWindow.Instance.m_Report);
			
            if (MigrationToURPUtilities.IsProjectUsingOtherSRP())
            {
                DrawOtherSRPNotSupportedPage();
                return;
            }

            DrawStepUI(DrawStepper());
        }

        const float k_ColumnWidth = 420f;

        // The only two colors the editor skin has no equivalent for; both read on light and dark skins.
        static readonly Color k_StepDoneColor = new Color(0.24f, 0.62f, 0.34f);
        static readonly Color k_BarTrackColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        [NoAutoStaticsCleanup]
        static GUIStyle s_LabelRight;
        static GUIStyle LabelRightStyle => s_LabelRight ??= new GUIStyle(EditorStyles.label)
        { alignment = TextAnchor.MiddleRight };

        struct CenteredRow : IDisposable
        {
            public CenteredRow()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            public void Dispose()
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        struct CenteredColumn : IDisposable
        {
            public CenteredColumn()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical(GUILayout.Width(k_ColumnWidth));
            }

            public void Dispose()
            {
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        static bool CenteredButton(GUIContent content)
        {
            using (new CenteredRow())
                return GUILayout.Button(content, GUILayout.Width(HomePage.k_ButtonWidth), GUILayout.Height(HomePage.k_ButtonHeight));
        }

        // Returns the first incomplete step, or -1 when every step is complete.
        int DrawStepper()
        {
            var window = ProjectAuditorWindow.Instance;

            GUILayout.Space(20);

            var count = Steps.Count;
            Span<bool> done = stackalloc bool[count];
            done.Clear();
            var firstIncomplete = -1;
            for (int i = 0; i < count; i++)
            {
                done[i] = Steps[i].IsComplete(window);
                if (!done[i] && firstIncomplete < 0)
                {
                    firstIncomplete = i;
                    break;
                }
            }
            var activeIndex = firstIncomplete;

            using (new CenteredRow())
            {
                for (int i = 0; i < count; i++)
                {
                    var current = i == activeIndex && !done[i];
                    var style = done[i] ? EditorStyles.label : current ? EditorStyles.boldLabel : EditorStyles.label;
                    var marker = done[i] ? "✓" : (i + 1).ToString();
                    GUILayout.Label($"{marker}  {Steps[i].Title}", style, GUILayout.ExpandWidth(false));
                    if (i < count - 1)
                        GUILayout.Label("→", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(false));
                }
            }

            GUILayout.Space(8);

            // From the first incomplete step, not a count of complete ones, so the bar and the markers agree.
            var fraction = firstIncomplete < 0 ? 1f : (float)firstIncomplete / count;
            using (new CenteredRow())
                DrawProgressBar(fraction);

            return (firstIncomplete >= 0) ? firstIncomplete : count - 1;
        }

        static void DrawOtherSRPNotSupportedPage()
        {
            DrawUnsupportedRenderPipelinePage(GetUnsupportedRenderPipelineMessage());
        }

        // The report pages refuse the same project as the wizard, in their own words.
        internal static void DrawReportUnavailablePage()
        {
            DrawUnsupportedRenderPipelinePage(GetReportUnavailableMessage());
        }

        static void DrawUnsupportedRenderPipelinePage(string message)
        {
            GUILayout.Space(HomePage.k_SpacingHeight * 2);
            using (new CenteredColumn())
            {
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }
        }

        // URP is in scope, so what stopped the wizard is either HDRP or a custom SRP.
        internal static string GetUnsupportedRenderPipelineMessage()
        {
            return MigrationToURPUtilities.IsProjectUsingHDRP()
                ? Contents.OtherSRPNotSupportedHDRP
                : Contents.OtherSRPNotSupportedCustomSRP;
        }

        internal static string GetReportUnavailableMessage()
        {
            return MigrationToURPUtilities.IsProjectUsingHDRP()
                ? Contents.ReportUnavailableHDRP
                : Contents.ReportUnavailableCustomSRP;
        }

        void DrawStepUI(int next)
        {
            using (new CenteredColumn())
            {
                HomePage.DrawTabTitleText(m_Steps[next].Heading, 20);

                if (!string.IsNullOrEmpty(m_Steps[next].Warning))
                    EditorGUILayout.HelpBox(m_Steps[next].Warning, MessageType.Warning);

                bool enabled = m_Steps[next].DrawUI?.Invoke() ?? true;
                if (!ProjectAuditorRulesPackage.IsInstalled)
                    enabled = false;

                var window = ProjectAuditorWindow.Instance;
                using (new EditorGUI.DisabledScope(!enabled))
                    m_Steps[next].DrawButtonUI(window, m_Steps[next], enabled, s_ConverterRunning);

                GUILayout.Space(16);
            }
        }

        static bool ConfirmAssignUrpAsset()
        {
            return EditorDialog.DisplayDecisionDialog(Contents.AssignUrpAssetTitle, Contents.AssignUrpAssetMessage, Contents.Assign, Contents.Cancel);
        }

        bool DrawBackupConfirmation()
        {
            bool toggled;
            using (new CenteredRow())
            {
                var width = Mathf.Min(EditorStyles.toggle.padding.left + EditorStyles.label.CalcSize(Contents.BackupAcknowledgement).x, k_ColumnWidth);

                var acknowledged = BackupAcknowledged;
                using (new EditorGUI.DisabledScope(!ProjectAuditorRulesPackage.IsInstalled))
                    toggled = EditorGUILayout.ToggleLeft(Contents.BackupAcknowledgement, acknowledged, GUILayout.Width(width));
                if (toggled != acknowledged)
                    BackupAcknowledged = toggled;
            }

            GUILayout.Space(12);
            return toggled;
        }

        void DrawProgressBar(float fraction)
        {
            var bar = GUILayoutUtility.GetRect(k_ColumnWidth, 6, GUILayout.Width(k_ColumnWidth));
            if (Event.current.type != EventType.Repaint)
                return;
            EditorGUI.DrawRect(bar, k_BarTrackColor);
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(fraction), bar.height),
                SharedStyles.TabBottomActiveColor);
        }

        static readonly GUILayoutOption k_InfoRowLabelWidth = GUILayout.Width(190);
        static readonly GUILayoutOption k_InfoRowContentWidth = GUILayout.Width(200);

        void DrawInfoRow(GUIContent label, string value)
        {
            using (new CenteredRow())
            {
                GUILayout.Label(label, LabelRightStyle, k_InfoRowLabelWidth);
                GUILayout.Space(8);
                GUILayout.Label(value, SharedStyles.Label, k_InfoRowContentWidth);
            }
        }

        void DrawRenderPipelineUI()
        {
            var currentName = MigrationToURPUtilities.GetCurrentRenderPipelineDisplayName();
            DrawInfoRow(Contents.CurrentRenderPipeline, currentName);
            GUILayout.Space(4);
            DrawInfoRow(Contents.TargetRenderPipeline, MigrationToURPUtilities.Contents.URP);
        }

        void DrawTargetPipelineDropdown()
        {
            using (new CenteredRow())
            {
                GUILayout.Label(Contents.TargetPipeline, LabelRightStyle, k_InfoRowLabelWidth);
                GUILayout.Space(20);

                var window = ProjectAuditorWindow.Instance;
                using (new EditorGUI.DisabledScope(s_ConverterRunning || window.m_ViewManager.HasPendingCategories()))
                {
                    var currentIndex = (SelectedTargetPipeline == MigrationToURPUtilities.Urp3DContainer) ? 0 : 1;
                    var newIndex = EditorGUILayout.Popup(currentIndex, Contents.TargetPipelineDisplayNames, k_InfoRowContentWidth);
                    if (newIndex != currentIndex)
                        SelectedTargetPipeline = (newIndex == 0) ? MigrationToURPUtilities.Urp3DContainer : MigrationToURPUtilities.Urp2DContainer;
                }
            }
        }

        void OnInstallUrp()
        {
            m_UrpAddRequest = Client.Add(MigrationToURPUtilities.k_UrpPackageName);
            EditorApplication.update += RepaintWhileInstalling;
        }

        void RepaintWhileInstalling()
        {
            if (!EditorWindow.HasOpenInstances<ProjectAuditorWindow>() || m_UrpAddRequest == null || m_UrpAddRequest.IsCompleted)
            {
                if (m_UrpAddRequest != null)
                {
                    if (m_UrpAddRequest.Status == StatusCode.Success)
                        Debug.Log("Installed: " + m_UrpAddRequest.Result.packageId);
                    else if (m_UrpAddRequest.Status >= StatusCode.Failure)
                        Debug.Log(m_UrpAddRequest.Error.message);
                }

                EditorApplication.update -= RepaintWhileInstalling;
                m_UrpAddRequest = null;
                return;
            }

            ProjectAuditorWindow.Instance.Repaint();
        }

        internal static void OnRunConverter()
        {
            s_ConverterRunning = true;
            EditorApplication.update += RepaintWhileConverterRunning;

            // Deferred so at least one repaint shows the running state, even if every converter turns out to
            // resolve synchronously (eg because the Search index was already up to date).
            var targetPipeline = SelectedTargetPipeline;
            EditorApplication.delayCall += () =>
                MigrationToURPUtilities.RunRenderPipelineConverterInBatchMode(targetPipeline, OnConverterFinished);
        }

        static void OnConverterFinished(string output)
        {
            if (!EditorWindow.HasOpenInstances<ProjectAuditorWindow>())
            {
                s_ConverterRunning = false;
                EditorApplication.update -= RepaintWhileConverterRunning;
                return;
            }

            var window = ProjectAuditorWindow.Instance;
            var report = window.m_Report;

            // Collect the converter issues.
            var unfixedConverterIssues = new List<ReportItem>();
            foreach (var issue in report.UnfixedIssues)
            {
                if (issue.Id == MigrationToURPModule.PAA7000)
                    unfixedConverterIssues.Add(issue);
            }

            var fixedCount = 0;
            var failedCount = 0;

			// Scan the conversion results and mark successful items as fixed.
            foreach (var result in MigrationToURPUtilities.ParseConversionResults(output))
            {
                if (!result.Succeeded)
                {
                    failedCount++;
                    continue;
                }

                var expectedDescription = string.Format(MigrationToURPModule.k_ConverterItemDescriptor.MessageFormat, result.ConverterType, result.Name);
                var issue = FindUnfixedConverterIssue(unfixedConverterIssues, expectedDescription, result.Info);
                if (issue != null)
                {
                    issue.WasFixed = true;
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
            {
                report.NeedsSaving = true;
                EditorApplication.delayCall += () => ProjectAuditorWindow.AutosaveReport(report);

                window.MarkSummaryViewsDirty();
            }

            if (failedCount > 0)
                Debug.Log($"{failedCount} item(s) failed to convert. Check the console for more information.");

            s_ConverterRunning = false;
            EditorApplication.update -= RepaintWhileConverterRunning;
            ProjectAuditorWindow.Instance?.Repaint();
        }

        // Matches a converted item back to the converter issue it came from. Skips issues already marked fixed
        // during this run, so a second result with the same description and location matches a different issue
        // rather than the same one twice. Prefers an exact Location match when both sides have one, since the
        // same description can repeat for same-named items in different locations.
        static ReportItem FindUnfixedConverterIssue(List<ReportItem> unfixedConverterIssues, string expectedDescription, string info)
        {
            ReportItem fallback = null;
            foreach (var issue in unfixedConverterIssues)
            {
                if (issue.WasFixed || issue.Description != expectedDescription)
                    continue;

                if (!string.IsNullOrEmpty(info) && issue.Location != null && issue.Location.Path == info)
                    return issue;

                fallback ??= issue;
            }

            return fallback;
        }

        static void RepaintWhileConverterRunning()
        {
            if (!EditorWindow.HasOpenInstances<ProjectAuditorWindow>())
            {
                s_ConverterRunning = false;
                EditorApplication.update -= RepaintWhileConverterRunning;
                return;
            }

            ProjectAuditorWindow.Instance.Repaint();
        }

        internal static class Contents
        {
            public static readonly GUIContent ReanalyzeButton = L10n.TextContent("Re-run Analysis", null, null, null);
            public static readonly GUIContent BackupAcknowledgement = L10n.TextContent(" I've backed my project up", "Confirm you have a backup or version control commit to return to.", null, null);
            public static readonly string AssignUrpAssetTitle = L10n.Tr("Assigning a URP asset cannot be undone", null);
            public static readonly string AssignUrpAssetMessage = L10n.Tr("Proceed only if you have backed up your project or are using version control.", null);
            public static readonly string Assign = L10n.Tr("Assign", null);
            public static readonly string Cancel = L10n.Tr("Cancel", null);
            public static readonly GUIContent CurrentRenderPipeline = L10n.TextContent("Current Render Pipeline:", null, null, null);
            public static readonly GUIContent TargetRenderPipeline = L10n.TextContent("Target Render Pipeline:", null, null, null);
            public static readonly GUIContent TargetPipeline = L10n.TextContent("Target Universal Render Pipeline:", null, null, null);
            public static readonly GUIContent InvalidReportProject = L10n.TextContent("The current report is for a different project.", null, null, null);
            public static readonly GUIContent InvalidReportMigrationScan = L10n.TextContent("The current report doesn't contain a successful scan of Render Pipeline Converter issues.", null, null, null);
            public static readonly GUIContent InvalidReportAreas = L10n.TextContent("The current report doesn't cover all the areas that need checking before migrating to URP.", null, null, null);
            public static readonly GUIContent InvalidReportTargetPipeline = L10n.TextContent("The current report was generated for a different target pipeline.", null, null, null);
            public static readonly GUIContent InvalidReportQualitySettings = L10n.TextContent("The current report was generated before the URP Quality Settings were applied.", null, null, null);
            public static readonly GUIContent RefreshReport = L10n.TextContent("Generate a new report if the project has changed", null, null, null);
            public static readonly GUIContent RunConverterButton = L10n.TextContent("Fix", "Convert the items automatically, without opening the Render Pipeline Converter window.", null, null);
            public static readonly GUIContent OpenConverterButton = L10n.TextContent("Review in Converter", "Open the Render Pipeline Converter", null, null);
            public static readonly GUIContent GoToManualFixesButton = L10n.TextContent("Go to Summary", null, null, null);

            public static readonly GUIContent[] InstallingPackageButtonInProgress;
            public static readonly GUIContent[] RunningConverterButton;

            public static readonly string TotalIssues = L10n.Tr("Total Issues: {0}", null);
            public static readonly string ConverterIssues = L10n.Tr("Issues that can be converted automatically: {0}", null);
            public static readonly string ManualIssues = L10n.Tr("Issues that need manual review: {0}", null);

            public static readonly string OtherSRPNotSupportedHDRP = L10n.Tr("This wizard only migrates projects that use the Built-in Render Pipeline. Your project uses the High Definition Render Pipeline.", null);
            public static readonly string OtherSRPNotSupportedCustomSRP = L10n.Tr("This wizard only migrates projects that use the Built-in Render Pipeline. Your project uses a custom Render Pipeline.", null);

            public static readonly string ReportUnavailableHDRP = L10n.Tr("The Migration analysis report is available only for projects migrating from the Built-in Render Pipeline. Your project uses the High Definition Render Pipeline.", null);
            public static readonly string ReportUnavailableCustomSRP = L10n.Tr("The Migration analysis report is available only for projects migrating from the Built-in Render Pipeline. Your project uses a custom Render Pipeline.", null);

            public static readonly string[] TargetPipelineDisplayNames =
            {
                L10n.Tr("Universal Renderer", null),
                L10n.Tr("2D Renderer", null)
            };

            static Contents()
            {
                InstallingPackageButtonInProgress = new GUIContent[12];
                RunningConverterButton = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                {
                    InstallingPackageButtonInProgress[i] = L10n.TextContentWithIcon(" Installing Package...", null, "WaitSpin" + i.ToString("00"), null);
                    RunningConverterButton[i] = L10n.TextContentWithIcon(" Running Converter...", null, "WaitSpin" + i.ToString("00"), null);
                }
            }
        }
    }
}
