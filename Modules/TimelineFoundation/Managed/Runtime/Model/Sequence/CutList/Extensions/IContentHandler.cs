// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model
{
    readonly struct BlendResult
    {
        public readonly UniqueID id;
        public readonly IItemContent transitionContent;

        public BlendResult(UniqueID id, IItemContent transitionContent = null)
        {
            this.id = id;
            this.transitionContent = transitionContent;
        }
    }

    interface IContentHandler
    {
        IItemContent Clone(IItemContent content);
        BlendResult Blend(IItemContent content1, IItemContent content2);
        bool CanBlend(IItemContent content1, IItemContent content2);
    }
}
