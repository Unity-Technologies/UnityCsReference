// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct TimeData : IReadOnlyData
    {
        public readonly DiscreteTime displayTime;
        public readonly TimeTransform localToDisplayTimeTransform;
        public readonly TimeTransform localToGlobalTimeTransform;

        public TimeData(TimeTransform localToGlobalTimeTransform, DiscreteTime displayTime, TimeTransform localToDisplayTimeTransform)
        {
            this.displayTime = displayTime;
            this.localToDisplayTimeTransform = localToDisplayTimeTransform;
            this.localToGlobalTimeTransform = localToGlobalTimeTransform;
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class TimeDataExtensions
    {
        public static DiscreteTime DisplayToLocal(this in TimeData timeData, DiscreteTime displayTime)
        {
            return timeData.localToDisplayTimeTransform.InverseTransform(displayTime);
        }

        public static DiscreteTime LocalToDisplay(this in TimeData timeData, DiscreteTime displayTime)
        {
            return timeData.localToDisplayTimeTransform.Transform(displayTime);
        }

        public static DiscreteTime GlobalToLocal(this in TimeData timeData, DiscreteTime globalTime)
        {
            return timeData.localToGlobalTimeTransform.InverseTransform(globalTime);
        }

        public static DiscreteTime LocalToGlobal(this in TimeData timeData, DiscreteTime localTime)
        {
            return timeData.localToGlobalTimeTransform.Transform(localTime);
        }

        public static DiscreteTime GlobalToDisplay(this in TimeData timeData, DiscreteTime globalTime)
        {
            return timeData.LocalToDisplay(timeData.GlobalToLocal(globalTime));
        }

        public static DiscreteTime DisplayToGlobal(this in TimeData timeData, DiscreteTime displayTime)
        {
            return timeData.LocalToGlobal(timeData.DisplayToLocal(displayTime));
        }
    }
}
