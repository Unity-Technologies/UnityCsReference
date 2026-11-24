// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;

namespace Unity.Timeline.Foundation.View
{
    class MixTrimOverlay : TrimBehaviourOverlay
    {
        const string k_Style = "trimMixOverlay";

        public MixTrimOverlay()
        {
            this.AddToTimelineClassList(k_Style);
        }

        /// <summary>
        /// Gets the time of the start indicator.
        /// If the indicator has a time value, the overlay's cursor is updated
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override DiscreteTime? GetStartIndicatorTime(Item item)
        {
            DiscreteTime? startIndicator = ManipulatorUtils.GetMixStartTrimIndicator(item);
            if (startIndicator.HasValue)
            {
                EditModeCursorUtils.CursorType type = !cursor.HasValue ? EditModeCursorUtils.CursorType.MixLeft
                    : EditModeCursorUtils.CursorType.MixBoth;
                cursor = EditModeCursorUtils.GetCursor(type);
            }

            return startIndicator;
        }
        /// <summary>
        /// Gets the time of the end indicator.
        /// If the indicator has a time value, the overlay's cursor is updated
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected override DiscreteTime? GetEndIndicatorTime(Item item)
        {
            DiscreteTime? endIndicator = ManipulatorUtils.GetMixEndTrimIndicator(item);
            if (endIndicator.HasValue)
            {
                EditModeCursorUtils.CursorType type = !cursor.HasValue ? EditModeCursorUtils.CursorType.MixRight
                    : EditModeCursorUtils.CursorType.MixBoth;
                cursor = EditModeCursorUtils.GetCursor(type);
            }

            return endIndicator;
        }
    }
}
