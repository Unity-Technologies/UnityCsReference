// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class MigrateToCoreCLRSummaryView : SummaryView
    {
        const int k_RuntimeLabelWidth = 140;

        private bool m_ShowTopTenIssues = true;

        TopTen m_TopTen = NewTopTen();
        StatSeverities m_Severities;
        int m_IgnoredIssueCount;
        int m_FixedIssueCount;

        public override string Description => "Resolve the following issues to migrate your project to the CoreCLR scripting backend.";

        public MigrateToCoreCLRSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The migration breakdown shows only migration issues.
        protected override bool MatchesSummaryFilter(ReportItem issue)
        {
            if (!issue.HasAnyAreas(Areas.MigrationToCoreCLR))
                return false;

            return true;
        }

        protected override void OnSummaryRefreshed()
        {
            m_TopTen.Refresh = true;
        }

        protected override void ResetStats()
        {
            base.ResetStats();
            m_Severities = new StatSeverities();
            m_IgnoredIssueCount = 0;
            m_FixedIssueCount = 0;
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

                if (issue.WasFixed)
                    m_FixedIssueCount++;
                else
                    m_IgnoredIssueCount++;

                AddSeverityStats(issue, ref m_Severities);
            }
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            // No report yet (e.g. analysis just started): the sections below dereference the report.
            if (m_ViewManager.Report == null)
                return;

            if (!m_ViewManager.HasPendingCategories() && !m_ViewManager.Report.HasCategory(IssueCategory.Code))
            {
                Utility.DrawHelpBoxWithButton(Contents.CodeNotAnalyzedText, SharedContents.Analyze, MessageType.Info, () =>
                {
                    ProjectAuditorNavigator.RequestHomePage(HomePage.Tab.MigrateToCoreCLR);
                });
                return;
            }

            EditorGUILayout.Space();
            DrawRuntimeVersions();
            EditorGUILayout.Space();
            DrawIssueBreakdownSection();

            if (!m_ViewManager.HasPendingCategories() && m_Severities.TotalExcludingIgnoredAndFixed > 0)
            {
                EditorGUILayout.Space();
                DrawTopTenSection();
            }

            EditorGUILayout.Space();
            DrawSessionInformationSection();
        }

        protected override bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            if (base.IsIssueIgnoredOrFiltered(item))
                return true;
            if (!item.HasAnyAreas(Areas.MigrationToCoreCLR))
                return true;

            return false;
        }

        void DrawRuntimeVersions()
        {
            var currentRuntimeName = GetCurrentRuntimeName();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.CurrentRuntime, SharedStyles.Label, GUILayout.Width(k_RuntimeLabelWidth));
                EditorGUILayout.LabelField(currentRuntimeName, SharedStyles.BoldLabel);
            }

            if (currentRuntimeName != Contents.CoreCLRRuntimeValue)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(Contents.TargetRuntime, SharedStyles.Label, GUILayout.Width(k_RuntimeLabelWidth));
                    EditorGUILayout.LabelField(Contents.CoreCLRRuntimeValue, SharedStyles.BoldLabel);
                }
            }
        }

        GUIContent GetCurrentRuntimeName()
        {
            var sessionInfo = m_ViewManager.Report.SessionInfo;

            switch (sessionInfo.ScriptingBackend.Value)
            {
                case ScriptingImplementation.CoreCLR:
                    return Contents.CoreCLRRuntimeValue;
                default:
                    return Utility.TempContent(sessionInfo.ScriptingBackend.ToString());
            }
        }

        void DrawIssueBreakdownSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(Contents.IssuesBySeverity, SharedStyles.BoldLabel);
            DrawChartBar(() => BuildSeverityChartElements(m_Severities));

            EditorGUILayout.EndVertical();
        }

        void DrawTopTenSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            m_ShowTopTenIssues = Utility.BoldFoldout(m_ShowTopTenIssues, Contents.TopTenIssuesContent);
            if (m_ShowTopTenIssues)
            {
                DrawTopTenLabel(
                    Contents.NoIssuesText, Contents.NoMajorIssuesText, Contents.SevereCountFormat, Contents.SevereCountPluralFormat,
                    m_Severities.TotalExcludingIgnoredAndFixed, m_Severities.MajorAndCritical);

                DrawTopTenIssues(m_TopTen, IsIssueIgnoredOrFiltered);
            }

            EditorGUILayout.EndVertical();
        }

        static class Contents
        {
            public static readonly GUIContent TopTenIssuesContent = L10n.TextContent("Top Ten Issues", null, null, null);
            public static readonly GUIContent CurrentRuntime = L10n.TextContent("Current runtime:", null, null, null);
            public static readonly GUIContent TargetRuntime = L10n.TextContent("Target runtime:", null, null, null);
            public static readonly GUIContent MonoRuntimeValue = L10n.TextContent("Mono (.NET Standard 2.1)", null, null, null);
            public static readonly GUIContent CoreCLRRuntimeValue = L10n.TextContent("CoreCLR (.NET 10)", null, null, null);
            public static readonly GUIContent IssuesBySeverity = L10n.TextContent("Issues by Severity", null, null, null);

            public static readonly string SevereCountFormat = L10n.Tr("{0} issue may cause compilation failures or runtime exceptions on CoreCLR.", null);
            public static readonly string SevereCountPluralFormat = L10n.Tr("{0} issues may cause compilation failures or runtime exceptions on CoreCLR.", null);
            public static readonly string NoIssuesText = L10n.Tr("No CoreCLR migration issues found.", null);
            public static readonly string NoMajorIssuesText = L10n.Tr("No major CoreCLR migration issues found.", null);
            public static readonly string CodeNotAnalyzedText = L10n.Tr("Code analysis is not yet included in this report.", null);
        }
    }
}
