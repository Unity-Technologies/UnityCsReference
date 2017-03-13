// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEngine
{
    public sealed partial class RectTransformUtility
    {
        private static Vector3[] s_Corners = new Vector3[4];

        private RectTransformUtility() {}

        public static bool RectangleContainsScreenPoint(RectTransform rect, Vector2 screenPoint)
        {
            return RectangleContainsScreenPoint(rect, screenPoint, null);
        }

        public static bool ScreenPointToWorldPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector3 worldPoint)
        {
            worldPoint = Vector2.zero;
            Ray ray = ScreenPointToRay(cam, screenPoint);
            var plane = new Plane(rect.rotation * Vector3.back, rect.position);

            float dist;
            if (!plane.Raycast(ray, out dist))
                return false;

            worldPoint = ray.GetPoint(dist);
            return true;
        }

        public static bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 screenPoint, Camera cam, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            Vector3 worldPoint;
            if (ScreenPointToWorldPointInRectangle(rect, screenPoint, cam, out worldPoint))
            {
                localPoint = rect.InverseTransformPoint(worldPoint);
                return true;
            }
            return false;
        }

        public static Ray ScreenPointToRay(Camera cam, Vector2 screenPos)
        {
            if (cam != null)
                return cam.ScreenPointToRay(screenPos);

            Vector3 pos = screenPos;
            pos.z -= 100f;
            return new Ray(pos, Vector3.forward);
        }

        public static Vector2 WorldToScreenPoint(Camera cam, Vector3 worldPoint)
        {
            if (cam == null)
                return new Vector2(worldPoint.x, worldPoint.y);

            return cam.WorldToScreenPoint(worldPoint);
        }

        public static Bounds CalculateRelativeRectTransformBounds(Transform root, Transform child)
        {
            RectTransform[] rects = child.GetComponentsInChildren<RectTransform>(false);

            if (rects.Length > 0)
            {
                Vector3 vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                Matrix4x4 toLocal = root.worldToLocalMatrix;

                for (int i = 0, imax = rects.Length; i < imax; i++)
                {
                    rects[i].GetWorldCorners(s_Corners);
                    for (int j = 0; j < 4; j++)
                    {
                        Vector3 v = toLocal.MultiplyPoint3x4(s_Corners[j]);
                        vMin = Vector3.Min(v, vMin);
                        vMax = Vector3.Max(v, vMax);
                    }
                }

                Bounds b = new Bounds(vMin, Vector3.zero);
                b.Encapsulate(vMax);
                return b;
            }
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        public static Bounds CalculateRelativeRectTransformBounds(Transform trans)
        {
            return CalculateRelativeRectTransformBounds(trans, trans);
        }

        public static void FlipLayoutOnAxis(RectTransform rect, int axis, bool keepPositioning, bool recursive)
        {
            if (rect == null)
                return;

            if (recursive)
            {
                for (int i = 0; i < rect.childCount; i++)
                {
                    RectTransform childRect = rect.GetChild(i) as RectTransform;
                    if (childRect != null)
                        FlipLayoutOnAxis(childRect, axis, false, true);
                }
            }

            Vector2 pivot = rect.pivot;
            pivot[axis] = 1.0f - pivot[axis];
            rect.pivot = pivot;

            if (keepPositioning)
                return;

            Vector2 anchoredPosition = rect.anchoredPosition;
            anchoredPosition[axis] = -anchoredPosition[axis];
            rect.anchoredPosition = anchoredPosition;

            Vector2 anchorMin = rect.anchorMin;
            Vector2 anchorMax = rect.anchorMax;
            float temp = anchorMin[axis];
            anchorMin[axis] = 1 - anchorMax[axis];
            anchorMax[axis] = 1 - temp;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
        }

        public static void FlipLayoutAxes(RectTransform rect, bool keepPositioning, bool recursive)
        {
            if (rect == null)
                return;

            if (recursive)
            {
                for (int i = 0; i < rect.childCount; i++)
                {
                    RectTransform childRect = rect.GetChild(i) as RectTransform;
                    if (childRect != null)
                        FlipLayoutAxes(childRect, false, true);
                }
            }

            rect.pivot = GetTransposed(rect.pivot);
            rect.sizeDelta = GetTransposed(rect.sizeDelta);

            if (keepPositioning)
                return;

            rect.anchoredPosition = GetTransposed(rect.anchoredPosition);
            rect.anchorMin = GetTransposed(rect.anchorMin);
            rect.anchorMax = GetTransposed(rect.anchorMax);
        }

        private static Vector2 GetTransposed(Vector2 input)
        {
            return new Vector2(input.y, input.x);
        }
    }
}
