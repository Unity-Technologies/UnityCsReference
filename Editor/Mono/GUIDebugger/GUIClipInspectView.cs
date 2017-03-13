// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class GUIClipInspectView : BaseInspectView
    {
        List<IMGUIClipInstruction> m_ClipList = new List<IMGUIClipInstruction>();
        private IMGUIClipInstruction m_Instruction;
        private Vector2 m_StacktraceScrollPos =  new Vector2();


        public GUIClipInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
            //TODO: find a better approach instead of getting the whole list everyframe.
            m_ClipList.Clear();
            GUIViewDebuggerHelper.GetClipInstructions(m_ClipList);
        }

        protected override int GetInstructionCount()
        {
            return m_ClipList.Count;
        }

        protected override void DoDrawInstruction(ListViewElement el, int id)
        {
            var clipInstruction = m_ClipList[el.row];
            var niceName = GetInstructionListName(el.row);

            GUIContent tempContent = GUIContent.Temp(niceName);

            var rect = el.position;
            rect.xMin += clipInstruction.level * 12;

            GUIViewDebuggerWindow.s_Styles.listItemBackground.Draw(el.position, false, false, m_ListViewState.row == el.row, false);
            GUIViewDebuggerWindow.s_Styles.listItem.Draw(rect, tempContent, id, m_ListViewState.row == el.row);
        }

        internal override void OnDoubleClickInstruction(int index)
        {
            throw new NotImplementedException();
        }

        protected override void DrawInspectedStacktrace()
        {
            var clipInstruction = m_ClipList[m_ListViewState.row];
            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.s_Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(clipInstruction.pushStacktrace);
            EditorGUILayout.EndScrollView();
        }

        internal override void DoDrawSelectedInstructionDetails(int index)
        {
            var clipInstruction = m_ClipList[index];

            GUILayout.Label("RenderOffset:");
            GUILayout.Label(clipInstruction.renderOffset.ToString());
            GUILayout.Label("ResetOffset:");
            GUILayout.Label(clipInstruction.resetOffset.ToString());
            GUILayout.Label("screenRect:");
            GUILayout.Label(clipInstruction.screenRect.ToString());
            GUILayout.Label("scrollOffset:");
            GUILayout.Label(clipInstruction.scrollOffset.ToString());
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            m_ListViewState.row = index;
            ShowOverlay();
        }

        public override void ShowOverlay()
        {
            if (!HasSelectedinstruction())
                return;

            var clipInstruction = m_ClipList[m_ListViewState.row];
            m_GuiViewDebuggerWindow.HighlightInstruction(m_GuiViewDebuggerWindow.m_Inspected, clipInstruction.unclippedScreenRect, GUIStyle.none);
        }

        internal override string GetInstructionListName(int index)
        {
            var clipInstruction = m_ClipList[index];
            var stacktrace = clipInstruction.pushStacktrace;

            if (stacktrace.Length == 0)
                return "Empty";

            int frameIndex = GetInterestingFrameIndex(stacktrace);

            StackFrame interestingFrame = stacktrace[frameIndex];
            string methodName = interestingFrame.methodName;
            return methodName;
        }

        private int GetInterestingFrameIndex(StackFrame[] stacktrace)
        {
            string currentProjectPath = Application.dataPath;

            int index = -1;

            for (int i = 0; i < stacktrace.Length; ++i)
            {
                StackFrame sf = stacktrace[i];
                if (string.IsNullOrEmpty(sf.sourceFile))
                    continue;
                if (sf.signature.StartsWith("UnityEngine.GUIClip"))
                    continue;

                if (index == -1)
                    index = i;

                if (sf.sourceFile.StartsWith(currentProjectPath))
                    return i;
            }

            if (index != -1)
                return index;

            return stacktrace.Length - 1;
        }
    }
}
