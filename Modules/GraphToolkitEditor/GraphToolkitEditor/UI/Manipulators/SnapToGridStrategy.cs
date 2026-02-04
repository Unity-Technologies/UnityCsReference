// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    class SnapToGridStrategy : SnapStrategy
    {
        class SnapToGridResult
        {
            public float Offset { get; set; }
            public float Distance => Math.Abs(Offset);
            public SnapReference SnappableReference { get; set; }
        }

        [UnityRestricted]
        internal struct BorderWidth
        {
            public float Top { get; set; }
            public float Bottom { get; set; }
            public float Left { get; set; }
            public float Right { get; set; }
        }

        float m_GridSpacing;
        BorderWidth m_BorderWidth;

        public override void BeginSnap(GraphElement selectedElement)
        {
            base.BeginSnap(selectedElement);

            m_BorderWidth = GetBorderWidth(selectedElement); // Needed to snap element on its content container's border

            m_GridSpacing = selectedElement.GraphView.SafeQ<GridBackground>().Spacing;
        }

        protected override Vector2 ComputeSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement)
        {
            var snappedPosition = sourceRect.position;

            List<SnapToGridResult> results = GetClosestGridLines(sourceRect);

            snapDirection = SnapDirection.SnapNone;
            foreach (SnapToGridResult result in results)
            {
                ApplySnapToGridResult(ref snapDirection, sourceRect.position, ref snappedPosition, result);
            }

            return snappedPosition;
        }

        SnapToGridResult GetClosestGridLine(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference endReference)
        {
            float sourcePos = GetPositionWithBorder(sourceRect, sourceRef);
            float offsetStart = sourcePos - GetClosestGridLine(sourceRect, startReference);
            float offsetEnd = sourcePos - GetClosestGridLine(sourceRect, endReference);
            float minOffset = offsetStart;

            SnapReference minSnappableReference = startReference;

            if (Math.Abs(minOffset) > Math.Abs(offsetEnd))
            {
                minOffset = offsetEnd;
                minSnappableReference = endReference;
            }

            SnapToGridResult minResult = new SnapToGridResult()
            {
                SnappableReference = minSnappableReference,
                Offset = minOffset
            };

            return minResult.Distance <= SnapDistance ? minResult : null;
        }

        List<SnapToGridResult> GetClosestGridLines(Rect sourceRect, PortOrientation orientation)
        {
            SnapReference startReference = orientation == PortOrientation.Horizontal ? SnapReference.LeftWire : SnapReference.TopWire;
            SnapReference centerReference = orientation == PortOrientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
            SnapReference endReference = orientation == PortOrientation.Horizontal ? SnapReference.RightWire : SnapReference.BottomWire;

            List<SnapToGridResult> results = new List<SnapToGridResult>(3);
            SnapToGridResult result = GetClosestGridLine(sourceRect, startReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestGridLine(sourceRect, centerReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestGridLine(sourceRect, endReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            // Look for the minimum
            if (results.Count > 0)
            {
                results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                float minDistance = results[0].Distance;
                results.RemoveAll(r => Math.Abs(r.Distance - minDistance) > 0.01f);
            }
            return results;
        }

        List<SnapToGridResult> GetClosestGridLines(Rect sourceRect)
        {
            List<SnapToGridResult> results = GetClosestGridLines(sourceRect, PortOrientation.Horizontal);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return results.Union(GetClosestGridLines(sourceRect, PortOrientation.Vertical)).ToList();
#pragma warning restore UA2001
        }

        static void ApplySnapToGridResult(ref SnapDirection snapDirection, Vector2 sourcePosition, ref Vector2 r1, SnapToGridResult result)
        {
            if (result.SnappableReference <= SnapReference.RightWire)
            {
                r1.x = sourcePosition.x - result.Offset;
                snapDirection |= SnapDirection.SnapX;
            }
            else
            {
                r1.y = sourcePosition.y - result.Offset;
                snapDirection |= SnapDirection.SnapY;
            }
        }

        public static BorderWidth GetBorderWidth(GraphElement element)
        {
            var borderWidth = new BorderWidth
            {
                Top = element.contentContainer.resolvedStyle.borderTopWidth,
                Bottom = element.contentContainer.resolvedStyle.borderBottomWidth,
                Left = element.contentContainer.resolvedStyle.borderLeftWidth,
                Right = element.contentContainer.resolvedStyle.borderRightWidth
            };

            return borderWidth;
        }

        float GetPositionWithBorder(Rect rect, SnapReference reference)
        {
            // We need to take account of the selected element's content container's border width to snap on it
            switch (reference)
            {
                case SnapReference.LeftWire:
                    return rect.x - m_BorderWidth.Left;
                case SnapReference.HorizontalCenter:
                    return rect.center.x;
                case SnapReference.RightWire:
                    return rect.xMax + m_BorderWidth.Right;
                case SnapReference.TopWire:
                    return rect.y - m_BorderWidth.Top;
                case SnapReference.VerticalCenter:
                    return rect.center.y;
                case SnapReference.BottomWire:
                    return rect.yMax + m_BorderWidth.Bottom;
                default:
                    return 0;
            }
        }

        float GetClosestGridLine(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftWire:
                    return GetClosestGridLine(rect.xMin);
                case SnapReference.HorizontalCenter:
                    return GetClosestGridLine(rect.center.x);
                case SnapReference.RightWire:
                    return GetClosestGridLine(rect.xMax);
                case SnapReference.TopWire:
                    return GetClosestGridLine(rect.yMin);
                case SnapReference.VerticalCenter:
                    return GetClosestGridLine(rect.center.y);
                case SnapReference.BottomWire:
                    return GetClosestGridLine(rect.yMax);
                default:
                    return 0;
            }
        }

        float GetClosestGridLine(float elementPosition)
        {
            // To find the closest grid line, we count the number of grid spacing to the selected element and we round it to the nearest grid line
            int spacingCount = (int)Math.Round(elementPosition / m_GridSpacing, 0);

            return spacingCount * m_GridSpacing;
        }
    }
}
