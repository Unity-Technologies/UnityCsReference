// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class DiagnosticView : AnalysisView
    {
        public override string Description => $"A list of {m_Desc.DisplayName} issues found in the project.";

        Vector2 m_DetailsScrollPos;
        Vector2 m_RecommendationScrollPos;

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

                if (dict.Count() > 1)
                    break;
            }

            var numSelectedIDs = dict.Count();
            bool noSelectedIDs = numSelectedIDs == 0;
            bool oneSelectedID = numSelectedIDs == 1;
            bool anySelectedIDs = numSelectedIDs > 0;
            bool multipleSelectedIDs = numSelectedIDs > 1;
            if (anySelectedIDs)
            {
                descriptor = selectedIssues[0].Id.GetDescriptor();
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.Details, SharedStyles.BoldLabel);
                {
                    if (anySelectedIDs)
                    {
                        if (GUILayout.Button(Contents.CopyToClipboard, SharedStyles.TabButton,
                            GUILayout.Width(LayoutSize.CopyToClipboardButtonSize),
                            GUILayout.Height(LayoutSize.CopyToClipboardButtonSize)))
                        {
                            EditorInterop.CopyToClipboard(Formatting.StripRichTextTags(descriptor.Description));
                        }
                    }
                }
            }

            m_DetailsScrollPos =
                EditorGUILayout.BeginScrollView(m_DetailsScrollPos, GUILayout.ExpandHeight(true));

            if (noSelectedIDs)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (multipleSelectedIDs)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else
            {
                GUILayout.TextArea(descriptor.Description, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.EndScrollView();

            ChartUtil.DrawLine(m_2D);
            GUILayout.Space(10);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Contents.Recommendation, SharedStyles.BoldLabel);
                {
                    if (anySelectedIDs)
                    {
                        if (GUILayout.Button(Contents.CopyToClipboard, SharedStyles.TabButton,
                            GUILayout.Width(LayoutSize.CopyToClipboardButtonSize),
                            GUILayout.Height(LayoutSize.CopyToClipboardButtonSize)))
                        {
                            EditorInterop.CopyToClipboard(Formatting.StripRichTextTags(descriptor.Recommendation));
                        }
                    }
                }
            }

            m_RecommendationScrollPos =
                EditorGUILayout.BeginScrollView(m_RecommendationScrollPos, GUILayout.ExpandHeight(true));

            if (noSelectedIDs)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (multipleSelectedIDs)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else
            {
                GUILayout.TextArea(descriptor.Recommendation, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }

            EditorGUILayout.EndScrollView();

            var issuesAreIgnored = AreIssuesIgnored(selectedIssues);
            if (oneSelectedID)
            {
                if (!string.IsNullOrEmpty(descriptor.DocumentationUrl))
                {
                    DrawActionButton(Contents.Documentation, () =>
                    {
                        Application.OpenURL(descriptor.DocumentationUrl);

                        m_ViewManager.OnSelectedIssuesDocumentationRequested?.Invoke(selectedIssues);
                    });
                }

                if (descriptor.Fixer != null)
                {
                    GUI.enabled = selectedIssues.Any(i => !i.WasFixed);

                    DrawActionButton(GUI.enabled ? Contents.QuickFix : Contents.QuickFixDone, () =>
                    {
                        foreach (var issue in selectedIssues)
                        {
                            descriptor.Fix(issue, m_ViewManager.Report.SessionInfo);
                        }

                        m_ViewManager.OnSelectedIssuesQuickFixRequested?.Invoke(selectedIssues);
                    });

                    GUI.enabled = true;
                }
            }

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

            EditorGUILayout.EndVertical();
        }

        public override void DrawFilters()
        {
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                var wasShowingCritical = m_ViewStates.onlyCriticalIssues;
                m_ViewStates.onlyCriticalIssues = EditorGUILayout.ToggleLeft("Only Major/Critical",
                    m_ViewStates.onlyCriticalIssues, GUILayout.Width(170));

                if (wasShowingCritical != m_ViewStates.onlyCriticalIssues)
                    m_ViewManager.OnMajorOrCriticalIssuesVisibilityChanged?.Invoke(m_ViewStates.onlyCriticalIssues);

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
                        if (!PackageFilterMatch(issue))
                            return false;

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
            public static readonly GUIContent Details = new GUIContent("Details", "Issue Details");
            public static readonly GUIContent Recommendation =
                new GUIContent("Recommendation", "Recommendation on how to solve the issue");
            public static readonly GUIContent Documentation = new GUIContent("Documentation", "Open the Unity documentation");
            public static readonly GUIContent QuickFix = new GUIContent("Quick Fix", "Automatically fix the issue");
            public static readonly GUIContent QuickFixDone = new GUIContent("Fixed", "Quick fix applied");
            public static readonly GUIContent ShowIgnoredIssuesButton = Utility.GetDisplayIgnoredIssuesIconWithLabel();
            public static readonly GUIContent HideIgnoredIssuesButton = Utility.GetHiddenIgnoredIssuesIconWithLabel();
            public static readonly GUIContent Ignore = new GUIContent("Ignore Issue", "Ignore selected issue");
            public static readonly GUIContent IgnoreAll = new GUIContent("Ignore Issues", "Ignore selected issues");
            public static readonly GUIContent Display = new GUIContent("Display", "Always show selected issue");
            public static readonly GUIContent DisplayAll = new GUIContent("Display All", "Always show selected issues");
            public static readonly GUIContent CopyToClipboard = Utility.GetIcon(Utility.IconType.CopyToClipboard);
        }
    }
}
