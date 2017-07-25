// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class GUILayoutInspectView : BaseInspectView
    {
        Vector2 m_StacktraceScrollPos = new Vector2();

        readonly List<IMGUILayoutInstruction> m_LayoutInstructions = new List<IMGUILayoutInstruction>();

        GUIStyle m_FakeMarginStyleForOverlay = new GUIStyle();

        public GUILayoutInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
            m_LayoutInstructions.Clear();
            GUIViewDebuggerHelper.GetLayoutInstructions(m_LayoutInstructions);
        }

        public override void ShowOverlay()
        {
            if (!isInstructionSelected)
            {
                debuggerWindow.ClearInstructionHighlighter();
                return;
            }

            IMGUILayoutInstruction instruction = m_LayoutInstructions[listViewState.row];

            RectOffset offset = new RectOffset();
            offset.left = instruction.marginLeft;
            offset.right = instruction.marginRight;
            offset.top = instruction.marginTop;
            offset.bottom = instruction.marginBottom;

            //TODO: right now the overlay only know about padding
            //For now we just save margin into padding
            //while the overlay isn't improved.
            m_FakeMarginStyleForOverlay.padding = offset;

            Rect rect = instruction.unclippedRect;

            rect = offset.Add(rect);

            debuggerWindow.HighlightInstruction(debuggerWindow.inspected, rect, m_FakeMarginStyleForOverlay);
        }

        protected override int GetInstructionCount()
        {
            return m_LayoutInstructions.Count;
        }

        protected override void DoDrawInstruction(ListViewElement el, int id)
        {
            IMGUILayoutInstruction instruction = m_LayoutInstructions[el.row];

            GUIContent tempContent = GUIContent.Temp(GetInstructionListName(el.row));

            var rect = el.position;
            rect.xMin += instruction.level * 10;

            GUIViewDebuggerWindow.Styles.listItemBackground.Draw(rect, false, false, listViewState.row == el.row, false);
            GUIViewDebuggerWindow.Styles.listItem.Draw(rect, tempContent, id, listViewState.row == el.row);
        }

        protected override void DrawInspectedStacktrace()
        {
            IMGUILayoutInstruction instruction = m_LayoutInstructions[listViewState.row];

            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(instruction.stack);
            EditorGUILayout.EndScrollView();
        }

        internal override void DoDrawSelectedInstructionDetails(int selectedInstructionIndex)
        {
            IMGUILayoutInstruction instruction = m_LayoutInstructions[selectedInstructionIndex];

            using (new EditorGUI.DisabledScope(true))
                DrawInspectedRect(instruction.unclippedRect);

            DoSelectableInstructionDataField("margin.left", instruction.marginLeft.ToString());
            DoSelectableInstructionDataField("margin.top", instruction.marginTop.ToString());
            DoSelectableInstructionDataField("margin.right", instruction.marginRight.ToString());
            DoSelectableInstructionDataField("margin.bottom", instruction.marginBottom.ToString());

            if (instruction.style != null)
                DoSelectableInstructionDataField("Style Name", instruction.style.name);

            if (instruction.isGroup == 1)
                return;

            DoSelectableInstructionDataField("IsVertical", (instruction.isVertical == 1).ToString());
        }

        internal override string GetInstructionListName(int index)
        {
            IMGUILayoutInstruction instruction = m_LayoutInstructions[index];
            var stacktrace = instruction.stack;

            int frameIndex = GetInterestingFrameIndex(stacktrace);

            if (frameIndex > 0)
                --frameIndex;

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
            //We try to find the first frame that belongs to the user project.
            //If there is no frame inside the user project, we will return the first frame outside any class starting with:
            // - UnityEngine.GUILayoutUtility
            string currentProjectPath = Application.dataPath;

            int index = -1;

            for (int i = 0; i < stacktrace.Length; ++i)
            {
                StackFrame sf = stacktrace[i];
                if (string.IsNullOrEmpty(sf.sourceFile))
                    continue;
                if (sf.signature.StartsWith("UnityEngine.GUIDebugger"))
                    continue;
                if (sf.signature.StartsWith("UnityEngine.GUILayoutUtility"))
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
