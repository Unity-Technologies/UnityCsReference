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

        Vector2 m_RecommendationScrollPos;

        bool m_OnlyCriticalIssues;

        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            Descriptor descriptor = null;
            Dictionary<DescriptorId, bool> dict = new Dictionary<DescriptorId, bool>();
            foreach (var issue in selectedIssues)
            {
                dict[issue.Id] = true;

                if (dict.Count > 1)
                    break;
            }

            var numSelectedIDs = dict.Count;
            bool noSelectedIDs = numSelectedIDs == 0;
            bool oneSelectedID = numSelectedIDs == 1;
            bool anySelectedIDs = numSelectedIDs > 0;
            bool multipleSelectedIDs = numSelectedIDs > 1;
            if (anySelectedIDs)
            {
                descriptor = selectedIssues[0].Id.GetDescriptor();
            }

            string selectedText = k_NoSelectionText;
            string recommendationText = k_NoSelectionText;
            string documentationUrl = null;
            if (numSelectedIDs > 1)
            {
                selectedText = k_MultipleSelectionText;
                recommendationText = k_MultipleSelectionText;
            }
            else if (numSelectedIDs == 1)
            {
                selectedText = descriptor.Description;
                recommendationText = descriptor.Recommendation;
                documentationUrl = descriptor.DocumentationUrl;
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

            var issuesAreIgnored = AreIssuesIgnored(selectedIssues);
            if (oneSelectedID)
            {
                using (new EditorGUI.DisabledScope(m_ViewManager.HasPendingCategories()))
                {
                    if (descriptor.Fixer != null)
                    {
                        bool isFixed = Array.TrueForAll(selectedIssues, i => i.WasFixed);
                        using (new EditorGUI.DisabledScope(isFixed))
                        {
                            var content = string.IsNullOrEmpty(descriptor.FixerLabel) ? SharedContents.QuickFix : EditorGUIUtility.TrTempContent(descriptor.FixerLabel);
                            DrawActionButton(isFixed ? SharedContents.QuickFixDone : content, () =>
                            {
                                foreach (var issue in selectedIssues)
                                {
                                    descriptor.Fix(issue, m_ViewManager.Report.SessionInfo);
                                }

                                m_ViewManager.OnSelectedIssuesQuickFixRequested?.Invoke(selectedIssues);
                            });
                        }
                    }

                    m_ViewManager.AssistantController.DrawAskAssistantButton(descriptor, selectedIssues[0], DrawActionButton);

                    if (selectedIssues.Length > 0)
                    {
                        if (issuesAreIgnored)
                        {
                            DrawActionButton(selectedIssues.Length > 1 ? Contents.DisplayAll : Contents.Display, () =>
                            {
                                foreach (var t in selectedIssues)
                                {
                                    t.IsIgnored = false;
                                }

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
            }

            EditorGUILayout.EndVertical();
        }

        public override void DrawFilters()
        {
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Show:", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                var wasShowingCritical = m_OnlyCriticalIssues;
                m_OnlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Major/Critical", m_OnlyCriticalIssues, GUILayout.Width(170));

                var guiContent = m_Table.showIgnoredIssues
                    ? Contents.ShowIgnoredIssuesButton
                    : Contents.HideIgnoredIssuesButton;

                var wasShowingIgnored = m_Table.showIgnoredIssues;
                m_Table.showIgnoredIssues = EditorGUILayout.ToggleLeft("Show Ignored Issues",
                    m_Table.showIgnoredIssues, GUILayout.Width(170));

                if (wasShowingIgnored != m_Table.showIgnoredIssues)
                {
                    m_ViewManager.OnIgnoredIssuesVisibilityChanged?.Invoke(m_Table.showIgnoredIssues);
                    MarkDirty();
                }
            }

            if (EditorGUI.EndChangeCheck())
                MarkDirty();
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField("\u2022 Use the Filters to reduce the number of reported issues");
            EditorGUILayout.LabelField("\u2022 Use the Ignore button to mark an issue as false-positive");
        }

        bool AreIssuesIgnored(ReportItem[] selectedIssues)
        {
            foreach (var issue in selectedIssues)
            {
                if (!issue.IsIgnored)
                    return false;
            }

            return true;
        }

        protected override void Export(Func<ReportItem, bool> predicate = null)
        {
            var path = EditorUtility.SaveFilePanel("Save to CSV file", UserPreferences.LoadSavePath, string.Format("project-auditor-{0}.csv", m_Desc.Category.ToString()).ToLower(),
                "csv");
            if (path.Length != 0)
            {
                using (var exporter = new CsvExporter(m_ViewManager.Report))
                {
                    exporter.Export(path, m_Layout.Category, (issue) =>
                    {
                        if (!issue.Id.IsValid())
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

            if (m_Table.showIgnoredIssues)
                return true;

            return !issue.IsIgnored;
        }

        internal static class Contents
        {
            public static readonly GUIContent ShowIgnoredIssuesButton = Utility.GetDisplayIgnoredIssuesIconWithLabel();
            public static readonly GUIContent HideIgnoredIssuesButton = Utility.GetHiddenIgnoredIssuesIconWithLabel();
            public static readonly GUIContent Ignore = new GUIContent("Ignore Issue", "Ignore selected issue");
            public static readonly GUIContent IgnoreAll = new GUIContent("Ignore Issues", "Ignore selected issues");
            public static readonly GUIContent Display = new GUIContent("Display", "Always show selected issue");
            public static readonly GUIContent DisplayAll = new GUIContent("Display All", "Always show selected issues");
        }
    }
}
