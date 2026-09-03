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
    // Its Issue Breakdown excludes Upgrade-area issues (those appear on the Upgrade page).
    class OptimizationSummaryView : SummaryView
    {
        bool m_ShowIssueBreakdown = true;
        bool m_ShowTopTenIssues = true;
        bool m_ShowAdditionalInsights = true;

        Stats m_Stats;
        TopTen m_TopTen = NewTopTen();

        readonly Dictionary<string, bool> m_FoldoutStates = new Dictionary<string, bool>();

        bool m_RefreshAdditionalInsights;

        bool m_AnyAdditionalInsights;
        bool m_AnyCompilationErrors;

        readonly Color[] m_DarkSkinSeverityColors =
        [
            new Color(0.6627f, 0.4118f, 0.9059f),   // Critical
            new Color(1.0000f, 0.2196f, 0.2078f),   // Major
            new Color(0.9608f, 0.5059f, 0.0000f),   // Moderate
            new Color(0.3137f, 0.5843f, 0.7922f),   // Minor
            new Color(0.6700f, 0.6700f, 0.6700f)    // Ignored
        ];

        readonly Color[] m_LightSkinSeverityColors =
        [
            new Color(0.5529f, 0.1059f, 0.8706f),   // Critical
            new Color(0.7020f, 0.1725f, 0.0000f),   // Major
            new Color(0.8431f, 0.4275f, 0.0000f),   // Moderate
            new Color(0.2235f, 0.5373f, 0.7725f),   // Minor
            new Color(0.4300f, 0.4300f, 0.4300f)    // Ignored
        ];

        public override string Description => "Project report summary.";

        bool m_SkipRepaintPass;

        public OptimizationSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The Optimization breakdown shows everything except Upgrade-area issues.
        protected override bool MatchesSummaryFilter(ReportItem issue) => !HasAnyAreas(issue, Areas.Upgrade);

        protected override void OnSummaryRefreshed()
        {
            m_TopTen.Refresh = true;
            m_RefreshAdditionalInsights = true;
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

            EditorGUILayout.Space();

            // Top Ten Issues section
            EditorGUILayout.BeginVertical(GUI.skin.box);
            m_ShowTopTenIssues = Utility.BoldFoldout(m_ShowTopTenIssues, Contents.TopTenIssuesContent);
            if (m_ShowTopTenIssues)
            {
                DrawTopTenIssues(m_TopTen, IsIssueIgnoredOrFiltered);
                EditorGUILayout.Space(10);
            }
            EditorGUILayout.EndVertical();

            // Additional Insights section, only drawn if any such insights exist
            if (m_RefreshAdditionalInsights)
            {
                var errorString = LogLevel.Error.ToString();

                m_AnyCompilationErrors = m_ViewManager.Report.GetAllIssues()
                    .Exists(i => i.Category == IssueCategory.CodeCompilerMessage
                        && i.GetProperty(PropertyType.LogLevel) == errorString);

                m_AnyAdditionalInsights = m_AnyCompilationErrors
                    || m_ViewManager.Report.HasCategory(IssueCategory.Assembly)
                    || m_ViewManager.Report.HasCategory(IssueCategory.BuildFile);

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
            if (item.IsUpgradeIssue)
                return true;
            if (item.Severity != Severity.Error && item.Severity != Severity.Critical && item.Severity != Severity.Major && item.Severity != Severity.Moderate)
                return true;

            return false;
        }

        // Draws the collapsible "Issue Breakdown" section, common to all summary pages.
        void DrawIssueBreakdownSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            m_ShowIssueBreakdown = Utility.BoldFoldout(m_ShowIssueBreakdown, Contents.IssueBreakdownContent);
            if (m_ShowIssueBreakdown)
            {
                GUILayout.Space(8);

                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(18);

                        using (new GUILayout.VerticalScope())
                        {
                            DrawSummaryItem("Code", m_Stats.NumCodeIssues, m_Timings.CodeAnalysisTime, IssueCategory.Code);
                            GUILayout.Space(8);
                            DrawSummaryItem("Assets", m_Stats.NumAssetIssues, m_Timings.AssetsAnalysisTime, IssueCategory.AssetIssue);
                            GUILayout.Space(8);
                            DrawSummaryItem("Game Objects", m_Stats.NumGameObjectIssues, m_Timings.GameObjectAnalysisTime, IssueCategory.GameObject);
                            GUILayout.Space(8);
                            DrawSummaryItem("Project Settings", m_Stats.NumSettingIssues, m_Timings.SettingsAnalysisTime, IssueCategory.ProjectSetting);
                            GUILayout.Space(8);
                        }
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        void DrawSummaryItem(string title, int value, long analysisTimeMs, IssueCategory category, GUIContent icon = null)
        {
            if (!m_ViewManager.HasView(category))
                return;

            if (!m_FoldoutStates.TryGetValue(title, out var foldoutState))
            {
                foldoutState = true;
                m_FoldoutStates.Add(title, foldoutState);
            }

            // Display analysis time in sensible units
            var timeSpan = TimeSpan.FromMilliseconds(analysisTimeMs);
            string time;
            if (timeSpan.TotalHours >= 1)
                time = $"{timeSpan.TotalHours:F0}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalMinutes >= 1)
                time = $"{timeSpan.Minutes}m {timeSpan.Seconds}s";
            else if (timeSpan.TotalSeconds >= 2)
                time = $"{timeSpan.TotalSeconds:F0} seconds";
            else if (timeSpan.TotalSeconds >= 1)
                time = $"{timeSpan.TotalSeconds:F0} second";
            else
                time = $"{timeSpan.TotalMilliseconds:F0}ms";
            time = $"found in {time}";

            bool newFoldoutState = true;
            using (new EditorGUILayout.HorizontalScope())
            {
                newFoldoutState = Utility.BoldFoldout(foldoutState, EditorGUIUtility.TrTempContent($"{title} ({value} issues)"));
                GUILayout.FlexibleSpace();
            }

            if (newFoldoutState != foldoutState)
            {
                m_FoldoutStates[title] = newFoldoutState;
            }

            if (newFoldoutState)
            {
                if (value == 0 || m_ViewManager.HasPendingCategory(category))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);

                        if (m_ViewManager.HasPendingCategory(category))
                        {
                            var text = string.Format(Contents.AnalysisInProgressText, title);
                            var content = EditorGUIUtility.TrTextContent($"{text}|{Utility.GetStatusWheelFrame()}", text, string.Empty, Utility.GetIcon(Utility.IconType.StatusWheel).image);
                            GUILayout.Label(content);
                        }
                        else if (m_ViewManager.Report.HasCategory(category))
                        {
                            GUILayout.Label($"No {title} issues {time}.");
                        }
                        else
                        {
                            GUILayout.Label($"{title} analysis is not yet included in this report.");
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Space(20);

                        using (new EditorGUI.DisabledScope(m_ViewManager.HasPendingCategory(category)))
                        {
                            if (GUILayout.Button($"Go to {title}", GUILayout.Width(k_NavigationButtonWidth)))
                            {
                                m_Window.GotoCategory(category);
                                GUIUtility.ExitGUI();
                            }
                        }

                        GUILayout.FlexibleSpace();
                    }

                    return;
                }

                GUILayout.Space(4);

                EditorGUILayout.BeginHorizontal();

                if (icon != null)
                    EditorGUILayout.LabelField(icon, SharedStyles.Label);

                EditorGUILayout.EndHorizontal();

                var error = m_Stats.SeveritiesByCategory[(int)category].Error;
                var critical = m_Stats.SeveritiesByCategory[(int)category].Critical;
                var major = m_Stats.SeveritiesByCategory[(int)category].Major;
                var moderate = m_Stats.SeveritiesByCategory[(int)category].Moderate;
                var minor = m_Stats.SeveritiesByCategory[(int)category].Minor;
                var ignored = m_Stats.SeveritiesByCategory[(int)category].Ignored;

                var colors = SharedStyles.IsDarkMode ? m_DarkSkinSeverityColors : m_LightSkinSeverityColors;

                List<ChartUtil.Element> inValues = new List<ChartUtil.Element>();
                if (error != 0)
                    inValues.Add(new ChartUtil.Element("Error", "Errors", error, colors[0], Utility.GetIcon(Utility.IconType.Error)));
                if (critical != 0)
                    inValues.Add(new ChartUtil.Element("Critical", "Critical issues", critical, colors[0], Utility.GetIcon(Utility.IconType.Critical)));
                if (major != 0)
                    inValues.Add(new ChartUtil.Element("Major", "Major issues", major, colors[1], Utility.GetIcon(Utility.IconType.Major)));
                if (moderate != 0)
                    inValues.Add(new ChartUtil.Element("Moderate", "Moderate issues", moderate, colors[2], Utility.GetIcon(Utility.IconType.Moderate)));
                if (minor != 0)
                    inValues.Add(new ChartUtil.Element("Minor", "Minor issues", minor, colors[3], Utility.GetIcon(Utility.IconType.Minor)));
                if (ignored != 0)
                    inValues.Add(new ChartUtil.Element("Ignored", "Ignored issues", ignored, colors[4], Utility.GetIcon(Utility.IconType.Ignored)));

                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(20);

                // Note: Using PA window's Draw2D allows custom geometry drawn here to be clipped (via Draw2D.SetClipRect) to stay inside scroll view handled in PA window
                ChartUtil.DrawHorizontalStackedBar(m_Window.Draw2D, 14, null, inValues, "{0}", "N0",
                    true, false, true, time);

                GUILayout.Space(20);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(20);

                if (GUILayout.Button($"Go to {title}", GUILayout.Width(k_NavigationButtonWidth)))
                {
                    m_Window.GotoCategory(category);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
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

                    DrawAdditionalInsightItem("Build Report", IssueCategory.BuildFile);

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
            public static readonly GUIContent IssueBreakdownContent = EditorGUIUtility.TrTextContent("Issue Breakdown");
            public static readonly GUIContent TopTenIssuesContent = EditorGUIUtility.TrTextContent("Top Ten Issues");
            public static readonly GUIContent AdditionalInsightsContent = EditorGUIUtility.TrTextContent("Additional Insights");

            public static readonly string AnalysisInProgressText = L10n.Tr("{0} analysis is still running in the background (see more in Window > General > Progress)", null);
        }
    }
}
