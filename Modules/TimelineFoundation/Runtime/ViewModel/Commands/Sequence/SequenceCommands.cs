// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Commands.Sequence
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SetDuration : ICommand
    {
        public readonly DiscreteTime time;

        public SetDuration(DiscreteTime time)
        {
            this.time = time;
        }
    }

    readonly struct SetFrameRate : ICommand
    {
        public readonly FrameRate frameRate;

        public SetFrameRate(FrameRate frameRate)
        {
            this.frameRate = frameRate;
        }
    }

    readonly struct CreateTrack : ICommand
    {
        public readonly TrackCreationData data;

        public CreateTrack(string name, System.Type type)
        {
            data = new TrackCreationData(name, type);
        }

        public CreateTrack(string name, System.Type type, ITrack parent)
        {
            data = new TrackCreationData(name, type, parent);
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct RemoveTrack : ICommand
    {
        public readonly Foundation.ViewModel.Track trackToRemove;

        public RemoveTrack(Foundation.ViewModel.Track trackToRemove)
        {
            this.trackToRemove = trackToRemove;
        }
    }

    readonly struct ReparentTracks : ICommand
    {
        public readonly TrackReparentingData data;

        public static ReparentTracks Create(IReadOnlyList<Track> track, Stack newParent, uint newIndex)
        {
            IStack parent = null;

            if (newParent is ViewModel.Sequence sequence)
                parent = sequence.model;
            else if (newParent is Track parentTrack)
                parent = parentTrack.model;

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return new ReparentTracks(new TrackReparentingData(new List<ITrack>(track.Select(t => t.model)), parent, newIndex));
#pragma warning restore UA2001
        }

        public static ReparentTracks Create(Track track, Stack newParent, uint newIndex)
        {
            IStack parent = null;
            if (newParent is ViewModel.Sequence sequence)
                parent = sequence.model;

            if (newParent is Track parentTrack)
                parent = parentTrack.model;

            return new ReparentTracks(new TrackReparentingData(track.model, parent, newIndex));
        }

        ReparentTracks(TrackReparentingData data)
        {
            this.data = data;
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct SetTrackName : ICommand
    {
        public readonly Foundation.ViewModel.Track target;
        public readonly string name;

        public SetTrackName(Foundation.ViewModel.Track target, string name)
        {
            this.target = target;
            this.name = name;
        }
    }

    readonly struct SetTrackContents : ICommand
    {
        public readonly CutList newCutList;
        public readonly MarkerList newMarkerList;
        public readonly ViewModel.Track track;

        public SetTrackContents(ViewModel.Track track, CutList cutList, MarkerList markerList = null)
        {
            this.track = track;
            newCutList = cutList;
            newMarkerList = markerList;
        }

        public SetTrackContents(ViewModel.Track track, MarkerList markerList)
            : this(track, null, markerList) { }
    }
}
