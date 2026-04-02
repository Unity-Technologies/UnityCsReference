// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using System.Linq;
using UnityEngine;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Modules;
using System;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ObsoleteApiView : AnalysisView
    {
        public override string Description => "A list of obsolete API in all Unity versions.";
        public static string InfoTitle => $@"This view shows all obsolete API across all Unity versions.";

        Vector2 m_RecommendationScrollPos;

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

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField(InfoTitle);
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectedDescriptors = selectedIssues.Select(i => i.GetCustomProperty(0)).Distinct().ToArray();
#pragma warning restore UA2001

            ReportItem issue = null;
            if (selectedDescriptors.Length > 0)
                issue = selectedIssues[0];

            string selectedText = k_NoSelectionText;
            string recommendationText = k_NoSelectionText;
            if (selectedDescriptors.Length > 1)
            {
                selectedText = k_MultipleSelectionText;
                recommendationText = k_MultipleSelectionText;
            }
            else if (selectedDescriptors.Length == 1)
            {
                selectedText = issue.Description;
                recommendationText = issue.GetCustomProperty(ObsoleteApiProperty.Recommendation);
            }

            EditorGUILayout.BeginVertical(GUILayout.Width(LayoutSize.FoldoutWidth));

            string docsUrl = null; // Can we get docs urls in here, in the future?

            DrawDetailsHeader(SharedContents.Details,
                (selectedDescriptors.Length > 0) ? selectedText : null,
                docsUrl);

            DrawDetailsContent(selectedText, docsUrl);

            GUILayout.Space(8);
            ChartUtil.DrawLine(m_2D);
            GUILayout.Space(8);

            DrawDetailsHeader(SharedContents.Recommendation,
                (selectedDescriptors.Length > 0) ? recommendationText : null,
                null);

            DrawDetailsContent(recommendationText, null, ref m_RecommendationScrollPos);

            EditorGUILayout.EndVertical();
        }
    }
}
