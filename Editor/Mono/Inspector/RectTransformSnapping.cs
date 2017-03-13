// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class SnapGuideCollection
    {
        private List<SnapGuide> currentGuides = null;

        private Dictionary<float, List<SnapGuide>> guides = new Dictionary<float, List<SnapGuide>>();

        public void Clear()
        {
            guides.Clear();
        }

        public void AddGuide(SnapGuide guide)
        {
            List<SnapGuide> guideList;
            if (!guides.TryGetValue(guide.value, out guideList))
            {
                guideList = new List<SnapGuide>();
                guides.Add(guide.value, guideList);
            }
            guideList.Add(guide);
        }

        public float SnapToGuides(float value, float snapDistance)
        {
            if (guides.Count == 0)
                return value;

            KeyValuePair<float, List<SnapGuide>> snapPair = new KeyValuePair<float, List<SnapGuide>>();
            float closestDistance = Mathf.Infinity;
            foreach (var kvp in guides)
            {
                float distance = Mathf.Abs(value - kvp.Key);
                if (distance < closestDistance)
                {
                    snapPair = kvp;
                    closestDistance = distance;
                }
            }
            if (closestDistance <= snapDistance)
            {
                value = snapPair.Key;
                currentGuides = snapPair.Value;
            }
            else
            {
                currentGuides = null;
            }
            return value;
        }

        public void OnGUI()
        {
            if (Event.current.type == EventType.MouseUp)
                currentGuides = null;
        }

        public void DrawGuides()
        {
            if (currentGuides != null)
                foreach (SnapGuide guide in currentGuides)
                    guide.Draw();
        }
    }

    internal class SnapGuide
    {
        public float value;
        public List<Vector3> lineVertices = new List<Vector3>();
        public bool safe = true;

        public SnapGuide(float value, params Vector3[] vertices) : this(value, true, vertices) {}

        public SnapGuide(float value, bool safe, params Vector3[] vertices)
        {
            this.value = value;
            this.lineVertices.AddRange(vertices);
            this.safe = safe;
        }

        public void Draw()
        {
            Handles.color = safe ? new Color(0.0f, 0.5f, 1.0f, 1.0f) : new Color(1.0f, 0.5f, 0.0f, 1.0f);
            for (int i = 0; i < lineVertices.Count; i += 2)
            {
                Vector3 v1 = lineVertices[i];
                Vector3 v2 = lineVertices[i + 1];
                if (v1 == v2)
                    continue;
                Vector3 dir = (v2 - v1).normalized * 0.05f;
                v1 -= dir * HandleUtility.GetHandleSize(v1);
                v2 += dir * HandleUtility.GetHandleSize(v2);
                Handles.DrawLine(v1, v2);
            }
        }
    }

    internal class RectTransformSnapping
    {
        internal const float kSnapThreshold = 0.05f;
        private static SnapGuideCollection[] s_SnapGuides = new SnapGuideCollection[] { new SnapGuideCollection(), new SnapGuideCollection() };
        private static float[] kSidesAndMiddle = new float[] { 0, 0.5f, 1 };
        private static Vector3[] s_Corners = new Vector3[4];

        internal static void OnGUI()
        {
            s_SnapGuides[0].OnGUI();
            s_SnapGuides[1].OnGUI();
        }

        internal static void DrawGuides()
        {
            if (EditorGUI.actionKey)
                return;

            s_SnapGuides[0].DrawGuides();
            s_SnapGuides[1].DrawGuides();
        }

        static Vector3 GetInterpolatedCorner(Vector3[] corners, int mainAxis, float alongMainAxis, float alongOtherAxis)
        {
            if (mainAxis != 0)
            {
                float temp = alongMainAxis;
                alongMainAxis = alongOtherAxis;
                alongOtherAxis = temp;
            }
            return corners[0] * (1 - alongMainAxis) * (1 - alongOtherAxis)
                +  corners[1] * (1 - alongMainAxis) * (alongOtherAxis)
                +  corners[3] * (alongMainAxis) * (1 - alongOtherAxis)
                +  corners[2] * (alongMainAxis) * (alongOtherAxis);
        }

        internal static void CalculatePivotSnapValues(Rect rect, Vector3 pivot, Quaternion rotation)
        {
            for (int axis = 0; axis < 2; axis++)
            {
                s_SnapGuides[axis].Clear();

                // Snap to min, center, and max
                for (int i = 0; i < kSidesAndMiddle.Length; i++)
                    s_SnapGuides[axis].AddGuide(new SnapGuide(kSidesAndMiddle[i], GetGuideLineForRect(rect, pivot, rotation, axis, kSidesAndMiddle[i])));
            }
        }

        internal static void CalculateAnchorSnapValues(Transform parentSpace, Transform self, RectTransform gui, int minmaxX, int minmaxY)
        {
            for (int axis = 0; axis < 2; axis++)
            {
                s_SnapGuides[axis].Clear();

                // Snap to edges of parent
                RectTransform parentRect = parentSpace.GetComponent<RectTransform>();
                parentRect.GetWorldCorners(s_Corners);
                for (int i = 0; i < kSidesAndMiddle.Length; i++)
                {
                    float val = kSidesAndMiddle[i];
                    s_SnapGuides[axis].AddGuide(new SnapGuide(val,
                            GetInterpolatedCorner(s_Corners, axis, val, 0),
                            GetInterpolatedCorner(s_Corners, axis, val, 1)));
                }

                // Snap to sibling anchor values
                foreach (Transform tr in parentSpace)
                {
                    if (tr == self)
                        continue;
                    RectTransform sibling = tr.GetComponent<RectTransform>();
                    if (sibling)
                    {
                        s_SnapGuides[axis].AddGuide(new SnapGuide(sibling.anchorMin[axis]));
                        s_SnapGuides[axis].AddGuide(new SnapGuide(sibling.anchorMax[axis]));
                    }
                }

                // Snap to own opposite anchor values
                int minmax = (axis == 0 ? minmaxX : minmaxY);
                if (minmax == 0)
                    s_SnapGuides[axis].AddGuide(new SnapGuide(gui.anchorMax[axis]));
                if (minmax == 1)
                    s_SnapGuides[axis].AddGuide(new SnapGuide(gui.anchorMin[axis]));
            }
        }

        // handle values are: 0 = min, 1 = middle, 2 = max
        internal static void CalculateOffsetSnapValues(Transform parentSpace, Transform self, RectTransform parentRect, RectTransform rect, int xHandle, int yHandle)
        {
            for (int axis = 0; axis < 2; axis++)
                s_SnapGuides[axis].Clear();

            if (parentSpace == null)
                return;

            List<SnapGuide> guides;
            for (int axis = 0; axis < 2; axis++)
            {
                int handle = (axis == 0 ? xHandle : yHandle);
                if (handle == 1)
                    continue;

                guides = GetSnapGuides(parentSpace, self, parentRect, rect, axis, handle);
                foreach (SnapGuide guide in guides)
                {
                    s_SnapGuides[axis].AddGuide(guide);
                }
            }
        }

        internal static void CalculatePositionSnapValues(Transform parentSpace, Transform self, RectTransform parentRect, RectTransform rect)
        {
            for (int axis = 0; axis < 2; axis++)
                s_SnapGuides[axis].Clear();

            if (parentSpace == null)
                return;

            List<SnapGuide> guides;
            for (int axis = 0; axis < 2; axis++)
            {
                for (int side = 0; side < kSidesAndMiddle.Length; side++)
                {
                    guides = GetSnapGuides(parentSpace, self, parentRect, rect, axis, side);
                    foreach (SnapGuide guide in guides)
                    {
                        guide.value = GetGuideValueForRect(rect, guide.value, axis, kSidesAndMiddle[side]);
                        s_SnapGuides[axis].AddGuide(guide);
                    }
                }
            }
        }

        // side is: 0 = min, 1 = middle, 2 = max
        private static List<SnapGuide> GetSnapGuides(Transform parentSpace, Transform self, RectTransform parentRect, RectTransform rect, int axis, int side)
        {
            List<SnapGuide> guides = new List<SnapGuide>();

            // For snapping we have a concept of safe and unsafe snapping.
            //  - Safe snapping means the objects will remain aligned when the parent rect changes size.
            //  - Unsafe snapping means that the objects are aligned right now, but will no longer be aligned
            //    if the parent rect changes size because of different anchoring.

            if (parentRect != null)
            {
                float val = kSidesAndMiddle[side];

                // Snap min side to min anchor, center to center point, max side to max anchor - this is always safe
                float normalized = Mathf.Lerp(rect.anchorMin[axis], rect.anchorMax[axis], val);
                guides.Add(new SnapGuide(normalized * parentRect.rect.size[axis],
                        GetGuideLineForRect(parentRect, axis, normalized)));

                // Snap to sides and center of the parent rect (if not the same as the anchor).
                // This snapping is not safe.
                float anchorVal = Mathf.Lerp(rect.anchorMin[axis], rect.anchorMax[axis], val);
                if (val != anchorVal)
                {
                    guides.Add(new SnapGuide(val * parentRect.rect.size[axis], false,
                            GetGuideLineForRect(parentRect, axis, val)));
                }
            }

            // Snap to siblings
            foreach (Transform tr in parentSpace)
            {
                if (tr == self)
                    continue;
                RectTransform sibling = tr.GetComponent<RectTransform>();
                bool safe = true;
                if (sibling)
                {
                    if (side == 0)
                    {
                        // Snap min to min side
                        // This is safe if both objects have the same min anchor.
                        safe = (sibling.anchorMin[axis] == rect.anchorMin[axis]);
                        guides.Add(new SnapGuide(sibling.GetRectInParentSpace().min[axis], safe,
                                GetGuideLineForRect(sibling, axis, 0)));

                        // Snap min to max side
                        // This is safe if min anchor of this object is equal to max anchor of the sibling.
                        safe = (sibling.anchorMax[axis] == rect.anchorMin[axis]);
                        guides.Add(new SnapGuide(sibling.GetRectInParentSpace().max[axis], safe,
                                GetGuideLineForRect(sibling, axis, 1)));
                    }

                    if (side == 2)
                    {
                        // Snap max to max side
                        // This is safe if both objects have the same max anchor.
                        safe = (sibling.anchorMax[axis] == rect.anchorMax[axis]);
                        guides.Add(new SnapGuide(sibling.GetRectInParentSpace().max[axis], safe,
                                GetGuideLineForRect(sibling, axis, 1)));

                        // Snap max to min side
                        // This is safe if max anchor of this object is equal to min anchor of the sibling.
                        safe = (sibling.anchorMin[axis] == rect.anchorMax[axis]);
                        guides.Add(new SnapGuide(sibling.GetRectInParentSpace().min[axis], safe,
                                GetGuideLineForRect(sibling, axis, 0)));
                    }

                    if (side == 1)
                    {
                        // Snap middle to middle
                        // This is safe if the difference between the min anchors is the opposite of the difference between the max anchors.
                        // This also includes the case where both objects have the same min and max anchors.
                        safe = (sibling.anchorMin[axis] - rect.anchorMin[axis] == -(sibling.anchorMax[axis] - rect.anchorMax[axis]));
                        guides.Add(new SnapGuide(sibling.GetRectInParentSpace().center[axis], safe,
                                GetGuideLineForRect(sibling, axis, 0.5f)));
                    }
                }
            }

            return guides;
        }

        private static Vector3[] GetGuideLineForRect(RectTransform rect, int axis, float side)
        {
            Vector3[] points = new Vector3[2];

            // Set end points of line on other axis
            points[0][1 - axis] = rect.rect.min[1 - axis];
            points[1][1 - axis] = rect.rect.max[1 - axis];

            // Set position of line on axis
            points[0][axis] = Mathf.Lerp(rect.rect.min[axis], rect.rect.max[axis], side);
            points[1][axis] = points[0][axis];

            // Transform to world space
            points[0] = rect.transform.TransformPoint(points[0]);
            points[1] = rect.transform.TransformPoint(points[1]);

            return points;
        }

        private static Vector3[] GetGuideLineForRect(Rect rect, Vector3 pivot, Quaternion rotation, int axis, float side)
        {
            Vector3[] points = new Vector3[2];

            // Set end points of line on other axis
            points[0][1 - axis] = rect.min[1 - axis];
            points[1][1 - axis] = rect.max[1 - axis];

            // Set position of line on axis
            points[0][axis] = Mathf.Lerp(rect.min[axis], rect.max[axis], side);
            points[1][axis] = points[0][axis];

            // Transform to world space
            points[0] = rotation * points[0] + pivot;
            points[1] = rotation * points[1] + pivot;

            return points;
        }

        private static float GetGuideValueForRect(RectTransform rect, float value, int axis, float side)
        {
            RectTransform parentRect = rect.transform.parent.GetComponent<RectTransform>();
            float parentSize = parentRect ? parentRect.rect.size[axis] : 0;
            float anchorReference = Mathf.Lerp(rect.anchorMin[axis], rect.anchorMax[axis], rect.pivot[axis]) * parentSize;
            float positionRelativeToSide = rect.rect.size[axis] * (rect.pivot[axis] - side);
            return value - anchorReference + positionRelativeToSide;
        }

        internal static Vector2 SnapToGuides(Vector2 value, Vector2 snapDistance)
        {
            return new Vector2(
                SnapToGuides(value.x, snapDistance.x, 0),
                SnapToGuides(value.y, snapDistance.y, 1)
                );
        }

        internal static float SnapToGuides(float value, float snapDistance, int axis)
        {
            if (EditorGUI.actionKey)
                return value;

            SnapGuideCollection guides = (axis == 0 ? s_SnapGuides[0] : s_SnapGuides[1]);
            return guides.SnapToGuides(value, snapDistance);
        }
    }
}
