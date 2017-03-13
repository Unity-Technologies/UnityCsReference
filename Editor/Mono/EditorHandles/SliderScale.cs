// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class SliderScale
    {
        private static float s_StartScale, s_ScaleDrawLength = 1.0f;
        private static float s_ValueDrag;
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;

        public static float DoAxis(int id, float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, float snap)
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position, position + direction * size));
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + direction * size, size * .2f));
                    break;
                case EventType.mouseDown:
                    // am I closest to the thingy?
                    if ((HandleUtility.nearestControl == id && evt.button == 0) || (GUIUtility.keyboardControl == id && evt.button == 2))
                    {
                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;    // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartScale = scale;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.mouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        float dist = 1 + HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, position, direction) / size;
                        dist = Handles.SnapValue(dist, snap);
                        scale = s_StartScale * dist;
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.mouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.repaint:
                    Color temp = Color.white;
                    if (id == GUIUtility.keyboardControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    float s = size;
                    if (GUIUtility.hotControl == id)
                    {
                        s = size * scale / s_StartScale;
                    }
                    Handles.CubeHandleCap(id, position + direction * s * s_ScaleDrawLength, rotation, size * .1f, EventType.Repaint);
                    Handles.DrawLine(position, position + direction * (s * s_ScaleDrawLength - size * .05f));

                    if (id == GUIUtility.keyboardControl)
                        Handles.color = temp;
                    break;
            }

            return scale;
        }

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        public static float DoCenter(int id, float value, Vector3 position, Quaternion rotation, float size, Handles.DrawCapFunction capFunc, float snap)
        #pragma warning restore 618
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size * .15f));
                    break;
                case EventType.mouseDown:
                    // am I closest to the thingy?
                    if ((HandleUtility.nearestControl == id && evt.button == 0) || (GUIUtility.keyboardControl == id && evt.button == 2))
                    {
                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;     // Grab mouse focus
                        s_StartScale = value;
                        s_ValueDrag = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.mouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_ValueDrag += HandleUtility.niceMouseDelta * .01f;
                        value = (Handles.SnapValue(s_ValueDrag, snap) + 1.0f) * s_StartScale;
                        s_ScaleDrawLength = value / s_StartScale;
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.keyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        // Cancel dragging on ESC
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            value = s_StartScale;
                            s_ScaleDrawLength = 1.0f;
                            GUIUtility.hotControl = 0;
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.mouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        s_ScaleDrawLength = 1.0f;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.repaint:
                    Color temp = Color.white;
                    if (id == GUIUtility.keyboardControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    capFunc(id, position, rotation, size * .15f);

                    if (id == GUIUtility.keyboardControl)
                        Handles.color = temp;
                    break;
            }

            return value;
        }

        public static float DoCenter(int id, float value, Vector3 position, Quaternion rotation, float size, Handles.CapFunction capFunction, float snap)
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.layout:
                    capFunction(id, position, rotation, size * .15f, EventType.Layout);
                    break;
                case EventType.mouseDown:
                    // am I closest to the thingy?
                    if ((HandleUtility.nearestControl == id && evt.button == 0) || (GUIUtility.keyboardControl == id && evt.button == 2))
                    {
                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;     // Grab mouse focus
                        s_StartScale = value;
                        s_ValueDrag = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.mouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_ValueDrag += HandleUtility.niceMouseDelta * .01f;
                        value = (Handles.SnapValue(s_ValueDrag, snap) + 1.0f) * s_StartScale;
                        s_ScaleDrawLength = value / s_StartScale;
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.keyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        // Cancel dragging on ESC
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            value = s_StartScale;
                            s_ScaleDrawLength = 1.0f;
                            GUIUtility.hotControl = 0;
                            GUI.changed = true;
                            evt.Use();
                        }
                    }
                    break;
                case EventType.mouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        s_ScaleDrawLength = 1.0f;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.repaint:
                    Color temp = Color.white;
                    if (id == GUIUtility.keyboardControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    capFunction(id, position, rotation, size * .15f, EventType.Repaint);

                    if (id == GUIUtility.keyboardControl)
                        Handles.color = temp;
                    break;
            }

            return value;
        }
    }
}
