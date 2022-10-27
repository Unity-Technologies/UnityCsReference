// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class AutoDistributingHelper_Internal : AutoPlacementHelper_Internal
    {
        float m_DistributingMargin;
        PortOrientation m_Orientation;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoDistributingHelper_Internal"/> class.
        /// </summary>
        /// <param name="graphView">The <see cref="GraphView"/> in charge of distributing the elements.</param>
        public AutoDistributingHelper_Internal(GraphView graphView)
        {
            m_GraphView = graphView;
        }

        /// <summary>
        /// Sends a command of type <see cref="AutoPlaceElementsCommand"/> to distribute the selected elements.
        /// </summary>
        /// <param name="orientation">The orientation of the distribution.</param>
        public void SendDistributeCommand(PortOrientation orientation)
        {
            m_Orientation = orientation;

            // Get distribute delta for each element
            var results = GetElementDeltaResults();

            // Dispatch command
            SendPlacementCommand(results.Keys.ToList(), results.Values.ToList());
        }

        float GetStartingPosition(Rect firstRect)
        {
            return (m_Orientation == PortOrientation.Horizontal ? firstRect.xMax : firstRect.yMax) + m_DistributingMargin;
        }

        protected override void UpdateReferencePosition(ref float referencePosition, Rect currentElementRect)
        {
            referencePosition += (m_Orientation == PortOrientation.Horizontal ? currentElementRect.width : currentElementRect.height) + m_DistributingMargin;
        }

        protected override Vector2 GetDelta(Rect elementPosition, float referencePosition)
        {
            var offset = referencePosition - (m_Orientation == PortOrientation.Horizontal ? elementPosition.x : elementPosition.y);

            return m_Orientation == PortOrientation.Horizontal ? new Vector2(offset, 0f) : new Vector2(0f, offset);
        }

        protected override Dictionary<Model, Vector2> GetDeltas(List<(Rect, List<Model>)> boundingRects)
        {
            var min = Mathf.Infinity;
            var max = -Mathf.Infinity;
            var firstRectIndex = 0;
            var lastRectIndex = 0;
            var occupiedLength = 0f;

            for (var i = 0; i < boundingRects.Count; ++i)
            {
                var rect = boundingRects[i].Item1;
                if (m_Orientation == PortOrientation.Horizontal)
                {
                    if (min > rect.xMin)
                    {
                        min = rect.xMin;
                        firstRectIndex = i;
                    }
                    if (max < rect.xMax)
                    {
                        max = rect.xMax;
                        lastRectIndex = i;
                    }
                    occupiedLength += rect.width;
                }
                else
                {
                    if (min > rect.yMin)
                    {
                        min = rect.yMin;
                        firstRectIndex = i;
                    }
                    if (max < rect.yMax)
                    {
                        max = rect.yMax;
                        lastRectIndex = i;
                    }
                    occupiedLength += rect.height;
                }
            }

            // The margin between each element that ensures all elements are equally distributed
            m_DistributingMargin = GetDistributingMargin(boundingRects, m_Orientation, firstRectIndex, lastRectIndex, occupiedLength);

            var startingPosition = GetStartingPosition(boundingRects[firstRectIndex].Item1);

            return ComputeDeltas(GetRectsToMove(boundingRects, m_Orientation, firstRectIndex, lastRectIndex), startingPosition);
        }

        static float GetDistributingMargin(IReadOnlyList<(Rect, List<Model>)> boundingRects, PortOrientation orientation, int firstIndex, int lastIndex, float occupiedLength)
        {
            var firstRect = boundingRects[firstIndex];
            var lastRect = boundingRects[lastIndex];

            var totalLength = orientation == PortOrientation.Horizontal ? lastRect.Item1.xMax - firstRect.Item1.xMin : lastRect.Item1.yMax - firstRect.Item1.yMin;

            return (totalLength - occupiedLength) / (boundingRects.Count - 1);
        }

        static IEnumerable<(Rect, List<Model>)> GetRectsToMove(IList<(Rect, List<Model>)> boundingRects, PortOrientation orientation, int firstRectIndex, int lastRectIndex)
        {
            // We do not want the first and last rects to move
            if (firstRectIndex == lastRectIndex)
            {
                boundingRects.RemoveAt(firstRectIndex);
            }
            else
            {
                boundingRects.RemoveAt(firstRectIndex < lastRectIndex ? lastRectIndex : firstRectIndex);
                boundingRects.RemoveAt(firstRectIndex < lastRectIndex ? firstRectIndex : lastRectIndex);
            }

            // The rects need to be placed in order
            return boundingRects.OrderBy(rect => orientation == PortOrientation.Horizontal ? rect.Item1.x : rect.Item1.y);
        }
    }
}
