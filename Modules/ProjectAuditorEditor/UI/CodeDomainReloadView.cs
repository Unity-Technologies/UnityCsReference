// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class CodeDomainReloadView : CodeDiagnosticView
    {
        const string k_RoslynDisabled = @"The UseRoslynAnalyzers option is disabled. This is required to see results from the Domain Reload Analyzer.

To enable Roslyn diagnostics reporting, make sure the corresponding option is enabled in Preferences > Analysis > " + ProjectAuditor.DisplayName + @" > Use Roslyn Analyzers.
To open the Preferences window, go to Edit > Preferences (macOS: Unity > Settings) in the main menu.";

        public CodeDomainReloadView(ViewManager viewManager) : base(viewManager)
        {
        }

        protected override void DrawInfo()
        {
            if (!m_ViewManager.Report.SessionInfo.UseRoslynAnalyzers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(k_RoslynDisabled, MessageType.Warning);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                base.DrawInfo();
            }
        }

        public override void DrawFilters()
        {
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Show :", GUILayout.ExpandWidth(true), GUILayout.Width(80));

                var guiContent = m_Table.showIgnoredIssues
                    ? Contents.ShowIgnoredIssuesButton
                    : Contents.HideIgnoredIssuesButton;

                var wasShowingIgnored = m_Table.showIgnoredIssues;
                m_Table.showIgnoredIssues = EditorGUILayout.ToggleLeft("Show Ignored Issues",
                    m_Table.showIgnoredIssues, GUILayout.Width(170));

                if (wasShowingIgnored != m_Table.showIgnoredIssues)
                {
                    m_ViewManager.OnIgnoredIssuesVisibilityChanged?.Invoke(m_Table.showIgnoredIssues);
                    MarkDirty();
                }
            }

            if (EditorGUI.EndChangeCheck())
                MarkDirty();
        }
    }
}
