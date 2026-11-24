// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class CompilerMessagesView : AnalysisView
    {
        bool m_ShowInfo;
        bool m_ShowWarn;
        bool m_ShowError;

        public override string Description => "C# Compiler messages and Roslyn Analyzer diagnostics.";

        public CompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
            m_ShowInfo = m_ShowWarn = m_ShowError = true;
        }

        protected override void DrawDetailsSummary(ReportItem[] selectedIssues)
        {
            var logLevel = selectedIssues[0].LogLevel;
            var showLogLevel = true;
            foreach (var issue in selectedIssues)
                showLogLevel &= issue.LogLevel == logLevel;

            EditorGUILayout.LabelField(
                (selectedIssues.Length == 1) ? Contents.CompilerMessage : SharedContents.MultipleIssues, SharedStyles.DetailsTitleLabel);

            GUILayout.Space(4.0f);

            if (showLogLevel)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    var logLevelContent = Utility.GetLogLevelIconWithText(logLevel);
                    var iconSize = EditorGUIUtility.singleLineHeight;
                    GUILayout.Label(logLevelContent.image, GUIStyle.none, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                    GUILayout.Label(logLevelContent.text, EditorStyles.label, GUILayout.ExpandWidth(false));
                }
            }

            EditorGUILayout.Space();
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            var issue = selectedIssues[0];
            var singleId = selectedIssues.TrueForAll(
                i => i.Description == issue.Description && i.RelativePath == issue.RelativePath && i.Line == issue.Line);

            string selectedText = k_MultipleSelectionText;
            string selectedLocationText = k_MultipleSelectionText;
            if (singleId)
            {
                selectedText = issue.Description ?? k_NoneSelectionText;

                if (!string.IsNullOrEmpty(issue.RelativePath))
                    selectedLocationText = issue.GetProperty(Core.PropertyType.Path);
                else
                    selectedLocationText = k_NoneSelectionText;
            }

            DrawDetailsHeader(SharedContents.Details, selectedText, null);
            DrawDetailsContent(selectedText, null);

            EditorGUILayout.Space();

            DrawDetailsHeader(Contents.Location, selectedLocationText, null);
            DrawDetailsContent(selectedLocationText, null, ref m_RecommendationScrollPos);
        }

        protected override void DrawInfo()
        {
            base.DrawInfo();
            EditorGUILayout.LabelField(Contents.Info, SharedStyles.TextArea);
        }

        public override void DrawViewOptions()
        {
            base.DrawViewOptions();

            EditorGUI.BeginChangeCheck();

            m_ShowInfo = GUILayout.Toggle(m_ShowInfo, Contents.ShowInfo, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            m_ShowWarn = GUILayout.Toggle(m_ShowWarn, Contents.ShowWarnings, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            m_ShowError = GUILayout.Toggle(m_ShowError, Contents.ShowErrors, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));

            if (EditorGUI.EndChangeCheck())
                MarkDirty();
        }

        public override bool Match(ReportItem issue)
        {
            switch (issue.Severity)
            {
                default:
                case Severity.Info:
                    if (!m_ShowInfo)
                        return false;
                    break;
                case Severity.Warning:
                    if (!m_ShowWarn)
                        return false;
                    break;
                case Severity.Error:
                    if (!m_ShowError)
                        return false;
                    break;
            }
            return base.Match(issue);
        }

        static class Contents
        {
            public static readonly GUIContent Info = L10n.TextContent("To view Roslyn Analyzer diagnostics, make sure Roslyn Analyzer DLLs use the <b>RoslynAnalyzer</b> label.", null, null, null);

            public static readonly GUIContent ShowInfo = Utility.GetIcon(Utility.IconType.Info, "Show info messages");
            public static readonly GUIContent ShowWarnings = Utility.GetIcon(Utility.IconType.Warning, "Show warnings");
            public static readonly GUIContent ShowErrors = Utility.GetIcon(Utility.IconType.Error, "Show errors");
            public static readonly GUIContent Location = L10n.TextContent("Location", null, null, null);
            
            public static readonly string CompilerMessage = L10n.Tr("Compiler Message", null);
        }
    }
}
