// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class EditorGUIExt
    {
        class Styles
        {
            public GUIStyle selectionRect = "SelectionRect";
        }
        static Styles ms_Styles = new Styles();

        // Copied from GUI class and modified slightly to not require
        // calls to methods that are internal to the GUI class
        static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
        {
            //GUIUtility.CheckOnGUI ();
            int id = GUIUtility.GetControlID(repeatButtonHash, focusType, position);
            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // If the mouse is inside the button, we say that we're the hot control
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    return false;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        Event.current.Use();

                        // But we only return true if the button was actually clicked
                        return position.Contains(Event.current.mousePosition);
                    }
                    return false;
                case EventType.Repaint:
                    style.Draw(position, content, id);
                    //          Handles.Repaint ();
                    return id == GUIUtility.hotControl && position.Contains(Event.current.mousePosition);
            }
            return false;
        }

        static int repeatButtonHash = "repeatButton".GetHashCode();

        static float nextScrollStepTime = 0;
        static int firstScrollWait = 250; // ms
        static int scrollWait = 30; // ms

        /// *undocumented*
        // Copied from GUI class and modified slightly to not require
        // calls to methods that are internal to the GUI class
        static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
        {
            bool changed = false;

            if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                bool firstClick = scrollControlID != scrollerID;
                scrollControlID = scrollerID;

                if (firstClick)
                {
                    changed = true;
                    nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * firstScrollWait;
                }
                else
                {
                    if (Time.realtimeSinceStartup >= nextScrollStepTime)
                    {
                        changed = true;
                        nextScrollStepTime = Time.realtimeSinceStartup + 0.001f * scrollWait;
                    }
                }

                if (Event.current.type == EventType.Repaint)
                    //  GUI.InternalRepaintEditorWindow();
                    HandleUtility.Repaint();
            }

            return changed;
        }

        static int scrollControlID;
        public static void MinMaxScroller(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            //GUIUtility.CheckOnGUI ();

            float scrollStepSize;
            if (horiz)
                scrollStepSize = size * 10 / position.width;
            else
                scrollStepSize = size * 10 / position.height;

            //

            Rect sliderRect, minRect, maxRect;

            if (horiz)
            {
                sliderRect = new Rect(
                        position.x + leftButton.fixedWidth, position.y,
                        position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height
                        );
                minRect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                maxRect = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                sliderRect = new Rect(
                        position.x, position.y + leftButton.fixedHeight,
                        position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight
                        );
                minRect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                maxRect = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }

            float newVisualStart = Mathf.Min(visualStart, value);
            float newVisualEnd   = Mathf.Max(visualEnd  , value + size);

            MinMaxSlider(sliderRect, ref value, ref size, newVisualStart, newVisualEnd, newVisualStart, newVisualEnd, slider, thumb, horiz);

            bool wasMouseUpEvent = false;
            if (Event.current.type == EventType.MouseUp)
                wasMouseUpEvent = true;

            if (ScrollerRepeatButton(id, minRect, leftButton))
                value -= scrollStepSize * (visualStart < visualEnd ? 1f : -1f);

            if (ScrollerRepeatButton(id, maxRect, rightButton))
                value += scrollStepSize * (visualStart < visualEnd ? 1f : -1f);

            if (wasMouseUpEvent && Event.current.type == EventType.Used) // repeat buttons ate mouse up event - release scrolling
                scrollControlID = 0;

            if (startLimit < endLimit)
                value = Mathf.Clamp(value, startLimit, endLimit - size);
            else
                value = Mathf.Clamp(value, endLimit, startLimit - size);
        }

        // State for when we're dragging a MinMax slider.
        class MinMaxSliderState
        {
            public float dragStartPos = 0;      // Start of the drag (mousePosition)
            public float dragStartValue = 0;        // Value at start of drag.
            public float dragStartSize = 0;     // Size at start of drag.
            public float dragStartValuesPerPixel = 0;
            public float dragStartLimit = 0;        // start limit at start of drag
            public float dragEndLimit = 0;      // end limit at start of drag
            public int whereWeDrag = -1;        // which part are we dragging? 0 = middle, 1 = min, 2 = max, 3 = min trough, 4 = max trough
        }

        static MinMaxSliderState s_MinMaxSliderState;
        static int kFirstScrollWait = 250; // ms
        static int kScrollWait = 30; // ms
        static System.DateTime s_NextScrollStepTime = System.DateTime.Now; // whatever but null

        // Mouse down position for
        private static Vector2 s_MouseDownPos = Vector2.zero;
        // Are we doing a drag selection (as opposed to when the mousedown was over a selection rect)
        enum DragSelectionState
        {
            None, DragSelecting, Dragging
        }
        static DragSelectionState s_MultiSelectDragSelection = DragSelectionState.None;
        static Vector2 s_StartSelectPos = Vector2.zero;
        static List<bool> s_SelectionBackup = null;
        static List<bool> s_LastFrameSelections = null;
        internal static int s_MinMaxSliderHash = "MinMaxSlider".GetHashCode();
        /// Make a double-draggable slider that will let you specify a range of values.
        /// @param position where to draw it
        /// @param value the current start position
        /// @param size the size of the covered range
        /// @param visualStart what is displayed as the start of the range. The user can drag beyond this, but the displays shows this as the limit. Set this to be the start of the relevant data.
        /// @param visualEnd what is displayed as the end of the range. The user can drag beyond this, but the displays shows this as the limit. Set this to be the end of the relevant data.
        /// @param startLimit what is the lowest possible value? The user can never slide beyond this in the minimum direction. If you don't want a limit, set it to -Mathf.Infinity
        /// @param endLimit what is the highes possible value? The user can never slide beyond this in the maximum direction. If you don't want a limit, set it to Mathf.Infinity
        public static void MinMaxSlider(Rect position, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
        {
            DoMinMaxSlider(position, GUIUtility.GetControlID(s_MinMaxSliderHash, FocusType.Passive), ref value, ref size, visualStart, visualEnd, startLimit, endLimit, slider, thumb, horiz);
        }

        internal static void DoMinMaxSlider(Rect position, int id, ref float value, ref float size, float visualStart, float visualEnd, float startLimit, float endLimit, GUIStyle slider, GUIStyle thumb, bool horiz)
        {
            Event evt = Event.current;
            bool usePageScrollbars = size == 0;

            float minVisual = Mathf.Min(visualStart, visualEnd);
            float maxVisual = Mathf.Max(visualStart, visualEnd);
            float minLimit = Mathf.Min(startLimit, endLimit);
            float maxLimit = Mathf.Max(startLimit, endLimit);

            MinMaxSliderState state = s_MinMaxSliderState;

            if (GUIUtility.hotControl == id && state != null)
            {
                minVisual = state.dragStartLimit;
                minLimit = state.dragStartLimit;
                maxVisual = state.dragEndLimit;
                maxLimit = state.dragEndLimit;
            }

            float minSize = 0;

            float displayValue = Mathf.Clamp(value, minVisual, maxVisual);
            float displaySize = Mathf.Clamp(value + size, minVisual, maxVisual) - displayValue;

            float sign = visualStart > visualEnd ? -1 : 1;


            if (slider == null || thumb == null)
                return;

            // Figure out the rects
            float pixelsPerValue;
            float mousePosition;
            Rect thumbRect;
            Rect thumbMinRect, thumbMaxRect;
            if (horiz)
            {
                float thumbSize = thumb.fixedWidth != 0 ? thumb.fixedWidth : thumb.padding.horizontal;
                pixelsPerValue = (position.width - slider.padding.horizontal - thumbSize) / (maxVisual - minVisual);
                thumbRect = new Rect(
                        (displayValue - minVisual) * pixelsPerValue + position.x + slider.padding.left,
                        position.y + slider.padding.top,
                        displaySize * pixelsPerValue + thumbSize,
                        position.height - slider.padding.vertical);
                thumbMinRect = new Rect(thumbRect.x, thumbRect.y, thumb.padding.left, thumbRect.height);
                thumbMaxRect = new Rect(thumbRect.xMax - thumb.padding.right, thumbRect.y, thumb.padding.right, thumbRect.height);
                mousePosition = evt.mousePosition.x - position.x;
            }
            else
            {
                float thumbSize = thumb.fixedHeight != 0 ? thumb.fixedHeight : thumb.padding.vertical;
                pixelsPerValue = (position.height - slider.padding.vertical - thumbSize) / (maxVisual - minVisual);
                thumbRect = new Rect(
                        position.x + slider.padding.left,
                        (displayValue - minVisual) * pixelsPerValue + position.y + slider.padding.top,
                        position.width - slider.padding.horizontal,
                        displaySize * pixelsPerValue + thumbSize);
                thumbMinRect = new Rect(thumbRect.x, thumbRect.y, thumbRect.width, thumb.padding.top);
                thumbMaxRect = new Rect(thumbRect.x, thumbRect.yMax - thumb.padding.bottom, thumbRect.width, thumb.padding.bottom);
                mousePosition = evt.mousePosition.y - position.y;
            }

            float mousePos;
            float thumbPos;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // if the click is outside this control, just bail out...
                    if (!position.Contains(evt.mousePosition) || minVisual - maxVisual == 0)
                        return;
                    if (state == null)
                        state = s_MinMaxSliderState = new MinMaxSliderState();

                    // These are required to be set whenever we grab hotcontrol, regardless of if we actually drag or not. (case 585577)
                    state.dragStartLimit = startLimit;
                    state.dragEndLimit = endLimit;

                    if (thumbRect.Contains(evt.mousePosition))
                    {
                        // We have a mousedown on the thumb
                        // Record where we're draging from, so the user can get back.
                        state.dragStartPos = mousePosition;
                        state.dragStartValue = value;
                        state.dragStartSize = size;
                        state.dragStartValuesPerPixel = pixelsPerValue;
                        if (thumbMinRect.Contains(evt.mousePosition))
                            state.whereWeDrag = 1;
                        else if (thumbMaxRect.Contains(evt.mousePosition))
                            state.whereWeDrag = 2;
                        else
                            state.whereWeDrag = 0;

                        GUIUtility.hotControl = id;
                        evt.Use();
                        return;
                    }
                    else
                    {
                        // We're outside the thumb, but inside the trough.
                        // If we have no background, we just bail out.
                        if (slider == GUIStyle.none)
                            return;

                        // If we have a scrollSize, we do pgup/pgdn style movements
                        // if not, we just snap to the current position and begin tracking
                        if (size != 0 && usePageScrollbars)
                        {
                            if (horiz)
                            {
                                if (mousePosition > thumbRect.xMax - position.x)
                                    value += size * sign * .9f;
                                else
                                    value -= size * sign * .9f;
                            }
                            else
                            {
                                if (mousePosition > thumbRect.yMax - position.y)
                                    value += size * sign * .9f;
                                else
                                    value -= size * sign * .9f;
                            }
                            state.whereWeDrag = 0;
                            GUI.changed = true;
                            s_NextScrollStepTime = System.DateTime.Now.AddMilliseconds(kFirstScrollWait);

                            mousePos = horiz ? evt.mousePosition.x : evt.mousePosition.y;
                            thumbPos = horiz ? thumbRect.x : thumbRect.y;

                            state.whereWeDrag = mousePos > thumbPos ? 4 : 3;
                        }
                        else
                        {
                            if (horiz)
                                value = ((float)mousePosition - thumbRect.width * .5f) / pixelsPerValue + minVisual - size * .5f;
                            else
                                value = ((float)mousePosition - thumbRect.height * .5f) / pixelsPerValue + minVisual - size * .5f;
                            state.dragStartPos = mousePosition;
                            state.dragStartValue = value;
                            state.dragStartSize = size;
                            state.dragStartValuesPerPixel = pixelsPerValue;
                            state.whereWeDrag = 0;
                            GUI.changed = true;
                        }
                        GUIUtility.hotControl = id;
                        value = Mathf.Clamp(value, minLimit, maxLimit - size);
                        evt.Use();
                        return;
                    }
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        return;

                    // Recalculate the value from the mouse position. this has the side effect that values are relative to the
                    // click point - no matter where inside the trough the original value was. Also means user can get back original value
                    // if he drags back to start position.
                    float deltaVal = (mousePosition - state.dragStartPos) / state.dragStartValuesPerPixel;
                    switch (state.whereWeDrag)
                    {
                        case 0: // normal drag
                            value = Mathf.Clamp(state.dragStartValue + deltaVal, minLimit, maxLimit - size);
                            break;
                        case 1:// min size drag
                            value = state.dragStartValue + deltaVal;
                            size = state.dragStartSize - deltaVal;
                            if (value < minLimit)
                            {
                                size -= minLimit - value;
                                value = minLimit;
                            }
                            if (size < minSize)
                            {
                                value -= minSize - size;
                                size = minSize;
                            }
                            break;
                        case 2:// max size drag
                            size = state.dragStartSize + deltaVal;
                            if (value + size > maxLimit)
                                size = maxLimit - value;
                            if (size < minSize)
                                size = minSize;
                            break;
                    }
                    GUI.changed = true;
                    evt.Use();
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        evt.Use();
                        GUIUtility.hotControl = 0;
                    }
                    break;
                case EventType.Repaint:
                    slider.Draw(position, GUIContent.none, id);
                    thumb.Draw(thumbRect, GUIContent.none, id);

                    // if the mouse is outside this control, just bail out...
                    if (GUIUtility.hotControl != id ||
                        !position.Contains(evt.mousePosition) || minVisual - maxVisual == 0)
                    {
                        return;
                    }

                    if (thumbRect.Contains(evt.mousePosition))
                    {
                        if (state != null && (state.whereWeDrag == 3 || state.whereWeDrag == 4)) // if was scrolling with "through" and the thumb reached mouse - sliding action over
                            GUIUtility.hotControl = 0;
                        return;
                    }


                    if (System.DateTime.Now < s_NextScrollStepTime)
                        return;

                    mousePos = horiz ? evt.mousePosition.x : evt.mousePosition.y;
                    thumbPos = horiz ? thumbRect.x : thumbRect.y;

                    int currentSide = mousePos > thumbPos ? 4 : 3;
                    if (currentSide != state.whereWeDrag)
                        return;

                    // If we have a scrollSize, we do pgup/pgdn style movements
                    if (size != 0 && usePageScrollbars)
                    {
                        if (horiz)
                        {
                            if (mousePosition > thumbRect.xMax - position.x)
                                value += size * sign * .9f;
                            else
                                value -= size * sign * .9f;
                        }
                        else
                        {
                            if (mousePosition > thumbRect.yMax - position.y)
                                value += size * sign * .9f;
                            else
                                value -= size * sign * .9f;
                        }
                        state.whereWeDrag = -1;
                        GUI.changed = true;
                    }
                    value = Mathf.Clamp(value, minLimit, maxLimit - size);

                    s_NextScrollStepTime = System.DateTime.Now.AddMilliseconds(kScrollWait);
                    break;
            }
        }

        private static bool adding = false;
        private static bool[] initSelections;
        private static int initIndex = 0;

        // Used for selecting multiple rows on the left in the animation window.
        public static bool DragSelection(Rect[] positions, ref bool[] selections, GUIStyle style)
        {
            int id = GUIUtility.GetControlID(34553287, FocusType.Keyboard);
            Event evt = Event.current;
            int selectedIndex = -1;
            for (int i = positions.Length - 1; i >= 0; i--)
                if (positions[i].Contains(evt.mousePosition))
                {
                    selectedIndex = i;
                    break;
                }

            EventType type = evt.GetTypeForControl(id);
            switch (type)
            {
                case EventType.Repaint:
                    for (int i = 0; i < positions.Length; i++)
                        style.Draw(positions[i], GUIContent.none, id, selections[i]);
                    break;
                case EventType.MouseDown:
                    if (evt.button == 0 && selectedIndex >= 0)
                    {
                        GUIUtility.keyboardControl = 0;

                        bool deselecting = false;
                        // If clicking on an already selected item
                        if (selections[selectedIndex])
                        {
                            int counter = 0;
                            foreach (bool sel in selections)
                            {
                                if (sel)
                                {
                                    counter++;
                                    if (counter > 1)
                                        break;
                                }
                            }
                            // ...and it's the only one selected, then deselect it
                            if (counter == 1)
                                deselecting = true;
                        }

                        // Shift click to add to current selection
                        if (!evt.shift && !EditorGUI.actionKey)
                            for (int i = 0; i < positions.Length; i++)
                                selections[i] = false;

                        initIndex = selectedIndex;
                        initSelections = (bool[])selections.Clone();

                        // Command click to toggle
                        adding = true;
                        if ((evt.shift || EditorGUI.actionKey) && selections[selectedIndex] == true)
                            adding = false;

                        selections[selectedIndex] = (deselecting ? false : adding);
                        GUIUtility.hotControl = id;
                        evt.Use();

                        return true;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.button == 0)
                        {
                            if (selectedIndex < 0)
                            {
                                // Clamp index to nearest positions if outside of range
                                // (so that less precision is required for hitting the last rect)
                                Rect dummyRect = new Rect(positions[0].x, positions[0].y - 200, positions[0].width, 200);
                                if (dummyRect.Contains(evt.mousePosition))
                                    selectedIndex = 0;
                                dummyRect.y = positions[positions.Length - 1].yMax;
                                if (dummyRect.Contains(evt.mousePosition))
                                    selectedIndex = selections.Length - 1;
                            }
                            if (selectedIndex < 0)
                                return false;

                            int min = Mathf.Min(initIndex, selectedIndex);
                            int max = Mathf.Max(initIndex, selectedIndex);
                            for (int i = 0; i < selections.Length; i++)
                            {
                                if (i >= min && i <= max)
                                    selections[i] = adding;
                                else
                                    selections[i] = initSelections[i];
                            }
                            evt.Use();
                            return true;
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                        GUIUtility.hotControl = 0;
                    break;
            }

            return false;
        }

        static bool Any(bool[] selections)
        {
            for (int i = 0; i < selections.Length; i++)
            {
                if (selections[i])
                    return true;
            }
            return false;
        }

        public static HighLevelEvent MultiSelection(
            Rect rect,
            Rect[] positions,
            GUIContent content,
            Rect[] hitPositions,
            ref bool[] selections,
            bool[] readOnly,
            out int clickedIndex,
            out Vector2 offset,
            out float startSelect,
            out float endSelect,
            GUIStyle style
            )
        {
            int id = GUIUtility.GetControlID(41623453, FocusType.Keyboard);
            Event evt = Event.current;

            offset = Vector2.zero;
            clickedIndex = -1;
            startSelect = endSelect = 0f;

            if (evt.type == EventType.Used)
                return HighLevelEvent.None;

            bool selected = false;
            if (Event.current.type != EventType.Layout)
            {
                if (GUIUtility.keyboardControl == id)
                    selected = true;
            }

            int selectedIndex;

            EventType type = evt.GetTypeForControl(id);
            switch (type)
            {
                case EventType.Repaint:
                    // Draw selection rect
                    if (GUIUtility.hotControl == id && s_MultiSelectDragSelection == DragSelectionState.DragSelecting)
                    {
                        float min = Mathf.Min(s_StartSelectPos.x, evt.mousePosition.x);
                        float max = Mathf.Max(s_StartSelectPos.x, evt.mousePosition.x);
                        Rect selRect = new Rect(0, 0, rect.width, rect.height);
                        selRect.x = min;
                        selRect.width = max - min;
                        // Display if bigger than 1 pixel.
                        if (selRect.width > 1)
                            GUI.Box(selRect, "", ms_Styles.selectionRect);
                    }

                    // Draw controls
                    Color tempCol = GUI.color;
                    for (int i = 0; i < positions.Length; i++)
                    {
                        if (readOnly != null && readOnly[i])
                            GUI.color = tempCol * new Color(0.90f, 0.90f, 0.90f, 0.5f);
                        else if (selections[i])
                            GUI.color = tempCol * new Color(0.30f, 0.55f, 0.95f, 1);
                        else
                            GUI.color = tempCol * new Color(0.90f, 0.90f, 0.90f, 1);
                        style.Draw(positions[i], content, id, selections[i]);
                    }
                    GUI.color = tempCol;
                    break;
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        s_StartSelectPos = evt.mousePosition;
                        selectedIndex = GetIndexUnderMouse(hitPositions, readOnly);

                        if (Event.current.clickCount == 2)
                        {
                            if (selectedIndex >= 0)
                            {
                                for (int i = 0; i < selections.Length; i++)
                                    selections[i] = false;

                                selections[selectedIndex] = true;

                                evt.Use();
                                clickedIndex = selectedIndex;
                                return HighLevelEvent.DoubleClick;
                            }
                        }

                        if (selectedIndex >= 0)
                        {
                            // Shift click to add to current selection
                            if (!evt.shift && !EditorGUI.actionKey && !selections[selectedIndex])
                                for (int i = 0; i < hitPositions.Length; i++)
                                    selections[i] = false;

                            if (evt.shift || EditorGUI.actionKey)
                                selections[selectedIndex] = !selections[selectedIndex];
                            else
                                selections[selectedIndex] = true;

                            s_MouseDownPos = evt.mousePosition;
                            s_MultiSelectDragSelection = DragSelectionState.None;
                            evt.Use();
                            clickedIndex = selectedIndex;
                            return HighLevelEvent.SelectionChanged;
                        }
                        else
                        {
                            // Shift click to add to current selection
                            bool changed = false;
                            if (!evt.shift && !EditorGUI.actionKey)
                            {
                                for (int i = 0; i < hitPositions.Length; i++)
                                    selections[i] = false;
                                changed = true;
                            }
                            else
                                changed = false;

                            s_SelectionBackup = new List<bool>(selections);
                            s_LastFrameSelections = new List<bool>(selections);

                            s_MultiSelectDragSelection = DragSelectionState.DragSelecting;
                            evt.Use();
                            return changed ? HighLevelEvent.SelectionChanged : HighLevelEvent.None;
                        }
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (s_MultiSelectDragSelection == DragSelectionState.DragSelecting)
                        {
                            float min = Mathf.Min(s_StartSelectPos.x, evt.mousePosition.x);
                            float max = Mathf.Max(s_StartSelectPos.x, evt.mousePosition.x);
                            s_SelectionBackup.CopyTo(selections);
                            for (int i = 0; i < hitPositions.Length; i++)
                            {
                                if (selections[i])
                                    continue;

                                float center = hitPositions[i].x + hitPositions[i].width * .5f;
                                if (center >= min && center <= max)
                                    selections[i] = true;
                            }
                            evt.Use();
                            startSelect = min;
                            endSelect = max;

                            // Check if the selections _actually_ changed from last call
                            bool changed = false;
                            for (int i = 0; i < selections.Length; i++)
                            {
                                if (selections[i] != s_LastFrameSelections[i])
                                {
                                    changed = true;
                                    s_LastFrameSelections[i] = selections[i];
                                }
                            }
                            return changed ? HighLevelEvent.SelectionChanged : HighLevelEvent.None;
                        }
                        else
                        {
                            offset = evt.mousePosition - s_MouseDownPos;
                            evt.Use();
                            if (s_MultiSelectDragSelection == DragSelectionState.None)
                            {
                                s_MultiSelectDragSelection = DragSelectionState.Dragging;
                                return HighLevelEvent.BeginDrag;
                            }
                            else
                            {
                                return HighLevelEvent.Drag;
                            }
                        }
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        if (s_StartSelectPos != evt.mousePosition)
                            evt.Use();

                        // TODO fix magic number for max dragging distance to be ignored
                        if (s_MultiSelectDragSelection == DragSelectionState.None)
                        {
                            clickedIndex = GetIndexUnderMouse(hitPositions, readOnly);
                            if (evt.clickCount == 1)
                                return HighLevelEvent.Click;
                        }
                        else
                        {
                            s_MultiSelectDragSelection = DragSelectionState.None;
                            s_SelectionBackup = null;
                            s_LastFrameSelections = null;
                            return HighLevelEvent.EndDrag;
                        }
                    }
                    break;
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:

                    if (selected)
                    {
                        bool execute = evt.type == EventType.ExecuteCommand;
                        switch (evt.commandName)
                        {
                            case "Delete":
                                evt.Use();
                                if (execute)
                                {
                                    return HighLevelEvent.Delete;
                                }
                                break;
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (selected)
                    {
                        if (evt.keyCode == KeyCode.Backspace || evt.keyCode == KeyCode.Delete)
                        {
                            evt.Use();
                            return HighLevelEvent.Delete;
                        }
                    }
                    break;
                case EventType.ContextClick:
                    selectedIndex = GetIndexUnderMouse(hitPositions, readOnly);
                    if (selectedIndex >= 0)
                    {
                        clickedIndex = selectedIndex;
                        GUIUtility.keyboardControl = id;
                        evt.Use();
                        return HighLevelEvent.ContextClick;
                    }
                    break;
            }

            return HighLevelEvent.None;
        }

        // Helper for MultiSelection above.
        static int GetIndexUnderMouse(Rect[] hitPositions, bool[] readOnly)
        {
            Vector2 mousePos = Event.current.mousePosition;

            for (int i = hitPositions.Length - 1; i >= 0; i--)
                if ((readOnly == null || !readOnly[i]) && hitPositions[i].Contains(mousePos))
                    return i;

            return -1;
        }

        // Small helper: Make a rect from MinMax values and make sure they're positive sizes
        internal static Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }
            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }
            return r;
        }
    }

    internal enum HighLevelEvent
    {
        None,
        Click,
        DoubleClick,
        ContextClick,
        BeginDrag,
        Drag,
        EndDrag,
        Delete,
        SelectionChanged,
    }
} //namespace
