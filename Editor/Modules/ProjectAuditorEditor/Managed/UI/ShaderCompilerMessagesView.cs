// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderCompilerMessagesView : AnalysisView
    {
        bool m_ShowWarn;
        bool m_ShowError;

        public override string Description => $"A list of shader compiler messages encountered during the build process.";

        public ShaderCompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
            m_ShowWarn = m_ShowError = true;
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            var issue = selectedIssues[0];
            var singleId = selectedIssues.TrueForAll(i => i.Description == issue.Description);

            string selectedText = singleId ? issue.Description : k_MultipleSelectionText;

            DrawDetailsHeader(SharedContents.Details, selectedText, null);
            DrawDetailsContent(selectedText, null);
        }

        public override void DrawViewOptions()
        {
            base.DrawViewOptions();

            EditorGUI.BeginChangeCheck();

            m_ShowWarn = GUILayout.Toggle(m_ShowWarn, Contents.ShowWarnings, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            m_ShowError = GUILayout.Toggle(m_ShowError, Contents.ShowErrors, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));

            if (EditorGUI.EndChangeCheck())
                MarkDirty();
        }

        public override bool Match(ReportItem issue)
        {
            switch (issue.Severity)
            {
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
            public static readonly GUIContent ShowWarnings = Utility.GetIcon(Utility.IconType.Warning, "Show warnings");
            public static readonly GUIContent ShowErrors = Utility.GetIcon(Utility.IconType.Error, "Show errors");
        }
    }
}
