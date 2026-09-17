// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class MigrateToURPSummaryView : SummaryView
    {
        private bool m_ShowTopTenIssues = true;

        TopTen m_TopTen = NewTopTen();
        StatSeverities m_Severities;

        public override string Description => "Resolve the following issues to migrate your project to the Universal Render Pipeline.";

        public MigrateToURPSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The migration breakdown shows only migration issues.
        protected override bool MatchesSummaryFilter(ReportItem issue)
        {
            if (!HasAnyAreas(issue, Areas.MigrationToURP))
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
            DrawSeverityBreakdown();
            EditorGUILayout.Space();
            DrawTopTenSection();
            EditorGUILayout.Space();
            DrawSessionInformationSection();
        }

        protected override bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            if (base.IsIssueIgnoredOrFiltered(item))
                return true;
            if (!HasAnyAreas(item, Areas.MigrationToURP))
                return true;

            return false;
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
                EditorGUILayout.LabelField(Contents.NoIssuesText, SharedStyles.Label);
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

        static void DrawAnalysisInProgress()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                DrawAnalysisInProgressLabel(Contents.Migration);
            }
        }

        void DrawTopTenSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            m_ShowTopTenIssues = Utility.BoldFoldout(m_ShowTopTenIssues, Contents.TopTenIssuesContent);
            if (m_ShowTopTenIssues)
                DrawTopTenIssues(m_TopTen, IsIssueIgnoredOrFiltered);

            EditorGUILayout.EndVertical();
        }

        static class Contents
        {
            public static readonly GUIContent TopTenIssuesContent = L10n.TextContent("Top Ten Issues", null, null, null);

            public static readonly string SevereCountFormat = L10n.Tr("{0} issue will prevent your project from functioning on URP.", null);
            public static readonly string SevereCountPluralFormat = L10n.Tr("{0} issues will prevent your project from functioning on URP.", null);
            public static readonly string NoIssuesText = L10n.Tr("No URP migration issues found.", null);
            public static readonly string NoMajorIssuesText = L10n.Tr("No major URP migration issues found.", null);
            public static readonly string Migration = L10n.Tr("Migration", null);
        }
    }
}
