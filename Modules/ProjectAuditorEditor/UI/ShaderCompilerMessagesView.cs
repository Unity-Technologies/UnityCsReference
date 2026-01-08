// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    class ShaderCompilerMessagesView : AnalysisView
    {
        const string k_Info = @"This view shows compiler error, warning and info messages.";

        bool m_ShowWarn;
        bool m_ShowError;

        public override string Description => $"A list of shader compiler messages encountered during the build process.";

        public ShaderCompilerMessagesView(ViewManager viewManager) : base(viewManager)
        {
            m_ShowWarn = m_ShowError = true;
        }

        public override void DrawDetails(ReportItem[] selectedIssues)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LayoutSize.FoldoutWidth)))
            {
                if (selectedIssues.Length == 0)
                {
                    GUILayout.TextArea(k_NoSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                    return;
                }

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var selectedDescriptors = selectedIssues.Select(i => i.GetCustomProperty(0)).Distinct().ToArray();
#pragma warning restore RS0030
                if (selectedDescriptors.Length > 1)
                {
                    GUILayout.TextArea(k_MultipleSelectionText, SharedStyles.TextAreaWithDynamicSize, GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
                    return;
                }

                GUILayout.TextArea(selectedIssues[0].Description, SharedStyles.TextAreaWithDynamicSize,
                    GUILayout.MaxHeight(LayoutSize.FoldoutMaxHeight));
            }
        }

        public override void DrawViewOptions()
        {
            base.DrawViewOptions();

            EditorGUI.BeginChangeCheck();
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
