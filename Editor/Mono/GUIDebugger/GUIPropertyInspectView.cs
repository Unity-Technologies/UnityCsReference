// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class GUIPropertyInspectView : BaseInspectView
    {
        Vector2 m_StacktraceScrollPos =  new Vector2();
        private GUIStyle m_FakeMargingStyleForOverlay = new GUIStyle();

        List<IMGUIPropertyInstruction> m_PropertyList = new List<IMGUIPropertyInstruction>();

        public GUIPropertyInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
            //TODO: find a better approach instead of getting the whole list everyframe.
            m_PropertyList.Clear();
            GUIViewDebuggerHelper.GetPropertyInstructions(m_PropertyList);
        }

        public override void ShowOverlay()
        {
            if (!isInstructionSelected)
            {
                debuggerWindow.ClearInstructionHighlighter();
                return;
            }

            var property = m_PropertyList[listViewState.row];
            debuggerWindow.HighlightInstruction(debuggerWindow.inspected, property.rect, m_FakeMargingStyleForOverlay);
        }

        protected override int GetInstructionCount()
        {
            return m_PropertyList.Count;
        }

        protected override void DoDrawInstruction(ListViewElement el, int id)
        {
            GUIContent tempContent = GUIContent.Temp(GetInstructionListName(el.row));

            var rect = el.position;

            GUIViewDebuggerWindow.Styles.listItemBackground.Draw(rect, false, false, listViewState.row == el.row, false);
            GUIViewDebuggerWindow.Styles.listItem.Draw(rect, tempContent, id, listViewState.row == el.row);
        }

        protected override void DrawInspectedStacktrace()
        {
            var clipInstruction = m_PropertyList[listViewState.row];
            m_StacktraceScrollPos = EditorGUILayout.BeginScrollView(m_StacktraceScrollPos, GUIViewDebuggerWindow.Styles.stacktraceBackground, GUILayout.ExpandHeight(false));
            DrawStackFrameList(clipInstruction.beginStacktrace);
            EditorGUILayout.EndScrollView();
        }

        internal override void DoDrawSelectedInstructionDetails(int selectedInstructionIndex)
        {
            var property = m_PropertyList[listViewState.row];

            using (new EditorGUI.DisabledScope(true))
                DrawInspectedRect(property.rect);

            DoSelectableInstructionDataField("Target Type Name", property.targetTypeName);
            DoSelectableInstructionDataField("Path", property.path);
        }

        internal override string GetInstructionListName(int index)
        {
            var clipInstruction = m_PropertyList[index];
            return clipInstruction.path;
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
    }
}
