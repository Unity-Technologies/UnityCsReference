// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    class SnapToBordersStrategy : SnapStrategy
    {
        class SnapToBordersResult
        {
            public Rect SnappableRect { get; set; }
            public float Offset { get; set; }
            public float Distance => Math.Abs(Offset);
            public SnapReference SourceReference { get; set; }
            public SnapReference SnappableReference { get; set; }
            public Line IndicatorLine;
        }

        static float GetPos(Rect rect, SnapReference reference)
        {
            switch (reference)
            {
                case SnapReference.LeftWire:
                    return rect.x;
                case SnapReference.HorizontalCenter:
                    return rect.center.x;
                case SnapReference.RightWire:
                    return rect.xMax;
                case SnapReference.TopWire:
                    return rect.y;
                case SnapReference.VerticalCenter:
                    return rect.center.y;
                case SnapReference.BottomWire:
                    return rect.yMax;
                default:
                    return 0;
            }
        }

        LineView m_LineView;
        List<Rect> m_SnappableRects = new List<Rect>();

        public override void BeginSnap(GraphElement selectedElement)
        {
            base.BeginSnap(selectedElement);

            var graphView = selectedElement.GraphView;
            m_LineView ??= new LineView();
            graphView.Add(m_LineView);
            m_SnappableRects = GetNotSelectedElementRectsInView(selectedElement);
        }

        protected override Vector2 ComputeSnappedPosition(out SnapDirection snapDirection, Rect sourceRect, GraphElement selectedElement)
        {
            var snappedRect = sourceRect;

            var results = GetClosestSnapElements(sourceRect);

            m_LineView.Lines.Clear();

            snapDirection = SnapDirection.SnapNone;
            foreach (var result in results)
            {
                ApplySnapToBordersResult(ref snapDirection, sourceRect, ref snappedRect, result);
                result.IndicatorLine = GetSnapLine(snappedRect, result.SourceReference, result.SnappableRect, result.SnappableReference);
                m_LineView.Lines.Add(result.IndicatorLine);
            }
            m_LineView.MarkDirtyRepaint();

            m_SnappableRects = GetNotSelectedElementRectsInView(selectedElement);

            return snappedRect.position;
        }

        public override void EndSnap()
        {
            base.EndSnap();

            m_SnappableRects.Clear();
            m_LineView.Lines.Clear();
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

        static readonly List<ChildView> k_GetNotSelectedElementRectsInViewAllUIs = new();
        List<Rect> GetNotSelectedElementRectsInView(GraphElement selectedElement)
        {
            var notSelectedElementRects = new List<Rect>();
            var graphView = selectedElement.GraphView;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var ignoredModels = graphView.GetSelection().Cast<Model>().ToList();
#pragma warning restore UA2001

            // Consider only the visible nodes.
            var rectToFit = graphView.layout;

            graphView.GraphModel.GetGraphElementModels().GetAllViews(graphView, null, k_GetNotSelectedElementRectsInViewAllUIs);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var element in k_GetNotSelectedElementRectsInViewAllUIs.OfType<ModelView>())
#pragma warning restore UA2001
            {
                if (selectedElement is Placemat placemat && element.layout.Overlaps(placemat.layout))
                {
                    // If the selected element is a placemat, we do not consider the elements under it
                    ignoredModels.Add(element.Model);
                }
                else if (element is WireView)
                {
                    // Don't consider wires
                    ignoredModels.Add(element.Model);
                }
                else if (!element.visible)
                {
                    // Don't consider not visible elements
                    ignoredModels.Add(element.Model);
                }
                else if (element is GraphElement ge && !ge.IsSelected() && !(ignoredModels.Contains(element.Model)))
                {
                    var localSelRect = graphView.ChangeCoordinatesTo(element, rectToFit);
                    if (element.Overlaps(localSelRect))
                    {
                        var geometryInContentViewContainerSpace = (element).parent.ChangeCoordinatesTo(graphView.ContentViewContainer, ge.layout);
                        notSelectedElementRects.Add(geometryInContentViewContainerSpace);
                    }
                }
            }

            k_GetNotSelectedElementRectsInViewAllUIs.Clear();

            return notSelectedElementRects;
        }

        SnapToBordersResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, Rect snappableRect, SnapReference startReference, SnapReference endReference)
        {
            var sourcePos = GetPos(sourceRect, sourceRef);
            var offsetStart = sourcePos - GetPos(snappableRect, startReference);
            var offsetEnd = sourcePos - GetPos(snappableRect, endReference);
            var minOffset = offsetStart;
            var minSnappableReference = startReference;
            if (Math.Abs(minOffset) > Math.Abs(offsetEnd))
            {
                minOffset = offsetEnd;
                minSnappableReference = endReference;
            }
            var minResult = new SnapToBordersResult
            {
                SourceReference = sourceRef,
                SnappableRect = snappableRect,
                SnappableReference = minSnappableReference,
                Offset = minOffset
            };

            return minResult.Distance <= SnapDistance ? minResult : null;
        }

        SnapToBordersResult GetClosestSnapElement(Rect sourceRect, SnapReference sourceRef, SnapReference startReference, SnapReference endReference)
        {
            SnapToBordersResult minResult = null;
            var minDistance = float.MaxValue;
            foreach (var snappableRect in m_SnappableRects)
            {
                var result = GetClosestSnapElement(sourceRect, sourceRef, snappableRect, startReference, endReference);
                if (result != null && minDistance > result.Distance)
                {
                    minDistance = result.Distance;
                    minResult = result;
                }
            }
            return minResult;
        }

        List<SnapToBordersResult> GetClosestSnapElements(Rect sourceRect, PortOrientation orientation)
        {
            var startReference = orientation == PortOrientation.Horizontal ? SnapReference.LeftWire : SnapReference.TopWire;
            var centerReference = orientation == PortOrientation.Horizontal ? SnapReference.HorizontalCenter : SnapReference.VerticalCenter;
            var endReference = orientation == PortOrientation.Horizontal ? SnapReference.RightWire : SnapReference.BottomWire;
            var results = new List<SnapToBordersResult>(3);
            var result = GetClosestSnapElement(sourceRect, startReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, centerReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            result = GetClosestSnapElement(sourceRect, endReference, startReference, endReference);
            if (result != null)
                results.Add(result);
            // Look for the minimum
            if (results.Count > 0)
            {
                results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
                var minDistance = results[0].Distance;
                results.RemoveAll(r => Math.Abs(r.Distance - minDistance) > 0.01f);
            }
            return results;
        }

        List<SnapToBordersResult> GetClosestSnapElements(Rect sourceRect)
        {
            var snapToBordersResults = GetClosestSnapElements(sourceRect, PortOrientation.Horizontal);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return snapToBordersResults.Union(GetClosestSnapElements(sourceRect, PortOrientation.Vertical)).ToList();
#pragma warning restore UA2001
        }

        static Line GetSnapLine(Rect r, SnapReference reference)
        {
            Vector2 start;
            Vector2 end;
            switch (reference)
            {
                case SnapReference.LeftWire:
                    start = r.position;
                    end = new Vector2(r.x, r.yMax);
                    break;
                case SnapReference.HorizontalCenter:
                    start = r.center;
                    end = start;
                    break;
                case SnapReference.RightWire:
                    start = new Vector2(r.xMax, r.yMin);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
                case SnapReference.TopWire:
                    start = r.position;
                    end = new Vector2(r.xMax, r.yMin);
                    break;
                case SnapReference.VerticalCenter:
                    start = r.center;
                    end = start;
                    break;
                default: // case SnapReference.BottomWire:
                    start = new Vector2(r.x, r.yMax);
                    end = new Vector2(r.xMax, r.yMax);
                    break;
            }
            return new Line(start, end);
        }

        static Line GetSnapLine(Rect r1, SnapReference reference1, Rect r2, SnapReference reference2)
        {
            var horizontal = reference1 <= SnapReference.RightWire;
            var line1 = GetSnapLine(r1, reference1);
            var line2 = GetSnapLine(r2, reference2);
            var p11 = line1.Start;
            var p12 = line1.End;
            var p21 = line2.Start;
            var p22 = line2.End;
            Vector2 start;
            Vector2 end;

            if (horizontal)
            {
                var x = p21.x;
                var yMin = Math.Min(p22.y, Math.Min(p21.y, Math.Min(p11.y, p12.y)));
                var yMax = Math.Max(p22.y, Math.Max(p21.y, Math.Max(p11.y, p12.y)));
                start = new Vector2(x, yMin);
                end = new Vector2(x, yMax);
            }
            else
            {
                var y = p22.y;
                var xMin = Math.Min(p22.x, Math.Min(p21.x, Math.Min(p11.x, p12.x)));
                var xMax = Math.Max(p22.x, Math.Max(p21.x, Math.Max(p11.x, p12.x)));
                start = new Vector2(xMin, y);
                end = new Vector2(xMax, y);
            }
            return new Line(start, end);
        }

        static void ApplySnapToBordersResult(ref SnapDirection snapDirection, Rect sourceRect, ref Rect r1, SnapToBordersResult result)
        {
            if (result.SnappableReference <= SnapReference.RightWire)
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
            m_LineView.Lines.Clear();
            m_LineView.MarkDirtyRepaint();
        }
    }
}
