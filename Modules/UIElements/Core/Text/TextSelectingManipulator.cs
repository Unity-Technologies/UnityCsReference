// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    internal class TextSelectingManipulator
    {
        internal TextSelectingUtilities m_SelectingUtilities;

        bool selectAllOnMouseUp;
        TextElement m_TextElement;
        Vector2 m_ClickStartPosition;
        bool m_Dragged;
        int m_ClickingPointerId = PointerId.invalidPointerId;

        const int k_DragThresholdSqr = 16;

        private int m_ConsecutiveMouseDownCount;
        private long m_LastMouseDownTimeStamp = 0;
        internal bool isClicking => m_ClickingPointerId != PointerId.invalidPointerId && m_TextElement.HasPointerCapture(m_ClickingPointerId);

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
                m_TextElement.edition.UpdateScrollOffset(false);
        }

        void OnCursorIndexChange()
        {
            m_TextElement.IncrementVersion(VersionChangeType.Repaint);

            if(HasSelection() && m_TextElement.focusController != null)
                m_TextElement.focusController.selectedTextElement = m_TextElement;

            if(m_SelectingUtilities.revealCursor)
                m_TextElement.edition.UpdateScrollOffset(false);
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
                m_TextElement.uitkTextHandle.RemoveFromPermanentCache();
            }
            else if ((evt is not PointerMoveEvent && evt is not MouseMoveEvent) || isClicking)
            {
                bool wasCached = m_TextElement.uitkTextHandle.IsCachedPermanent;
                m_TextElement.uitkTextHandle.AddToPermanentCacheAndGenerateMesh();
                if (!wasCached)
                    m_SelectingUtilities.SyncStateToNative();
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
                case PointerCancelEvent pce:
                    OnPointerCancelEvent(pce);
                    break;
                case PointerCaptureOutEvent pcoe:
                    OnPointerCaptureOutEvent(pcoe);
                    break;
            }
        }

        void OnFocusEvent()
        {
            selectAllOnMouseUp = false;

            // If focus was given to this element from a mouse click or a Panel.Focus call, allow select on mouse up.
            if (isClicking)
                selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp;

            m_SelectingUtilities.OnFocus(m_TextElement.selection.selectAllOnFocus && !isClicking);
        }

        void OnBlurEvent()
        {
            selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!m_TextElement.hasFocus)
                return;

            if (m_SelectingUtilities.HandleKeyEvent(evt.keyCode, evt.modifiers))
                evt.StopPropagation();
        }

        //Changed to not rely on evt.clickCount to fix https://fogbugz.unity3d.com/f/cases/1409098/
        //This is necessary because default event system doesn't support triple click
        void OnPointerDownEvent(PointerDownEvent evt)
        {
            var pointerPosition = evt.localPosition - (Vector3)m_TextElement.contentRect.min;
            if (evt.button == (int)MouseButton.LeftMouse)
            {
                if (isClicking && evt.pointerId != m_ClickingPointerId)
                {
                    evt.StopPropagation();
                    return;
                }

                //only move cursor to position if it wasn't a double or triple click.
                if (evt.timestamp - m_LastMouseDownTimeStamp < Event.GetDoubleClickTime())
                    m_ConsecutiveMouseDownCount = ( m_ConsecutiveMouseDownCount  % 3) +1 ; // goest to 3 then come back to 1
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
                    m_SelectingUtilities.MouseDragSelectsWholeWords(true);
                    m_SelectingUtilities.DblClickSnap(TextSelectingUtilities.DblClickSnapping.WORDS);
                }
                else if (m_ConsecutiveMouseDownCount == 3 && m_TextElement.selection.tripleClickSelectsLine)
                {
                    m_SelectingUtilities.SelectCurrentParagraph();
                    m_SelectingUtilities.MouseDragSelectsWholeWords(true);
                    m_SelectingUtilities.DblClickSnap(TextSelectingUtilities.DblClickSnapping.PARAGRAPHS);
                }
                else
                {
                    m_SelectingUtilities.MoveCursorToPosition_Internal(pointerPosition, evt.shiftKey);
                    m_TextElement.edition.UpdateScrollOffset(false);
                    m_SelectingUtilities.MouseDragSelectsWholeWords(false);
                    m_SelectingUtilities.DblClickSnap(TextSelectingUtilities.DblClickSnapping.WORDS);
                }

                m_LastMouseDownTimeStamp = evt.timestamp;
                m_ClickingPointerId = evt.pointerId;
                m_TextElement.CapturePointer(evt.pointerId);
                m_ClickStartPosition = pointerPosition;
                evt.StopPropagation();
            }
        }

        // Needed for testing purposes
        internal void ResetClickCount()
        {
            m_ConsecutiveMouseDownCount = 0;
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (!IsClickingPointer(evt.pointerId))
                return;

            var pointerPosition = evt.localPosition - (Vector3)m_TextElement.contentRect.min;
            m_Dragged = m_Dragged || MoveDistanceQualifiesForDrag(m_ClickStartPosition, pointerPosition);
            if (m_Dragged)
            {
                m_SelectingUtilities.SelectToPosition(pointerPosition);
                m_TextElement.edition.UpdateScrollOffset(false);

                selectAllOnMouseUp = m_TextElement.selection.selectAllOnMouseUp && !m_SelectingUtilities.hasSelection;
            }

            evt.StopPropagation();
        }

        void OnPointerUpEvent(PointerUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !IsClickingPointer(evt.pointerId))
                return;

            if (selectAllOnMouseUp)
            {
                m_SelectingUtilities.SelectAll();
                ResetClickCount();
            }

            EndClick();
            m_TextElement.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerCancelEvent(PointerCancelEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse || !IsClickingPointer(evt.pointerId))
                return;

            EndClick();
            m_TextElement.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerCaptureOutEvent(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId == m_ClickingPointerId)
                EndClick();
        }

        internal bool IsClickingPointer(int pointerId) => pointerId == m_ClickingPointerId && isClicking;

        void EndClick()
        {
            selectAllOnMouseUp = false;
            m_Dragged = false;
            m_ClickingPointerId = PointerId.invalidPointerId;
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
