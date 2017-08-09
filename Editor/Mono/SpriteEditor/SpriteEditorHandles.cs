// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    public static class SpriteEditorHandles
    {
        private static Vector2 s_CurrentMousePosition;
        private static Vector2 s_DragStartScreenPosition;
        private static Vector2 s_DragScreenOffset;

        static internal Vector2 PointSlider(Vector2 pos, MouseCursor cursor, GUIStyle dragDot, GUIStyle dragDotActive)
        {
            int id = GUIUtility.GetControlID("Slider1D".GetHashCode(), FocusType.Keyboard);
            Vector2 screenVal = Handles.matrix.MultiplyPoint(pos);
            Rect handleScreenPos = new Rect(
                    screenVal.x - dragDot.fixedWidth * .5f,
                    screenVal.y - dragDot.fixedHeight * .5f,
                    dragDot.fixedWidth,
                    dragDot.fixedHeight
                    );

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Repaint:
                    if (GUIUtility.hotControl == id)
                        dragDotActive.Draw(handleScreenPos, GUIContent.none, id);
                    else
                        dragDot.Draw(handleScreenPos, GUIContent.none, id);
                    break;
            }
            return ScaleSlider(pos, cursor, handleScreenPos);
        }

        static internal Vector2 ScaleSlider(Vector2 pos, MouseCursor cursor, Rect cursorRect)
        {
            int id = GUIUtility.GetControlID("Slider1D".GetHashCode(), FocusType.Keyboard);
            return ScaleSlider(id, pos, cursor, cursorRect);
        }

        static private Vector2 ScaleSlider(int id, Vector2 pos, MouseCursor cursor, Rect cursorRect)
        {
            Vector2 screenVal = Handles.matrix.MultiplyPoint(pos);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (evt.button == 0 &&
                        cursorRect.Contains(Event.current.mousePosition) &&
                        !evt.alt)
                    {
                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;    // Grab mouse focus
                        s_CurrentMousePosition = evt.mousePosition;
                        s_DragStartScreenPosition = evt.mousePosition;
                        s_DragScreenOffset = s_CurrentMousePosition - screenVal;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        Vector2 oldPos = pos;
                        pos = Handles.inverseMatrix.MultiplyPoint(s_CurrentMousePosition);
                        if (!Mathf.Approximately((oldPos - pos).magnitude, 0f))
                            GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            pos = Handles.inverseMatrix.MultiplyPoint(s_DragStartScreenPosition - s_DragScreenOffset);
                            GUIUtility.hotControl = 0;
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(cursorRect, cursor, id);
                    break;
            }
            return pos;
        }

        static internal Vector2 PivotSlider(Rect sprite, Vector2 pos, GUIStyle pivotDot, GUIStyle pivotDotActive)
        {
            int id = GUIUtility.GetControlID("Slider1D".GetHashCode(), FocusType.Keyboard);

            // Convert from normalized space to texture space
            pos = new Vector2(sprite.xMin + sprite.width * pos.x, sprite.yMin + sprite.height * pos.y);

            Vector2 screenVal = Handles.matrix.MultiplyPoint(pos);

            Rect handleScreenPos = new Rect(
                    screenVal.x - pivotDot.fixedWidth * .5f,
                    screenVal.y - pivotDot.fixedHeight * .5f,
                    pivotDotActive.fixedWidth,
                    pivotDotActive.fixedHeight
                    );

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (evt.button == 0 && handleScreenPos.Contains(Event.current.mousePosition) && !evt.alt)
                    {
                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;    // Grab mouse focus
                        s_CurrentMousePosition = evt.mousePosition;
                        s_DragStartScreenPosition = evt.mousePosition;
                        Vector2 rectScreenCenter = Handles.matrix.MultiplyPoint(pos);
                        s_DragScreenOffset = s_CurrentMousePosition - rectScreenCenter;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        Vector2 oldPos = pos;
                        Vector3 scrPos = Handles.inverseMatrix.MultiplyPoint(s_CurrentMousePosition - s_DragScreenOffset);
                        pos = new Vector2(scrPos.x, scrPos.y);
                        if (!Mathf.Approximately((oldPos - pos).magnitude, 0f))
                            GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            pos = Handles.inverseMatrix.MultiplyPoint(s_DragStartScreenPosition - s_DragScreenOffset);
                            GUIUtility.hotControl = 0;
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(handleScreenPos, MouseCursor.Arrow, id);

                    if (GUIUtility.hotControl == id)
                        pivotDotActive.Draw(handleScreenPos, GUIContent.none, id);
                    else
                        pivotDot.Draw(handleScreenPos, GUIContent.none, id);

                    break;
            }

            // Convert from texture space back to normalized space
            pos = new Vector2((pos.x - sprite.xMin) / sprite.width, (pos.y - sprite.yMin) / sprite.height);

            return pos;
        }

        static internal Rect SliderRect(Rect pos)
        {
            int id = GUIUtility.GetControlID("SliderRect".GetHashCode(), FocusType.Keyboard);

            Event evt = Event.current;

            // SpriteEditorWindow is telling us we got selected and so we fake a mousedown on our Repaint event to get "one-click dragging" going on
            if (SpriteEditorWindow.s_OneClickDragStarted && evt.type == EventType.Repaint)
            {
                HandleSliderRectMouseDown(id, evt, pos);
                SpriteEditorWindow.s_OneClickDragStarted = false;
            }

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (evt.button == 0 && pos.Contains(Handles.inverseMatrix.MultiplyPoint(Event.current.mousePosition)) && !evt.alt)
                    {
                        HandleSliderRectMouseDown(id, evt, pos);
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;

                        Vector2 oldCenter = pos.center;
                        pos.center = Handles.inverseMatrix.MultiplyPoint(s_CurrentMousePosition - s_DragScreenOffset);
                        if (!Mathf.Approximately((oldCenter - pos.center).magnitude, 0f))
                            GUI.changed = true;

                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            pos.center = Handles.inverseMatrix.MultiplyPoint(s_DragStartScreenPosition - s_DragScreenOffset);
                            GUIUtility.hotControl = 0;
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.Repaint:
                    Vector2 topleft = Handles.inverseMatrix.MultiplyPoint(new Vector2(pos.xMin, pos.yMin));
                    Vector2 bottomright = Handles.inverseMatrix.MultiplyPoint(new Vector2(pos.xMax, pos.yMax));
                    EditorGUIUtility.AddCursorRect(new Rect(topleft.x, topleft.y, bottomright.x - topleft.x, bottomright.y - topleft.y), MouseCursor.Arrow, id);
                    break;
            }

            return pos;
        }

        static internal void HandleSliderRectMouseDown(int id, Event evt, Rect pos)
        {
            GUIUtility.hotControl = GUIUtility.keyboardControl = id; // Grab mouse focus
            s_CurrentMousePosition = evt.mousePosition;
            s_DragStartScreenPosition = evt.mousePosition;

            Vector2 rectScreenCenter = Handles.matrix.MultiplyPoint(pos.center);
            s_DragScreenOffset = s_CurrentMousePosition - rectScreenCenter;

            EditorGUIUtility.SetWantsMouseJumping(1);
        }

        static int s_RectSelectionID = GUIUtility.GetPermanentControlID();

        static internal Rect RectCreator(Rect textureArea, GUIStyle rectStyle)
        {
            Event evt = Event.current;
            Vector2 mousePos = evt.mousePosition;
            int id = s_RectSelectionID;
            Rect result = new Rect();

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (evt.button == 0)
                    {
                        GUIUtility.hotControl = id;

                        // Make sure that the starting position is clamped to inside texture area
                        Vector2 point = Handles.inverseMatrix.MultiplyPoint(mousePos);

                        point.x = Mathf.Min(Mathf.Max(point.x, textureArea.xMin), textureArea.xMax);
                        point.y = Mathf.Min(Mathf.Max(point.y, textureArea.yMin), textureArea.yMax);

                        // Save clamped starting position for later use
                        s_DragStartScreenPosition = Handles.matrix.MultiplyPoint(point);

                        // Actual position
                        s_CurrentMousePosition = mousePos;

                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition = new Vector2(mousePos.x, mousePos.y);
                        evt.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (GUIUtility.hotControl == id && ValidRect(s_DragStartScreenPosition, s_CurrentMousePosition))
                    {
                        // TODO: use rectstyle
                        //rectStyle.Draw (GetCurrentRect (true, textureWidth, textureHeight, s_DragStartScreenPosition, s_CurrentMousePosition), GUIContent.none, false, false, false, false);
                        SpriteEditorUtility.BeginLines(Color.green * 1.5f);
                        SpriteEditorUtility.DrawBox(GetCurrentRect(false, textureArea, s_DragStartScreenPosition, s_CurrentMousePosition));
                        SpriteEditorUtility.EndLines();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && evt.button == 0)
                    {
                        if (ValidRect(s_DragStartScreenPosition, s_CurrentMousePosition))
                        {
                            result = GetCurrentRect(false, textureArea, s_DragStartScreenPosition, s_CurrentMousePosition);
                            GUI.changed = true;
                            evt.Use();
                        }

                        GUIUtility.hotControl = 0;
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            GUIUtility.hotControl = 0;
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
            }
            return result;
        }

        static internal Rect RectCreator(float textureWidth, float textureHeight, GUIStyle rectStyle)
        {
            return RectCreator(new Rect(0, 0, textureWidth, textureHeight), rectStyle);
        }

        static private bool ValidRect(Vector2 startPoint, Vector2 endPoint)
        {
            return Mathf.Abs((endPoint - startPoint).x) > 5f && Mathf.Abs((endPoint - startPoint).y) > 5f;
        }

        static private Rect GetCurrentRect(bool screenSpace, Rect clampArea, Vector2 startPoint, Vector2 endPoint)
        {
            Rect r = EditorGUIExt.FromToRect(Handles.inverseMatrix.MultiplyPoint(startPoint), Handles.inverseMatrix.MultiplyPoint(endPoint));
            r = SpriteEditorUtility.ClampedRect(SpriteEditorUtility.RoundToInt(r), clampArea, false);

            if (screenSpace)
            {
                Vector2 topleft = Handles.matrix.MultiplyPoint(new Vector2(r.xMin, r.yMin));
                Vector2 bottomright = Handles.matrix.MultiplyPoint(new Vector2(r.xMax, r.yMax));

                r = new Rect(topleft.x, topleft.y, bottomright.x - topleft.x, bottomright.y - topleft.y);
            }
            return r;
        }
    }
}
