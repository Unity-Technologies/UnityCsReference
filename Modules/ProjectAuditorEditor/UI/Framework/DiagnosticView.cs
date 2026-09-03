// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
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

        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            Descriptor descriptor = null;
            var descriptorIdSet = new HashSet<string>();
            bool allFixed = true;
            //bool anyFixable = false;
            bool allFixable = true;
            foreach (var issue in selectedIssues)
            {
                var currentDescriptor = issue.Id.GetDescriptor();
                descriptorIdSet.Add(currentDescriptor.Id);
                if (allFixed && currentDescriptor.Fixer != null && issue.WasFixed == false)
                    allFixed = false;
                //if (!anyFixable && currentDescriptor.Fixer != null)
                //    anyFixable = true;
                if (allFixable && currentDescriptor.Fixer == null)
                    allFixable = false;
            }

            var numSelectedIDs = descriptorIdSet.Count;
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
                                ApplyQuickFixes(selectedIssues);
                            });
                        }
                    }
                }

                if (selectedIssues.Length == 1)
                    m_ViewManager.AssistantController.DrawAskAssistantButton(descriptor, selectedIssues[0], DrawActionButton);

                if (anySelectedIDs)
                {
                    var saveChanges = () =>
                    {
                        ProjectAuditorSettings.instance.Save();
                        m_ViewManager.OnSelectedIssuesIgnoreRequested?.Invoke(selectedIssues);

                        m_Table.Clear();
                        m_Table.AddIssues(m_Issues);
                        m_Table.Reload();
                    };

                    var setIgnoredForSelectedDescriptors = (bool ignored) =>
                    {
                        foreach (var issue in m_Issues)
                        {
                            if (descriptorIdSet.Contains(issue.Id))
                                issue.IsIgnored = ignored;
                        }
                    };

                    // Ignore
                    var issuesAreIgnored = Array.TrueForAll(selectedIssues, i => i.IsIgnored);
                    if (issuesAreIgnored)
                    {
                        DrawActionButton(selectedIssues.Length > 1 ? Contents.DisplayAll : Contents.Display, () =>
                        {
                            foreach (var t in selectedIssues)
                                t.IsIgnored = false;

                            saveChanges();
                        });
                    }
                    else
                    {
                        DrawActionButton(selectedIssues.Length > 1 ? Contents.IgnoreAll : Contents.Ignore, () =>
                        {
                            foreach (var t in selectedIssues)
                                t.IsIgnored = true;

                            saveChanges();
                        });
                    }

                    // Suppress
                    var suppressedDiagnostics = UserPreferences.BuildSuppressedDiagnosticsSet();
                    var issuesAreSuppressed = Array.TrueForAll(selectedIssues, i => suppressedDiagnostics.Contains(i.Id));

                    if (issuesAreSuppressed)
                    {
                        DrawActionButton(multipleSelectedIDs ? Contents.UnsuppressAll : Contents.Unsuppress, () =>
                        {
                            setIgnoredForSelectedDescriptors(false);
                            foreach (var id in descriptorIdSet)
                                UserPreferences.RemoveSuppressedDiagnostic(id, suppressedDiagnostics);

                            saveChanges.Invoke();
                        });
                    }
                    else
                    {
                        DrawActionButton(multipleSelectedIDs ? Contents.SuppressAll : Contents.Suppress, () =>
                        {
                            var title = multipleSelectedIDs ? Contents.SuppressAllTitle : Contents.SuppressTitle;
                            var message =
                                (multipleSelectedIDs ? string.Format(Contents.SuppressAllBody, descriptorIdSet.Count) : Contents.SuppressBody)
                                + "\n\n"
                                + Contents.SuppressReview;

                            if (!EditorUtility.DisplayDialog(title, message, Contents.SuppressConfirm, Contents.SuppressCancel))
                                return;

                            setIgnoredForSelectedDescriptors(true);
                            foreach (var id in descriptorIdSet)
                                UserPreferences.AddSuppressedDiagnostic(id, suppressedDiagnostics);

                            saveChanges.Invoke();
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

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
                ClearSelection();
            }
        }

        // Draws the Unity target-version selector shared by the Upgrade pages, narrowing displayed
        // upgrade issues to those relevant when upgrading to the chosen version. Assigned as a page's
        // drawFilters delegate (see Page.drawFilters), so it is drawn by the window's Filters panel.
        internal static void DrawUpgradeTargetVersionFilter(ViewStates viewStates)
        {
            if (!ObsoleteLibrary.HasAnyUpgradeVersions)
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(" ", ProjectAuditorWindow.LayoutSize.FilterOptionsLabelWidth);

                var oldIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                EditorGUILayout.LabelField(Contents.UpgradeTargetVersion, GUILayout.Width(160));
                Utility.DrawUpgradePopup(viewStates);

                EditorGUI.indentLevel = oldIndent;
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

            // Filter upgrade issues based on the selected target version. Whether upgrade issues appear at
            // all is decided by the page's filter (Upgrade vs. Optimization).
            if (!ObsoleteLibrary.MatchesTargetVersion(issue, m_ViewStates.upgradeTargetVersion))
                return false;

            if (m_Table.showIgnoredIssues)
                return true;

            return !issue.IsIgnored;
        }

        internal static class Contents
        {
            public static readonly GUIContent Ignore = EditorGUIUtility.TrTextContent("Ignore Issue", "Ignore selected issue");
            public static readonly GUIContent IgnoreAll = EditorGUIUtility.TrTextContent("Ignore Issues", "Ignore selected issues");
            public static readonly GUIContent Suppress = EditorGUIUtility.TrTextContent("Suppress Issue Type", "Add this issue type to the list of suppressed issues that are not included in future reports");
            public static readonly GUIContent SuppressAll = EditorGUIUtility.TrTextContent("Suppress Issue Types", "Add these issue types to the list of suppressed issues that are not included in future reports");
            public static readonly GUIContent Display = EditorGUIUtility.TrTextContent("Display Issues", "Show selected issue");
            public static readonly GUIContent DisplayAll = EditorGUIUtility.TrTextContent("Display Issues", "Show selected issues");
            public static readonly GUIContent Unsuppress = EditorGUIUtility.TrTextContent("Unsuppress Issue Type", "Include this issue type in future reports");
            public static readonly GUIContent UnsuppressAll = EditorGUIUtility.TrTextContent("Unsuppress Issue Types", "Include these issue types in future reports");
            public static readonly GUIContent OnlyMajor = EditorGUIUtility.TrTextContent("Only Major/Critical", "Only display the most important issues");
            public static readonly GUIContent OnlyQuickFixes = EditorGUIUtility.TrTextContent("Only Quick Fixes", "Only show issues where a Quick Fix is available");
            public static readonly GUIContent OnlyPerformanceCritical = EditorGUIUtility.TrTextContent("Only Performance Critical", "Only show issues occurring in frequently executed code, such as per-frame Update loops");
            public static readonly GUIContent UpgradeTargetVersion = EditorGUIUtility.TrTextContent("Upgrade Target Version:");

            public static readonly string SuppressTitle = L10n.Tr("Suppress Issue Type", null);
            public static readonly string SuppressAllTitle = L10n.Tr("Suppress Issue Types", null);
            public static readonly string SuppressBody = L10n.Tr("Suppressing this issue type will ignore it in the current report, and exclude it from all future reports.", null);
            public static readonly string SuppressAllBody = L10n.Tr("Suppressing these {0} issue types will ignore them in the current report, and exclude them from all future reports.", null);
            public static readonly string SuppressReview = L10n.Tr("You can review and manage suppressed issue types by navigating to " + ProjectAuditor.k_PreferencesPath + " > Suppressed Issues.", null);
            public static readonly string SuppressConfirm = L10n.Tr("Suppress", null);
            public static readonly string SuppressCancel = L10n.Tr("Cancel", null);
        }
    }
}
