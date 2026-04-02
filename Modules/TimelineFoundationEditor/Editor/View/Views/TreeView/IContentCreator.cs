// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View
{
    interface IContentCreator
    {
        TrackHeaderElement CreateTrackHeaderElement(Track track);
        TrackElement CreateTrackElement(Track track);
    }
}
