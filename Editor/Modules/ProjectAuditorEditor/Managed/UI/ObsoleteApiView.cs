// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ObsoleteApiView : AnalysisView
    {
        public override string Description => "A list of obsolete API in all Unity versions.";

        public ObsoleteApiView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ReportItem> allIssues)
        {
            // This view does not show issues from the report.
            // It shows obsolete API loaded from a database file in the Rules Package
            if (m_Issues.Count == 0 && ProjectAuditorRulesPackage.IsInstalled)
                base.AddIssues(ObsoleteLibrary.LibraryList);
        }

        protected override IReadOnlyCollection<ReportItem> GetIssuesToExport()
        {
            return m_Issues;
        }

        protected override void DrawDetailsSummary(ReportItem[] selectedIssues)
        {
            var issue = selectedIssues[0];

            EditorGUILayout.LabelField(
                (selectedIssues.Length == 1) ? GetMemberName(issue.Description, false) : SharedContents.MultipleIssues, SharedStyles.DetailsTitleLabel);

            EditorGUILayout.Space();
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            var issue = selectedIssues[0];
            var singleId = Array.TrueForAll(selectedIssues, i => i.Description == issue.Description);

            string selectedText = k_MultipleSelectionText;
            string recommendationText = k_MultipleSelectionText;
            if (singleId)
            {
                selectedText = GetMemberName(issue.Description, true);
                recommendationText = issue.GetCustomProperty(ObsoleteApiProperty.Recommendation);
            }

            string docsUrl = null; // Can we get docs urls in here, in the future?

            DrawDetailsHeader(SharedContents.Details, selectedText, docsUrl);
            DrawDetailsContent(selectedText, docsUrl);

            EditorGUILayout.Space();

            DrawDetailsHeader(SharedContents.Recommendation, recommendationText, null);
            DrawDetailsContent(recommendationText, null, ref m_RecommendationScrollPos);
        }

        static string GetMemberName(string description, bool keepParameters)
        {
            if (!keepParameters)
            {
                var paramsStart = description.IndexOf('(');
                if (paramsStart >= 0)
                    description = description.Substring(0, paramsStart);
            }

            return description.Substring(description.LastIndexOf('.') + 1);
        }
    }
}
