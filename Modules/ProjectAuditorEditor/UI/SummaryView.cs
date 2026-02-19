// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class SummaryView : AnalysisView
    {
#pragma warning disable CS0649
        struct StatSeverities
        {
            public int Error;
            public int Critical;
            public int Major;
            public int Moderate;
            public int Minor;
            public int Ignored;
        }

        struct Stats
        {
            public int NumBuildSteps;
            public int NumCodeIssues;
            public int NumCompiledAssemblies;
            public int NumCompilerErrors;
            public int NumSettingIssues;
            public int NumTotalAssemblies;
            public int NumAssetIssues;
            public int NumGameObjectIssues;
            public int NumShaders;
            public int NumPackages;

            public long CodeAnalysisTime;
            public long AssetsAnalysisTime;
            public long SettingsAnalysisTime;
            public long GameObjectAnalysisTime;

            public StatSeverities[] SeveritiesByCategory;
            public int IgnoredIssues;
        }
#pragma warning restore CS0649

        Stats m_Stats;

        bool m_ShowIssueBreakdown = true;
        bool m_ShowTopTenIssues = true;
        bool m_ShowAdditionalInsightChecks = true;
        bool m_ShowSessionInformation = true;

        Dictionary<string, bool> m_FoldoutStates = new Dictionary<string, bool>();
        Dictionary<string, bool> m_TopTenFoldoutStates = new Dictionary<string, bool>();

        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        List<IGrouping<string, ReportItem>> m_TopTenIssues = new List<IGrouping<string, ReportItem>>();
#pragma warning restore UA2001

        bool m_RefreshTopTenIssues;

        bool m_RefreshAdditionalInsights;
        bool m_AnyAdditionalInsights;
        bool m_AnyCompilationErrors;

        Color[] m_SeverityColors =
        [
            new Color(0.96f, 0.3f, 0.26f),          // Critical
            new Color(0.902f, 0.314f, 0f),          // Major
            new Color(0.788f, 0.451f, 0.067f),      // Moderate
            new Color(0.055f, 0.502f, 0.945f),      // Minor
            new Color(0.768f, 0.768f, 0.768f, 1f)   // Ignored
        ];

        readonly Areas[] k_AreasPriorityList =
        [
            Areas.Support, Areas.Requirement, Areas.Quality, Areas.IterationTime, Areas.Memory, Areas.CPU, Areas.GPU,
            Areas.LoadTime, Areas.BuildTime, Areas.BuildSize
        ];

        public override string Description => "A high level overview of the Project Report.";
        public override bool ShowVerticalScrollView => true;

        bool m_SkipRepaintPass;

        readonly float k_NavigationButtonWidth = 180f;

        public SummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        void AddSeverityStats(ReportItem newIssue, ref StatSeverities codeSeverities)
        {
            if (newIssue.Severity == Severity.None || newIssue.Severity == Severity.Hidden || IsIgnored(newIssue))
                codeSeverities.Ignored++;
            else if (newIssue.Severity == Severity.Error)
                codeSeverities.Error++;
            else if (newIssue.Severity == Severity.Critical)
                codeSeverities.Critical++;
            else if (newIssue.Severity == Severity.Major)
                codeSeverities.Major++;
            else if (newIssue.Severity == Severity.Moderate || newIssue.Severity == Severity.Default)
                codeSeverities.Moderate++;
            else if (newIssue.Severity == Severity.Minor)
                codeSeverities.Minor++;
        }

        bool IsIgnored(ReportItem issue)
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

        bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            if (IsIgnored(item))
                return true;

            if (item.WasFixed)
                return true;

            if (item.Category != IssueCategory.Code)
                return false;

            return false;
        }

        void AddDiagnosticStats(IEnumerable<ReportItem> newIssues)
        {
            if (m_Stats.SeveritiesByCategory == null || m_Stats.SeveritiesByCategory.Length == 0)
                m_Stats.SeveritiesByCategory = new StatSeverities[Enum.GetNames(typeof(IssueCategory)).Length];

            foreach (var issue in newIssues)
            {
                switch (issue.Category)
                {
                    case IssueCategory.Code:
                        m_Stats.NumCodeIssues++;
                        AddSeverityStats(issue, ref m_Stats.SeveritiesByCategory[(int)issue.Category]);
                        break;
                    case IssueCategory.ProjectSetting:
                        m_Stats.NumSettingIssues++;
                        AddSeverityStats(issue, ref m_Stats.SeveritiesByCategory[(int)issue.Category]);
                        break;
                    case IssueCategory.AssetIssue:
                        m_Stats.NumAssetIssues++;
                        AddSeverityStats(issue, ref m_Stats.SeveritiesByCategory[(int)issue.Category]);
                        break;
                    case IssueCategory.GameObject:
                        m_Stats.NumGameObjectIssues++;
                        AddSeverityStats(issue, ref m_Stats.SeveritiesByCategory[(int)issue.Category]);
                        break;
                    default:
                        break;
                }
            }
        }

        void RefreshStats(Report report)
        {
            if (report == null)
                return;

            var allIssues = report.GetAllIssues();

            // Stats that also count issues by severity
            AddDiagnosticStats(allIssues);

            m_Stats.CodeAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.Code);
            m_Stats.AssetsAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.AssetIssue);
            m_Stats.SettingsAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.ProjectSetting);
            m_Stats.GameObjectAnalysisTime = report.CalculateIssueCategoryAnalysisDuration(IssueCategory.GameObject);

            // Various stats
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_Stats.NumBuildSteps += allIssues.Count(i => i.Category == IssueCategory.BuildStep);
            m_Stats.NumShaders += allIssues.Count(i => i.Category == IssueCategory.Shader);
            m_Stats.NumPackages += allIssues.Count(i => i.Category == IssueCategory.Package);

            var compilerMessages = allIssues.Where(i => i.Category == IssueCategory.CodeCompilerMessage);
            m_Stats.NumCompilerErrors += compilerMessages.Count(i => i.Severity == Severity.Error);

            m_Stats.NumCompiledAssemblies += allIssues.Count(i => i.Category == IssueCategory.Assembly && i.Severity != Severity.Error);
            m_Stats.NumTotalAssemblies += allIssues.Count(i => i.Category == IssueCategory.Assembly);
