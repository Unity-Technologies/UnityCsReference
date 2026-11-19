// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    static class ItemBuilderExtensions
    {
        public static CutList.ItemBuilder WithClipIn(this CutList.ItemBuilder builder, DiscreteTime clipIn)
        {
            builder.clipIn = clipIn;
            return builder;
        }

        public static CutList.ItemBuilder WithDuration(this CutList.ItemBuilder builder, DiscreteTime duration)
        {
            builder.duration = duration;
            return builder;
        }

        public static CutList.ItemBuilder WithContent(this CutList.ItemBuilder builder, IItemContent content)
        {
            builder.content = content;
            return builder;
        }

        public static CutList.ItemBuilder WithEndTransition(this CutList.ItemBuilder builder, UniqueID id, DiscreteTime duration, IItemContent content = null)
        {
            if (duration < DiscreteTime.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative");

            builder.transitionId = id;
            builder.transitionDuration = duration;
            builder.transitionContent = content;
            return builder;
        }

        public static CutList.ItemBuilder WithClipIn(in this CutList.ItemBuilder builder, double clipIn)
        {
            return builder.WithClipIn(new DiscreteTime(clipIn));
        }

        public static CutList.ItemBuilder WithDuration(in this CutList.ItemBuilder builder, double duration)
        {
            return builder.WithDuration(new DiscreteTime(duration));
        }

        public static CutList.ItemBuilder WithEndTransition(this CutList.ItemBuilder builder, DiscreteTime duration, IItemContent content = null)
        {
            return builder.WithEndTransition(UniqueID.Generate(), duration, content);
        }

        public static CutList.ItemBuilder WithEndTransition(this CutList.ItemBuilder builder, double duration, IItemContent content = null)
        {
            return builder.WithEndTransition(UniqueID.Generate(), new DiscreteTime(duration), content);
        }

        public static CutList.ItemBuilder WithEndTransition(this CutList.ItemBuilder builder, UniqueID id, double duration, IItemContent content = null)
        {
            return builder.WithEndTransition(id, new DiscreteTime(duration), content);
        }
    }
}
