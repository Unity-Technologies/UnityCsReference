// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class BuildReportView : AnalysisView
    {
        public virtual string InfoTitle => $"";
        const string k_BulletPointUnicode = " \u2022";
        static readonly string k_BuildInstructions =
            $@"To create a clean build, follow these steps:
{k_BulletPointUnicode} Open the <b>Build Settings</b> window.
{k_BulletPointUnicode} Next to the <b>Build button</b>, select the drop-down.
{k_BulletPointUnicode} Select <b>Clean Build</b>.

If your project uses a custom build script, ensure that it passes the <b>BuildOptions.CleanBuildCache</b> option to <b>BuildPipeline.BuildPlayer</b>.";
        static readonly string k_CleanBuildInfoBox = $"A clean build is important for capturing accurate information about build times and steps. For this reason, {ProjectAuditor.DisplayName} does not display the results of incremental builds.";

        List<ReportItem> m_MetaData = new List<ReportItem>();

        public BuildReportView(ViewManager viewManager) :
            base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ReportItem> allIssues)
        {
            base.AddIssues(allIssues);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_MetaData.AddRange(allIssues.Where(i => i.Category == IssueCategory.BuildSummary));
#pragma warning restore UA2001
        }

        public override void Clear()
        {
            base.Clear();
            m_MetaData.Clear();
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField(InfoTitle);

            if (m_Issues.Count == 0)
            {
                EditorGUILayout.LabelField(k_BuildInstructions, SharedStyles.TextArea);
                EditorGUILayout.HelpBox(k_CleanBuildInfoBox, MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical();
            foreach (var issue in m_MetaData)
            {
                DrawKeyValue(issue.Description, issue.GetCustomProperty(BuildReportMetaData.Value));
            }
            EditorGUILayout.EndVertical();
        }

        Vector2 m_DetailsScrollPos;

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            EditorGUILayout.BeginVertical();

            if (selectedIssues.Length == 0)
                GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else if (selectedIssues.Length > 1)
                GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            else // if (selectedDescriptors.Length == 1)
            {
                var description = GetIssueDescription(selectedIssues[0]);

                m_DetailsScrollPos = EditorGUILayout.BeginScrollView(m_DetailsScrollPos, GUILayout.ExpandHeight(true));
                GUILayout.TextArea(description, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        public virtual string GetIssueDescription(ReportItem issue)
        {
            return issue.Description;
        }

        void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(string.Format("{0}:", key), SharedStyles.Label, GUILayout.ExpandWidth(false));
            EditorGUILayout.LabelField(value, SharedStyles.Label, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
    }
}