#pragma warning restore UA2001
        }

        public override void Clear()
        {
            base.Clear();

            m_Stats = new Stats();
            m_Stats.SeveritiesByCategory = new StatSeverities[Enum.GetNames(typeof(IssueCategory)).Length];
        }

        protected override void DrawInfo()
        {
        }

        public bool DrawCustomFoldout(bool foldoutExpanded, GUIContent content)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (foldoutExpanded)
                    EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.FoldoutExpanded),
                        GUILayout.Width(19), GUILayout.Height(19));
                else
                    EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.FoldoutFolded),
                        GUILayout.Width(19), GUILayout.Height(19));

                EditorGUILayout.LabelField(content, SharedStyles.BoldLabel);
            }

            if (Event.current.isMouse && Event.current.type == EventType.MouseDown)
            {
                var rect = GUILayoutUtility.GetLastRect();
                if (rect.Contains(Event.current.mousePosition))
                {
                    foldoutExpanded = !foldoutExpanded;
                    m_Window?.Repaint();
                }
            }

            return foldoutExpanded;
        }

        public override void DrawContent()
        {
            if (m_Dirty)
            {
                m_RefreshTopTenIssues = true;
                m_RefreshAdditionalInsights = true;
                m_Dirty = false;

                m_Stats = new Stats();
                m_Stats.SeveritiesByCategory = new StatSeverities[Enum.GetNames(typeof(IssueCategory)).Length];
                RefreshStats(m_ViewManager.Report);
            }

            if (m_ViewManager.Report == null)
            {
                m_SkipRepaintPass = true;
            }
            // Skip one repaint, after report just got valid
            else if (m_SkipRepaintPass && Event.current.type == EventType.Repaint)
            {
                m_SkipRepaintPass = false;
                return;
            }

            // Issue Breakdown section
            m_ShowIssueBreakdown = DrawCustomFoldout(m_ShowIssueBreakdown, Contents.IssueBreakdownContent);
            if (m_ShowIssueBreakdown)
                DrawIssueBreakdown();

            EditorGUILayout.Space();

            // Top Ten Issues section
            m_ShowTopTenIssues = DrawCustomFoldout(m_ShowTopTenIssues, Contents.TopTenIssuesContent);
            if (m_ShowTopTenIssues)
            {
                DrawTopTenIssues();
                EditorGUILayout.Space(10);
            }

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

                EditorGUILayout.BeginVertical();

                m_ShowAdditionalInsightChecks = DrawCustomFoldout(m_ShowAdditionalInsightChecks,
                    Contents.AdditionalInsightChecksContent);

                if (m_ShowAdditionalInsightChecks)
                    DrawAdditionalInsights();

                EditorGUILayout.EndVertical();
            }

            // Session Information section
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();

            m_ShowSessionInformation = DrawCustomFoldout(m_ShowSessionInformation,
                Contents.SessionInformationContent);

            if (m_ShowSessionInformation)
                DrawSessionInfo();

            EditorGUILayout.EndVertical();
        }

        void DrawIssueBreakdown()
        {
            GUILayout.Space(8);

            using (new EditorGUILayout.VerticalScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(18);

                    using (new GUILayout.VerticalScope())
                    {
                        DrawSummaryItem("Code", m_Stats.NumCodeIssues, m_Stats.CodeAnalysisTime, IssueCategory.Code);
                        GUILayout.Space(8);
                        DrawSummaryItem("Assets", m_Stats.NumAssetIssues, m_Stats.AssetsAnalysisTime, IssueCategory.AssetIssue);
                        GUILayout.Space(8);
                        DrawSummaryItem("Game Objects", m_Stats.NumGameObjectIssues, m_Stats.GameObjectAnalysisTime, IssueCategory.GameObject);
                        GUILayout.Space(8);
                        DrawSummaryItem("Project Settings", m_Stats.NumSettingIssues, m_Stats.SettingsAnalysisTime, IssueCategory.ProjectSetting);
                        GUILayout.Space(8);
                    }
                }
            }
        }

        int GetTopTenAreasOrder(Areas areas)
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

        Severity GetHighestGroupSeverity(IGrouping<string, ReportItem> group)
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

        void DrawTopTenIssues()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(12);

                if (m_RefreshTopTenIssues)
                {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2005 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    m_TopTenIssues = m_ViewManager.Report.GetAllIssues()
                        .Where(i =>
                            !m_ViewManager.HasPendingCategory(i.Category)
                            && !IsIssueIgnoredOrFiltered(i)
                            && (i.Severity == Severity.Error || i.Severity == Severity.Critical || i.Severity == Severity.Major || i.Severity == Severity.Moderate)
                        ).GroupBy(i => i.DescriptorIdAsString)
                        .OrderBy(GetHighestGroupSeverity)
                        .ThenByDescending(group => group.Count())
                        .ThenBy(group => GetTopTenAreasOrder(group.First().Id.GetDescriptor().Areas))
                        .ThenBy(group => group.First().Id.GetDescriptor().Title)
                        .Take(10)
                        .ToList();
#pragma warning restore UA2001
#pragma warning restore UA2005
                    int oldSize = m_TopTenFoldoutStates.Count;

                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var key in m_TopTenFoldoutStates.Keys.ToArray().Where(key => !m_TopTenIssues.Exists(group => group.First().DescriptorIdAsString == key)))
