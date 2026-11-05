// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    // The Optimization page summary: a high-level overview of the project report (issue breakdown,
    // top ten issues and additional insights), followed by the shared Session Information section.
    // Its Issue Breakdown excludes issues from areas with their own dedicated page (those appear on
    // the Upgrade and migration pages instead).
    class OptimizationSummaryView : SummaryView
    {
        bool m_ShowTopTenIssues = true;
        bool m_ShowAdditionalInsights = true;

        Stats m_Stats;
        TopTen m_TopTen = NewTopTen();

        bool m_RefreshAdditionalInsights;

        bool m_AnyAdditionalInsights;
        bool m_AnyCompilationErrors;
        bool m_AnyTopTenIssues;

        public override string Description => "Project report summary.";

        bool m_SkipRepaintPass;

        public OptimizationSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The Optimization breakdown excludes Upgrade and migration issues.
        protected override bool MatchesSummaryFilter(ReportItem issue) => !issue.IsIssue() || issue.Id.GetDescriptor().HasAnyAreas(~AreasExtensions.AllUpgradeAreas);

        protected override void OnSummaryRefreshed()
        {
            m_TopTen.Refresh = true;
            m_RefreshAdditionalInsights = true;
        }

        protected override void ResetStats()
        {
            base.ResetStats();
            m_Stats = NewStats();
            m_AnyTopTenIssues = false;
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

                // The Top Ten list leaves out categories that are still being analysed
                if (!IsIssueIgnoredOrFiltered(issue) && !m_ViewManager.HasPendingCategory(issue.Category))
                    m_AnyTopTenIssues = true;
            }
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            if (m_ViewManager.Report == null)
            {
                // No report yet (e.g. analysis just started but OnAnalysisStarted hasn't fired):
                // nothing to draw, and the sections below dereference the report.
                m_SkipRepaintPass = true;
                return;
            }
            // Skip one repaint, after report just got valid
            else if (m_SkipRepaintPass && Event.current.type == EventType.Repaint)
            {
                m_SkipRepaintPass = false;
                return;
            }

            EditorGUILayout.Space();

            // Issue Breakdown section (shared with all summary pages)
            DrawIssueBreakdownSection();

            // Top Ten Issues section, only drawn if any issue would appear in it
            if (m_AnyTopTenIssues && !m_ViewManager.HasPendingCategories())
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(GUI.skin.box);
                m_ShowTopTenIssues = Utility.BoldFoldout(m_ShowTopTenIssues, Contents.TopTenIssuesContent);
                if (m_ShowTopTenIssues)
                {
                    DrawTopTenLabel(
                        Contents.NoIssuesText, Contents.NoMajorIssuesText, Contents.SevereCountFormat, Contents.SevereCountPluralFormat,
                        m_Stats.TotalExcludingIgnoredAndFixed, m_Stats.MajorAndCritical);

                    DrawTopTenIssues(m_TopTen, IsIssueIgnoredOrFiltered);
                    EditorGUILayout.Space(10);
                }
                EditorGUILayout.EndVertical();
            }

            // Additional Insights section, only drawn if any such insights exist
            if (m_RefreshAdditionalInsights)
            {
                var errorString = LogLevel.Error.ToString();

                m_AnyCompilationErrors = m_ViewManager.Report.GetAllIssues()
                    .Exists(i => i.Category == IssueCategory.CodeCompilerMessage
                        && i.GetProperty(PropertyType.LogLevel) == errorString);

                m_AnyAdditionalInsights = m_AnyCompilationErrors
                    || m_ViewManager.Report.HasCategory(IssueCategory.Assembly);

                m_RefreshAdditionalInsights = false;
            }

            if (m_AnyAdditionalInsights)
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(GUI.skin.box);

                m_ShowAdditionalInsights = Utility.BoldFoldout(m_ShowAdditionalInsights, Contents.AdditionalInsightsContent);

                if (m_ShowAdditionalInsights)
                    DrawAdditionalInsights();

                EditorGUILayout.EndVertical();
            }

            // Session Information section (shared with all summary pages)
            DrawSessionInformationSection();
        }

        protected override bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            if (base.IsIssueIgnoredOrFiltered(item))
                return true;
            if (item.Severity != Severity.Error && item.Severity != Severity.Critical && item.Severity != Severity.Major && item.Severity != Severity.Moderate)
                return true;
            if (!MatchesSummaryFilter(item))
                return true;

            return false;
        }

        // Draws the collapsible "Issue Breakdown" section, common to all summary pages.
        void DrawIssueBreakdownSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(SharedContents.IssuesByArea, SharedStyles.BoldLabel);

            var ignoredAndFixedInfo = BuildIgnoredFixedInfo(m_Stats.NumIgnoredAreaIssues, m_Stats.NumFixedAreaIssues);
            DrawChartBar(() => BuildCategoryChartElements(m_Stats), ignoredAndFixedInfo);

            EditorGUILayout.EndVertical();
        }

        private void DrawAdditionalInsights()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);

                using (new EditorGUILayout.VerticalScope())
                {
                    if (m_AnyCompilationErrors)
                        DrawAdditionalInsightItem("Compilation Errors", IssueCategory.CodeCompilerMessage);

                    var assemblyView = m_ViewManager.GetView(IssueCategory.Assembly);
                    if (assemblyView?.NumIssues > 0)
                        DrawAdditionalInsightItem("Compiled Assemblies", IssueCategory.Assembly);
                }
            }
        }

        void DrawAdditionalInsightItem(string title, IssueCategory category)
        {
            if (!m_ViewManager.HasView(category))
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(title);

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button($"Go to {title}", GUILayout.Width(k_NavigationButtonWidth)))
                    {
                        m_Window.GotoCategory(category);
                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.Space(20);
            }
        }

        static class Contents
        {
            public static readonly GUIContent TopTenIssuesContent = L10n.TextContent("Top Ten Issues", null, null, null);
            public static readonly GUIContent AdditionalInsightsContent = L10n.TextContent("Additional Insights", null, null, null);

            public static readonly string SevereCountFormat = L10n.Tr("{0} major optimization issue found.", null);
            public static readonly string SevereCountPluralFormat = L10n.Tr("{0} major optimization issues found.", null);
            public static readonly string NoIssuesText = L10n.Tr("No optimization issues found.", null);
            public static readonly string NoMajorIssuesText = L10n.Tr("No major optimization issues found.", null);
        }
    }
}
