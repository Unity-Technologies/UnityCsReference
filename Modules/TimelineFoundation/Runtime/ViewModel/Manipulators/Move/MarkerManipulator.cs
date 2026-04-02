// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Manipulations;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.ViewModel.Internals
{
    static class MarkerManipulator
    {
        public static MoveMarkersState MarkersMoveStateFromContext(in ManipulationContext context)
        {
            var movedMarkers = new List<MarkerList>(context.manipulatedTracks.Count);
            var destination = new List<MarkerList>(context.manipulatedTracks.Count);
            foreach (ManipulatedTrack manipulatedTrack in context.manipulatedTracks)
            {
                MarkerList trackMarkers = manipulatedTrack.track.GetMarkerList_Internal();
                var moved = new List<Marker>();
                var dest = new List<Marker>(trackMarkers);

                foreach (Item item in manipulatedTrack.manipulatedItems.OnlyMarkers())
                {
                    Marker marker = trackMarkers.GetMarkerForId(item.ID);
                    dest.Remove(marker);
                    moved.Add(marker);
                }

                movedMarkers.Add(moved.ToMarkerList());
                destination.Add(dest.ToMarkerList());
            }

            return new MoveMarkersState(movedMarkers, destination);
        }

        public static void Insert(IViewModel viewModel, IReadOnlyList<Track> destinationTracks, DiscreteTime delta, MoveMarkersState state)
        {
            if (!state.shouldMoveMarkers) return;
            for (var i = 0; i < destinationTracks.Count; i++)
            {
                Track track = destinationTracks[i];
                var command = new InsertMarkers(state.markerDestinations[i], state.movedMarkers[i], track, delta);
                viewModel.Dispatch(command);
            }
        }

        public static void RippleMove(IViewModel viewModel, IReadOnlyList<Track> destinationTracks, DiscreteTime delta, MoveMarkersState state)
        {
            if (!state.shouldMoveMarkers) return;
            for (var i = 0; i < destinationTracks.Count; i++)
            {
                Track track = destinationTracks[i];
                var rippleInsertCommand = new RippleInsertMarkers(state.markerDestinations[i], state.movedMarkers[i], track, delta);
                viewModel.Dispatch(rippleInsertCommand);
            }
        }

        public static IEnumerable<Item> GetManipulatedItems(ManipulationContext context, TimeRange rippleRange)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return context.manipulatedTracks.SelectMany(t => t.items.OnlyMarkers().Where(i => rippleRange.Intersects(i.start)));
#pragma warning restore UA2001
        }

        public static IEnumerable<Item> GetFirstItems(IEnumerable<ManipulatedTrack> tracks)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2011 // Pre-existing usage of FirstOrDefault.
            return tracks.Select(t => t.manipulatedItems.OnlyMarkers().FirstOrDefault());
#pragma warning restore UA2011
#pragma warning restore UA2001
        }
    }
}
