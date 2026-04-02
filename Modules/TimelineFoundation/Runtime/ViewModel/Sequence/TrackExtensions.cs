// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class TrackExtensions
    {
        public static ItemView Clips(this Track track) => track.Items.OnlyClips();
        public static ItemView Gaps(this Track track) => track.Items.OnlyGaps();
        public static ItemView Transitions(this Track track) => track.Items.OnlyTransitions();
        public static ItemView Markers(this Track track) => track.Items.OnlyMarkers();
        public static ItemView Intervals(this Track track) => track.Items.OnlyIntervals();
        public static bool SupportsMultiSelection(this Track track) => track.GetGenericMetadata().supportsMultiSelect;
    }
}
