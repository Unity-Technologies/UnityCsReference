// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    static class ClipBuilderExtensions
    {
        public static CutList.ClipBuilder WithClipIn(this CutList.ClipBuilder builder, DiscreteTime clipIn)
        {
            builder.clipIn = clipIn;
            return builder;
        }

        public static CutList.ClipBuilder WithDuration(this CutList.ClipBuilder builder, DiscreteTime duration)
        {
            builder.duration = duration;
            return builder;
        }

        public static CutList.ClipBuilder WithContent(this CutList.ClipBuilder builder, IItemContent content)
        {
            builder.content = content;
            return builder;
        }

        public static CutList.ClipBuilder WithEndTransition(this CutList.ClipBuilder builder, UniqueID id, DiscreteTime duration, IItemContent content = null)
        {
            if (duration < DiscreteTime.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative");

            builder.transitionId = id;
            builder.transitionDuration = duration;
            builder.transitionContent = content;
            return builder;
        }

        public static CutList.ClipBuilder WithClipIn(this CutList.ClipBuilder builder, double clipIn)
        {
            return builder.WithClipIn(new DiscreteTime(clipIn));
        }

        public static CutList.ClipBuilder WithDuration(this CutList.ClipBuilder builder, double duration)
        {
            return builder.WithDuration(new DiscreteTime(duration));
        }

        public static CutList.ClipBuilder WithEndTransition(this CutList.ClipBuilder builder, DiscreteTime duration, IItemContent content = null)
        {
            return builder.WithEndTransition(UniqueID.Generate(), duration, content);
        }

        public static CutList.ClipBuilder WithEndTransition(this CutList.ClipBuilder builder, double duration, IItemContent content = null)
        {
            return builder.WithEndTransition(UniqueID.Generate(), new DiscreteTime(duration), content);
        }

        public static CutList.ClipBuilder WithEndTransition(this CutList.ClipBuilder builder, UniqueID id, double duration, IItemContent content = null)
        {
            return builder.WithEndTransition(id, new DiscreteTime(duration), content);
        }
    }
}
