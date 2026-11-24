// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    abstract class MoveBehaviourOverlay : ManipulationBehaviourOverlay
    {
        bool m_IsUsingItemPreview;
        List<TimeRange> m_ItemPreviewRanges;
        SequenceLookup m_SequenceLookup;
        MoveBehaviour m_MoveBehaviour;

        public void UpdateIndicators(MoveBehaviour moveBehaviour, SequenceLookup lookup)
        {
            m_IsUsingItemPreview = false;
            m_MoveBehaviour = moveBehaviour;
            m_SequenceLookup = lookup;
            MarkDirtyRepaint();
        }

        public void UpdateIndicatorsWithItemPreview(MoveBehaviour moveBehaviour, IEnumerable<TimeRange> itemPreviewRanges)
        {
            m_IsUsingItemPreview = true;
            m_MoveBehaviour = moveBehaviour;
            m_ItemPreviewRanges = new List<TimeRange>(itemPreviewRanges);
            m_ItemPreviewRanges.Sort((x, y) => x.start.CompareTo(y.start));
            MarkDirtyRepaint();
        }

        protected override void GenerateVisualContent(MeshGenerationContext context)
        {
            ClearIndicators();

            if (m_MoveBehaviour == null)
                return;

            if (m_IsUsingItemPreview)
                UpdateIndicatorsPositions(m_ItemPreviewRanges, m_MoveBehaviour.targets[0].ID);
            else
                UpdateIndicatorsPositions(m_MoveBehaviour, m_SequenceLookup);

            base.GenerateVisualContent(context);
        }

        protected virtual void UpdateIndicatorsPositions(MoveBehaviour moveBehaviour, SequenceLookup lookup) { }
        protected virtual void UpdateIndicatorsPositions(IEnumerable<TimeRange> itemPreviewRanges, UniqueID currentTrack) { }
    }
}
