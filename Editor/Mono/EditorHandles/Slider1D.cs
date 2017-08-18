// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class Slider1D
    {
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        private static Vector3 s_StartPosition;

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        internal static Vector3 Do(int id, Vector3 position, Vector3 direction, float size, Handles.DrawCapFunction drawFunc, float snap)
        #pragma warning restore 618
        {
            return Do(id, position, direction, direction, size, drawFunc, snap);
        }

        internal static Vector3 Do(int id, Vector3 position, Vector3 direction, float size, Handles.CapFunction capFunction, float snap)
        {
            return Do(id, position, Vector3.zero, direction, direction, size, capFunction, snap);
        }

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        internal static Vector3 Do(int id, Vector3 position, Vector3 handleDirection, Vector3 slideDirection, float size, Handles.DrawCapFunction drawFunc, float snap)
        #pragma warning disable 618
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    // This is an ugly hack. It would be better if the drawFunc can handle it's own layout.
                    if (drawFunc == Handles.ArrowCap)
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position, position + slideDirection * size));
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + slideDirection * size, size * .2f));
                    }
                    else
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size * .2f));
                    }
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if ((HandleUtility.nearestControl == id && evt.button == 0) && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = id;    // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        float dist = HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, slideDirection);

                        dist = Handles.SnapValue(dist, snap);

                        Vector3 worldDirection = Handles.matrix.MultiplyVector(slideDirection);
                        Vector3 worldPosition = Handles.matrix.MultiplyPoint(s_StartPosition) + worldDirection * dist;
                        position = Handles.inverseMatrix.MultiplyPoint(worldPosition);
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
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.Repaint:
                    Color temp = Color.white;

                    if (id == GUIUtility.hotControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.preselectionColor;
                    }
                    drawFunc(id, position, Quaternion.LookRotation(handleDirection), size);

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }

        internal static Vector3 Do(int id, Vector3 position, Vector3 offset, Vector3 handleDirection, Vector3 slideDirection, float size, Handles.CapFunction capFunction, float snap)
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (capFunction != null)
                        capFunction(id, position + offset, Quaternion.LookRotation(handleDirection), size, EventType.Layout);
                    else
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position + offset, size * .2f));
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && GUIUtility.hotControl == 0)
                    {
                        GUIUtility.hotControl = id;    // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        float dist = HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, slideDirection);

                        dist = Handles.SnapValue(dist, snap);

                        Vector3 worldDirection = Handles.matrix.MultiplyVector(slideDirection);
                        Vector3 worldPosition = Handles.matrix.MultiplyPoint(s_StartPosition) + worldDirection * dist;
                        position = Handles.inverseMatrix.MultiplyPoint(worldPosition);
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
                case EventType.MouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.Repaint:
                    Color temp = Color.white;

                    if (id == GUIUtility.hotControl)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    else if (id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.preselectionColor;
                    }

                    capFunction(id, position + offset, Quaternion.LookRotation(handleDirection), size, EventType.Repaint);

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }
    }
}
