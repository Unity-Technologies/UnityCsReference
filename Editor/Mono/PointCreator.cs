// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    internal interface ICreatablePoint
    {
        void AddPositions(List<Vector3> newPositions);
    }

    internal class PointCreator
    {
        private static List<Vector3> s_CreationPoints = new List<Vector3>();
        private static bool s_IsCreating;

        private static bool GetCreationPoint(out Vector3 position, bool useRaycast, LayerMask raycastMask, float raycastNormalOffset)
        {
            position = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (useRaycast)
            {
                position.y = Screen.height - position.y;
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit, float.MaxValue, raycastMask))
                {
                    position = hit.point;
                    if (!Mathf.Approximately(raycastNormalOffset, 0.0f))
                    {
                        var offset = new Vector3(hit.normal.x * raycastNormalOffset, hit.normal.y * raycastNormalOffset, hit.normal.z * raycastNormalOffset);
                        position += offset;
                    }
                }
                else
                    return false;
            }
            else
            {
                var offset = new Vector3(ray.direction.x * raycastNormalOffset, ray.direction.y * raycastNormalOffset, ray.direction.z * raycastNormalOffset);
                position = ray.origin + offset;
            }
            return true;
        }

        public static void CreatePoints(ICreatablePoint pointCreator, bool allowDrawing, bool useRaycast, LayerMask raycastMask, float raycastNormalOffset, float minDistanceBetweenPoints)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);

            if (Event.current.alt && Event.current.type != EventType.Repaint)
                return;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        s_IsCreating = true;
                        s_CreationPoints.Clear();
                        Vector3 pos;
                        if (GetCreationPoint(out pos, useRaycast, raycastMask, raycastNormalOffset))
                        {
                            s_CreationPoints.Add(pos);
                        }

                        // Use the mouse down event so no other controls get them
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    // If we got the mousedown event, the mouseup is ours as well - this is where we clean up.
                    if (evt.button == 0)
                    {
                        s_IsCreating = false;

                        // Selection has changed. set GUI.changed to true so caller can react (e.g. repaint inspector).
                        GUI.changed = true;

                        pointCreator.AddPositions(s_CreationPoints);

                        s_CreationPoints.Clear();
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (s_IsCreating && allowDrawing)
                    {
                        Vector3 pos;
                        if (GetCreationPoint(out pos, useRaycast, raycastMask, raycastNormalOffset))
                        {
                            if (s_CreationPoints.Count == 0)
                            {
                                s_CreationPoints.Add(pos);
                            }
                            else if (Vector3.Distance(s_CreationPoints[s_CreationPoints.Count - 1], pos) >= minDistanceBetweenPoints)
                            {
                                s_CreationPoints.Add(pos);
                            }
                        }

                        evt.Use();
                    }
                    break;
            }
        }

        public static void Draw()
        {
            if (s_CreationPoints.Count > 0)
            {
                Handles.color = Color.yellow;
                Handles.DrawAAPolyLine(15, s_CreationPoints.Count, s_CreationPoints.ToArray());
            }
        }
    }
}
