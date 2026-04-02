// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View
{
    class RippleMoveOverlay : MoveBehaviourOverlay
    {
        const string k_Style = "moveRippleOverlay";

        public RippleMoveOverlay()
        {
            this.AddToTimelineClassList(k_Style);
        }

        protected override void UpdateIndicatorsPositions(MoveBehaviour moveBehaviour, SequenceLookup lookup)
        {
            cursor = EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.Ripple);
            foreach (var track in moveBehaviour.targets)
            {
                DiscreteTime? start = ManipulatorUtils.GetRippleStartMoveIndicator(track, moveBehaviour.GetManipulatedItems(), lookup);
                if (start.HasValue)
                    AddStartIndicatorAtTime(start.Value, track.ID);
            }
        }

        protected override void UpdateIndicatorsPositions(IEnumerable<TimeRange> itemPreviewRanges, UniqueID track)
        {
#pragma warning disable UA2011 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var firstItemRange = itemPreviewRanges.FirstOrDefault();
#pragma warning restore UA2011
            if (firstItemRange.Equals(TimeRange.Empty))
                return;
            AddStartIndicatorAtTime(firstItemRange.start, track);
        }
    }
}
