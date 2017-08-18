// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class Button
    {
        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        public static bool Do(int id, Vector3 position, Quaternion direction, float size, float pickSize, Handles.DrawCapFunction capFunc)
        #pragma warning restore 618
        {
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (GUI.enabled)
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, pickSize));
                    break;
                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                        HandleUtility.Repaint();
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = id; // Grab mouse focus
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (HandleUtility.nearestControl == id)
                            return true;
                    }
                    break;
                case EventType.Repaint:
                    Color origColor = Handles.color;
                    if (HandleUtility.nearestControl == id && GUI.enabled && GUIUtility.hotControl == 0)
                        Handles.color = Handles.preselectionColor;

                    capFunc(id, position, direction, size);

                    Handles.color = origColor;
                    break;
            }
            return false;
        }

        public static bool Do(int id, Vector3 position, Quaternion direction, float size, float pickSize, Handles.CapFunction capFunction)
        {
            Event evt = Event.current;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    if (GUI.enabled)
                        capFunction(id, position, direction, pickSize, EventType.Layout);
                    break;
                case EventType.MouseMove:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                        HandleUtility.Repaint();
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = id; // Grab mouse focus
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if (HandleUtility.nearestControl == id)
                            return true;
                    }
                    break;
                case EventType.Repaint:
                    Color origColor = Handles.color;
                    if (HandleUtility.nearestControl == id && GUI.enabled && GUIUtility.hotControl == 0)
                        Handles.color = Handles.preselectionColor;

                    capFunction(id, position, direction, size, EventType.Repaint);

                    Handles.color = origColor;
                    break;
            }
            return false;
        }
    }
}
