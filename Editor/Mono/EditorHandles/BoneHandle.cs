// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    public sealed partial class Handles
    {
        private const float k_BoneThickness = 0.08f;

        internal static float DistanceToPolygone(Vector3[] vertices)
        {
            return HandleUtility.DistanceToPolyLine(vertices);
        }

        internal static void DoBoneHandle(Transform target)
        {
            DoBoneHandle(target, null);
        }

        internal static void DoBoneHandle(Transform target, Dictionary<Transform, bool> validBones)
        {
            int id = target.name.GetHashCode();
            Event evt = Event.current;

            bool hasValidChildBones = false;
            if (validBones != null)
            {
                foreach (Transform child in target)
                {
                    if (validBones.ContainsKey(child))
                    {
                        hasValidChildBones = true;
                        break;
                    }
                }
            }

            Vector3 basePoint = target.position;

            List<Vector3> endPoints = new List<Vector3>();
            // [case 525602] do not draw root.
            if (!hasValidChildBones && target.parent != null)
            {
                endPoints.Add(target.position + (target.position - target.parent.position) * 0.4f);
            }
            else
            {
                foreach (Transform child in target)
                {
                    // Only render bone connections to valid bones
                    // (except if no child bones are valid - then draw all connections)
                    if (validBones != null && !validBones.ContainsKey(child))
                        continue;

                    endPoints.Add(child.position);
                }
            }

            for (int i = 0; i < endPoints.Count; i++)
            {
                Vector3 endPoint = endPoints[i];


                switch (evt.GetTypeForControl(id))
                {
                    case EventType.Layout:
                    {
                        float len = Vector3.Magnitude(endPoint - basePoint);
                        float size = len * k_BoneThickness;
                        Vector3[] vertices = GetBoneVertices(endPoint, basePoint, size);

                        HandleUtility.AddControl(id, DistanceToPolygone(vertices));

                        break;
                    }
                    case EventType.MouseMove:
                        if (id == HandleUtility.nearestControl)
                            HandleUtility.Repaint();
                        break;
                    case EventType.MouseDown:
                    {
                        // am I closest to the thingy?
                        if (!evt.alt  && HandleUtility.nearestControl == id && evt.button == 0)
                        {
                            GUIUtility.hotControl = id; // Grab mouse focus
                            if (evt.shift)
                            {
                                Object[] selected = Selection.objects;
                                if (ArrayUtility.Contains(selected, target) == false)
                                {
                                    ArrayUtility.Add(ref selected, target);
                                    Selection.objects = selected;
                                }
                            }
                            else
                                Selection.activeObject = target;

                            EditorGUIUtility.PingObject(target);

                            evt.Use();
                        }
                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if (!evt.alt && GUIUtility.hotControl == id)
                        {
                            DragAndDrop.PrepareStartDrag();
                            DragAndDrop.objectReferences = new UnityEngine.Object[] {target};
                            DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(target));

                            // having a hot control set during drag makes the control eat the drag events
                            // and dragging of bones no longer works over the avatar configure window
                            // see case 912016
                            GUIUtility.hotControl = 0;

                            evt.Use();
                        }
                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                        {
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                    case EventType.Repaint:
                    {
                        float len = Vector3.Magnitude(endPoint - basePoint);
                        if (len > 0)
                        {
                            color = GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id ? Handles.preselectionColor : color;

                            // size used to be based on sqrt of length but that makes bones for
                            // huge creatures hair-thin and bones for tiny creatures bulky.
                            // So base on a fixed proportion instead.
                            float size = len * k_BoneThickness;
                            if (hasValidChildBones)
                                Handles.DrawBone(endPoint, basePoint, size);
                            else
                                Handles.SphereHandleCap(id, basePoint, target.rotation, size * .2f, EventType.Repaint);
                        }
                        break;
                    }
                }
            }
        }

        internal static void DrawBone(Vector3 endPoint, Vector3 basePoint, float size)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            const int sConeSides = 3;

            Vector3[] vertices = GetBoneVertices(endPoint, basePoint, size);

            HandleUtility.ApplyWireMaterial();

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            for (int i = 0; i < sConeSides; i++)
            {
                GL.Vertex(vertices[i * 6 + 0]);
                GL.Vertex(vertices[i * 6 + 1]);
                GL.Vertex(vertices[i * 6 + 2]);

                GL.Vertex(vertices[i * 6 + 3]);
                GL.Vertex(vertices[i * 6 + 4]);
                GL.Vertex(vertices[i * 6 + 5]);
            }
            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(color * new Color(1, 1, 1, 0) + new Color(0, 0, 0, 1.0f));
            for (int i = 0; i < sConeSides; i++)
            {
                GL.Vertex(vertices[i * 6 + 0]);
                GL.Vertex(vertices[i * 6 + 1]);

                GL.Vertex(vertices[i * 6 + 1]);
                GL.Vertex(vertices[i * 6 + 2]);

                // No need to draw the third edge of the triangle; it's drawn by the triangles that shares the edge.
            }
            GL.End();
        }

        internal static Vector3[] GetBoneVertices(Vector3 endPoint, Vector3 basePoint, float radius)
        {
            const int sConeSides = 3;

            Vector3 direction = Vector3.Normalize(endPoint - basePoint);
            Vector3 tangent = Vector3.Cross(direction, Vector3.up);
            if (Vector3.SqrMagnitude(tangent) < 0.1f)
                tangent = Vector3.Cross(direction, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(direction, tangent);

            Vector3[] vertices = new Vector3[sConeSides * 6];
            const float kDelta = Mathf.PI * 2.0f / sConeSides;
            float phi = 0.0f;
            for (int i = 0; i < sConeSides; ++i)
            {
                float cs1 = Mathf.Cos(phi);
                float ss1 = Mathf.Sin(phi);
                float cs2 = Mathf.Cos(phi + kDelta);
                float ss2 = Mathf.Sin(phi + kDelta);
                Vector3 p1 = basePoint + tangent * (cs1 * radius) + bitangent * (ss1 * radius);
                Vector3 p2 = basePoint + tangent * (cs2 * radius) + bitangent * (ss2 * radius);

                // triangle of the cone
                vertices[i * 6 + 0] = endPoint;
                vertices[i * 6 + 1] = p1;
                vertices[i * 6 + 2] = p2;

                // triangle of the base point disk
                vertices[i * 6 + 3] = basePoint;
                vertices[i * 6 + 4] = p2;
                vertices[i * 6 + 5] = p1;
                phi += kDelta;
            }

            return vertices;
        }
    }
}
