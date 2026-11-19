// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View.Internals
{
    sealed class SequenceElementBuilder
    {
        public ISequenceViewModel viewModel { get; set; }
        public readonly TrackElementBuilder<TrackHeaderElement> trackHeaderBuilder = new();
        public readonly TrackElementBuilder<TrackElement> trackContentsBuilder = new();
        public readonly ItemElementBuilder itemBuilder = new();

        public TrackElement BuildTrackElement(Track track)
        {
            var context = new TrackElementContext(viewModel, track);
            TrackElement trackElement = trackContentsBuilder.BuildElement(context);
            trackElement.Initialize(viewModel);
            return trackElement;
        }

        public TrackHeaderElement BuildTrackHeaderElement(Track track)
        {
            var context = new TrackElementContext(viewModel, track);
            TrackHeaderElement headerElement = trackHeaderBuilder.BuildElement(context);
            headerElement.Initialize(viewModel);
            return headerElement;
        }

        public ItemElement BuildItemElement(Item item)
        {
            var context = new ItemElementContext(viewModel, item);
            ItemElement itemElement = itemBuilder.BuildElement(context);
            return itemElement;
        }
    }
}
