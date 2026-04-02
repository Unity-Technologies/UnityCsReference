// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.ViewModel.Internals;

namespace Unity.Timeline.Foundation.ViewModel
{
    class MixMoveBehaviour : MoveBehaviour
    {
        protected override MoveManipulationResult BeginInsert()
        {
            UpdateItemAndMarkerStatesFromContext();
            return new MoveManipulationResult(isValid: true, needsPreview: false);
        }

        protected override void ChangeTrackTarget(Track previousTrack, Track newTrack)
        {
            if (m_MoveItemsState.shouldMoveItems)
                m_MoveItemsState = new MoveItemsState(m_MoveItemsState.movedItems, new List<CutList> { newTrack.GetCutList_Internal() });
            if (m_MarkersState.shouldMoveMarkers)
                m_MarkersState = new MoveMarkersState(m_MarkersState.movedMarkers, new List<MarkerList> { newTrack.GetMarkerList_Internal() });
        }

        protected override MoveManipulationResult TryInsert(InsertionParameters parameters)
        {
            m_MoveItemsState = ItemManipulator.Move(parameters.delta, m_MoveItemsState);

            if (!ItemManipulator.IsInsertionValid(m_MoveItemsState, handler))
                return new MoveManipulationResult(isValid: false, needsPreview: true);

            ItemManipulator.MixInsert(viewModel, parameters.destinationTracks, m_MoveItemsState, handler);
            MarkerManipulator.Insert(viewModel, parameters.destinationTracks, parameters.totalDelta, m_MarkersState);
            return new MoveManipulationResult(isValid: true, needsPreview: false);
        }

        protected override void RevertInsert(IReadOnlyList<Track> targetsToRevert)
        {
            for (var i = 0; i < targetsToRevert.Count; i++)
            {
#pragma warning disable UA2013 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var command = new SetTrackContents(targetsToRevert[i],
                    m_MoveItemsState.itemsDestinations?.ElementAtOrDefault(i),
                    m_MarkersState.markerDestinations?.ElementAtOrDefault(i));
#pragma warning restore UA2013
                viewModel.Dispatch(command);
            }
        }

        protected override MoveStateBundle GetCurrentMoveStateBundle(bool updateState = false)
        {
            if (updateState)
                UpdateItemAndMarkerStatesFromContext();
            return new MoveStateBundle(m_MoveItemsState, m_MarkersState, !ItemManipulator.IsInsertionValid(m_MoveItemsState, handler));
        }

        protected override void ApplyMoveStateBundle(MoveStateBundle bundle)
        {
            if (bundle.Invalid)
                throw new ArgumentException($"{nameof(bundle)} cannot be default.");

            m_MoveItemsState = bundle.moveItemsState;
            m_MarkersState = bundle.moveMarkersState;
        }
    }
}
