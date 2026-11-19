// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    static class GapBuilderExtensions
    {
        public static CutList.GapBuilder WithDuration(this CutList.GapBuilder builder, DiscreteTime duration)
        {
            builder.duration = duration;
            return builder;
        }

        public static CutList.GapBuilder WithContent(this CutList.GapBuilder builder, IItemContent content)
        {
            builder.content = content;
            return builder;
        }

        public static CutList.GapBuilder WithDuration(this CutList.GapBuilder builder, double duration)
        {
            builder.duration = new DiscreteTime(duration);
            return builder;
        }
    }
}
