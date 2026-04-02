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
    static class ItemManipulator
    {
        public static MoveItemsState ItemMoveStateFromContext(in ManipulationContext context, IManipulationHandler handler)
        {
            var moved = new List<CutList>(context.manipulatedTracks.Count);
            var destinations = new List<CutList>(context.manipulatedTracks.Count);
            foreach (var manipulatedTrack in context.manipulatedTracks)
            {
                var movedItems = new CutList.Editor();
                CutList trackCutList = manipulatedTrack.track.GetCutList_Internal();
                var destination = new CutList.Editor(trackCutList);

                foreach (Item item in manipulatedTrack.manipulatedItems)
                {
                    if (item.isTransition | item.isMarker)
                        continue;

                    CutList.Iterator itr = trackCutList.IteratorAtId(item.ID);
                    CutList extracted = trackCutList.Extract(itr);

                    movedItems.InsertMix(extracted, handler);
                    var iterator = destination.IteratorAtId(item.ID);
                    destination.RemoveItem(iterator);
                }

                moved.Add(movedItems.Finish());
                destinations.Add(destination.Finish());
            }

            return new MoveItemsState(moved, destinations);
        }

        public static MoveItemsState Move(DiscreteTime delta, MoveItemsState state)
        {
            if (!state.shouldMoveItems)
                return state;

            var moved = new List<CutList>(state.movedItems);
            for (var i = 0; i < moved.Count; i++)
            {
                CutList toMove = moved[i];
                var toMoveEditor = new CutList.Editor(toMove);
                toMoveEditor.RippleMove(toMove.GetIteratorAtStart(), delta);
                moved[i] = toMoveEditor.Finish();
            }

            return new MoveItemsState(moved, state.itemsDestinations);
        }

        public static bool IsInsertionValid(MoveItemsState state, IManipulationHandler handler)
        {
            if (!state.shouldMoveItems)
                return true;

            for (var i = 0; i < state.itemsDestinations.Count; i++)
            {
                if (!state.itemsDestinations[i].ValidateInsertMix(state.movedItems[i], handler))
                    return false;
            }
            return true;
        }

        public static IEnumerable<Item> GetManipulatedItems(in ManipulationContext context, TimeRange rippleRange)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return context.manipulatedTracks.SelectMany(t => t.items.OnlyClips().Where(i => rippleRange.Overlaps(i.GetVisibleRange())));
#pragma warning restore UA2001
        }

        public static IEnumerable<Item> GetFirstItems(IEnumerable<ManipulatedTrack> tracks)
        {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
#pragma warning disable UA2011 // pre-existing usage of FirstOrDefault 
            return tracks.Select(t => t.manipulatedItems.OnlyClips().FirstOrDefault());
#pragma warning restore UA2011
#pragma warning restore UA2001
        }

        public static void MixInsert(IViewModel viewModel, IReadOnlyList<Track> destinationTracks, MoveItemsState state, IManipulationHandler handler)
        {
            if (!state.shouldMoveItems) return;

            for (var i = 0; i < destinationTracks.Count; i++)
            {
                var command = new MixInsert(state.itemsDestinations[i], state.movedItems[i], destinationTracks[i], handler);
                viewModel.Dispatch(command);
            }
        }

        public static void ReplaceInsert(IViewModel viewModel, IReadOnlyList<Track> destinationTracks, MoveItemsState state, IManipulationHandler handler)
        {
            if (!state.shouldMoveItems) return;

            for (var i = 0; i < destinationTracks.Count; i++)
            {
                var command = new ReplaceInsert(state.itemsDestinations[i], state.movedItems[i], destinationTracks[i], handler);
                viewModel.Dispatch(command);
            }
        }

        public static void RippleInsert(IViewModel viewModel, IReadOnlyList<Track> destinationTracks, MoveItemsState state, IManipulationHandler handler)
        {
            if (!state.shouldMoveItems) return;

            for (var i = 0; i < destinationTracks.Count; i++)
            {
                var command = new RippleInsert(state.itemsDestinations[i], state.movedItems[i], destinationTracks[i], handler);
                viewModel.Dispatch(command);
            }
        }

        public static void RippleMove(in ManipulationContext context, IViewModel viewModel, InsertionParameters parameters, MoveItemsState state)
        {
            if (!state.shouldMoveItems) return;

            foreach (ManipulatedTrack manipulatedTrack in context.manipulatedTracks)
            {
#pragma warning disable UA2011 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                Item firstItem = manipulatedTrack.manipulatedItems.FilterOutMarkers().FirstOrDefault();
#pragma warning restore UA2011
                if (firstItem.IsValid())
                {
                    var command = new RippleMove(manipulatedTrack.track, manipulatedTrack.originalCutList, firstItem.start, parameters.totalDelta);
                    viewModel.Dispatch(command);
                }
            }
        }
    }
}
