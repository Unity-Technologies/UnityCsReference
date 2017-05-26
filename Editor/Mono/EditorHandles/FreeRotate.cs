// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class FreeRotate
    {
        private static Vector2 s_CurrentMousePosition;

        public static Quaternion Do(int id, Quaternion rotation, Vector3 position, float size)
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix = Handles.matrix;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldPosition, size) + HandleUtility.kPickDistance);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.mouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id; // Grab mouse focus
                        Tools.LockHandlePosition();
                        s_CurrentMousePosition = evt.mousePosition;
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.mouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        // rayDrag rotates object to look at ray hit
                        bool rayDrag = EditorGUI.actionKey && evt.shift;
                        if (rayDrag)
                        {
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();

                            object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null)
                            {
                                RaycastHit rh = (RaycastHit)hit;
                                Quaternion newRotation = Quaternion.LookRotation(rh.point - position);
                                if (Tools.pivotRotation == PivotRotation.Global)
                                {
                                    Transform t = Selection.activeTransform;
                                    if (t)
                                    {
                                        Quaternion delta = Quaternion.Inverse(t.rotation) * rotation;
                                        newRotation = newRotation * delta;
                                    }
                                }
                                rotation = newRotation;
                            }
                        }
                        else
                        {
                            s_CurrentMousePosition += evt.delta;
                            Vector3 rotDir = Camera.current.transform.TransformDirection(new Vector3(-evt.delta.y, -evt.delta.x, 0));
                            rotation = Quaternion.AngleAxis(evt.delta.magnitude, rotDir.normalized) * rotation;
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

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    Handles.DrawWireDisc(worldPosition, Camera.current.transform.forward, size);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return rotation;
        }
    }
}
