// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class FreeMove
    {
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        private static Vector3 s_StartPosition;

        // DrawCapFunction was marked plannned obsolete by @juha on 2016-03-16, marked obsolete warning by @adamm on 2016-12-21
        [Obsolete("DrawCapFunction is obsolete. Use the version with CapFunction instead. Example: Change SphereCap to SphereHandleCap.")]
        #pragma warning disable 618
        public static Vector3 Do(int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.DrawCapFunction capFunc)
        #pragma warning restore 618
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix = Handles.matrix;

            VertexSnapping.HandleKeyAndMouseMove(id);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldPosition, size * 1.2f));
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        bool rayDrag = EditorGUI.actionKey && evt.shift;

                        if (rayDrag)
                        {
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();

                            object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null)
                            {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                position = Handles.inverseMatrix.MultiplyPoint(rh.point + (rh.normal * offset));
                            }
                            else
                            {
                                rayDrag = false;
                            }
                        }

                        if (!rayDrag)
                        {
                            // normal drag
                            s_CurrentMousePosition += new Vector2(evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint(Camera.current.ScreenToWorldPoint(screenPos));

                            // Due to floating point inaccuracies, the back-and-forth transformations used may sometimes introduce
                            // tiny unintended movement in wrong directions. People notice when using a straight top/left/right ortho camera.
                            // In that case, just restrain the movement to the plane.
                            if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                                position.z = s_StartPosition.z;
                            if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                                position.y = s_StartPosition.y;
                            if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                                position.x = s_StartPosition.x;

                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near))
                                {
                                    position = Handles.inverseMatrix.MultiplyPoint(near);
                                }
                            }

                            if (EditorGUI.actionKey && !evt.shift)
                            {
                                Vector3 delta = position - s_StartPosition;
                                delta.x = Handles.SnapValue(delta.x, snap.x);
                                delta.y = Handles.SnapValue(delta.y, snap.y);
                                delta.z = Handles.SnapValue(delta.z, snap.z);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        HandleUtility.ignoreRaySnapObjects = null;
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

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    capFunc(id, worldPosition, Camera.current.transform.rotation, size);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }

        public static Vector3 Do(int id, Vector3 position, Quaternion rotation, float size, Vector3 snap, Handles.CapFunction handleFunction)
        {
            Vector3 worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix = Handles.matrix;

            VertexSnapping.HandleKeyAndMouseMove(id);

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction(id, worldPosition, Camera.current.transform.rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;
                    break;
                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartPosition = position;
                        HandleUtility.ignoreRaySnapObjects = null;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        bool rayDrag = EditorGUI.actionKey && evt.shift;

                        if (rayDrag)
                        {
                            if (HandleUtility.ignoreRaySnapObjects == null)
                                Handles.SetupIgnoreRaySnapObjects();

                            object hit = HandleUtility.RaySnap(HandleUtility.GUIPointToWorldRay(evt.mousePosition));
                            if (hit != null)
                            {
                                RaycastHit rh = (RaycastHit)hit;
                                float offset = 0;
                                if (Tools.pivotMode == PivotMode.Center)
                                {
                                    float geomOffset = HandleUtility.CalcRayPlaceOffset(HandleUtility.ignoreRaySnapObjects, rh.normal);
                                    if (geomOffset != Mathf.Infinity)
                                    {
                                        offset = Vector3.Dot(position, rh.normal) - geomOffset;
                                    }
                                }
                                position = Handles.inverseMatrix.MultiplyPoint(rh.point + (rh.normal * offset));
                            }
                            else
                            {
                                rayDrag = false;
                            }
                        }

                        if (!rayDrag)
                        {
                            // normal drag
                            s_CurrentMousePosition += new Vector2(evt.delta.x, -evt.delta.y) * EditorGUIUtility.pixelsPerPoint;
                            Vector3 screenPos = Camera.current.WorldToScreenPoint(Handles.matrix.MultiplyPoint(s_StartPosition));
                            screenPos += (Vector3)(s_CurrentMousePosition - s_StartMousePosition);
                            position = Handles.inverseMatrix.MultiplyPoint(Camera.current.ScreenToWorldPoint(screenPos));

                            // Due to floating point inaccuracies, the back-and-forth transformations used may sometimes introduce
                            // tiny unintended movement in wrong directions. People notice when using a straight top/left/right ortho camera.
                            // In that case, just restrain the movement to the plane.
                            if (Camera.current.transform.forward == Vector3.forward || Camera.current.transform.forward == -Vector3.forward)
                                position.z = s_StartPosition.z;
                            if (Camera.current.transform.forward == Vector3.up || Camera.current.transform.forward == -Vector3.up)
                                position.y = s_StartPosition.y;
                            if (Camera.current.transform.forward == Vector3.right || Camera.current.transform.forward == -Vector3.right)
                                position.x = s_StartPosition.x;

                            if (Tools.vertexDragging)
                            {
                                if (HandleUtility.ignoreRaySnapObjects == null)
                                    Handles.SetupIgnoreRaySnapObjects();
                                Vector3 near;
                                if (HandleUtility.FindNearestVertex(evt.mousePosition, null, out near))
                                {
                                    position = Handles.inverseMatrix.MultiplyPoint(near);
                                }
                            }

                            if (EditorGUI.actionKey && !evt.shift)
                            {
                                Vector3 delta = position - s_StartPosition;
                                delta.x = Handles.SnapValue(delta.x, snap.x);
                                delta.y = Handles.SnapValue(delta.y, snap.y);
                                delta.z = Handles.SnapValue(delta.z, snap.z);
                                position = s_StartPosition + delta;
                            }
                        }
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        HandleUtility.ignoreRaySnapObjects = null;
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

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction(id, worldPosition, Camera.current.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if (id == GUIUtility.hotControl || id == HandleUtility.nearestControl && GUIUtility.hotControl == 0)
                        Handles.color = temp;
                    break;
            }
            return position;
        }
    }
}
