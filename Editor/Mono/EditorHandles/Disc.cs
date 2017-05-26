// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class Disc
    {
        static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        static Vector3 s_StartPosition, s_StartAxis;
        static Quaternion s_StartRotation;
        static float s_RotationDist;


        public static Quaternion Do(int id, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, float snap)
        {
            if (Mathf.Abs(Vector3.Dot(Camera.current.transform.forward, axis)) > .999f)
                cutoffPlane = false;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.layout:
                {
                    float d;
                    if (cutoffPlane)
                    {
                        Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                        d = HandleUtility.DistanceToArc(position, axis, from, 180, size) / 2;
                    }
                    else
                    {
                        d = HandleUtility.DistanceToDisc(position, axis, size) / 2;
                    }

                    HandleUtility.AddControl(id, d);
                    break;
                }
                case EventType.mouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;    // Grab mouse focus
                        Tools.LockHandlePosition();
                        if (cutoffPlane)
                        {
                            Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                            s_StartPosition = HandleUtility.ClosestPointToArc(position, axis, from, 180, size);
                        }
                        else
                        {
                            s_StartPosition = HandleUtility.ClosestPointToDisc(position, axis, size);
                        }
                        s_RotationDist = 0;
                        s_StartRotation = rotation;
                        s_StartAxis = axis;
                        s_CurrentMousePosition = s_StartMousePosition = Event.current.mousePosition;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.mouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        // handle look to point rotation
                        bool rayDrag = EditorGUI.actionKey && evt.shift;
                        if (rayDrag)
                        {
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();
                            object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null && Vector3.Dot(axis.normalized, rotation * Vector3.forward) < 0.999)
                            {
                                RaycastHit rh = (RaycastHit)hit;
                                Vector3 lookPoint = rh.point - position;
                                Vector3 lookPointProjected = lookPoint - Vector3.Dot(lookPoint, axis.normalized) * axis.normalized;
                                rotation = Quaternion.LookRotation(lookPointProjected, rotation * Vector3.up);
                            }
                        }
                        else
                        {
                            Vector3 direction = Vector3.Cross(axis, position - s_StartPosition).normalized;
                            s_CurrentMousePosition += evt.delta;
                            s_RotationDist = HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, s_StartPosition, direction) / size * 30;
                            s_RotationDist = Handles.SnapValue(s_RotationDist, snap);
                            rotation = Quaternion.AngleAxis(s_RotationDist * -1, s_StartAxis) * s_StartRotation;
                        }

                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.mouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        Tools.UnlockHandlePosition();
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.mouseMove:
                    if (id == HandleUtility.nearestControl)
                        HandleUtility.Repaint();
                    break;
                case EventType.KeyDown:
                    if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
                    {
                        // We do not use the event nor clear hotcontrol to ensure auto revert value kicks in from native side
                        Tools.UnlockHandlePosition();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.repaint:
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

                    // If we're dragging it, we'll go a bit further and draw a selection pie
                    if (GUIUtility.hotControl == id)
                    {
                        Color t = Handles.color;
                        Vector3 from = (s_StartPosition - position).normalized;
                        Handles.color = Handles.secondaryColor;
                        Handles.DrawLine(position, position + from * size * 1.1f);
                        float d = Mathf.Repeat(-s_RotationDist - 180, 360) - 180;
                        Vector3 to = Quaternion.AngleAxis(d, axis) * from;
                        Handles.DrawLine(position, position + to * size * 1.1f);

                        Handles.color = Handles.secondaryColor * new Color(1, 1, 1, .2f);
                        Handles.DrawSolidArc(position, axis, from, d, size);
                        Handles.color = t;
                    }

                    if (cutoffPlane)
                    {
                        Vector3 from = Vector3.Cross(axis, Camera.current.transform.forward).normalized;
                        Handles.DrawWireArc(position, axis, from, 180, size);
                    }
                    else
                    {
                        Handles.DrawWireDisc(position, axis, size);
                    }


                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }

            return rotation;
        }
    }
}
