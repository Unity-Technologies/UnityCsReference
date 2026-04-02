// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct MoveManipulationResult
    {
        public static readonly TimeRange defaultRange = new TimeRange(DiscreteTime.Zero, DiscreteTime.MaxValue);

        public readonly bool isValid;
        public readonly bool needsPreview;
        public readonly TimeRange validRange;

        public MoveManipulationResult(bool isValid, bool needsPreview, TimeRange validRange)
        {
            this.isValid = isValid;
            this.needsPreview = needsPreview;
            this.validRange = validRange;
        }

        public MoveManipulationResult(bool isValid, bool needsPreview)
            : this(isValid, needsPreview, defaultRange) { }
    }
}
