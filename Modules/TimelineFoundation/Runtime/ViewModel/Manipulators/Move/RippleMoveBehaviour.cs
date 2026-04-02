// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Sequence;
using Unity.Timeline.Foundation.Model;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.ViewModel.Internals;

namespace Unity.Timeline.Foundation.ViewModel
{
    class RippleMoveBehaviour : MoveBehaviour
    {
        bool m_IsDetached;
        TimeRange m_ValidRange;

        protected override MoveManipulationResult BeginInsert()
        {
            m_IsDetached = false;
            UpdateItemAndMarkerStatesFromContext();
            m_ValidRange = GetValidRange();
            return new MoveManipulationResult(isValid: true, needsPreview: false, validRange: m_ValidRange);
        }

        protected override void ChangeTrackTarget(Track previousTrack, Track newTrack)
        {
            m_IsDetached = true;
            if (m_MoveItemsState.shouldMoveItems)
                m_MoveItemsState = new MoveItemsState(m_MoveItemsState.movedItems, new List<CutList> { newTrack.GetCutList_Internal() });
            if (m_MarkersState.shouldMoveMarkers)
                m_MarkersState = new MoveMarkersState(m_MarkersState.movedMarkers, new List<MarkerList> { newTrack.GetMarkerList_Internal() });
        }

        protected override MoveManipulationResult TryInsert(InsertionParameters parameters)
        {
            m_MoveItemsState = ItemManipulator.Move(parameters.delta, m_MoveItemsState);

            if (m_IsDetached)
                return new MoveManipulationResult(isValid: true, needsPreview: true);

            return DoRippleMove(parameters);
        }

        protected override void RevertInsert(IReadOnlyList<Track> targetsToRevert)
        {
            for (var i = 0; i < targetsToRevert.Count; i++)
#pragma warning disable UA2013 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                viewModel.Dispatch(new SetTrackContents(targetsToRevert[i],
                    m_MoveItemsState.itemsDestinations?.ElementAtOrDefault(i),
                    m_MarkersState.markerDestinations?.ElementAtOrDefault(i)));
#pragma warning restore UA2013
        }

        MoveManipulationResult DoRippleMove(InsertionParameters parameters)
        {
            ItemManipulator.RippleMove(context, viewModel, parameters, m_MoveItemsState);
            MarkerManipulator.RippleMove(viewModel, parameters.destinationTracks, parameters.totalDelta, m_MarkersState);
            return new MoveManipulationResult(isValid: true, needsPreview: false, validRange: m_ValidRange);
        }

        protected override void FinishInsert(InsertionParameters insertionParameters)
        {
            if (!m_IsDetached)
                return;

            ItemManipulator.RippleInsert(viewModel, insertionParameters.destinationTracks, m_MoveItemsState, handler);
            MarkerManipulator.Insert(viewModel, insertionParameters.destinationTracks, insertionParameters.totalDelta, m_MarkersState);
        }

        public override IReadOnlyList<Item> GetManipulatedItems()
        {
            if (m_IsDetached)
                return base.GetManipulatedItems();

            var rippleRange = new TimeRange(context.totalRange.start, DiscreteTime.MaxValue);
            var manipulatedContent = new List<Item>();
            if (m_MoveItemsState.shouldMoveItems)
                manipulatedContent.AddRange(ItemManipulator.GetManipulatedItems(context, rippleRange));
            if (m_MarkersState.shouldMoveMarkers)
                manipulatedContent.AddRange(MarkerManipulator.GetManipulatedItems(context, rippleRange));

            return manipulatedContent;
        }

        protected override MoveStateBundle GetCurrentMoveStateBundle(bool updateState = false)
        {
            if (updateState)
                UpdateItemAndMarkerStatesFromContext();
            return new MoveStateBundle(m_MoveItemsState, m_MarkersState, m_IsDetached);
        }

        protected override void ApplyMoveStateBundle(MoveStateBundle bundle)
        {
            if (bundle.Invalid)
                throw new ArgumentException($"{nameof(bundle)} cannot be invalid.");

            m_MoveItemsState = bundle.moveItemsState;
            m_MarkersState = bundle.moveMarkersState;
            m_IsDetached = bundle.detached;
            m_ValidRange = GetValidRange();

            if (SupportsMoveToTrack() && m_IsDetached)
                ChangeManipulatedTrack(targets[0]);
        }

        TimeRange GetValidRange()
        {
            var offset = DiscreteTime.MaxValue;

            IEnumerable<Item> firstItems = m_MoveItemsState.shouldMoveItems ? ItemManipulator.GetFirstItems(context.manipulatedTracks) : Array.Empty<Item>();
            IEnumerable<Item> firstMarkers = m_MarkersState.shouldMoveMarkers ? MarkerManipulator.GetFirstItems(context.manipulatedTracks) : Array.Empty<Item>();
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (Item item in firstItems.Concat(firstMarkers))
#pragma warning restore UA2001
            {
                if (item.IsValid())
                    offset = DiscreteTimeTimeExtensions.Min(offset, item.start - CalculateLeftBound(item));
            }

            offset = DiscreteTimeTimeExtensions.Max(DiscreteTime.Zero, offset);
            return new TimeRange(context.totalRange.start - offset, DiscreteTime.MaxValue);
        }

        static DiscreteTime CalculateLeftBound(Item item)
        {
            Item previousItem = item.Previous(item.isMarker ? ItemTypeFlags.Marker : ItemTypeFlags.Interval);

            if (previousItem.IsValid())
                return previousItem.isGap ? previousItem.start : previousItem.end;

            return DiscreteTime.Zero;
        }
    }
}
