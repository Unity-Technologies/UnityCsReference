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

        private int m_ConsecutiveMouseDownCount;
        private long m_LastMouseDownTimeStamp = 0;
        internal bool isClicking
        {
            get => m_IsClicking;
            private set
            {
                if (m_IsClicking == value) return;
                m_IsClicking = value;
            }
        }

        public TextSelectingManipulator(TextElement textElement)
        {
            m_TextElement = textElement;
            m_SelectingUtilities = new TextSelectingUtilities(m_TextElement.uitkTextHandle);

            m_SelectingUtilities.OnCursorIndexChange += OnCursorIndexChange;
            m_SelectingUtilities.OnSelectIndexChange += OnSelectIndexChange;
            m_SelectingUtilities.OnRevealCursorChange += OnRevealCursor;
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

        void OnRevealCursor()
        {
            m_TextElement.IncrementVersion(VersionChangeType.Repaint);
        }

        void OnSelectIndexChange()
        {
            m_TextElement.IncrementVersion(VersionChangeType.Repaint);

            if(HasSelection() && m_TextElement.focusController != null)
                m_TextElement.focusController.selectedTextElement = m_TextElement;

            if(m_SelectingUtilities.revealCursor)
                m_TextElement.edition.UpdateScrollOffset?.Invoke(false);
        }

        void OnCursorIndexChange()
        {
            m_TextElement.IncrementVersion(VersionChangeType.Repaint);

            if(HasSelection() && m_TextElement.focusController != null)
                m_TextElement.focusController.selectedTextElement = m_TextElement;

            if(m_SelectingUtilities.revealCursor)
                m_TextElement.edition.UpdateScrollOffset?.Invoke(false);
        }

        internal bool RevealCursor()
        {
            return m_SelectingUtilities.revealCursor;
        }

        internal bool HasSelection()
        {
            return m_SelectingUtilities.hasSelection;
        }

        internal bool HasFocus()
        {
            return m_TextElement.hasFocus;
        }

        internal void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is BlurEvent)
            {
                m_TextElement.uitkTextHandle.RemoveTextInfoFromCache();
            }
            else if ((evt is not PointerMoveEvent && evt is not MouseMoveEvent) || isClicking)
            {
                m_TextElement.uitkTextHandle.AddTextInfoToCache();
            }

            switch (evt)
            {
                case FocusEvent:
                    OnFocusEvent();
                    break;
                case BlurEvent:
                    OnBlurEvent();
                    break;
                case ValidateCommandEvent vce:
                    OnValidateCommandEvent(vce);
                    break;
                case ExecuteCommandEvent ece:
                    OnExecuteCommandEvent(ece);
                    break;
                case KeyDownEvent kde:
                    OnKeyDown(kde);
                    break;
                case PointerDownEvent pde:
                    OnPointerDownEvent(pde);
                    break;
                case PointerMoveEvent pme:
                    OnPointerMoveEvent(pme);
                    break;
                case PointerUpEvent pue:
                    OnPointerUpEvent(pue);
                    break;
            }
        }

        void OnFocusEvent()
        {
            selectAllOnMouseUp = false;

            // If focus was given to this element from a mouse click or a Panel.Focus call, allow select on mouse up.
            if (PointerDeviceState.GetPressedButtons(PointerId.mousePointerId) != 0 ||
                m_TextElement.panel.contextType == ContextType.Editor && Event.current == null)
                selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp;

            m_SelectingUtilities.OnFocus(m_TextElement.selection.selectAllOnFocus && !isClicking);
        }

        void OnBlurEvent()
        {
            selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp;
        }

        readonly Event m_ImguiEvent = new Event();
        void OnKeyDown(KeyDownEvent evt)
        {
            if (!m_TextElement.hasFocus)
                return;

            evt.GetEquivalentImguiEvent(m_ImguiEvent);
            if (m_SelectingUtilities.HandleKeyEvent(m_ImguiEvent))
                evt.StopPropagation();
        }

        //Changed to not rely on evt.clickCount to fix https://fogbugz.unity3d.com/f/cases/1409098/
        //This is necessary because default event system doesn't support triple click
        void OnPointerDownEvent(PointerDownEvent evt)
        {
            var pointerPosition = evt.localPosition - (Vector3)m_TextElement.contentRect.min;
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                //only move cursor to position if it wasn't a double or triple click.
                if (evt.timestamp - m_LastMouseDownTimeStamp < Event.GetDoubleClickTime())
                    m_ConsecutiveMouseDownCount++;
                else
                    m_ConsecutiveMouseDownCount = 1;

                if (m_ConsecutiveMouseDownCount == 2 && m_TextElement.selection.doubleClickSelectsWord)
                {
                    // We need to assign the correct cursor and select index to the current cursor position
                    // prior to selecting the current word. Because selectAllOnMouseUp is true, it'll always
                    // use a cursorIndex of 0.
                    if (cursorIndex == 0 && cursorIndex != selectIndex)
                        m_SelectingUtilities.MoveCursorToPosition_Internal(pointerPosition, evt.shiftKey);

                    m_SelectingUtilities.SelectCurrentWord();
                }
                else if (m_ConsecutiveMouseDownCount == 3 && m_TextElement.selection.tripleClickSelectsLine)
                    m_SelectingUtilities.SelectCurrentParagraph();
                else
                {
                    m_SelectingUtilities.MoveCursorToPosition_Internal(pointerPosition, evt.shiftKey);
                    m_TextElement.edition.UpdateScrollOffset?.Invoke(false);
                }

                m_LastMouseDownTimeStamp = evt.timestamp;
                isClicking = true;
                m_TextElement.CapturePointer(evt.pointerId);
                m_ClickStartPosition = pointerPosition;
                evt.StopPropagation();
            }
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (!isClicking)
                return;

            var pointerPosition = evt.localPosition - (Vector3)m_TextElement.contentRect.min;
            m_Dragged = m_Dragged || MoveDistanceQualifiesForDrag(m_ClickStartPosition, pointerPosition);
            if (m_Dragged)
            {
                m_SelectingUtilities.SelectToPosition(pointerPosition);
                m_TextElement.edition.UpdateScrollOffset?.Invoke(false);

                selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp && !m_SelectingUtilities.hasSelection;
            }

            evt.StopPropagation();
        }

        void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !isClicking)
                return;

            if (selectAllOnMouseUp)
                m_SelectingUtilities.SelectAll();

            selectAllOnMouseUp = false;
            m_Dragged = false;
            isClicking = false;
            m_TextElement.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnValidateCommandEvent(ValidateCommandEvent evt)
        {
            if (!m_TextElement.hasFocus)
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
            if (!m_TextElement.hasFocus)
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
