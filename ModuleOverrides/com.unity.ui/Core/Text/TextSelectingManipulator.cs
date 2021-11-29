// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    internal class TextSelectingManipulator
    {
        internal TextSelectingUtilities m_SelectingUtilities;

        bool selectAllOnMouseUp;
        TextElement m_TextElement;
        Vector2 m_ClickStartPosition;
        bool m_Dragged;
        bool m_IsClicking;

        const int k_DragThresholdSqr = 16;

        private bool isClicking
        {
            get => m_IsClicking;
            set
            {
                if (m_IsClicking == value) return;
                m_IsClicking = value;
                if (m_IsClicking)
                    m_TextElement.CaptureMouse();
                else
                    m_TextElement.ReleaseMouse();
            }
        }

        public TextSelectingManipulator(TextElement textElement)
        {
            m_TextElement = textElement;
            m_SelectingUtilities = new TextSelectingUtilities(m_TextElement.uitkTextHandle.textHandle);

            m_SelectingUtilities.OnCursorIndexChange += OnCursorIndexChange;
            m_SelectingUtilities.OnSelectIndexChange += OnSelectIndexChange;

            SyncSelectingUtilitiesWithTextElement();
        }

        internal int cursorIndex
        {
            get => m_SelectingUtilities?.cursorIndex ?? -1;
            set => m_SelectingUtilities.cursorIndex = value;
        }

        internal int selectIndex
        {
            get => m_SelectingUtilities?.selectIndex ?? -1;
            set => m_SelectingUtilities.selectIndex = value;
        }

        private void SyncSelectingUtilitiesWithTextElement()
        {
            m_SelectingUtilities.multiline = m_TextElement.edition.multiline;
            m_SelectingUtilities.text = m_TextElement.text;
        }

        void OnSelectIndexChange()
        {
            m_TextElement.IncrementVersion(VersionChangeType.Repaint);
            m_TextElement.edition.UpdateScrollOffset?.Invoke();
        }

        void OnCursorIndexChange()
        {
            m_TextElement.IncrementVersion(VersionChangeType.Repaint);
            m_TextElement.edition.UpdateScrollOffset?.Invoke();
        }

        internal bool RevealCursor()
        {
            return m_SelectingUtilities.revealCursor;
        }

        internal bool HasSelection()
        {
            return m_SelectingUtilities.hasSelection;
        }

        internal void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            switch (evt)
            {
                case FocusEvent fe:
                    OnFocusEvent(fe);
                    break;
                case BlurEvent be:
                    OnBlurEvent(be);
                    break;
                // TODO change to pointerup
                case MouseDownEvent mde:
                    OnMouseDownEvent(mde);
                    break;
                case MouseMoveEvent mme:
                    OnMouseMoveEvent(mme);
                    break;
                case MouseUpEvent mue:
                    OnMouseUpEvent(mue);
                    break;
                case ValidateCommandEvent vce:
                    OnValidateCommandEvent(vce);
                    break;
                case ExecuteCommandEvent ece:
                    OnExecuteCommandEvent(ece);
                    break;
            }
        }

        void OnFocusEvent(FocusEvent evt)
        {
            selectAllOnMouseUp = false;

            // If focus was given to this element from a mouse click or a Panel.Focus call, allow select on mouse up.
            if (PointerDeviceState.GetPressedButtons(PointerId.mousePointerId) != 0 ||
                m_TextElement.panel.contextType == ContextType.Editor && Event.current == null)
                selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp;

            m_SelectingUtilities.OnFocus(m_TextElement.selection.selectAllOnFocus);
        }

        void OnBlurEvent(BlurEvent evt)
        {
            m_SelectingUtilities.OnLostFocus();
            selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp;
        }

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (evt.clickCount == 2 && m_TextElement.selection.doubleClickSelectsWord)
                    m_SelectingUtilities.SelectCurrentWord();
                else if (evt.clickCount == 3 && m_TextElement.selection.tripleClickSelectsLine)
                    m_SelectingUtilities.SelectCurrentParagraph();
                else
                    m_SelectingUtilities.MoveCursorToPosition_Internal(evt.localMousePosition, evt.shiftKey);

                isClicking = true;
                m_ClickStartPosition = evt.localMousePosition;
                evt.StopPropagation();

                // Scroll offset might need to be updated
                m_TextElement.edition.UpdateScrollOffset?.Invoke();
            }
        }

        void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !isClicking)
                return;

            m_Dragged = m_Dragged || MoveDistanceQualifiesForDrag(m_ClickStartPosition, evt.localMousePosition);
            if (m_Dragged)
            {
                m_SelectingUtilities.SelectToPosition(evt.localMousePosition);
                // Scroll offset might need to be updated
                m_TextElement.edition.UpdateScrollOffset?.Invoke();

                selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp && !m_SelectingUtilities.hasSelection;
            }

            evt.StopPropagation();
        }

        void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !isClicking)
                return;

            if (selectAllOnMouseUp)
            {
                m_SelectingUtilities.SelectAll();

                // Scroll offset might need to be updated
                m_TextElement.edition.UpdateScrollOffset?.Invoke();
            }

            selectAllOnMouseUp = false;
            m_Dragged = false;
            isClicking = false;
            evt.StopPropagation();
        }

        void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (!m_TextElement.edition.hasFocus)
                return;

            switch (evt.commandName)
            {
                case EventCommandNames.Cut:
                case EventCommandNames.Paste:
                case EventCommandNames.Delete:
                case EventCommandNames.UndoRedoPerformed:
                    return;
                case EventCommandNames.Copy:
                    if (!m_SelectingUtilities.hasSelection)
                        return;
                    break;
                case EventCommandNames.SelectAll:
                    break;
            }
            evt.StopPropagation();
        }

        void OnExecuteCommandEvent(ExecuteCommandEvent evt)
        {
            if (!m_TextElement.edition.hasFocus)
                return;

            switch (evt.commandName)
            {
                case EventCommandNames.OnLostFocus:
                    evt.StopPropagation();
                    return;
                case EventCommandNames.Copy:
                    m_SelectingUtilities.Copy();
                    evt.StopPropagation();
                    return;
                case EventCommandNames.SelectAll:
                    m_SelectingUtilities.SelectAll();
                    evt.StopPropagation();
                    return;
            }
        }


        private bool MoveDistanceQualifiesForDrag(Vector2 start, Vector2 current)
        {
            return (start - current).sqrMagnitude >= k_DragThresholdSqr;
        }
    }
}
