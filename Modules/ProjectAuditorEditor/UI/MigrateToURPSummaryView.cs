// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class MigrateToURPSummaryView : SummaryView
    {
        bool m_ShowAutomaticFixes = true;
        bool m_ShowManualFixes = true;

        readonly TopTen m_AutomaticFixesTopTen = NewTopTen();
        readonly TopTen m_ManualFixesTopTen = NewTopTen();

        Stats m_Stats;

        public int RenderPipelineConverterIssueCount { get; private set; }
        public int QualitySettings2DIssueCount { get; private set; }
        public int QualitySettings3DIssueCount { get; private set; }
        public int ManualIssueCount => TotalIssueCount - RenderPipelineConverterIssueCount;
        public int TotalIssueCount => m_Stats.TotalExcludingIgnoredAndFixed;

        public override string Description => "Resolve the following issues to migrate your project to the Universal Render Pipeline.";

        public MigrateToURPSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The migration breakdown shows only migration issues.
        protected override bool MatchesSummaryFilter(ReportItem issue) => issue.HasAnyAreas(Areas.MigrationToURP);

        protected override void OnSummaryRefreshed()
        {
            m_AutomaticFixesTopTen.Refresh = true;
            m_ManualFixesTopTen.Refresh = true;
        }

        protected override void ResetStats()
        {
            base.ResetStats();
            m_Stats = NewStats();
            RenderPipelineConverterIssueCount = 0;
            QualitySettings2DIssueCount = 0;
            QualitySettings3DIssueCount = 0;
        }

        protected override void RefreshStats()
        {
            base.RefreshStats();

            var report = m_ViewManager.Report;
            if (report == null)
                return;

            foreach (var issue in report.GetAllIssues())
            {
                if (!MatchesSummaryFilter(issue))
                    continue;

                // Not while analyzing.
                if (m_ViewManager.HasPendingCategory(issue.Category))
                    continue;

                // Count Converter issues.
                if (issue.Id == MigrationToURPModule.k_ConverterItemDescriptor.Id)
                {
                    if (!IsIgnoredOrHidden(issue))
                        RenderPipelineConverterIssueCount++;
                }

                if (issue.Id == MigrationToURPModule.k_QualitySettings2DNotReadyDescriptor.Id)
                    QualitySettings2DIssueCount++;
                else if (issue.Id == MigrationToURPModule.k_QualitySettings3DNotReadyDescriptor.Id)
                    QualitySettings3DIssueCount++;
                else
                    AccumulateStat(issue, ref m_Stats);
            }
        }

        // The migration pages report against a project that can still migrate, so HDRP and custom SRPs
        // leave nothing that applies - including under a report analyzed before the switch.
        public override bool IsPageOutOfScope()
        {
            return MigrationToURPUtilities.IsProjectUsingOtherSRP();
        }

        public override void DrawOutOfScopePage()
        {
            MigrationWorkflowView.DrawReportUnavailablePage();
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            // No report yet (e.g. analysis just started): the sections below dereference the report.
            if (m_ViewManager.Report == null)
                return;

            bool reportIsCompatible = DrawWizardNotice();
            if (reportIsCompatible)
            {
                EditorGUILayout.Space();
                DrawCategoryBreakdown();

                if (!m_ViewManager.HasPendingCategories())
                {
                    if (RenderPipelineConverterIssueCount > 0)
                    {
                        EditorGUILayout.Space();
                        DrawAutomaticFixesSection();
                    }

                    if (ManualIssueCount > 0)
                    {
                        EditorGUILayout.Space();
                        DrawManualFixesSection();
                    }
                }
            }

            EditorGUILayout.Space();
            DrawSessionInformationSection();
        }

        protected override bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            return base.IsIssueIgnoredOrFiltered(item) || !MatchesSummaryFilter(item);
        }

        bool IsAutomaticIssueIgnoredOrFiltered(ReportItem item)
        {
            if (IsIssueIgnoredOrFiltered(item))
                return true;

            return item.Id != MigrationToURPModule.k_ConverterItemDescriptor.Id;
        }

        bool IsManualIssueIgnoredOrFiltered(ReportItem item)
        {
            if (IsIssueIgnoredOrFiltered(item))
                return true;

            return item.Id == MigrationToURPModule.k_ConverterItemDescriptor.Id;
        }

        // Pointing back to the wizard takes priority over re-running the analysis.
        static bool DrawWizardNotice()
        {
            var window = ProjectAuditorWindow.Instance;

            // A report loaded straight to this page never went through the wizard, so adopt its target first.
            MigrationWorkflowView.SyncTargetPipelineFromReport(window.m_Report);

            if (MigrationWorkflowView.IsWizardBeforeAnalysisStep())
            {
                Utility.DrawHelpBoxWithButton(Contents.CompleteWizardFirst, Contents.GoToWizardButton, MessageType.Info, () => ProjectAuditorNavigator.RequestHomePage(HomePage.Tab.MigrateToURP));
                return false;
            }

            if (!window.m_ViewManager.HasPendingCategories())
            {
                if (MigrationWorkflowView.TryGetInvalidReportMessage(window, out var message))
                {
                    Utility.DrawHelpBoxWithButton(message, MigrationWorkflowView.Contents.ReanalyzeButton, MessageType.Info, () =>
                    {
                        if (HomePage.ConfirmDiscardCurrentReport())
                            MigrationWorkflowView.Reanalyze(window, PageId.MigrationToURP);
                    });
                    return false;
                }
            }

            // Report is valid
            return true;
        }

        void DrawCategoryBreakdown()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(Contents.IssuesByArea, SharedStyles.BoldLabel);

            var ignoredAndFixedInfo = BuildIgnoredFixedInfo(m_Stats.NumIgnoredAreaIssues, m_Stats.NumFixedAreaIssues);
            DrawChartBar(() => BuildCategoryChartElements(m_Stats), ignoredAndFixedInfo);
            EditorGUILayout.EndVertical();
        }

        void DrawConverterButtons()
        {
            var converterRunning = MigrationWorkflowView.ConverterRunning;
            var disabled = RenderPipelineConverterIssueCount == 0 || converterRunning || m_ViewManager.HasPendingCategories();

            using (new EditorGUI.DisabledScope(disabled))
            {
                var runContent = converterRunning
                    ? MigrationWorkflowView.Contents.RunningConverterButton[Utility.GetStatusWheelFrame()]
                    : MigrationWorkflowView.Contents.RunConverterButton;

                if (GUILayout.Button(runContent, GUILayout.Width(HomePage.k_ButtonWidth)))
                {
                    MigrationWorkflowView.OnRunConverter();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(MigrationWorkflowView.Contents.OpenConverterButton, GUILayout.Width(HomePage.k_ButtonWidth)))
                {
                    MigrationToURPUtilities.OpenRenderPipelineConverter(null, null);
                    GUIUtility.ExitGUI();
                }
            }
        }

        void DrawAutomaticFixesSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_ShowAutomaticFixes = Utility.BoldFoldout(m_ShowAutomaticFixes, Contents.TopTenAutomaticIssuesContent, $"({RenderPipelineConverterIssueCount})");
                GUILayout.FlexibleSpace();
                DrawConverterButtons();
            }

            if (m_ShowAutomaticFixes)
            {
                GUILayout.Space(6);
                DrawTopTenIssues(m_AutomaticFixesTopTen, IsAutomaticIssueIgnoredOrFiltered);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawManualFixesSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            using (new EditorGUILayout.HorizontalScope())
            {
                m_ShowManualFixes = Utility.BoldFoldout(m_ShowManualFixes, Contents.TopTenManualIssuesContent, $"({ManualIssueCount})");
                GUILayout.FlexibleSpace();
            }

            if (m_ShowManualFixes)
            {
                GUILayout.Space(6);
                DrawTopTenLabel(
                    Contents.NoIssuesText, Contents.NoMajorIssuesText, Contents.SevereCountFormat, Contents.SevereCountPluralFormat,
                    m_Stats.TotalExcludingIgnoredAndFixed, m_Stats.MajorAndCritical);

                GUILayout.Space(6);
                DrawTopTenIssues(m_ManualFixesTopTen, IsManualIssueIgnoredOrFiltered);
            }

            EditorGUILayout.EndVertical();
        }

        static class Contents
        {
            public static readonly GUIContent TopTenAutomaticIssuesContent = L10n.TextContent("Automatic Fixes", null, null, null);
            public static readonly GUIContent TopTenManualIssuesContent = L10n.TextContent("Manual Fixes", null, null, null);

            public static readonly string CompleteWizardFirst = L10n.Tr("The migration to URP is not complete. Finish the remaining migration steps to see your migration issues.", null);
            public static readonly GUIContent GoToWizardButton = L10n.TextContent("Continue Migration", null, null, null);

            public static readonly string SevereCountFormat = L10n.Tr("{0} issue might prevent your project from functioning on URP.", null);
            public static readonly string SevereCountPluralFormat = L10n.Tr("{0} issues might prevent your project from functioning on URP.", null);
            public static readonly string NoIssuesText = L10n.Tr("No URP migration issues found.", null);
            public static readonly string NoMajorIssuesText = L10n.Tr("No major URP migration issues found.", null);
            public static readonly string IssuesByArea = L10n.Tr("Issues by Area", null);
        }
    }
}
