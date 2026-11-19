// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    readonly struct TrackElementContext
    {
        public readonly ISequenceViewModel viewModel;
        public readonly Track track;

        public TrackElementContext(ISequenceViewModel vm, Track track)
        {
            viewModel = vm;
            this.track = track;
        }
    }
}
