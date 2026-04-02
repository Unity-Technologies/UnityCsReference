// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Commands.Time
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SetDisplayTime : ICommand
    {
        public readonly DiscreteTime time;

        public SetDisplayTime(DiscreteTime time)
        {
            this.time = time;
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SetLocalTime : ICommand
    {
        public readonly DiscreteTime time;

        public SetLocalTime(DiscreteTime time)
        {
            this.time = time;
        }
    }

    readonly struct Step : ICommand
    {
        public enum Direction { None = 0, Backward, Forward }

        public readonly Direction direction;

        public Step(Direction direction)
        {
            this.direction = direction;
        }
    }
}
