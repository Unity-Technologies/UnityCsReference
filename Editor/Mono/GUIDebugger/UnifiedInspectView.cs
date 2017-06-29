// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    class UnifiedInspectView : BaseInspectView
    {
        Vector2 m_StacktraceScrollPos =  new Vector2();

        readonly List<IMGUIInstruction> m_Instructions = new List<IMGUIInstruction>();
        BaseInspectView m_InstructionClipView;
        BaseInspectView m_InstructionStyleView;
        BaseInspectView m_InstructionPropertyView;
        BaseInspectView m_InstructionLayoutView;
        private BaseInspectView m_InstructionNamedControlView;

        public UnifiedInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
            m_InstructionClipView = new GUIClipInspectView(guiViewDebuggerWindow);
            m_InstructionStyleView = new StyleDrawInspectView(guiViewDebuggerWindow);
            m_InstructionLayoutView = new GUILayoutInspectView(guiViewDebuggerWindow);
            m_InstructionPropertyView = new GUIPropertyInspectView(guiViewDebuggerWindow);
            m_InstructionNamedControlView = new GUINamedControlInspectView(guiViewDebuggerWindow);
        }

        public override void UpdateInstructions()
        {
            m_InstructionClipView.UpdateInstructions();
            m_InstructionStyleView.UpdateInstructions();
            m_InstructionLayoutView.UpdateInstructions();
            m_InstructionPropertyView.UpdateInstructions();
            m_InstructionNamedControlView.UpdateInstructions();

            /*
            This is an expensive operation, it will resolve the callstacks for all instructions.
            Currently we marshall all instructions for every GUI Event.
            This works okay for windows that doesn't repaint automatically.
            We can optmize by only marshalling the visible instructions and caching it.
            But I don't really think its needed right now.
            */
            m_Instructions.Clear();
            GUIViewDebuggerHelper.GetUnifiedInstructions(m_Instructions);
        }

        public override void ShowOverlay()
        {
            if (!isInstructionSelected)
            {
                debuggerWindow.ClearInstructionHighlighter();
                return;
            }

            IMGUIInstruction instruction = m_Instructions[listViewState.row];
            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.ShowOverlay();
        }

        protected override int GetInstructionCount()
        {
            return m_Instructions.Count;
        }

        protected override void DoDrawInstruction(ListViewElement el, int controlId)
        {
            IMGUIInstruction instruction = m_Instructions[el.row];

            string listDisplayName = GetInstructionListName(el.row);
            GUIContent tempContent = GUIContent.Temp(listDisplayName);

            var rect = el.position;
            rect.xMin += instruction.level * 10;

            GUIViewDebuggerWindow.Styles.listItemBackground.Draw(rect, false, false, listViewState.row == el.row, false);

            GUIViewDebuggerWindow.Styles.listItem.Draw(rect, tempContent, controlId, listViewState.row == el.row);
        }

        protected override void DrawInspectedStacktrace()
        {
            IMGUIInstruction instruction = m_Instructions[listViewState.row];

            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(instruction.stack);
            EditorGUILayout.EndScrollView();
        }

        internal override void DoDrawSelectedInstructionDetails(int selectedInstructionIndex)
        {
            IMGUIInstruction instruction = m_Instructions[selectedInstructionIndex];

            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.DoDrawSelectedInstructionDetails(instruction.typeInstructionIndex);
        }

        internal override string GetInstructionListName(int index)
        {
            IMGUIInstruction instruction = m_Instructions[index];

            //string listDisplayName = instruction.type + " #" + instruction.typeInstructionIndex;
            //return listDisplayName;

            var viewForType = GetInspectViewForType(instruction.type);
            return viewForType.GetInstructionListName(instruction.typeInstructionIndex);
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            listViewState.row = index;

            if (listViewState.row >= -0)
            {
                IMGUIInstruction instruction = m_Instructions[listViewState.row];

                var viewForType = GetInspectViewForType(instruction.type);
                viewForType.OnSelectedInstructionChanged(instruction.typeInstructionIndex);

                ShowOverlay();
            }
            else
            {
                debuggerWindow.ClearInstructionHighlighter();
            }
        }

        internal override void OnDoubleClickInstruction(int index)
        {
            IMGUIInstruction instruction = m_Instructions[index];

            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.OnDoubleClickInstruction(instruction.typeInstructionIndex);
        }

        BaseInspectView GetInspectViewForType(InstructionType type)
        {
            switch (type)
            {
                case InstructionType.kStyleDraw:
                    return m_InstructionStyleView;

                case InstructionType.kClipPop:
                case InstructionType.kClipPush:
                    return m_InstructionClipView;

                case InstructionType.kLayoutBeginGroup:
                case InstructionType.kLayoutEndGroup:
                case InstructionType.kLayoutEntry:
                    return m_InstructionLayoutView;
                case InstructionType.kPropertyBegin:
                case InstructionType.kPropertyEnd:
                    return m_InstructionPropertyView;
                case InstructionType.kLayoutNamedControl:
                    return m_InstructionNamedControlView;
                default:
                    throw new NotImplementedException("Unhandled InstructionType");
            }
        }
    }
}
