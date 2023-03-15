// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.CodeEditor;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    class CodeLine : Label
    {
        string m_FileName;
        int m_LineNumber;

        public int hashCode { get; private set; }

        public void Init(string textName, string fileName, int lineNumber, int lineHashCode)
        {
            text = textName;
            m_FileName = fileName;
            m_LineNumber = lineNumber;
            this.hashCode = lineHashCode;
        }

        [EventInterest(typeof(ContextualMenuPopulateEvent),
            typeof(PointerDownEvent), typeof(PointerUpEvent), typeof(PointerMoveEvent), typeof(KeyUpEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (elementPanel != null && elementPanel.contextualMenuManager != null)
            {
                elementPanel.contextualMenuManager.DisplayMenuIfEventMatches(evt, this);
            }

            if (evt.eventTypeId == ContextualMenuPopulateEvent.TypeId())
            {
                ContextualMenuPopulateEvent e = evt as ContextualMenuPopulateEvent;
                e.menu.AppendAction("Go to callback registration point", (e) => GotoCode(), DropdownMenuAction.AlwaysEnabled);
            }
        }

        public void GotoCode()
        {
            CodeEditor.Editor.CurrentCodeEditor.OpenProject(m_FileName, m_LineNumber);
        }

        public override string ToString()
        {
            return $"{m_FileName} ({m_LineNumber})";
        }
    }
}
