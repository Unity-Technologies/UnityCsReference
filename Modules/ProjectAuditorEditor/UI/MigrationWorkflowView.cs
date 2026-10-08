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
    internal class MigrationWorkflowView
    {
        const ProjectAreaFlags k_ProjectAreaFlags =
            ProjectAreaFlags.ProjectSettings | ProjectAreaFlags.Code |
            ProjectAreaFlags.Assets | ProjectAreaFlags.GameObjects;

        const CodeAnalysisFlags k_CodeAnalysisFlags =
            CodeAnalysisFlags.Player | CodeAnalysisFlags.Packages;

        List<Step> m_Steps;

        UnityEditor.PackageManager.Requests.AddRequest m_UrpAddRequest;

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

        public MigrationWorkflowView()
        {
        }

        internal sealed class Step
        {
            public string Title;
            public GUIContent Heading;
            public string Warning;
            public GUIContent[] Contents;

            public Func<ProjectAuditorWindow, bool> IsComplete;
            public Func<bool> DrawUI;
            public Action<ProjectAuditorWindow, Step, bool> DrawButtonUI;
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
                    GUILayout.FlexibleSpace();
                    DrawRenderPipelineUI();
                    return DrawBackupConfirmation() && (m_UrpAddRequest == null);
                },

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled) =>
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
                Heading = L10n.TextContent("Unity will now assign a Render Pipeline asset as your project's default, creating one if needed.", null, null, null),
                Contents =
                [
                    L10n.TextContent("Create URP Asset", "Create the Render Pipeline Asset and assign it as the project's default.", null, null),
                    L10n.TextContent("Create URP Asset", $"Please install the rules package before starting migration ({ProjectAuditorRulesPackage.Name}).", null, null),
                ],

                IsComplete = (_) => MigrationToURPUtilities.HasDefaultUrpRenderPipeline(),

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled) =>
                {
                    if (CenteredButton(enabled ? step.Contents[0] : step.Contents[1]))
                    {
                        MigrationToURPUtilities.EnsureDefaultRenderPipelineAsset();
                        GUIUtility.ExitGUI();
                    }

                    GUILayout.FlexibleSpace();
                }
            },
            new Step
            {
                Title = L10n.Tr("Fix Issues", null),
                Heading = L10n.TextContent("Analyze which issues can be converted automatically and which need manual conversion.", null, null, null),

                IsComplete = (ProjectAuditorWindow window) =>
                {
                    if (!IsReportValidForMigration(out var _))
                        return false;
                    if (window.m_ViewManager.HasPendingCategories())
                        return false;
                    return GetSummaryView(window).TotalIssueCount == 0;
                },

                Contents =
                [
                    L10n.TextContent("Run Converter", "Open the Render Pipeline Converter.", null, null),
                    L10n.TextContent("Run Converter", "There are no issues to convert.", null, null)
                ],

                DrawButtonUI = (ProjectAuditorWindow window, Step step, bool enabled) =>
                {
                    if (IsReportValidForMigration(out var _))
                    {
                        var summaryView = GetSummaryView(window);

                        GUILayout.FlexibleSpace();

                        // Total issues
                        var content = Utility.TempContent(string.Format(Contents.TotalIssues, summaryView.TotalIssueCount));
                        GUILayout.Label(content, SharedStyles.LargeLabel);
                        GUILayout.Space(10);

                        // Render Pipeline Converter
                        GUIContent pipelineConverterLabel = Utility.TempContent(string.Format(Contents.ConverterIssues, summaryView.RenderPipelineConverterIssueCount));
                        GUILayout.Label(pipelineConverterLabel);
                        using (new EditorGUI.DisabledScope(summaryView.RenderPipelineConverterIssueCount == 0))
                        {
                            if (GUILayout.Button(Contents.OpenConverterButton, GUILayout.Width(HomePage.k_ButtonWidth)))
                            {
                                MigrationToURPUtilities.OpenRenderPipelineConverter(null, null);
                                GUIUtility.ExitGUI();
                            }
                        }

                        GUILayout.Space(10);

                        // Manual fixes (go to summary)
                        GUIContent manualFixesLabel = Utility.TempContent(string.Format(Contents.ManualIssues, summaryView.TotalIssueCount - summaryView.RenderPipelineConverterIssueCount));
                        GUILayout.Label(manualFixesLabel);
                        if (GUILayout.Button(Contents.GoToManualFixesButton, GUILayout.Width(HomePage.k_ButtonWidth)))
                        {
                            window.GotoCategory(IssueCategory.MigrateToURPSummary);
                            GUIUtility.ExitGUI();
                        }

                        GUILayout.Space(40);
                    }

                    // Start Analysis
                    using (new CenteredColumn())
                    {
                        GUIContent buttonLabel;
                        if (window.m_ViewManager.HasPendingCategories())
                        {
                            int frame = Utility.GetStatusWheelFrame();
                            buttonLabel = HomePage.Contents.AnalyzeButtonInProgress[frame];
                        }
                        else if (window.m_Report == null || !window.m_Report.IsValid())
                        {
                            buttonLabel = enabled ? HomePage.Contents.AnalyzeButton : HomePage.Contents.AnalyzeButtonDisabled;
                        }
                        else if (!IsReportValidForMigration(out var reason))
                        {
                            using (new CenteredRow())
                            {
                                switch (reason)
                                {
                                    case InvalidReportReason.ReportIsNotForCurrentProject:
                                        GUILayout.Label(Contents.InvalidReportProject, SharedStyles.TextAreaCentered);
                                        GUILayout.Space(4);
                                        break;
                                    case InvalidReportReason.ReportDoesNotHaveCorrectAreas:
                                        GUILayout.Label(Contents.InvalidReportAreas, SharedStyles.TextAreaCentered);
                                        GUILayout.Space(4);
                                        break;
                                    case InvalidReportReason.ReportDoesNotHaveConverterScan:
                                        GUILayout.Label(Contents.InvalidReportMigrationScan, SharedStyles.TextAreaCentered);
                                        GUILayout.Space(4);
                                        break;
                                }
                            }

                            buttonLabel = enabled ? Contents.ReanalyzeButton : HomePage.Contents.AnalyzeButtonDisabled;
                        }
                        else
                        {
                            using (new CenteredRow())
                                GUILayout.Label(Contents.RefreshReport, SharedStyles.TextAreaCentered);
                            buttonLabel = enabled ? Contents.ReanalyzeButton : HomePage.Contents.AnalyzeButtonDisabled;
                        }

                        using (new EditorGUI.DisabledScope(window.m_ViewManager.HasPendingCategories()))
                        {
                            if (CenteredButton(buttonLabel))
                            {
                                window.Analyze(k_ProjectAreaFlags, k_CodeAnalysisFlags, PageId.None);
                                GUIUtility.ExitGUI();
                            }
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
            ReportDoesNotHaveConverterScan
        }

        bool IsReportValidForMigration(out InvalidReportReason reason)
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

            // must have a valid converter scan
            if (reason == InvalidReportReason.None && !ReportRecordsConverterScan(report))
                reason = InvalidReportReason.ReportDoesNotHaveConverterScan;

            return (reason == InvalidReportReason.None);
        }

        static bool ReportRecordsConverterScan(Report report)
        {
            return report.TryGetModuleResult(MigrationToURPModule.k_ModuleName, out var result) &&
                result == AnalysisResult.Success;
        }

        public void DrawContent()
        {
            // Checked before the stepper: out of scope there is no step to make progress on, nor one that may run.
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
        unsafe int DrawStepper()
        {
            var window = ProjectAuditorWindow.Instance;

            GUILayout.Space(HomePage.k_SpacingHeight);

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
                HomePage.DrawTabTitleText(m_Steps[next].Heading);

                if (!string.IsNullOrEmpty(m_Steps[next].Warning))
                    EditorGUILayout.HelpBox(m_Steps[next].Warning, MessageType.Warning);

                bool enabled = m_Steps[next].DrawUI?.Invoke() ?? true;
                if (!ProjectAuditorRulesPackage.IsInstalled)
                    enabled = false;

                var window = ProjectAuditorWindow.Instance;
                using (new EditorGUI.DisabledScope(!enabled))
                    m_Steps[next].DrawButtonUI(window, m_Steps[next], enabled);

                GUILayout.Space(16);
            }
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

        void DrawInfoRow(GUIContent label, string value)
        {
            using (new CenteredRow())
            {
                GUILayout.Label(label, LabelRightStyle, GUILayout.Width(190));
                GUILayout.Space(8);
                GUILayout.Label(value, SharedStyles.Label, GUILayout.Width(200));
            }
        }

        void DrawRenderPipelineUI()
        {
            var currentName = MigrationToURPUtilities.GetDefaultRenderPipelineDisplayName();
            DrawInfoRow(Contents.CurrentRenderPipeline, string.IsNullOrEmpty(currentName) ? Contents.BuiltIn : currentName);
            GUILayout.Space(4);
            DrawInfoRow(Contents.TargetRenderPipeline, Contents.URP);
            GUILayout.Space(32);
        }

        void OnInstallUrp()
        {
            m_UrpAddRequest = Client.Add(MigrationToURPUtilities.k_UrpPackageName);
            EditorApplication.update += RepaintWhileInstalling;
        }

        void RepaintWhileInstalling()
        {
            if (ProjectAuditorWindow.Instance == null || m_UrpAddRequest == null || m_UrpAddRequest.IsCompleted)
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

        internal static class Contents
        {
            public static readonly GUIContent ReanalyzeButton = L10n.TextContent("Re-run Analysis", null, null, null);
            public static readonly GUIContent BackupAcknowledgement = L10n.TextContent(" I've backed my project up", "Confirm you have a backup or version control commit to return to.", null, null);
            public static readonly GUIContent CurrentRenderPipeline = L10n.TextContent("Current Render Pipeline:", null, null, null);
            public static readonly GUIContent TargetRenderPipeline = L10n.TextContent("Target Render Pipeline:", null, null, null);
            public static readonly GUIContent InvalidReportProject = L10n.TextContent("The current report is for a different project.", null, null, null);
            public static readonly GUIContent InvalidReportMigrationScan = L10n.TextContent("This report doesn't contain a successful scan of Render Pipeline Converter issues.", null, null, null);
            public static readonly GUIContent InvalidReportAreas = L10n.TextContent("This report doesn't cover all the areas that need checking before migrating to URP.", null, null, null);
            public static readonly GUIContent RefreshReport = L10n.TextContent("Generate a new report if the project has changed", null, null, null);
            public static readonly GUIContent OpenConverterButton = L10n.TextContent("Review in Converter", "Open the Render Pipeline Converter", null, null);
            public static readonly GUIContent GoToManualFixesButton = L10n.TextContent("Go to Summary", null, null, null);

            public static readonly GUIContent[] InstallingPackageButtonInProgress;

            public static readonly string TotalIssues = L10n.Tr("Total Issues: {0}", null);
            public static readonly string ConverterIssues = L10n.Tr("Issues that can be converted automatically: {0}", null);
            public static readonly string ManualIssues = L10n.Tr("Issues that need manual review: {0}", null);

            public static readonly string OtherSRPNotSupportedHDRP = L10n.Tr("This wizard only migrates projects that use the Built-in Render Pipeline. Your project uses the High Definition Render Pipeline.", null);
            public static readonly string OtherSRPNotSupportedCustomSRP = L10n.Tr("This wizard only migrates projects that use the Built-in Render Pipeline. Your project uses a custom Render Pipeline.", null);

            public static readonly string ReportUnavailableHDRP = L10n.Tr("The Migration analysis report is available only for projects migrating from the Built-in Render Pipeline. Your project uses the High Definition Render Pipeline.", null);
            public static readonly string ReportUnavailableCustomSRP = L10n.Tr("The Migration analysis report is available only for projects migrating from the Built-in Render Pipeline. Your project uses a custom Render Pipeline.", null);

            public static readonly string URP = L10n.Tr("Universal Render Pipeline", null);
            public static readonly string HDRP = L10n.Tr("High Definition Render Pipeline", null);
            public static readonly string BuiltIn = L10n.Tr("Built-in Render Pipeline", null);

            static Contents()
            {
                InstallingPackageButtonInProgress = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    InstallingPackageButtonInProgress[i] = L10n.TextContentWithIcon(" Installing Package...", null, "WaitSpin" + i.ToString("00"), null);
            }
        }
    }
}
