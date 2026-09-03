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

        Stats m_Stats;
        TopTen m_TopTen = NewTopTen();

        public override string Description => "Resolve the following issues to migrate your your project to the Universal Render Pipeline.";

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
            m_Stats = NewStats();
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

                AccumulateStat(issue, ref m_Stats);
            }
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            // No report yet (e.g. analysis just started): the sections below dereference the report.
            if (m_ViewManager.Report == null)
                return;

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
            public static readonly GUIContent TopTenIssuesContent = EditorGUIUtility.TrTextContent("Top Ten Issues");
        }
    }
}
