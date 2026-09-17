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

        public override string Description => "Resolve the following issues to migrate your your project to the CoreCLR scripting backend.";

        public MigrateToCoreCLRSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The migration breakdown shows only migration issues.
        protected override bool MatchesSummaryFilter(ReportItem issue)
        {
            if (!HasAnyAreas(issue, Areas.MigrationToCoreCLR))
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

                AddSeverityStats(issue, ref m_Severities);
            }
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            // No report yet (e.g. analysis just started): the sections below dereference the report.
            if (m_ViewManager.Report == null)
                return;

            EditorGUILayout.Space();
            DrawRuntimeVersions();
            EditorGUILayout.Space();
            DrawSeverityBreakdown();

            if (!m_ViewManager.HasPendingCategories())
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
            if (!HasAnyAreas(item, Areas.MigrationToCoreCLR))
                return true;

            return false;
        }

        void DrawRuntimeVersions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.CurrentRuntime, SharedStyles.Label, GUILayout.Width(k_RuntimeLabelWidth));
                EditorGUILayout.LabelField(GetCurrentRuntimeName(), SharedStyles.BoldLabel);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.TargetRuntime, SharedStyles.Label, GUILayout.Width(k_RuntimeLabelWidth));
                EditorGUILayout.LabelField(Contents.TargetRuntimeValue, SharedStyles.BoldLabel);
            }
        }

        string GetCurrentRuntimeName()
        {
            var sessionInfo = m_ViewManager.Report.SessionInfo;

            switch (sessionInfo.ScriptingBackend.Value)
            {
                case ScriptingImplementation.Mono2x:
                    return "Mono (.NET Standard 2.1)";
                case ScriptingImplementation.CoreCLR:
                    return "CoreCLR (.NET 10)";
                default:
                    return sessionInfo.ScriptingBackend.ToString();
            }
        }

        void DrawSeverityBreakdown()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(SharedContents.Summary, SharedStyles.BoldLabel);

            // In progress
            if (m_ViewManager.HasPendingCategories())
            {
                DrawAnalysisInProgress();
                EditorGUILayout.EndVertical();
                return;
            }

            // No issues
            if (m_Severities.Total == 0)
            {
                EditorGUILayout.LabelField(
                    m_ViewManager.Report.HasCategory(IssueCategory.Code) ? Contents.NoIssuesText : Contents.CodeNotAnalyzedText,
                    SharedStyles.Label);
                EditorGUILayout.EndVertical();
                return;
            }

            // Major issues count
            if (m_Severities.MajorAndCritical == 0)
                EditorGUILayout.LabelField(Contents.NoMajorIssuesText, SharedStyles.Label);
            else
                EditorGUILayout.LabelField(string.Format(m_Severities.MajorAndCritical == 1 ? Contents.SevereCountFormat : Contents.SevereCountPluralFormat, m_Severities.MajorAndCritical), SharedStyles.Label);

            GUILayout.Space(6);

            DrawSeverityBar(m_Severities, horizontalPadding: 2);

            EditorGUILayout.EndVertical();
        }

        void DrawTopTenSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            m_ShowTopTenIssues = Utility.BoldFoldout(m_ShowTopTenIssues, Contents.TopTenIssuesContent);
            if (m_ShowTopTenIssues)
                DrawTopTenIssues(m_TopTen, IsIssueIgnoredOrFiltered);

            EditorGUILayout.EndVertical();
        }

        static void DrawAnalysisInProgress()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                DrawAnalysisInProgressLabel(Contents.Migration);
            }
        }

        static class Contents
        {
            public static readonly GUIContent TopTenIssuesContent = L10n.TextContent("Top Ten Issues", null, null, null);
            public static readonly GUIContent CurrentRuntime = L10n.TextContent("Current runtime:", null, null, null);
            public static readonly GUIContent TargetRuntime = L10n.TextContent("Target runtime:", null, null, null);
            public static readonly GUIContent TargetRuntimeValue = L10n.TextContent("CoreCLR (.NET 10)", null, null, null);

            public static readonly string SevereCountFormat = L10n.Tr("{0} issue will cause compilation failures or runtime exceptions on CoreCLR.", null);
            public static readonly string SevereCountPluralFormat = L10n.Tr("{0} issues will cause compilation failures or runtime exceptions on CoreCLR.", null);
            public static readonly string NoIssuesText = L10n.Tr("No CoreCLR migration issues found.", null);
            public static readonly string NoMajorIssuesText = L10n.Tr("No major CoreCLR migration issues found.", null);
            public static readonly string CodeNotAnalyzedText = L10n.Tr("Code analysis is not yet included in this report.", null);
            public static readonly string Migration = L10n.Tr("Migration", null);
        }
    }
}
