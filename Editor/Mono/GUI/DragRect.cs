// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    class DragRectGUI
    {
        static int dragRectHash = "DragRect".GetHashCode();
        static int s_DragCandidateState = 0;
        static float s_DragSensitivity = 1.0f;

        public static int DragRect(Rect position, int value, int minValue, int maxValue)
        {
            Event evt = Event.current;

            int id = GUIUtility.GetControlID(dragRectHash, FocusType.Passive, position);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition) && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        s_DragCandidateState = 1;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && s_DragCandidateState != 0)
                    {
                        GUIUtility.hotControl = 0;
                        s_DragCandidateState = 0;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        switch (s_DragCandidateState)
                        {
                            case 1:
                                value += (int)(HandleUtility.niceMouseDelta * s_DragSensitivity);
                                GUI.changed = true;
                                evt.Use();

                                if (value < minValue)
                                    value = minValue;
                                else if (value > maxValue)
                                    value = maxValue;
                                break;
                        }
                    }
                    break;
                case EventType.Repaint:
                    EditorGUIUtility.AddCursorRect(position, MouseCursor.SlideArrow);
                    break;
            }

            return value;
        }
    }
}
