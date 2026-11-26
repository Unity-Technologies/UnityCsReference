// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class DiagnosticView : AnalysisView
    {
        public override bool OnlyCriticalIssues() { return m_OnlyCriticalIssues; }
        public override bool OnlyPerfCriticalIssues() { return m_OnlyPerfCriticalIssues; }
        public override bool OnlyFixableIssues() { return m_OnlyFixableIssues; }
        public override bool IncludeGeneratedSource() { return m_IncludeGeneratedSource; }

        bool m_OnlyCriticalIssues;
        bool m_OnlyPerfCriticalIssues;
        bool m_OnlyFixableIssues;
        bool m_IncludeGeneratedSource = true;

        public DiagnosticView(ViewManager viewManager) : base(viewManager)
        {
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
                if (m_Desc.ShowGeneratedSource)
                    m_IncludeGeneratedSource = EditorGUILayout.ToggleLeft(Contents.GeneratedSource, m_IncludeGeneratedSource, GUILayout.Width(180));

                EditorGUI.BeginChangeCheck();
                m_Table.showIgnoredIssues = EditorGUILayout.ToggleLeft(SharedContents.ShowIgnoredIssues, m_Table.showIgnoredIssues, GUILayout.Width(180));
                if (EditorGUI.EndChangeCheck())
                    m_ViewManager.OnIgnoredIssuesVisibilityChanged?.Invoke(m_Table.showIgnoredIssues);

                EditorGUI.indentLevel = oldIndent;
            }

            if (EditorGUI.EndChangeCheck())
                OnFiltersChanged();
        }

        internal void OnFiltersChanged()
        {
            MarkDirty();
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

        protected override void Export(Func<ReportItem, bool> predicate = null)
        {
            base.Export((issue) =>
            {
                if (!issue.Id.IsValid())
                    return false;
                if (issue.IsIgnored && !m_Table.showIgnoredIssues)
                    return false;
                if (predicate != null && !predicate(issue))
                    return false;

                return m_Rules.GetAction(issue.Id, issue.GetContext()) != Severity.None;
            });
        }

        public override bool Match(ReportItem issue)
        {
            if (!MatchWithoutIgnoredState(issue))
                return false;

            if (m_Table.showIgnoredIssues)
                return true;

            return !issue.IsIgnored;
        }

        internal override bool MatchWithoutIgnoredState(ReportItem issue)
        {
            if (!base.Match(issue))
                return false;

            // Filter upgrade issues based on the selected target version. Whether upgrade issues appear at
            // all is decided by the page's filter (Upgrade vs. Optimization).
            return ObsoleteLibrary.MatchesTargetVersion(issue, m_ViewStates.upgradeTargetVersion);
        }

        internal static class Contents
        {
            public static readonly GUIContent OnlyMajor = L10n.TextContent("Only Major/Critical", "Only display the most important issues", null, null);
            public static readonly GUIContent OnlyQuickFixes = L10n.TextContent("Only Quick Fixes", "Only show issues where a Quick Fix is available", null, null);
            public static readonly GUIContent OnlyPerformanceCritical = L10n.TextContent("Only Performance Critical", "Only show issues occurring in frequently executed code, such as per-frame Update loops", null, null);
            public static readonly GUIContent GeneratedSource = L10n.TextContent("Generated Source", "Show issues in code created by source generators", null, null);
            public static readonly GUIContent UpgradeTargetVersion = L10n.TextContent("Upgrade Target Version:", null, null, null);
        }
    }
}
