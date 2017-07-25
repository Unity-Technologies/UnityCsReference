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
        Vector2 m_StacktraceScrollPos =  new Vector2();

        List<IMGUIClipInstruction> m_ClipList = new List<IMGUIClipInstruction>();

        public GUIClipInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
            //TODO: find a better approach instead of getting the whole list everyframe.
            m_ClipList.Clear();
            GUIViewDebuggerHelper.GetClipInstructions(m_ClipList);
        }

        public override void ShowOverlay()
        {
            if (!isInstructionSelected)
            {
                debuggerWindow.ClearInstructionHighlighter();
                return;
            }

            var clipInstruction = m_ClipList[listViewState.row];
            debuggerWindow.HighlightInstruction(debuggerWindow.inspected, clipInstruction.unclippedScreenRect, GUIStyle.none);
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

            GUIViewDebuggerWindow.Styles.listItemBackground.Draw(el.position, false, false, listViewState.row == el.row, false);
            GUIViewDebuggerWindow.Styles.listItem.Draw(rect, tempContent, id, listViewState.row == el.row);
        }

        protected override void DrawInspectedStacktrace()
        {
            var clipInstruction = m_ClipList[listViewState.row];
            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(clipInstruction.pushStacktrace);
            EditorGUILayout.EndScrollView();
        }

        internal override void DoDrawSelectedInstructionDetails(int selectedInstructionIndex)
        {
            var clipInstruction = m_ClipList[selectedInstructionIndex];

            DoSelectableInstructionDataField("RenderOffset", clipInstruction.renderOffset.ToString());
            DoSelectableInstructionDataField("ResetOffset", clipInstruction.resetOffset.ToString());
            DoSelectableInstructionDataField("screenRect", clipInstruction.screenRect.ToString());
            DoSelectableInstructionDataField("scrollOffset", clipInstruction.scrollOffset.ToString());
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

        internal override void OnDoubleClickInstruction(int index)
        {
            throw new NotImplementedException();
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            listViewState.row = index;
            ShowOverlay();
        }

        int GetInterestingFrameIndex(StackFrame[] stacktrace)
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
