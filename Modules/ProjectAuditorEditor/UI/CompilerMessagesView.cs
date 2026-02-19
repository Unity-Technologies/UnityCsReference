// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class CompilerMessagesView : AnalysisView
    {
        const string k_Info = @"This view shows compiler error, warning and info messages.

To view Roslyn Analyzer diagnostics, make sure Roslyn Analyzer DLLs use the <b>RoslynAnalyzer</b> label.";
        const string k_RoslynDisabled = "The UseRoslynAnalyzers option is disabled. To enable Roslyn diagnostics reporting, make sure the corresponding option is enabled in Preferences > Analysis > " + ProjectAuditor.DisplayName + ".";

        bool m_ShowInfo;
        bool m_ShowWarn;
        bool m_ShowError;

        public override string Description => "C# Compiler messages and Roslyn Analyzer diagnostics.";

        public CompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
            m_ShowInfo = m_ShowWarn = m_ShowError = true;
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var selectedDescriptors = selectedIssues.Select(i => i.GetCustomProperty(0)).Distinct().ToArray();
#pragma warning restore UA2001

            string selectedText = k_NoSelectionText;
            if (selectedDescriptors.Length > 1)
            {
                selectedText = k_MultipleSelectionText;
            }
            else if (selectedDescriptors.Length == 1)
            {
                selectedText = $"{selectedIssues[0].Description}\n\n{selectedIssues[0].GetProperty(Core.PropertyType.Path)}";
            }

            DrawDetailsHeader(SharedContents.Details,
                (selectedIssues.Length > 0) ? selectedText : null,
                null);

            DrawDetailsContent(selectedText, null);
        }

        protected override void DrawInfo()
        {
            EditorGUILayout.LabelField(k_Info, SharedStyles.TextArea);

            if (!m_ViewManager.Report.SessionInfo.UseRoslynAnalyzers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_RoslynDisabled, MessageType.Info);
                EditorGUILayout.EndHorizontal();
            }
        }

        public override void DrawViewOptions()
        {
            base.DrawViewOptions();

            EditorGUI.BeginChangeCheck();
            m_ShowInfo = GUILayout.Toggle(m_ShowInfo, Utility.GetIcon(Utility.IconType.Info, "Show info messages"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            m_ShowWarn = GUILayout.Toggle(m_ShowWarn, Utility.GetIcon(Utility.IconType.Warning, "Show warnings"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            m_ShowError = GUILayout.Toggle(m_ShowError, Utility.GetIcon(Utility.IconType.Error, "Show errors"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }
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
    }
}
