// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    static class ManipulationHandlerExtensions
    {
        public static void Manipulate(this IManipulationHandler handler, IEnumerable<Track> manipulatedTracks)
        {
            if (handler == null) return;
            foreach (Track track in manipulatedTracks)
                handler.Manipulate(track);
        }
    }
}
