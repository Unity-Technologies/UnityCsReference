// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal class CodeDiagnosticView : DiagnosticView
    {
        int m_NumCompilerErrors = 0;

        public CodeDiagnosticView(ViewManager viewManager) : base(viewManager)
        {
        }

        public override void AddIssues(IEnumerable<ReportItem> allIssues)
        {
            base.AddIssues(allIssues);

            if (m_Desc.Category == IssueCategory.Code)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var compilerMessages = allIssues.Where(i => i.Category == IssueCategory.CodeCompilerMessage);
                m_NumCompilerErrors += compilerMessages.Count(i => i.Severity == Severity.Error);
#pragma warning restore RS0030
            }
        }

        public override void Clear()
        {
            base.Clear();

            m_NumCompilerErrors = 0;
        }

        protected override void DrawInfo()
        {
            base.DrawInfo();

            if (m_Desc.Category == IssueCategory.Code && m_NumCompilerErrors > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Utility.GetIcon(Utility.IconType.Error), GUILayout.MaxWidth(36));
                EditorGUILayout.LabelField(new GUIContent("Code Analysis is incomplete due to compilation errors"), GUILayout.Width(330), GUILayout.ExpandWidth(false));
                if (GUILayout.Button("View", EditorStyles.miniButton, GUILayout.Width(50)))
                    m_ViewManager.ChangeView(IssueCategory.CodeCompilerMessage);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
