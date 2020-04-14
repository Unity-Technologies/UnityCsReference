// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.UIElements.GraphView
{
    enum SnapReference
    {
        LeftEdge,
        HorizontalCenter,
        RightEdge,
        TopEdge,
        VerticalCenter,
        BottomEdge
    }

    class SnapResult
    {
        public Rect sourceRect { get; set; }
        public SnapReference sourceReference { get; set; }
        public Rect snappableRect { get; set; }
        public SnapReference snappableReference { get; set; }
        public float offset { get; set; }
        public float distance { get { return Math.Abs(offset); } }
        public Line2 indicatorLine;
        public SnapResult()
        {
        }
    }

    class SnapService
    {
        const float k_DefaultSnapDistance = 8.0f;
        float m_CurrentScale = 1.0f;
        List<Rect> m_SnappableRects = new List<Rect>();
        public bool active { get; private set; }
        public float snapDistance { get; set; }
        public SnapService()
        {
            snapDistance = k_DefaultSnapDistance;
        }

        internal static float GetPos(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    return rect.x;
                case SnapReference.HorizontalCenter:
                    return rect.center.x;
                case SnapReference.RightEdge:
                    return rect.xMax;
                case SnapReference.TopEdge:
                    return rect.y;
                case SnapReference.VerticalCenter:
                    return rect.center.y;
                case SnapReference.BottomEdge:
                    return rect.yMax;
                default:
                    return 0;
            }
        }

        virtual public void BeginSnap(List<Rect> snappableRects)
        {
            if (active)
            {
                throw new InvalidOperationException("SnapService.BeginSnap: Already active. Call EndSnap() first.");
            }
            active = true;
            m_SnappableRects = new List<Rect>(snappableRects);
        }

        public void UpdateSnapRects(List<Rect> snappableRects)
        {
            m_SnappableRects = snappableRects;
        }

        public Rect GetSnappedRect(Rect sourceRect, out List<SnapResult> results, float scale = 1.0f)
        {
            if (!active)
            {
                throw new InvalidOperationException("SnapService.GetSnappedRect: Already active. Call BeginSnap() first.");
            }
            Rect snappedRect = sourceRect;
            m_CurrentScale = scale;
            results = GetClosestSnapElements(sourceRect);
            foreach (SnapResult result in results)
            {
                ApplyResult(sourceRect, ref snappedRect, result);
            }
            foreach (SnapResult result in results)
            {
                result.indicatorLine = GetSnapLine(snappedRect, result.sourceReference, result.snappableRect, result.snappableReference);
            }
            return snappedRect;
        }

        virtual public void EndSnap()
        {
            if (!active)
            {
                throw new InvalidOperationException("SnapService.End: Already active. Call BeginSnap() first.");
            }
            m_SnappableRects.Clear();
            active = false;
        }

        SnapResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, Rect snappableRect, SnapReference startReference, SnapReference centerReference, SnapReference endReference)
        {
            float sourcePos = GetPos(sourceRect, sourceRef);
            float offsetStart = sourcePos - GetPos(snappableRect, startReference);
            float offsetEnd = sourcePos - GetPos(snappableRect, endReference);
            float minOffset = offsetStart;
            SnapReference minSnappableReference = startReference;

            if (Math.Abs(minOffset) > Math.Abs(offsetEnd))
            {
                minOffset = offsetEnd;
                minSnappableReference = endReference;
            }
            SnapResult minResult = new SnapResult
            {
                sourceRect = sourceRect,
                sourceReference = sourceRef,
                snappableRect = snappableRect,
                snappableReference = minSnappableReference,
                offset = minOffset
            };
            if (minResult.distance <= snapDistance * 1 / m_CurrentScale)
                return minResult;
            else
                return null;
        }

        SnapResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference centerReference, SnapReference endReference)
        {
            SnapResult minResult = null;
            float minDistance = float.MaxValue;
            foreach (Rect snappableRect in m_SnappableRects)
            {
                SnapResult result = GetClosestSnapElement(sourceRect, sourceRef, snappableRect, startReference, centerReference, endReference);
                if (result != null && minDistance > result.distance)
                {
                    minDistance = result.distance;
                    minResult = result;
                }
            }
            return minResult;
        }

        List<SnapResult> GetClosestSnapElements(Rect sourceRect, Orientation orientation)
        {
            SnapReference startReference = orientation == Orientation.Horizontal ? SnapReference.LeftEdge : SnapReference.TopEdge;
            SnapReference centerReference = orientation == Orientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
            SnapReference endReference = orientation == Orientation.Horizontal ? SnapReference.RightEdge : SnapReference.BottomEdge;
            List<SnapResult> results = new List<SnapResult>(3);
            SnapResult result = GetClosestSnapElement(sourceRect, startReference, startReference, centerReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, centerReference, startReference, centerReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, endReference, startReference, centerReference, endReference);
            if (result != null)
                results.Add(result);
            // Look for the minimum
            if (results.Count > 0)
            {
                results.Sort((a, b) => a.distance.CompareTo(b.distance));
                float minDistance = results[0].distance;
                results.RemoveAll(r => Math.Abs(r.distance - minDistance) > 0.01f);
            }
            return results;
        }

        List<SnapResult> GetClosestSnapElements(Rect sourceRect)
        {
            List<SnapResult> snapResults = GetClosestSnapElements(sourceRect, Orientation.Horizontal);
            return snapResults.Union(GetClosestSnapElements(sourceRect, Orientation.Vertical)).ToList();
        }

        Line2 GetSnapLine(Rect r, SnapReference reference)
        {
            Vector2 start = Vector2.zero,
                    end = Vector2.zero;
            switch (reference)
            {
                case SnapReference.LeftEdge:
                    start = r.position;
                    end = new Vector2(r.x, r.yMax);
                    break;
                case SnapReference.HorizontalCenter:
                    start = r.center;
                    end = start;
                    break;
                case SnapReference.RightEdge:
                    start = new Vector2(r.xMax, r.yMin);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
                case SnapReference.TopEdge:
                    start = r.position;
                    end = new Vector2(r.xMax, r.yMin);
                    break;
                case SnapReference.VerticalCenter:
                    start = r.center;
                    end = start;
                    break;
                default: // case SnapReference.BottomEdge:
                    start = new Vector2(r.x, r.yMax);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
            }
            return new Line2(start, end);
        }

        Line2 GetSnapLine(Rect r1, SnapReference reference1, Rect r2, SnapReference reference2)
        {
            bool horizontal = reference1 <= SnapReference.RightEdge;
            Line2 line1 = GetSnapLine(r1, reference1);
            Line2 line2 = GetSnapLine(r2, reference2);
            Vector2 p11 = line1.start
            , p12 = line1.end
            , p21 = line2.start
            , p22 = line2.end
            , start = Vector2.zero
            , end = Vector2.zero;
            if (horizontal)
            {
                float x = p21.x;
                float yMin = Math.Min(p22.y, Math.Min(p21.y, Math.Min(p11.y, p12.y)));
                float yMax = Math.Max(p22.y, Math.Max(p21.y, Math.Max(p11.y, p12.y)));
                start = new Vector2(x, yMin);
                end = new Vector2(x, yMax);
            }
            else
            {
                float y = p22.y;
                float xMin = Math.Min(p22.x, Math.Min(p21.x, Math.Min(p11.x, p12.x)));
                float xMax = Math.Max(p22.x, Math.Max(p21.x, Math.Max(p11.x, p12.x)));
                start = new Vector2(xMin, y);
                end = new Vector2(xMax, y);
            }
            return new Line2(start, end);
        }

        void ApplyResult(Rect sourceRect, ref Rect r1, SnapResult result)
        {
            if (result.snappableReference <= SnapReference.RightEdge)
                r1.x = sourceRect.x - result.offset;
            else
                r1.y = sourceRect.y - result.offset;
        }
    }
}
