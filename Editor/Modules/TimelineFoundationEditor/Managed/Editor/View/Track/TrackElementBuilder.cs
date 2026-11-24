// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.Common;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View.Internals
{
    class TrackElementBuilder<TTrackElement> : ElementBuilder<TrackElementContext, ITrackMetadata, TTrackElement>
        where TTrackElement : VisualElement, ITrackElement
    {
        protected override ITrackMetadata GetKey(TrackElementContext context)
        {
            return context.track.GetGenericMetadata();
        }
    }
}
