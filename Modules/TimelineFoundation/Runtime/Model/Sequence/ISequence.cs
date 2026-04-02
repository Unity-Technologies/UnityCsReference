// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    interface ISequence : IStack
    {
        DiscreteTime GetDuration();
        FrameRate GetFrameRate();
        void SetFrameRate(FrameRate frameRate);
        void SetDuration(DiscreteTime time);

        ITrack CreateTrack(TrackCreationData data);
        void RemoveTrack(ITrack track);
        void ReparentTracks(TrackReparentingData data);
        void SetTrackName(ITrack track, string name);
        void SetTrackMetadata(ITrack track, ITrackMetadata metadata);
    }

    interface IStack
    {
        UniqueID ID { get; }
        IEnumerable<ITrack> GetChildTracks();
    }

    interface ITrack : IStack
    {
        string name { get; }
        ITrackMetadata metadata { get; }
        IStack parent { get; }

        CutList GetCutList();
        void SetCutList(CutList cutList);
        MarkerList GetMarkers();
        void SetMarkers(MarkerList markers);
    }

    readonly struct TrackCreationData
    {
        public Type type { get; }
        public string name { get; }
        public ITrack parent { get; }

        public TrackCreationData(string name, Type type)
        {
            this.name = name;
            this.type = type;
            parent = null;
        }

        public TrackCreationData(string name, Type type, ITrack parent)
        {
            this.name = name;
            this.type = type;
            this.parent = parent;
        }
    }

    readonly struct TrackReparentingData
    {
        public readonly IReadOnlyList<ITrack> tracks;
        public readonly IStack newParent;
        public readonly uint newIndex;

        /// <param name="track">The track to be reparented</param>
        /// <param name="newParent">The new parent</param>
        /// <param name="newIndex">The new index. </param>
        public TrackReparentingData(ITrack track, IStack newParent, uint newIndex)
        {
            tracks = track == null ? null : new List<ITrack> { track };
            this.newParent = newParent;
            this.newIndex = newIndex;
        }

        /// <param name="tracks">The list of tracks to be reparented</param>
        /// <param name="newParent">The new parent</param>
        /// <param name="newIndex">The new index. Must be between 0 and the number of children in the new parent (inclusive)</param>
        public TrackReparentingData(IReadOnlyList<ITrack> tracks, IStack newParent, uint newIndex)
        {
            this.tracks = tracks;
            this.newParent = newParent;
            this.newIndex = newIndex;
        }
    }
}
