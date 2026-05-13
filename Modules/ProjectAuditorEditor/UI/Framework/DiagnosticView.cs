// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class DiagnosticView : AnalysisView
    {
        public override string Description => $"A list of {m_Desc.DisplayName} issues found in the project.";
        public override bool OnlyCriticalIssues() { return m_OnlyCriticalIssues; }
        public override bool OnlyPerfCriticalIssues() { return m_OnlyPerfCriticalIssues; }
        public override bool OnlyFixableIssues() { return m_OnlyFixableIssues; }

        Vector2 m_RecommendationScrollPos;

        bool m_OnlyCriticalIssues;
        bool m_OnlyPerfCriticalIssues;
        bool m_OnlyFixableIssues;
        bool m_ShowUpgradeRecommendations;

        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            Descriptor descriptor = null;
            var descriptorDictionary = new HashSet<Descriptor>();
            bool allFixed = true;
            //bool anyFixable = false;
            bool allFixable = true;
            foreach (var issue in selectedIssues)
            {
                var currentDescriptor = issue.Id.GetDescriptor();
                descriptorDictionary.Add(currentDescriptor);
                if (allFixed && currentDescriptor.Fixer != null && issue.WasFixed == false)
                    allFixed = false;
                //if (!anyFixable && currentDescriptor.Fixer != null)
                //    anyFixable = true;
                if (allFixable && currentDescriptor.Fixer == null)
                    allFixable = false;
            }

            var numSelectedIDs = descriptorDictionary.Count;
            bool oneSelectedID = numSelectedIDs == 1;
            bool anySelectedIDs = numSelectedIDs > 0;
            bool multipleSelectedIDs = numSelectedIDs > 1;

            string selectedText = k_NoSelectionText;
            string recommendationText = k_NoSelectionText;
            string documentationUrl = null;
            if (multipleSelectedIDs)
            {
                selectedText = k_MultipleSelectionText;
                recommendationText = k_MultipleSelectionText;
            }
            else if (oneSelectedID)
            {
                descriptor = selectedIssues[0].Id.GetDescriptor();
                selectedText = descriptor.Description;
                recommendationText = descriptor.Recommendation;
                documentationUrl = descriptor.DocumentationUrl;

                if (selectedIssues[0].IsUpgradeIssue)
                {
                    var recommendation = selectedIssues[0].UpgradeProperties[(int)UpgradeProperties.Recommendation];
                    recommendationText += $"\n\n<i>{recommendation}</i>";
                }
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            DrawDetailsHeader(SharedContents.Details,
                anySelectedIDs ? selectedText : null,
                documentationUrl);

            DrawDetailsContent(selectedText, documentationUrl);

            GUILayout.Space(8);
            ChartUtil.DrawLine(m_2D);
            GUILayout.Space(8);

            DrawDetailsHeader(SharedContents.Recommendation,
                anySelectedIDs ? recommendationText : null,
                null);

            DrawDetailsContent(recommendationText, null, ref m_RecommendationScrollPos);

            using (new EditorGUI.DisabledScope(m_ViewManager.HasPendingCategories()))
            {
                if (anySelectedIDs)
                {
                    if (allFixable) // If Quick Fix displayed a preview before applying fixes, we could show the button if only part of the selection waas fixable (anyFixable), but for now, hide it to avoid confusion
                    {
                        using (new EditorGUI.DisabledScope(allFixed))
                        {
                            var content = (multipleSelectedIDs || string.IsNullOrEmpty(descriptor.FixerLabel))
                                ? SharedContents.QuickFix
                                : EditorGUIUtility.TrTempContent(descriptor.FixerLabel);

                            DrawActionButton(allFixed ? SharedContents.QuickFixDone : content, () =>
                            {
                                foreach (var issue in selectedIssues)
                                    issue.Id.GetDescriptor().Fix(issue, m_ViewManager.Report.SessionInfo);

                                m_ViewManager.OnSelectedIssuesQuickFixRequested?.Invoke(selectedIssues);
                            });
                        }
                    }
                }

                if (selectedIssues.Length == 1)
                    m_ViewManager.AssistantController.DrawAskAssistantButton(descriptor, selectedIssues[0], DrawActionButton);

                if (anySelectedIDs)
                {
                    var issuesAreIgnored = Array.TrueForAll(selectedIssues, i => i.IsIgnored);
                    if (issuesAreIgnored)
                    {
                        DrawActionButton(selectedIssues.Length > 1 ? Contents.DisplayAll : Contents.Display, () =>
                        {
                            foreach (var t in selectedIssues)
                                t.IsIgnored = false;

                            ProjectAuditorSettings.instance.Save();
                            m_ViewManager.OnSelectedIssuesDisplayRequested?.Invoke(selectedIssues);

                            m_Table.Clear();
                            m_Table.AddIssues(m_Issues);
                            m_Table.Reload();
                        });
                    }
                    else
                    {
                        DrawActionButton(selectedIssues.Length > 1 ? Contents.IgnoreAll : Contents.Ignore, () =>
                        {
                            foreach (var t in selectedIssues)
                            {
                                t.IsIgnored = true;
                            }

                            ProjectAuditorSettings.instance.Save();
                            m_ViewManager.OnSelectedIssuesIgnoreRequested?.Invoke(selectedIssues);

                            m_Table.Clear();
                            m_Table.AddIssues(m_Issues);
                            m_Table.Reload();
                        });
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        public override void DrawFilters()
        {
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(SharedContents.Show, GUILayout.ExpandWidth(true), ProjectAuditorWindow.LayoutSize.FilterOptionsLabelWidth);

                var oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                m_OnlyCriticalIssues = EditorGUILayout.ToggleLeft(Contents.OnlyMajor, m_OnlyCriticalIssues, GUILayout.Width(180));
                if (m_Desc.ShowQuickFixes)
                    m_OnlyFixableIssues = EditorGUILayout.ToggleLeft(Contents.OnlyQuickFixes, m_OnlyFixableIssues, GUILayout.Width(180));
                if (m_Desc.ShowPerformanceCritical)
                    m_OnlyPerfCriticalIssues = EditorGUILayout.ToggleLeft(Contents.OnlyPerformanceCritical, m_OnlyPerfCriticalIssues, GUILayout.Width(180));

                EditorGUI.BeginChangeCheck();
                m_Table.showIgnoredIssues = EditorGUILayout.ToggleLeft(SharedContents.ShowIgnoredIssues, m_Table.showIgnoredIssues, GUILayout.Width(180));
                if (EditorGUI.EndChangeCheck())
                    m_ViewManager.OnIgnoredIssuesVisibilityChanged?.Invoke(m_Table.showIgnoredIssues);

                EditorGUI.indentLevel = oldIndent;
            }

            if (ObsoleteLibrary.HasAnyUpgradeVersions)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(" ", ProjectAuditorWindow.LayoutSize.FilterOptionsLabelWidth);

                    var oldIndent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    m_ShowUpgradeRecommendations = EditorGUILayout.ToggleLeft(Contents.ShowUpgradeRecommendations, m_ShowUpgradeRecommendations, GUILayout.Width(180));

                    using (new EditorGUI.DisabledScope(!m_ShowUpgradeRecommendations))
                    {
                        EditorGUILayout.LabelField(Contents.UpgradeTargetVersionLabel, GUILayout.Width(90));

                        int selectedIndex = Array.IndexOf(ObsoleteLibrary.UnityVersions, m_ViewStates.upgradeTargetVersion);
                        if (selectedIndex == -1)
                        {
                            m_ViewStates.upgradeTargetVersion = ObsoleteLibrary.UnityVersions[^1];
                            selectedIndex = ObsoleteLibrary.UnityVersions.Length - 1;
                        }

                        selectedIndex = EditorGUILayout.Popup(selectedIndex, ObsoleteLibrary.UnityVersions, GUILayout.Width(100));
                        m_ViewStates.upgradeTargetVersion = ObsoleteLibrary.UnityVersions[selectedIndex];
                    }

                    EditorGUI.indentLevel = oldIndent;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
                ClearSelection();
            }
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField("\u2022 Use the Filters to reduce the number of reported issues");
            EditorGUILayout.LabelField("\u2022 Use the Ignore button to mark an issue as false-positive");
        }

        protected override void Export(Func<ReportItem, bool> predicate = null)
        {
            var path = EditorUtility.SaveFilePanel("Save to CSV file", UserPreferences.LoadSavePath, string.Format("project-auditor-{0}.csv", m_Desc.Category).ToLower(), "csv");
            if (path.Length != 0)
            {
                using (var exporter = new CsvExporter(m_ViewManager.Report))
                {
                    var issues = GetIssuesToExport();
                    exporter.Export(path, m_Layout.Category, issues, (issue) =>
                    {
                        if (!issue.Id.IsValid())
                            return false;
                        if (!Match(issue))
                            return false;

                        if (predicate != null && !predicate(issue))
                            return false;

                        return m_Rules.GetAction(issue.Id, issue.GetContext()) != Severity.None;
                    });
                }

                EditorUtility.RevealInFinder(path);

                m_ViewManager.OnViewExportCompleted?.Invoke();

                UserPreferences.LoadSavePath = Path.GetDirectoryName(path);
            }
        }

        public override bool Match(ReportItem issue)
        {
            if (!base.Match(issue))
                return false;

            if (ObsoleteLibrary.HasAnyUpgradeVersions)
            {
                // Check the upgrade target version, for issues that need filtering per-version
                if (issue.IsUpgradeIssue)
                {
                    if (!m_ShowUpgradeRecommendations)
                        return false;

                    var targetVersion = m_ViewStates.upgradeTargetVersion;
                    var realTargetVersionInt = Utility.VersionToInt(targetVersion);

                    var upgradeProblemSince = issue.UpgradeProperties[(int)UpgradeProperties.MinVersion];
                    var upgradeProblemUntil = issue.UpgradeProperties[(int)UpgradeProperties.MaxVersion];

                    var upgradeProblemSinceInt = Utility.VersionToInt(upgradeProblemSince);
                    var upgradeProblemUntilInt = string.IsNullOrEmpty(upgradeProblemUntil) ? int.MaxValue : Utility.VersionToInt(upgradeProblemUntil);

                    if (upgradeProblemSinceInt > realTargetVersionInt || upgradeProblemUntilInt <= realTargetVersionInt)
                        return false;
                }
            }

            if (m_Table.showIgnoredIssues)
                return true;

            return !issue.IsIgnored;
        }

        internal static class Contents
        {
            public static readonly GUIContent Ignore = new GUIContent("Ignore Issue", "Ignore selected issue");
            public static readonly GUIContent IgnoreAll = new GUIContent("Ignore Issues", "Ignore selected issues");
            public static readonly GUIContent Display = new GUIContent("Display", "Always show selected issue");
            public static readonly GUIContent DisplayAll = new GUIContent("Display All", "Always show selected issues");
            public static readonly GUIContent OnlyMajor = new GUIContent("Only Major/Critical", "Only display the most important issues");
            public static readonly GUIContent OnlyQuickFixes = new GUIContent("Only Quick Fixes", "Only show issues where a Quick Fix is available");
            public static readonly GUIContent OnlyPerformanceCritical = new GUIContent("Only Performance Critical", "Only show issues occurring in frequently executed code, such as per-frame Update loops");
            public static readonly GUIContent ShowUpgradeRecommendations = new GUIContent("Upgrade Recommendations", "Show issues relating to upgrading to future Unity versions");
            public static readonly GUIContent UpgradeTargetVersionLabel = new GUIContent("Target Version:");
        }
    }
}
