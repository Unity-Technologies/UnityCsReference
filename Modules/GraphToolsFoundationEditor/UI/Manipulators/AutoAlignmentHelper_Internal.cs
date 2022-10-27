// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class AutoAlignmentHelper_Internal : AutoPlacementHelper_Internal
    {
        AlignmentReference m_AlignmentReference;

        public enum AlignmentReference
        {
            Left,
            HorizontalCenter,
            Right,
            Top,
            VerticalCenter,
            Bottom
        }

        public AutoAlignmentHelper_Internal(GraphView graphView)
        {
            m_GraphView = graphView;
        }

        public void SendAlignCommand(AlignmentReference reference)
        {
            m_AlignmentReference = reference;

            // Get alignment delta for each element
            Dictionary<Model, Vector2> results = GetElementDeltaResults();

            // Dispatch command
            SendPlacementCommand(results.Keys.ToList(), results.Values.ToList());
        }

        protected override Dictionary<Model, Vector2> GetDeltas(List<(Rect, List<Model>)> boundingRects)
        {
            return ComputeDeltas(boundingRects, GetStartingPosition(boundingRects));
        }

        float GetStartingPosition(List<(Rect, List<Model>)> boundingRects)
        {
            float alignmentBorderPosition;
            if (m_AlignmentReference == AlignmentReference.Left || m_AlignmentReference == AlignmentReference.Top)
            {
                alignmentBorderPosition = boundingRects.Min(rect => GetPosition(rect.Item1, m_AlignmentReference));
            }
            else if (m_AlignmentReference == AlignmentReference.Right || m_AlignmentReference == AlignmentReference.Bottom)
            {
                alignmentBorderPosition = boundingRects.Max(rect => GetPosition(rect.Item1, m_AlignmentReference));
            }
            else
            {
                alignmentBorderPosition = boundingRects.Average(rect => GetPosition(rect.Item1, m_AlignmentReference));
            }

            return alignmentBorderPosition;
        }

        protected override void UpdateReferencePosition(ref float referencePosition, Rect currentElementRect) {}

        protected override Vector2 GetDelta(Rect elementPosition, float referencePosition)
        {
            float offset = Math.Abs(GetPosition(elementPosition, m_AlignmentReference) - referencePosition);
            Vector2 delta;
            switch (m_AlignmentReference)
            {
                case AlignmentReference.Left:
                    delta = new Vector2(-offset, 0f);
                    break;
                case AlignmentReference.HorizontalCenter:
                    delta = new Vector2(elementPosition.center.x < referencePosition ? offset : -offset, 0f);
                    break;
                case AlignmentReference.Right:
                    delta = new Vector2(offset, 0f);
                    break;
                case AlignmentReference.Top:
                    delta = new Vector2(0f, -offset);
                    break;
                case AlignmentReference.VerticalCenter:
                    delta = new Vector2(0f, elementPosition.center.y < referencePosition ? offset : -offset);
                    break;
                case AlignmentReference.Bottom:
                    delta = new Vector2(0f, offset);
                    break;
                default:
                    return Vector2.zero;
            }

            return delta;
        }

        static float GetPosition(Rect rect, AlignmentReference reference)
        {
            switch (reference)
            {
                case AlignmentReference.Left:
                    return rect.x;
                case AlignmentReference.HorizontalCenter:
                    return rect.center.x;
                case AlignmentReference.Right:
                    return rect.xMax;
                case AlignmentReference.Top:
                    return rect.y;
                case AlignmentReference.VerticalCenter:
                    return rect.center.y;
                case AlignmentReference.Bottom:
                    return rect.yMax;
                default:
                    return 0;
            }
        }
    }
}
