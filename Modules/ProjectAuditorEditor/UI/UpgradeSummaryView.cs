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

        Stats m_BeforeUpgradeStats;
        Stats m_AfterUpgradeStats;

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

            m_BeforeUpgradeStats = NewStats();
            m_AfterUpgradeStats = NewStats();
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

                if (CanFixBeforeUpgrade(issue))
                    AccumulateStat(issue, ref m_BeforeUpgradeStats);
                else
                    AccumulateStat(issue, ref m_AfterUpgradeStats);
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

        static int TotalIssues(Stats stats)
        {
            return stats.NumCodeIssues + stats.NumAssetIssues + stats.NumGameObjectIssues + stats.NumSettingIssues;
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
            DrawBeforeUpgradeSection();
            EditorGUILayout.Space();
            DrawAfterUpgradeSection();
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

        void DrawBeforeUpgradeSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            var issueCount = TotalIssues(m_BeforeUpgradeStats);
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

            var issueCount = TotalIssues(m_AfterUpgradeStats);
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
            public static readonly GUIContent CurrentVersion = EditorGUIUtility.TrTextContent("Current Unity version:");
            public static readonly GUIContent TargetVersion = EditorGUIUtility.TrTextContent("Target Unity version:");
            public static readonly GUIContent TargetVersionWhatsNew = EditorGUIUtility.TrTextContent("What's new?");
            public static readonly GUIContent BeforeUpgradeDescription = EditorGUIUtility.TrTextContent("These issues can be fixed before you upgrade.");
            public static readonly GUIContent AfterUpgradeDescription = EditorGUIUtility.TrTextContent("These issues cannot be fixed until after you upgrade.");
            public static readonly GUIContent BeforeUpgradeNoIssuesDescription = EditorGUIUtility.TrTextContent("There are no known issues to fix before you upgrade!");
            public static readonly GUIContent AfterUpgradeNoIssuesDescription = EditorGUIUtility.TrTextContent("There are no known issues to fix after you upgrade!");

            public static readonly string BeforeUpgrade = L10n.Tr("Before you upgrade ({0} issue)", null);
            public static readonly string BeforeUpgradePlural = L10n.Tr("Before you upgrade ({0} issues)", null);
            public static readonly string AfterUpgrade = L10n.Tr("After you upgrade ({0} issue)", null);
            public static readonly string AfterUpgradePlural = L10n.Tr("After you upgrade ({0} issues)", null);
        }
    }
}
