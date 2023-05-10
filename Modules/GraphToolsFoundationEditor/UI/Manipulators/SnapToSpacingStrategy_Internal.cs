// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class SnapToSpacingStrategy_Internal : SnapStrategy
    {
        class SnapToSpacingResult
        {
            public float Offset { get; set; }
            public float Distance => Math.Abs(Offset);
            public ReferenceRects ReferenceRects;
        }

        struct ReferenceRects
        {
            public List<Rect> Rects;
            public PortOrientation Orientation;
        }

        class SpacingLine
        {
            public const float DefaultSpacingLineSideLength = 10f;
            public Line_Internal StartSideLine;
            public Line_Internal LineInBetween;
            public Line_Internal EndSideLine;

            public List<Line_Internal> Lines => new List<Line_Internal> { StartSideLine, EndSideLine, LineInBetween };
        }

        static Vector2 GetMaxPos(Rect rect, SnapReference_Internal reference)
        {
            switch (reference)
            {
                case SnapReference_Internal.LeftWire:
                    return new Vector2(rect.x, rect.yMax);
                case SnapReference_Internal.HorizontalCenter:
                    return new Vector2(rect.center.x, rect.yMax);
                case SnapReference_Internal.RightWire:
                    return new Vector2(rect.xMax, rect.yMax);
                case SnapReference_Internal.TopWire:
                    return new Vector2(rect.xMax, rect.y);
                case SnapReference_Internal.VerticalCenter:
                    return new Vector2(rect.xMax, rect.center.y);
                case SnapReference_Internal.BottomWire:
                    return new Vector2(rect.xMax, rect.yMax);
                default:
                    return Vector2.zero;
            }
        }

        LineView_Internal m_LineView;
        Dictionary<float, ReferenceRects> m_SpacingPositions = new Dictionary<float, ReferenceRects>();

        ReferenceRects m_VerticalReferenceRects = new ReferenceRects
        {
            Rects = new List<Rect>(),
            Orientation = PortOrientation.Vertical
        };

        ReferenceRects m_HorizontalReferenceRects = new ReferenceRects
        {
            Rects = new List<Rect>(),
            Orientation = PortOrientation.Horizontal
        };

        public override void BeginSnap(GraphElement selectedElement)
        {
            base.BeginSnap(selectedElement);

            var graphView = selectedElement.GraphView;
            if (m_LineView == null)
            {
                m_LineView = new LineView_Internal(graphView);
            }
            graphView.Add(m_LineView);
        }

        protected override Vector2 ComputeSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement)
        {
            Rect selectedElementRect = selectedElement.parent.ChangeCoordinatesTo(selectedElement.GraphView.ContentViewContainer, selectedElement.layout);
            UpdateSpacingPositions(selectedElement, selectedElementRect);

            var snappedPosition = sourceRect.position;

            m_LineView.lines.Clear();

            List<SnapToSpacingResult> results = GetClosestSpacingPositions(sourceRect);

            snapDirection = SnapDirection.SnapNone;
            foreach (var result in results.Where(result => result != null))
            {
                ApplySnapToSpacingResult(ref snapDirection, sourceRect.position, ref snappedPosition, result);

                // Make sure the element is snapped before drawing the lines
                if (IsSnapped(snappedPosition, sourceRect.position, result.ReferenceRects.Orientation))
                {
                    foreach (SpacingLine spacingLine in GetSpacingLines(result.ReferenceRects.Rects, result.ReferenceRects.Orientation))
                    {
                        m_LineView.lines.AddRange(spacingLine.Lines);
                    }
                }
            }
            m_LineView.MarkDirtyRepaint();

            return snappedPosition;
        }

        public override void EndSnap()
        {
            base.EndSnap();

            ClearRectsToConsider();

            m_LineView.lines.Clear();
            m_LineView.Clear();
            m_LineView.RemoveFromHierarchy();
        }

        /// <inheritdoc />
        public override void PauseSnap(bool isPaused)
        {
            base.PauseSnap(isPaused);

            if (IsPaused)
            {
                ClearSnapLines();
            }
        }

        bool IsSnapped(Vector2 snappedRect, Vector2 sourcePosition, PortOrientation orientation)
        {
            float draggedDistance = Math.Abs(orientation == PortOrientation.Horizontal ? snappedRect.x - sourcePosition.x : snappedRect.y - sourcePosition.y);

            return draggedDistance < SnapDistance - 1;
        }

        void ClearRectsToConsider()
        {
            m_HorizontalReferenceRects.Rects.Clear();
            m_VerticalReferenceRects.Rects.Clear();
            m_SpacingPositions.Clear();
        }

        void UpdateSpacingPositions(GraphElement selectedElement, Rect sourceRect)
        {
            ClearRectsToConsider();
            GetRectsToConsiderInView(selectedElement);
            SortReferenceRects();
            ComputeSpacingPositions(m_VerticalReferenceRects, sourceRect);
            ComputeSpacingPositions(m_HorizontalReferenceRects, sourceRect);
        }

        static readonly List<ChildView> k_GetRectsToConsiderInViewAllUIs = new();
        void GetRectsToConsiderInView(GraphElement selectedElement)
        {
            var graphView = selectedElement.GraphView;
            // Consider only the visible nodes.
            Rect rectToFit = graphView.layout;

            graphView.GraphModel.GraphElementModels.GetAllViewsInList_Internal(graphView, null, k_GetRectsToConsiderInViewAllUIs);
            foreach (GraphElement element in k_GetRectsToConsiderInViewAllUIs.OfType<GraphElement>())
            {
                if (!IsIgnoredElement(selectedElement, element, rectToFit))
                {
                    Rect geometryInContentViewContainerSpace = element.parent.ChangeCoordinatesTo(graphView.ContentViewContainer, element.layout);
                    AddReferenceRects(selectedElement, element, geometryInContentViewContainerSpace);
                }
            }

            k_GetRectsToConsiderInViewAllUIs.Clear();
        }

        bool IsIgnoredElement(GraphElement selectedElement, GraphElement element, Rect rectToFit)
        {
            if (selectedElement is Placemat placemat && element.layout.Overlaps(placemat.layout) || element is Wire || !element.visible
                || element.IsSelected() || element.layout.Overlaps(selectedElement.layout))
            {
                return true;
            }

            Rect localSelRect = selectedElement.GraphView.ChangeCoordinatesTo(element, rectToFit);
            return !element.Overlaps(localSelRect);
        }

        void AddReferenceRects(GraphElement selectedElement, GraphElement element, Rect rectToAdd)
        {
            // Check if element is within selectedElement's vertical boundaries before adding
            if (AreElementsSuperposed(selectedElement.layout, element.layout, PortOrientation.Horizontal))
            {
                m_VerticalReferenceRects.Rects.Add(rectToAdd);
            }
            // Check if element is within selectedElement's horizontal boundaries before adding
            if (AreElementsSuperposed(selectedElement.layout, element.layout, PortOrientation.Vertical))
            {
                m_HorizontalReferenceRects.Rects.Add(rectToAdd);
            }
        }

        void SortReferenceRects()
        {
            // We want to iterate through rects from the start to the end (xMin to xMax OR yMin to yMax depending on the orientation)
            m_VerticalReferenceRects.Rects.Sort((rectA, rectB) => rectA.yMax.CompareTo(rectB.yMax));
            m_HorizontalReferenceRects.Rects.Sort((rectA, rectB) => rectA.xMax.CompareTo(rectB.xMax));
        }

        void ComputeSpacingPositions(ReferenceRects referenceRects, Rect sourceRect)
        {
            SnapReference_Internal startReference = referenceRects.Orientation == PortOrientation.Vertical ? SnapReference_Internal.TopWire : SnapReference_Internal.LeftWire;
            SnapReference_Internal endReference = referenceRects.Orientation == PortOrientation.Vertical ? SnapReference_Internal.BottomWire : SnapReference_Internal.RightWire;

            for (int i = 0; i < referenceRects.Rects.Count; ++i)
            {
                Rect firstRect = referenceRects.Rects[i];
                int nextRectIndex = i + 1; // After rect i is done, we don't consider it anymore for the next iterations

                for (int j = nextRectIndex; j < referenceRects.Rects.Count; ++j)
                {
                    Rect secondRect = referenceRects.Rects[j];

                    // For each rect i, we find the 3 spacing positions: (examples are for horizontal orientation)
                    //        - 1. position before rect i
                    //              +-----+    +-----+    +-----+
                    //              | pos |    |  i  |    |  j  |
                    //              +-----+    +-----+    +-----+
                    //        - 2. position between rect i and rect j
                    //              +-----+    +-----+    +-----+
                    //              |  i  |    | pos |    |  j  |
                    //              +-----+    +-----+    +-----+
                    //        - 3. position after rect j
                    //              +-----+    +-----+    +-----+
                    //              |  i  |    |  j  |    | pos |
                    //              +-----+    +-----+    +-----+

                    List<float> spacingPositions = GetSpacingPositions(sourceRect, firstRect, secondRect, startReference, endReference, referenceRects.Orientation);
                    AddSpacingPositions(spacingPositions, sourceRect, firstRect, secondRect, referenceRects.Orientation);
                }
            }
        }

        static bool AreElementsSuperposed(Rect firstElementRect, Rect secondElementRect, PortOrientation orientation)
        {
            if (orientation == PortOrientation.Vertical)
            {
                if (firstElementRect.yMin < secondElementRect.yMax && firstElementRect.yMax > secondElementRect.yMin)
                {
                    return true;
                }
            }
            else
            {
                if (firstElementRect.xMin < secondElementRect.xMax && firstElementRect.xMax > secondElementRect.xMin)
                {
                    return true;
                }
            }

            return false;
        }

        static List<float> GetSpacingPositions(Rect sourceRect, Rect firstRect, Rect secondRect, SnapReference_Internal startReference, SnapReference_Internal endReference, PortOrientation orientation)
        {
            if (AreElementsSuperposed(firstRect, secondRect, orientation))
            {
                return null;
            }

            Vector2 firstRectStartPos = GetMaxPos(firstRect, startReference);
            Vector2 firstRectEndPos = GetMaxPos(firstRect, endReference);
            Vector2 secondRectStartPos = GetMaxPos(secondRect, startReference);
            Vector2 secondRectEndPos = GetMaxPos(secondRect, endReference);

            List<float> positions = orientation == PortOrientation.Vertical ?
                ComputeSpacingPositions(firstRectStartPos.y, firstRectEndPos.y, secondRectStartPos.y, secondRectEndPos.y, sourceRect.height * 0.5f) :
                ComputeSpacingPositions(firstRectStartPos.x, firstRectEndPos.x, secondRectStartPos.x, secondRectEndPos.x, sourceRect.width * 0.5f);

            return positions;
        }

        static List<float> ComputeSpacingPositions(float firstRectStartPos, float firstRectEndPos, float secondRectStartPos, float secondRectEndPos, float sourceRectOffset)
        {
            List<float> positions = new List<float>(3); // There are always 3 positions

            float distance = Math.Abs(firstRectEndPos - secondRectStartPos);
            float offset = distance + sourceRectOffset;

            // Position before firstRect
            positions.Add(firstRectStartPos - offset);

            // Position between firstRect and secondRect
            positions.Add(firstRectEndPos + distance * 0.5f);

            // Position after secondRect
            positions.Add(secondRectEndPos + offset);

            return positions;
        }

        void AddSpacingPositions(List<float> positions, Rect sourceRect, Rect firstRect, Rect secondRect, PortOrientation orientation)
        {
            if (positions != null)
            {
                // Position before firstRect
                AddSpacingPosition(positions[0], new ReferenceRects
                {
                    Rects = new List<Rect> { sourceRect, firstRect, secondRect },
                    Orientation = orientation
                });

                // Position between firstRect and secondRect
                AddSpacingPosition(positions[1], new ReferenceRects
                {
                    Rects = new List<Rect> { firstRect, sourceRect, secondRect },
                    Orientation = orientation
                });

                // Position after secondRect
                AddSpacingPosition(positions[2], new ReferenceRects
                {
                    Rects = new List<Rect> { firstRect, secondRect, sourceRect },
                    Orientation = orientation
                });
            }
        }

        void AddSpacingPosition(float spacingPos, ReferenceRects referenceRects)
        {
            if (!m_SpacingPositions.ContainsKey(spacingPos))
            {
                m_SpacingPositions.Add(spacingPos, referenceRects);
            }
        }

        SnapToSpacingResult GetClosestSpacingPosition(Rect sourceRect, PortOrientation orientation)
        {
            SnapToSpacingResult minResult = null;
            float minDistance = float.MaxValue;

            foreach (var spacingPos in m_SpacingPositions.Where(spacingPos => spacingPos.Value.Orientation == orientation))
            {
                SnapToSpacingResult result = GetSnapToSpacingResult(sourceRect, spacingPos.Key, spacingPos.Value);
                if (result != null && minDistance > result.Distance)
                {
                    minDistance = result.Distance;
                    minResult = result;
                }
            }

            return minResult;
        }

        List<SnapToSpacingResult> GetClosestSpacingPositions(Rect sourceRect)
        {
            List<SnapToSpacingResult> results = new List<SnapToSpacingResult>();

            SnapToSpacingResult horizontalResult = GetClosestSpacingPosition(sourceRect, PortOrientation.Horizontal);
            if (horizontalResult != null)
            {
                results.Add(horizontalResult);
            }

            SnapToSpacingResult verticalResult = GetClosestSpacingPosition(sourceRect, PortOrientation.Vertical);
            if (verticalResult != null)
            {
                results.Add(verticalResult);
            }

            return results;
        }

        SnapToSpacingResult GetSnapToSpacingResult(Rect sourceRect, float middlePos, ReferenceRects referenceRects)
        {
            float sourceRectCenter = referenceRects.Orientation == PortOrientation.Vertical ? GetMaxPos(sourceRect, SnapReference_Internal.VerticalCenter).y : GetMaxPos(sourceRect, SnapReference_Internal.HorizontalCenter).x;
            float offset = sourceRectCenter - middlePos;

            SnapToSpacingResult minResult = new SnapToSpacingResult
            {
                Offset = offset,
                ReferenceRects = referenceRects
            };

            return minResult.Distance <= SnapDistance ? minResult : null;
        }

        static SpacingLine GetSpacingLine(float maxCoordinate, float spacingLineSideLength, Vector2 startPos, Vector2 endPos, PortOrientation orientation)
        {
            // Start side's line of spacingLine
            Vector2 start = orientation == PortOrientation.Vertical ? new Vector2(maxCoordinate, startPos.y) : new Vector2(startPos.x, maxCoordinate);
            Vector2 end = startPos;
            Line_Internal startSideLine = new Line_Internal(start, end);

            // Line in between of spacingLine
            float linePos = maxCoordinate - spacingLineSideLength * 0.5f;

            start = orientation == PortOrientation.Vertical ? new Vector2(linePos, startPos.y) : new Vector2(startPos.x, linePos);
            end = orientation == PortOrientation.Vertical ? new Vector2(linePos, endPos.y) : new Vector2(endPos.x, linePos);
            Line_Internal lineInBetween = new Line_Internal(start, end);

            // End side's line of spacingLine
            start = orientation == PortOrientation.Vertical ? new Vector2(maxCoordinate, endPos.y) : new Vector2(endPos.x, maxCoordinate);
            end = endPos;
            Line_Internal endSideLine = new Line_Internal(start, end);

            return new SpacingLine
            {
                StartSideLine = startSideLine,
                LineInBetween = lineInBetween,
                EndSideLine = endSideLine
            };
        }

        List<SpacingLine> GetSpacingLines(List<Rect> rects, PortOrientation orientation)
        {
            SnapReference_Internal startReference = orientation == PortOrientation.Vertical ? SnapReference_Internal.BottomWire : SnapReference_Internal.RightWire;
            SnapReference_Internal endReference = orientation == PortOrientation.Vertical ? SnapReference_Internal.TopWire : SnapReference_Internal.LeftWire;

            float maxCoordinate = rects.Max(rect => orientation == PortOrientation.Vertical ? rect.xMax : rect.yMax) + SpacingLine.DefaultSpacingLineSideLength;
            float spacingLineSideLength = SpacingLine.DefaultSpacingLineSideLength;

            Vector2 firstSidePos = GetMaxPos(rects[0], startReference);
            Vector2 secondSidePos = GetMaxPos(rects[1], endReference);
            Vector2 thirdSidePos = GetMaxPos(rects[1], startReference);
            Vector2 fourthSidePos = GetMaxPos(rects[2], endReference);

            return new List<SpacingLine>
            {
                GetSpacingLine(maxCoordinate, spacingLineSideLength, firstSidePos, secondSidePos, orientation),
                GetSpacingLine(maxCoordinate, spacingLineSideLength, thirdSidePos, fourthSidePos, orientation)
            };
        }

        static void ApplySnapToSpacingResult(ref SnapDirection snapDirection, Vector2 sourceRect, ref Vector2 r1, SnapToSpacingResult result)
        {
            if (result.ReferenceRects.Orientation == PortOrientation.Horizontal)
            {
                r1.x = sourceRect.x - result.Offset;
                snapDirection |= SnapDirection.SnapX;
            }
            else
            {
                r1.y = sourceRect.y - result.Offset;
                snapDirection |= SnapDirection.SnapY;
            }
        }

        void ClearSnapLines()
        {
            m_LineView.lines.Clear();
            m_LineView.MarkDirtyRepaint();
        }
    }
}
