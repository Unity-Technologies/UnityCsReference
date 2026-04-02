// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct ViewData : IReadOnlyData
    {
        public readonly TimeRange displayRange;
        public readonly float verticalScrollOffset;
        public readonly float headerWidth;

        public ViewData(TimeRange displayRange,
                        float verticalScrollOffset,
                        float headerWidth)
        {
            this.displayRange = displayRange;
            this.verticalScrollOffset = verticalScrollOffset;
            this.headerWidth = headerWidth;
        }

        public DiscreteTime displayStartTime => displayRange.start;
        public DiscreteTime displayEndTime => displayRange.end;
    }
}