#pragma warning restore UA2001
                        m_TopTenFoldoutStates.Remove(key);

                    m_RefreshTopTenIssues = false;
                }

                using (new GUILayout.VerticalScope())
                {
                    int count = 0;
                    foreach (var issueGroup in m_TopTenIssues)
                    {
                        DrawDiagnostic(issueGroup, count++);
                    }
                }

                GUILayout.Space(20);
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

        void DrawDiagnostic(IGrouping<string, ReportItem> issueGroup, int itemIndex)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var firstIssue = issueGroup.First();
#pragma warning restore UA2001
            var descriptorIdString = firstIssue.DescriptorIdAsString;

            if (!m_TopTenFoldoutStates.ContainsKey(descriptorIdString))
                m_TopTenFoldoutStates.Add(descriptorIdString, false);

            bool isExpanded = m_TopTenFoldoutStates[firstIssue.DescriptorIdAsString];

            var descriptor = firstIssue.Id.GetDescriptor();

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

                    #pragma warning disable UA2005 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    DrawDiagnosticLabel(descriptor, issueGroup.Count());
#pragma warning restore UA2005
                }

                if (Event.current.isMouse && Event.current.type == EventType.MouseDown && descriptor != null)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        m_TopTenFoldoutStates[descriptorIdString] = !isExpanded;
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
                                DrawDetailsHeader(SharedContents.Recommendation, descriptor.Recommendation, null);

                                using (new EditorGUILayout.VerticalScope(SharedStyles.TabBackground,
                                    GUILayout.MinWidth(boxMinWidth), GUILayout.MinHeight(boxMinHeight), GUILayout.ExpandHeight(false)))
                                {
                                    EditorGUILayout.LabelField(descriptor.Recommendation, SharedStyles.TextArea);
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

                                using (new EditorGUI.DisabledScope(m_ViewManager.HasPendingCategories() || firstIssue.WasFixed))
                                {
                                    if (descriptor.Fixer != null)
                                    {
                                        var content = string.IsNullOrEmpty(descriptor.FixerLabel) ? SharedContents.QuickFix : EditorGUIUtility.TrTempContent(descriptor.FixerLabel);
                                        if (GUILayout.Button(firstIssue.WasFixed ? SharedContents.QuickFixDone : content, EditorStyles.miniButton,
                                            GUILayout.Width(buttonWidth)))
                                        {
                                            descriptor.Fix(firstIssue, m_ViewManager.Report.SessionInfo);
                                            m_ViewManager.OnSelectedIssuesQuickFixRequested?.Invoke([firstIssue]);
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

        static bool ButtonWithDropdownList(GUIContent content, string[] buttonNames,
            GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(content, SharedStyles.MiniPulldown, options);
            var dropDownRect = rect;

            const float kDropDownButtonWidth = 20f;
            dropDownRect.xMin = dropDownRect.xMax - kDropDownButtonWidth;

            if (Event.current.type == EventType.MouseDown && dropDownRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                for (var i = 0; i != buttonNames.Length; i++)
                    menu.AddItem(new GUIContent(buttonNames[i]), false, callback, i);

                menu.DropDown(rect);
                Event.current.Use();

                return false;
            }

            return GUI.Button(rect, content, SharedStyles.MiniPulldown);
        }

        internal void SwitchTab(IssueCategory category, string searchString = null)
        {
            switch (category)
            {
                case IssueCategory.ProjectSetting:
                    SwitchTab(TabId.Settings);
                    break;
                case IssueCategory.DomainReload:
                    m_ViewManager.ChangeView(category);
                    break;
                case IssueCategory.Code:
                    SwitchTab(TabId.Code);
                    break;
                case IssueCategory.AssetIssue:
                    SwitchTab(TabId.Assets);
                    break;
                case IssueCategory.GameObject:
                    SwitchTab(TabId.GameObjects);
                    break;
                default:
                    Debug.LogWarning($"SummaryView.SwitchTab has unhandled category: {category}");
                    break;
            }
            if (searchString != null)
                m_ViewManager.GetActiveView().SetSearch(searchString);
        }

        void SwitchTab(TabId selectedTab)
        {
            var category = TabToCategory(selectedTab);

            m_ViewManager.ChangeView(category);
        }

        IssueCategory TabToCategory(TabId tabId)
        {
            switch (tabId)
            {
                case TabId.Settings:
                    return IssueCategory.ProjectSetting;
                case TabId.Code:
                    return IssueCategory.Code;
                case TabId.GameObjects:
                    return IssueCategory.GameObject;
                default:
                    return IssueCategory.AssetIssue;
            }
        }

        static void DrawDiagnosticLabel(Descriptor descriptor, int count)
        {
            var text = descriptor.Title;

            var content = new GUIContent(text);
            var size = SharedStyles.LabelRichText.CalcSize(content);
            EditorGUILayout.LabelField(content, SharedStyles.LabelRichText, GUILayout.Width(size.x));

            if (count > 1)
                EditorGUILayout.LabelField($"({count} Items)", SharedStyles.LabelDarkWithDynamicSize);
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
                new KeyValuePair<string, string>("Project Areas", sessionInfo.ProjectAreas.ToString()),
                new KeyValuePair<string, string>("Analysis Platform", Formatting.GetModernBuildTargetName(sessionInfo.Platform))
            ]);

            if ((sessionInfo.ProjectAreas & ProjectAreaFlags.Code) != 0)
            {
                keyValues.Add(new KeyValuePair<string, string>("Code Analysis Areas", sessionInfo.CodeAnalysisFlags.ToString()));
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
                newFoldoutState = EditorGUILayout.Foldout(foldoutState, $"{title} ({value} issues)", SharedStyles.Foldout);
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
                            var text = $"{title} analysis still running...";
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
                                if (m_ViewManager.Report.HasCategory(category))
                                    m_ViewManager.ChangeView(category);
                                else
                                    m_Window.GotoNonAnalyzedCategory(category);

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

                List<ChartUtil.Element> inValues = new List<ChartUtil.Element>();
                if (error != 0)
                    inValues.Add(new ChartUtil.Element("Error", "Errors", error, m_SeverityColors[0], Utility.GetIcon(Utility.IconType.Error)));
                if (critical != 0)
                    inValues.Add(new ChartUtil.Element("Critical", "Critical issues", critical, m_SeverityColors[0], Utility.GetIcon(Utility.IconType.Critical)));
                if (major != 0)
                    inValues.Add(new ChartUtil.Element("Major", "Major issues", major, m_SeverityColors[1], Utility.GetIcon(Utility.IconType.Major)));
                if (moderate != 0)
                    inValues.Add(new ChartUtil.Element("Moderate", "Moderate issues", moderate, m_SeverityColors[2], Utility.GetIcon(Utility.IconType.Moderate)));
                if (minor != 0)
                    inValues.Add(new ChartUtil.Element("Minor", "Minor issues", minor, m_SeverityColors[3], Utility.GetIcon(Utility.IconType.Minor)));
                inValues.Add(new ChartUtil.Element("Ignored", "Ignored issues", ignored, m_SeverityColors[4], Utility.GetIcon(Utility.IconType.Ignored)));

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
                    m_ViewManager.ChangeView(category);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
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
                        if (m_ViewManager.Report.HasCategory(category))
                            m_ViewManager.ChangeView(category);
                        else
                            m_Window.GotoNonAnalyzedCategory(category);

                        GUIUtility.ExitGUI();
                    }
                }

                GUILayout.Space(20);
            }
        }

        static class Contents
        {
            public static readonly GUIContent IssueBreakdownContent = new GUIContent("Issue Breakdown");
            public static readonly GUIContent TopTenIssuesContent = new GUIContent("Top Ten Issues");
            public static readonly GUIContent AdditionalInsightChecksContent = new GUIContent("Additional Insights");
            public static readonly GUIContent SessionInformationContent = new GUIContent("Session Information");
            public static readonly GUIContent MoreDetails = new GUIContent("More Details");
        }
    }
}
