// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    class GUINamedControlInspectView : BaseInspectView
    {
        private readonly List<IMGUINamedControlInstruction> m_NamedControlInstructions = new List<IMGUINamedControlInstruction>();

        private GUIStyle m_FakeMargingStyleForOverlay = new GUIStyle();

        public GUINamedControlInspectView(GUIViewDebuggerWindow guiViewDebuggerWindow) : base(guiViewDebuggerWindow)
        {
        }

        public override void UpdateInstructions()
        {
            m_NamedControlInstructions.Clear();
            GUIViewDebuggerHelper.GetNamedControlInstructions(m_NamedControlInstructions);
        }

        protected override int GetInstructionCount()
        {
            return m_NamedControlInstructions.Count;
        }

        protected override void DoDrawInstruction(ListViewElement el, int id)
        {
            GUIContent tempContent = GUIContent.Temp(GetInstructionListName(el.row));

            var rect = el.position;

            GUIViewDebuggerWindow.Styles.listItemBackground.Draw(rect, false, false, listViewState.row == el.row, false);
            GUIViewDebuggerWindow.Styles.listItem.Draw(rect, tempContent, id, listViewState.row == el.row);
        }

        internal override string GetInstructionListName(int index)
        {
            IMGUINamedControlInstruction instruction = m_NamedControlInstructions[index];
            return "\"" + instruction.name + "\"";
        }

        internal override void OnDoubleClickInstruction(int index)
        {
        }

        protected override void DrawInspectedStacktrace()
        {
        }

        internal override void DoDrawSelectedInstructionDetails(int index)
        {
            var instruction = m_NamedControlInstructions[listViewState.row];

            using (new EditorGUI.DisabledScope(true))
                DrawInspectedRect(instruction.rect);
            DoSelectableInstructionDataField("Name", instruction.name);
            DoSelectableInstructionDataField("ID", instruction.id.ToString());
        }

        internal override void OnSelectedInstructionChanged(int index)
        {
            listViewState.row = index;
            ShowOverlay();
        }

        public override void ShowOverlay()
        {
            if (!isInstructionSelected)
            {
                debuggerWindow.ClearInstructionHighlighter();
                return;
            }

            var instruction = m_NamedControlInstructions[listViewState.row];

            debuggerWindow.HighlightInstruction(debuggerWindow.inspected, instruction.rect, m_FakeMargingStyleForOverlay);
        }
    }
}
