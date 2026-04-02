// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;

namespace Unity.Timeline.Foundation.View
{
    class RippleTrimOverlay : TrimBehaviourOverlay
    {
        const string k_Style = "trimRippleOverlay";

        public RippleTrimOverlay()
        {
            this.AddToTimelineClassList(k_Style);
        }

        protected override DiscreteTime? GetStartIndicatorTime(Item item)
        {
            DiscreteTime? startIndicator = ManipulatorUtils.GetRippleStartTrimIndicator(item);
            if (startIndicator.HasValue)
                cursor = EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.Ripple);
            return startIndicator;
        }

        protected override DiscreteTime? GetEndIndicatorTime(Item item)
        {
            DiscreteTime? endIndicator = ManipulatorUtils.GetRippleEndTrimIndicator(item);
            if (endIndicator.HasValue)
                cursor = EditModeCursorUtils.GetCursor(EditModeCursorUtils.CursorType.Ripple);
            return endIndicator;
        }
    }
}
