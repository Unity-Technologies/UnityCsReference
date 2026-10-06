// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class UpgradeSummaryView : SummaryView
    {
        private bool m_ShowBeforeUpgrade = true;
        private bool m_ShowAfterUpgrade = true;

        StatSeverities m_CombinedSeverities;
        StatSeverities m_BeforeUpgradeSeverities;
        StatSeverities m_AfterUpgradeSeverities;

        TopTen m_BeforeUpgradeTopTen = NewTopTen();
        TopTen m_AfterUpgradeTopTen = NewTopTen();

        static readonly int kUnityVersionInt = Utility.VersionToInt(Application.unityVersion);

        public override string Description => "Resolve the following issues to upgrade your project to a specific version of the Unity Editor.";

        public UpgradeSummaryView(ViewManager viewManager) : base(viewManager)
        {
        }

        // The Upgrade breakdown shows only Upgrade-area issues relevant to the selected target version.
        protected override bool MatchesSummaryFilter(ReportItem issue)
        {
            if (!HasAnyAreas(issue, Areas.Upgrade))
                return false;

            if (!ObsoleteLibrary.MatchesTargetVersion(issue, m_ViewStates.upgradeTargetVersion))
                return false;

            return true;
        }

        protected override void OnSummaryRefreshed()
        {
            m_BeforeUpgradeTopTen.Refresh = true;
            m_AfterUpgradeTopTen.Refresh = true;
        }

        protected override void ResetStats()
        {
            base.ResetStats();

            m_CombinedSeverities = new StatSeverities();
            m_BeforeUpgradeSeverities = new StatSeverities();
            m_AfterUpgradeSeverities = new StatSeverities();
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

                AddSeverityStats(issue, ref m_CombinedSeverities);

                if (CanFixBeforeUpgrade(issue))
                    AddSeverityStats(issue, ref m_BeforeUpgradeSeverities);
                else
                    AddSeverityStats(issue, ref m_AfterUpgradeSeverities);
            }
        }

        // An upgrade issue can be addressed before upgrading if it already applies to the current Unity version (e.g. the API is already deprecated)
        static bool CanFixBeforeUpgrade(ReportItem issue)
        {
            if (!issue.IsUpgradeIssue)
                return true;

            var minVersion = issue.UpgradeProperties[(int)UpgradeProperties.MinVersion];
            if (string.IsNullOrEmpty(minVersion))
                return true;

            return Utility.VersionToInt(minVersion) <= kUnityVersionInt;
        }

        public override void DrawContent()
        {
            RefreshIfDirty();

            // No report yet (e.g. analysis just started): the sections below dereference the report.
            if (m_ViewManager.Report == null)
                return;

            EditorGUILayout.Space();
            DrawUpgradeVersions();
            EditorGUILayout.Space();
            DrawSeverityBreakdown();

            if (!m_ViewManager.HasPendingCategories() && m_CombinedSeverities.TotalExcludingIgnored > 0)
            {
                EditorGUILayout.Space();
                DrawBeforeUpgradeSection();
                EditorGUILayout.Space();
                DrawAfterUpgradeSection();
            }

            EditorGUILayout.Space();
            DrawSessionInformationSection();
        }

        protected override bool IsIssueIgnoredOrFiltered(ReportItem item)
        {
            if (base.IsIssueIgnoredOrFiltered(item))
                return true;
            if (!item.IsUpgradeIssue)
                return true;
            if (!ObsoleteLibrary.MatchesTargetVersion(item, m_ViewStates.upgradeTargetVersion))
                return true;

            return false;
        }

        bool IsIssueIgnoredOrFilteredBeforeUpgrade(ReportItem item)
        {
            if (IsIssueIgnoredOrFiltered(item))
                return true;

            return !CanFixBeforeUpgrade(item);
        }

        bool IsIssueIgnoredOrFilteredAfterUpgrade(ReportItem item)
        {
            if (IsIssueIgnoredOrFiltered(item))
                return true;

            return CanFixBeforeUpgrade(item);
        }

        void DrawUpgradeVersions()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Current version
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Contents.CurrentVersion, SharedStyles.Label);
            EditorGUILayout.LabelField(Application.unityVersion, SharedStyles.BoldLabel, GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // Target version
            if (ObsoleteLibrary.UnityVersions.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(Contents.TargetVersion, SharedStyles.Label);

                // Changing the target version re-filters the breakdown stats (see MatchesSummaryFilter).
                EditorGUI.BeginChangeCheck();
                Utility.DrawUpgradePopup(m_ViewStates);
                if (EditorGUI.EndChangeCheck())
                    MarkDirty();

                if (GUILayout.Button(Contents.TargetVersionWhatsNew, SharedStyles.LinkLabel, GUILayout.Height(14)))
                {
                    var digits = new StringBuilder();

                    foreach (char c in m_ViewStates.upgradeTargetVersion)
                    {
                        if (char.IsDigit(c) && c != '0')
                            digits.Append(c);
                    }

                    string help = Help.FindHelpNamed($"UpgradeGuideUnity{digits.ToString()}");
                    Help.BrowseURL(help);
                }

                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                GUILayout.FlexibleSpace();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        void DrawSeverityBreakdown()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.LabelField(SharedContents.Summary, SharedStyles.BoldLabel);

            // In progress
            if (m_ViewManager.HasPendingCategories())
            {
                DrawAnalysisInProgress();
                EditorGUILayout.EndVertical();
                return;
            }

            // No issues
            if (m_CombinedSeverities.Total == 0)
            {
                EditorGUILayout.LabelField(Contents.NoIssuesText, SharedStyles.Label);
                EditorGUILayout.EndVertical();
                return;
            }

            // Major issue count
            if (m_CombinedSeverities.MajorAndCritical == 0)
                EditorGUILayout.LabelField(Contents.NoMajorIssuesText, SharedStyles.Label);
            else
                EditorGUILayout.LabelField(string.Format(m_CombinedSeverities.MajorAndCritical == 1 ? Contents.SevereCountFormat : Contents.SevereCountPluralFormat, m_CombinedSeverities.MajorAndCritical), SharedStyles.Label);

            GUILayout.Space(6);

            DrawSeverityBar(m_CombinedSeverities, horizontalPadding: 2);

            EditorGUILayout.EndVertical();
        }

        static void DrawAnalysisInProgress()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20);
                DrawAnalysisInProgressLabel(Contents.Upgrade);
            }
        }

        void DrawBeforeUpgradeSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            var issueCount = m_BeforeUpgradeSeverities.TotalExcludingIgnored;
            var foldoutLabel = (issueCount == 1) ? Contents.BeforeUpgrade : Contents.BeforeUpgradePlural;
            m_ShowBeforeUpgrade = Utility.BoldFoldout(m_ShowBeforeUpgrade, Utility.TempContent(string.Format(foldoutLabel, issueCount)));
            if (m_ShowBeforeUpgrade)
            {
                EditorGUI.indentLevel++;
                var label = (issueCount > 0) ? Contents.BeforeUpgradeDescription : Contents.BeforeUpgradeNoIssuesDescription;
                EditorGUILayout.LabelField(label);
                EditorGUI.indentLevel--;
                DrawTopTenIssues(m_BeforeUpgradeTopTen, IsIssueIgnoredOrFilteredBeforeUpgrade);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawAfterUpgradeSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            var issueCount = m_AfterUpgradeSeverities.TotalExcludingIgnored;
            var foldoutLabel = (issueCount == 1) ? Contents.AfterUpgrade : Contents.AfterUpgradePlural;
            m_ShowAfterUpgrade = Utility.BoldFoldout(m_ShowAfterUpgrade, Utility.TempContent(string.Format(foldoutLabel, issueCount)));
            if (m_ShowAfterUpgrade)
            {
                EditorGUI.indentLevel++;
                var label = (issueCount > 0) ? Contents.AfterUpgradeDescription : Contents.AfterUpgradeNoIssuesDescription;
                EditorGUILayout.LabelField(label);
                EditorGUI.indentLevel--;
                DrawTopTenIssues(m_AfterUpgradeTopTen, IsIssueIgnoredOrFilteredAfterUpgrade);
            }

            EditorGUILayout.EndVertical();
        }

        static class Contents
        {
            public static readonly GUIContent CurrentVersion = L10n.TextContent("Current Unity version:", null, null, null);
            public static readonly GUIContent TargetVersion = L10n.TextContent("Target Unity version:", null, null, null);
            public static readonly GUIContent TargetVersionWhatsNew = L10n.TextContent("What's new?", null, null, null);
            public static readonly GUIContent BeforeUpgradeDescription = L10n.TextContent("These issues can be fixed before you upgrade.", null, null, null);
            public static readonly GUIContent AfterUpgradeDescription = L10n.TextContent("These issues cannot be fixed until after you upgrade.", null, null, null);
            public static readonly GUIContent BeforeUpgradeNoIssuesDescription = L10n.TextContent("There are no known issues to fix before you upgrade!", null, null, null);
            public static readonly GUIContent AfterUpgradeNoIssuesDescription = L10n.TextContent("There are no known issues to fix after you upgrade!", null, null, null);

            public static readonly string SevereCountFormat = L10n.Tr("{0} issue might prevent your project from functioning.", null);
            public static readonly string SevereCountPluralFormat = L10n.Tr("{0} issues might prevent your project from functioning.", null);
            public static readonly string NoIssuesText = L10n.Tr("No upgrade issues found.", null);
            public static readonly string NoMajorIssuesText = L10n.Tr("No major upgrade issues found.", null);
            public static readonly string BeforeUpgrade = L10n.Tr("Before you upgrade ({0} issue)", null);
            public static readonly string BeforeUpgradePlural = L10n.Tr("Before you upgrade ({0} issues)", null);
            public static readonly string AfterUpgrade = L10n.Tr("After you upgrade ({0} issue)", null);
            public static readonly string AfterUpgradePlural = L10n.Tr("After you upgrade ({0} issues)", null);
            public static readonly string Upgrade = L10n.Tr("Upgrade", null);
        }
    }
}
