// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Common;

namespace Unity.Timeline.Foundation.Model.Internals
{
    static class ContentHandlerExtensions
    {
        public static IItemContent CloneSafe(this IContentHandler handler, IItemContent content)
        {
            return handler != null ? handler.Clone(content) : content;
        }

        public static bool CanBlendSafe(this IContentHandler handler, IItemContent c1, IItemContent c2)
        {
            return handler != null ? handler.CanBlend(c1, c2) : true;
        }

        public static BlendResult BlendSafe(this IContentHandler handler, IItemContent c1, IItemContent c2)
        {
            return handler != null ? handler.Blend(c1, c2) : new BlendResult(UniqueID.Generate());
        }
    }
}
