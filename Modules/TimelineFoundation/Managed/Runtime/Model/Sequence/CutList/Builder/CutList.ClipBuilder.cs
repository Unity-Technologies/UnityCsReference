// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    partial class CutList
    {
        public struct ClipBuilder
        {
            public UniqueID id;
            public DiscreteTime clipIn;
            public DiscreteTime duration;
            public UniqueID transitionId;
            public DiscreteTime transitionDuration;
            public IItemContent content;
            public IItemContent transitionContent;

            public ClipBuilder(UniqueID id, DiscreteTime clipIn = default, DiscreteTime duration = default,
                UniqueID transitionId = default, DiscreteTime transitionDuration = default,
                IItemContent content = null, IItemContent transitionContent = null)
            {
                this.id = id;
                this.clipIn = clipIn;
                this.duration = duration;
                this.transitionId = transitionId;
                this.transitionDuration = transitionDuration;
                this.content = content;
                this.transitionContent = transitionContent;
            }

            public static implicit operator ItemBuilder(in ClipBuilder clip)
            {
                return new ItemBuilder(
                    clip.id,
                    ItemType.Clip,
                    clip.clipIn,
                    clip.duration,
                    clip.transitionId,
                    clip.transitionDuration,
                    clip.content,
                    clip.transitionContent);
            }
        }
    }
}
