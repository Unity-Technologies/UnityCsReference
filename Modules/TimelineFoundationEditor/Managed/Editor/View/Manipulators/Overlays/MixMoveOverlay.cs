// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View
{
    class MixMoveOverlay : MoveBehaviourOverlay
    {
        const string k_Style = "moveMixOverlay";

        public MixMoveOverlay()
        {
            this.AddToTimelineClassList(k_Style);
        }

        protected override void UpdateIndicatorsPositions(MoveBehaviour moveBehaviour, SequenceLookup lookup)
        {
            EditModeCursorUtils.CursorType type = EditModeCursorUtils.CursorType.None;

            foreach (var track in moveBehaviour.targets)
            {
                foreach (var start in ManipulatorUtils.GetMixStartMoveIndicators(track, moveBehaviour.GetManipulatedItems(), lookup))
                {
                    AddStartIndicatorAtTime(start, track.ID);
                    type = EditModeCursorUtils.CursorType.MixLeft;
                }
                foreach (var end in ManipulatorUtils.GetMixEndMoveIndicators(track, moveBehaviour.GetManipulatedItems(), lookup))
                {
                    AddEndIndicatorAtTime(end, track.ID);
                    type = type == EditModeCursorUtils.CursorType.None ? EditModeCursorUtils.CursorType.MixRight : EditModeCursorUtils.CursorType.MixBoth;
                }
            }
            cursor = EditModeCursorUtils.GetCursor(type);
        }
    }
}
