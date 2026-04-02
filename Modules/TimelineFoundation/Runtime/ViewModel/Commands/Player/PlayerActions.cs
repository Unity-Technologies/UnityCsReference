// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.IntegerTime;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Commands.Player
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct Play : ICommand { }
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct Pause : ICommand { }
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct Stop : ICommand { }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct Evaluate : ICommand
    {
        public readonly DiscreteTime time;

        public Evaluate(DiscreteTime time)
        {
            this.time = time;
        }
    }
}
