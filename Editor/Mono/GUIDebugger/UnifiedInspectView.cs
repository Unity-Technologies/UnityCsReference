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
        private readonly List<IMGUIInstruction> m_Instructions = new List<IMGUIInstruction>();

        private Vector2 m_StacktraceScrollPos =  new Vector2();

        private BaseInspectView m_InstructionClipView;
        private BaseInspectView m_InstructionStyleView;
        private BaseInspectView m_InstructionLayoutView;

        public UnifiedInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
            m_InstructionClipView = new GUIClipInspectView(guiViewDebuggerWindow);
            m_InstructionStyleView = new StyleDrawInspectView(guiViewDebuggerWindow);
            m_InstructionLayoutView = new GUILayoutInspectView(guiViewDebuggerWindow);
        }

        protected BaseInspectView GetInspectViewForType(InstructionType type)
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

                default:
                    throw new NotImplementedException("Unhandled InstructionType");
            }
        }

        public override void UpdateInstructions()
        {
            m_InstructionClipView.UpdateInstructions();
            m_InstructionStyleView.UpdateInstructions();
            m_InstructionLayoutView.UpdateInstructions();

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

            GUIViewDebuggerWindow.s_Styles.listItemBackground.Draw(rect, false, false, m_ListViewState.row == el.row, false);

            GUIViewDebuggerWindow.s_Styles.listItem.Draw(rect, tempContent, controlId, m_ListViewState.row == el.row);
        }

        internal override string GetInstructionListName(int index)
        {
            IMGUIInstruction instruction = m_Instructions[index];

            //string listDisplayName = instruction.type + " #" + instruction.typeInstructionIndex;
            //return listDisplayName;


            var viewForType = GetInspectViewForType(instruction.type);
            return viewForType.GetInstructionListName(instruction.typeInstructionIndex);
        }

        internal override void OnDoubleClickInstruction(int index)
        {
            IMGUIInstruction instruction = m_Instructions[index];

            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.OnDoubleClickInstruction(instruction.typeInstructionIndex);
        }

        protected override void DrawInspectedStacktrace()
        {
            IMGUIInstruction instruction = m_Instructions[m_ListViewState.row];

            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.s_Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(instruction.stack);
            EditorGUILayout.EndScrollView();
        }

        internal override void DoDrawSelectedInstructionDetails(int index)
        {
            IMGUIInstruction instruction = m_Instructions[index];

            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.DoDrawSelectedInstructionDetails(instruction.typeInstructionIndex);
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            m_ListViewState.row = index;

            IMGUIInstruction instruction = m_Instructions[m_ListViewState.row];

            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.OnSelectedInstructionChanged(instruction.typeInstructionIndex);

            ShowOverlay();
        }

        public override void ShowOverlay()
        {
            if (!HasSelectedinstruction())
                return;

            IMGUIInstruction instruction = m_Instructions[m_ListViewState.row];
            var viewForType = GetInspectViewForType(instruction.type);
            viewForType.ShowOverlay();
        }
    }
}
