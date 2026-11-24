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
                #pragma warning disable UAC2001 // Avoid Linq
                var compilerMessages = allIssues.Where(i => i.Category == IssueCategory.CodeCompilerMessage);
                m_NumCompilerErrors += compilerMessages.Count(i => i.Severity == Severity.Error);
#pragma warning restore UAC2001
            }
        }

        public override void Clear()
        {
            base.Clear();

            m_NumCompilerErrors = 0;
        }

        public override void DrawDescriptionNotice()
        {
            if (m_Desc.Category == IssueCategory.Code && m_NumCompilerErrors > 0)
            {
                EditorGUILayout.Space();
                Utility.DrawHelpBoxWithButton("Code Analysis is incomplete due to compilation errors", Utility.TempContent("View"), MessageType.Error,
                    () => m_Window.GotoCategory(IssueCategory.CodeCompilerMessage));
            }
        }
    }
}
