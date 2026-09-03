// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    // Base class for the summary-style pages (Optimization and Upgrade). It owns the shared
    // "Issue Breakdown" and "Session Information" sections. Subclasses select which issues feed the
    // breakdown via MatchesSummaryFilter, and add their own page-specific content.
    abstract class SummaryView : AnalysisView
    {
        protected struct StatSeverities
        {
            public int Error;
            public int Critical;
            public int Major;
            public int Moderate;
            public int Minor;
            public int Ignored;
        }

        protected struct Stats
        {
            public int NumCodeIssues;
            public int NumAssetIssues;
            public int NumGameObjectIssues;
            public int NumSettingIssues;

            public StatSeverities[] SeveritiesByCategory;
        }

        protected static Stats NewStats()
        {
            return new Stats
            {
                SeveritiesByCategory = new StatSeverities[Enum.GetNames(typeof(IssueCategory)).Length]
            };
        }

        protected struct Timings
        {
            public long CodeAnalysisTime;
            public long AssetsAnalysisTime;
            public long SettingsAnalysisTime;
            public long GameObjectAnalysisTime;
        }

        protected class TopTen
        {
            public Dictionary<string, bool> FoldoutStates;
            public List<List<ReportItem>> Issues;
            public bool Refresh;
        }

        protected static TopTen NewTopTen()
        {
            return new TopTen
            {
                FoldoutStates = new Dictionary<string, bool>(),
                Issues = new List<List<ReportItem>>(10),
                Refresh = true
            };
        }

        static readonly Areas[] k_AreasPriorityList =
        [
            Areas.Support, Areas.Requirement, Areas.Quality, Areas.IterationTime, Areas.Memory, Areas.CPU, Areas.GPU,
            Areas.LoadTime, Areas.BuildTime, Areas.BuildSize
        ];

        protected Timings m_Timings;

        bool m_ShowSessionInformation = true;

        protected const float k_NavigationButtonWidth = 180.0f;

        public override bool ShowVerticalScrollView => true;

        protected SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // Whether an issue should be counted in this summary's Issue Breakdown.
        protected abstract bool MatchesSummaryFilter(ReportItem issue);

        // True if the issue is flagged with the specific areas.
        protected static bool HasAnyAreas(ReportItem issue, Areas areas)
        {
            if (!issue.Id.IsValid())
                return false;

            return (issue.Id.GetDescriptor().Areas & areas) != 0;
        }

        protected override void DrawInfo()
        {
        }

        public override void Clear()
        {
            base.Clear();
            ResetStats();
        }

        // Recompute the breakdown stats when the view is dirty. Subclasses can hook OnSummaryRefreshed
        // to refresh their own derived data at the same time.
        protected void RefreshIfDirty()
        {
            if (!m_Dirty)
                return;

            m_Dirty = false;
            RefreshStats();
            OnSummaryRefreshed();
        }

        protected virtual void OnSummaryRefreshed()
        {
        }

        protected virtual void ResetStats()
        {
            m_Timings = new Timings();
        }

        protected virtual void RefreshStats()
        {
            ResetStats();

            var report = m_ViewManager.Report;
            if (report == null)
                return;

            m_Timings.CodeAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.Code);
            m_Timings.AssetsAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.AssetIssue);
            m_Timings.SettingsAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.ProjectSetting);
            m_Timings.GameObjectAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.GameObject);
        }

        protected void AccumulateStat(ReportItem issue, ref Stats stats)
        {
            switch (issue.Category)
            {
                case IssueCategory.Code:
                    stats.NumCodeIssues++;
                    AddSeverityStats(issue, ref stats.SeveritiesByCategory[(int)issue.Category]);
                    break;
                case IssueCategory.ProjectSetting:
                    stats.NumSettingIssues++;
                    AddSeverityStats(issue, ref stats.SeveritiesByCategory[(int)issue.Category]);
                    break;
                case IssueCategory.AssetIssue:
                    stats.NumAssetIssues++;
                    AddSeverityStats(issue, ref stats.SeveritiesByCategory[(int)issue.Category]);
                    break;
                case IssueCategory.GameObject:
                    stats.NumGameObjectIssues++;
                    AddSeverityStats(issue, ref stats.SeveritiesByCategory[(int)issue.Category]);
                    break;
                default:
                    break;
            }
        }

        protected void AddSeverityStats(ReportItem newIssue, ref StatSeverities severities)
        {
            if (newIssue.Severity == Severity.None || newIssue.Severity == Severity.Hidden || IsIgnored(newIssue))
                severities.Ignored++;
            else if (newIssue.Severity == Severity.Error)
                severities.Error++;
            else if (newIssue.Severity == Severity.Critical)
                severities.Critical++;
            else if (newIssue.Severity == Severity.Major)
                severities.Major++;
            else if (newIssue.Severity == Severity.Moderate || newIssue.Severity == Severity.Default)
                severities.Moderate++;
            else if (newIssue.Severity == Severity.Minor)
                severities.Minor++;
        }

        protected bool IsIgnored(ReportItem issue)
        {
            if (issue.IsIgnored)
                return true;

            if (!issue.Id.IsValid())
                return true;

            var id = issue.Id;
            var rule = m_Rules.GetRule(id, issue.GetContext());
            if (rule == null)
                rule = m_Rules.GetRule(id); // try to find non-specific rule
            if (rule != null && rule.Severity == Severity.None)
                return true;

            return false;
        }

        protected void DrawTopTenIssues(TopTen topTen, Func<ReportItem, bool> filter)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);

                if (topTen.Refresh)
                {
#pragma warning disable UAC2001, UAC2005, UAC2010 // Avoid Linq
                    topTen.Issues = m_ViewManager.Report.GetAllIssues()
                        .Where(i =>
                            !m_ViewManager.HasPendingCategory(i.Category)
                            && !filter.Invoke(i)
                        ).GroupBy(i => i.DescriptorIdAsString)
                        .OrderBy(GetHighestGroupSeverity)
                        .ThenByDescending(group => group.Count())
                        .ThenBy(group => GetTopTenAreasOrder(group.First().Id.GetDescriptor().Areas))
                        .ThenBy(group => group.First().Id.GetDescriptor().Title)
                        .Take(10)
                        .Select(g => g.ToList())
                        .ToList();
#pragma warning restore UAC2001, UAC2005, UAC2010
                    int oldSize = topTen.FoldoutStates.Count;

#pragma warning disable UAC2001 // Avoid Linq
                    foreach (var key in topTen.FoldoutStates.Keys.ToArray().Where(key => !topTen.Issues.Exists(group => group[0].DescriptorIdAsString == key)))
#pragma warning restore UAC2001
                        topTen.FoldoutStates.Remove(key);

                    topTen.Refresh = false;
                }

                using (new GUILayout.VerticalScope())
                {
                    int count = 0;
                    foreach (var issueGroup in topTen.Issues)
                    {
                        DrawDiagnostic(topTen, issueGroup, count++);
                    }
                }

                GUILayout.Space(20);
            }
        }

        protected virtual bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            if (IsIgnored(item))
                return true;

            if (item.WasFixed)
                return true;

            return false;
        }

        static Severity GetHighestGroupSeverity(IEnumerable<ReportItem> group)
        {
            var highestSeverity = Severity.Minor;
            foreach (var item in group)
            {
                if (item.Severity == Severity.Error)
                    return Severity.Error;

                if (item.Severity < highestSeverity)
                    highestSeverity = item.Severity;
            }

            return highestSeverity;
        }

        static int GetTopTenAreasOrder(Areas areas)
        {
            // Return the areas flag value we find at the lowest index, which means the highest priority
            int priority = 0;
            while (priority < k_AreasPriorityList.Length)
            {
                var area = k_AreasPriorityList[priority];
                if (areas.HasFlag(area))
                    return priority;

                priority++;
            }

            return priority;
        }

        void DrawDiagnostic(TopTen topTen, List<ReportItem> issueGroup, int itemIndex)
        {
            var firstIssue = issueGroup[0];
            var descriptorIdString = firstIssue.DescriptorIdAsString;

            if (!topTen.FoldoutStates.ContainsKey(descriptorIdString))
                topTen.FoldoutStates.Add(descriptorIdString, false);

            bool isExpanded = topTen.FoldoutStates[firstIssue.DescriptorIdAsString];

            var descriptor = firstIssue.Id.GetDescriptor();

            var recommendationText = descriptor.Recommendation;
            if (firstIssue.IsUpgradeIssue)
            {
                var recommendation = firstIssue.UpgradeProperties[(int)UpgradeProperties.Recommendation];
                recommendationText += $"\n\n<i>{recommendation}</i>";
            }

            // Customized foldout per diagnostic issue
            using (new EditorGUILayout.HorizontalScope(itemIndex % 2 == 0 ? SharedStyles.Row : SharedStyles.RowAlternate))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (isExpanded)
                        EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.FoldoutExpanded),
                            GUILayout.Width(19), GUILayout.Height(19));
                    else
                        EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.FoldoutFolded),
                            GUILayout.Width(19), GUILayout.Height(19));

                    EditorGUILayout.LabelField(Utility.GetSeverityIcon(GetHighestGroupSeverity(issueGroup)), SharedStyles.Label,
                        GUILayout.Width(36));

                    DrawDiagnosticLabel(descriptor, issueGroup.Count);
                }

                if (Event.current.isMouse && Event.current.type == EventType.MouseDown && descriptor != null)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        topTen.FoldoutStates[descriptorIdString] = !isExpanded;
                        m_Window?.Repaint();
                    }
                }
            }

            if (isExpanded)
            {
                using (new EditorGUILayout.HorizontalScope(itemIndex % 2 == 0
                    ? SharedStyles.RowBackground : SharedStyles.RowBackgroundAlternate))
                {
                    GUILayout.Space(10);

                    using (new EditorGUILayout.VerticalScope(itemIndex % 2 == 0
                        ? SharedStyles.RowBackground : SharedStyles.RowBackgroundAlternate))
                    {
                        GUILayout.Space(10);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            const int boxMinWidth = 280;
                            const int boxMinHeight = 80;
                            const int buttonWidth = 170;

                            EditorGUILayout.Space(10); // padding

                            // Details text area
                            using (new EditorGUILayout.VerticalScope(SharedStyles.TextBoxBackground))
                            {
                                DrawDetailsHeader(SharedContents.Details, descriptor.Description, descriptor.DocumentationUrl);

                                using (new EditorGUILayout.VerticalScope(SharedStyles.TabBackground,
                                    GUILayout.MinWidth(boxMinWidth), GUILayout.MinHeight(boxMinHeight), GUILayout.ExpandHeight(false)))
                                {
                                    EditorGUILayout.LabelField(descriptor.Description, SharedStyles.TextArea);
                                    DrawDetailsExternalDocsLink(descriptor.DocumentationUrl);
                                    EditorGUILayout.Space(4); // vertical padding
                                }
                            }

                            EditorGUILayout.Space(10); // padding

                            // Recommendation text area
                            using (new EditorGUILayout.VerticalScope(SharedStyles.TextBoxBackground))
                            {
                                DrawDetailsHeader(SharedContents.Recommendation, recommendationText, null);

                                using (new EditorGUILayout.VerticalScope(SharedStyles.TabBackground,
                                    GUILayout.MinWidth(boxMinWidth), GUILayout.MinHeight(boxMinHeight), GUILayout.ExpandHeight(false)))
                                {
                                    EditorGUILayout.LabelField(recommendationText, SharedStyles.TextArea);
                                    EditorGUILayout.Space(4); // vertical padding
                                }
                            }

                            GUILayout.FlexibleSpace();

                            // Buttons for details, quick fix, and more
                            using (new EditorGUILayout.VerticalScope())
                            {
                                if (GUILayout.Button(Contents.MoreDetails, EditorStyles.miniButton,
                                    GUILayout.Width(buttonWidth)))
                                {
                                    SwitchTab(firstIssue.Category);

                                    m_ViewManager.GetActiveView()
                                        .SetSelection(i => i.Id.Equals(descriptor.Id));

                                    m_ViewManager.GetActiveView().FrameSelection();

                                    GUIUtility.ExitGUI();
                                }

                                using (new EditorGUI.DisabledScope(m_ViewManager.HasPendingCategories() || issueGroup.TrueForAll(i => i.WasFixed)))
                                {
                                    if (descriptor.Fixer != null)
                                    {
                                        var content = string.IsNullOrEmpty(descriptor.FixerLabel) ? SharedContents.QuickFix : EditorGUIUtility.TrTempContent(descriptor.FixerLabel);
                                        if (GUILayout.Button(firstIssue.WasFixed ? SharedContents.QuickFixDone : content, EditorStyles.miniButton,
                                            GUILayout.Width(buttonWidth)))
                                        {
                                            ApplyQuickFixes(issueGroup);
                                        }
                                    }

                                    m_ViewManager.AssistantController.DrawAskAssistantButton(descriptor, firstIssue, (GUIContent guiContent, Action onClick) =>
                                    {
                                        if (GUILayout.Button(guiContent, EditorStyles.miniButton, GUILayout.Width(buttonWidth)))
                                        {
                                            onClick();
                                        }
                                    });
                                }

                            }

                            EditorGUILayout.Space(10); // padding
                        }

                        EditorGUILayout.Space(10); // padding
                    }

                    GUILayout.Space(10);
                }
            }
        }

        void SwitchTab(IssueCategory category, string searchString = null)
        {
            // Navigate to the page for this category (in the current group), then apply the search.
            m_Window.GotoCategory(category);

            if (searchString != null)
                m_ViewManager.GetActiveView().SetSearch(searchString);
        }

        static void DrawDiagnosticLabel(Descriptor descriptor, int count)
        {
            var text = descriptor.Title;

            var content = new GUIContent(text);
            var size = SharedStyles.LabelRichText.CalcSize(content);
            EditorGUILayout.LabelField(content, SharedStyles.LabelRichText, GUILayout.Width(size.x));

            if (count > 1)
                EditorGUILayout.LabelField($"({count} Items)", SharedStyles.LabelGreyWithDynamicSize);
        }

        // Draws the collapsible "Session Information" section, common to all summary pages.
        protected void DrawSessionInformationSection()
        {
            if (m_ViewManager.Report == null)
                return;

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            m_ShowSessionInformation = Utility.BoldFoldout(m_ShowSessionInformation,
                Contents.SessionInformationContent);

            if (m_ShowSessionInformation)
                DrawSessionInfo();

            EditorGUILayout.EndVertical();
        }

        void DrawSessionInfo()
        {
            SessionInfo sessionInfo = m_ViewManager.Report.SessionInfo;

            var keyValues = new List<KeyValuePair<string, string>>(
            [
                new KeyValuePair<string, string>("Date and Time", Formatting.FormatDateTime(Utils.Json.DeserializeDateTime(sessionInfo.DateTime))),
                new KeyValuePair<string, string>("Host Name", sessionInfo.HostName),
                new KeyValuePair<string, string>("Host Platform", sessionInfo.HostPlatform),
                new KeyValuePair<string, string>("Company Name", sessionInfo.CompanyName),
                new KeyValuePair<string, string>("Project Name", sessionInfo.ProjectName),
                new KeyValuePair<string, string>("Project Revision", sessionInfo.ProjectRevision),
                new KeyValuePair<string, string>("Unity Version", sessionInfo.UnityVersion),
                new KeyValuePair<string, string>("Project ID", sessionInfo.ProjectId),
                new KeyValuePair<string, string>("Rules Version", sessionInfo.ProjectAuditorRulesVersion),
                new KeyValuePair<string, string>("Project Areas", ObjectNames.NicifyVariableName(sessionInfo.ProjectAreas.Value.ToString())),
                new KeyValuePair<string, string>("Analysis Platform", Formatting.GetModernBuildTargetName(sessionInfo.Platform)),
            ]);

            if ((sessionInfo.ProjectAreas & ProjectAreaFlags.Code) != 0)
            {
                keyValues.Add(new KeyValuePair<string, string>("Code Analysis Areas", ObjectNames.NicifyVariableName(sessionInfo.CodeAnalysisFlags.Value.ToString())));
                if (Unsupported.IsDeveloperMode())
                    keyValues.Add(new KeyValuePair<string, string>("Code Owners", sessionInfo.CodeOwnerFlags.ToString()));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);

                using (new EditorGUILayout.VerticalScope())
                {
                    var itemIndex = 0;
                    foreach (var pair in keyValues)
                    {
                        using (new EditorGUILayout.HorizontalScope(itemIndex++ % 2 == 0
                            ? SharedStyles.Row
                            : SharedStyles.RowAlternate))
                        {
                            EditorGUILayout.LabelField($"{pair.Key}:", SharedStyles.Label, GUILayout.Width(160));
                            EditorGUILayout.LabelField(pair.Value, SharedStyles.Label, GUILayout.ExpandWidth(true));
                        }
                    }
                }

                GUILayout.Space(20);
            }
        }

        static class Contents
        {
            public static readonly GUIContent SessionInformationContent = EditorGUIUtility.TrTextContent("Session Information");
            public static readonly GUIContent MoreDetails = EditorGUIUtility.TrTextContent("More Details");
        }
    }
}
