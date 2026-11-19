// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Model.Internals;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        public struct ItemBuilder
        {
            public UniqueID handle;
            public ItemType type;
            public DiscreteTime clipIn;
            public DiscreteTime duration;
            public UniqueID transitionId;
            public DiscreteTime transitionDuration;
            public IItemContent content;
            public IItemContent transitionContent;

            public ItemBuilder(UniqueID handle, ItemType type, DiscreteTime clipIn = default, DiscreteTime duration = default,
                UniqueID transitionId = default, DiscreteTime transitionDuration = default,
                IItemContent content = null, IItemContent transitionContent = null)
            {
                this.handle = handle;
                this.type = type;
                this.clipIn = clipIn;
                this.duration = duration;
                this.transitionId = transitionId;
                this.transitionDuration = transitionDuration;
                this.content = content;
                this.transitionContent = transitionContent;
            }

            internal ItemData GetItemData_Internal()
            {
                return new ItemData
                {
                    id = handle,
                    type = type,
                    range = new TimeRange(clipIn, clipIn + duration),
                    endTransitionId = transitionId,
                    endTransitionDuration = transitionDuration,
                    content = new Internals.Content(content, transitionContent)
                };
            }

            public static implicit operator ItemBuilder(in Item item)
            {
                var data = item.ToItemData_Internal();
                return new ItemBuilder(data.id, data.type,
                    data.range.start, data.range.duration,
                    data.endTransitionId, data.endTransitionDuration,
                    data.content.itemContent, data.content.transitionContent);
            }

            public static ClipBuilder Clip(UniqueID handle)
            {
                return new ClipBuilder(handle);
            }

            public static ClipBuilder Clip()
            {
                return new ClipBuilder(UniqueID.Generate());
            }

            public static GapBuilder Gap(UniqueID handle)
            {
                return new GapBuilder(handle);
            }

            public static GapBuilder Gap()
            {
                return new GapBuilder(UniqueID.Generate());
            }
        }
    }
}
