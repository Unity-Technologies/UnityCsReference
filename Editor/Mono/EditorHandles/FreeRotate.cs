// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class FreeRotate
    {
        static readonly Color s_DimmingColor = new Color(0f, 0f, 0f, 0.078f);
        private static Vector2 s_CurrentMousePosition;

        public static Quaternion Do(int id, Quaternion rotation, Vector3 position, float size)
        {
            return Do(id, rotation, position, size, true);
        }

        internal static Quaternion Do(int id, Quaternion rotation, Vector3 position, float size, bool drawCircle)
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix = Handles.matrix;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldPosition, size) + HandleUtility.kPickDistance);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
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
                case EventType.MouseDrag:
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
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        Tools.UnlockHandlePosition();
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;
                case EventType.MouseMove:
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
                case EventType.Repaint:
                    Color temp = Color.white;
                    var isHot = id == GUIUtility.hotControl;
                    var isPreselected = id == HandleUtility.nearestControl && GUIUtility.hotControl == 0;

                    if (isHot)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }
                    else if (isPreselected)
                    {
                        temp = Handles.color;
                        Handles.color = Handles.preselectionColor;
                    }

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    if (drawCircle)
                        Handles.DrawWireDisc(worldPosition, Camera.current.transform.forward, size);
                    if (isPreselected || isHot)
                    {
                        Handles.color = s_DimmingColor;
                        Handles.DrawSolidDisc(worldPosition, Camera.current.transform.forward, size);
                    }
                    Handles.matrix = origMatrix;

                    if (isHot || isPreselected)
                        Handles.color = temp;
                    break;
            }
            return rotation;
        }
    }
}
