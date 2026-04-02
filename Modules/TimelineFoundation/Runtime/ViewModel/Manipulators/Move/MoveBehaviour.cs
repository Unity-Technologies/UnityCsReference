// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Manipulations;
using Unity.Timeline.Foundation.ViewModel.Internals;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class MoveBehaviour
    {
        protected MoveItemsState m_MoveItemsState;
        protected MoveMarkersState m_MarkersState;

        /// <summary>
        /// Use this method to prepare a insertion operation.
        /// </summary>
        /// <param name="manipulationContext">Items to insert</param>
        /// <returns>Valid range for the insertion operation.</returns>
        protected virtual MoveManipulationResult BeginInsert() =>
            new MoveManipulationResult(false, true);

        /// <summary>
        /// Change the insertion destination.
        /// Changing the destination is only supported for single track operations.
        /// </summary>
        /// <param name="previousTrack">The previous destination track</param>
        /// <param name="newTrack">The new insertion destination track</param>
        protected virtual void ChangeTrackTarget(Track previousTrack, Track newTrack) { }

        /// <summary>
        /// Insert the items to their destination. The items should be fetched from the `context` object in <see cref="BeginInsert"/>
        /// </summary>
        /// <param name="insertionParameters"> Provides information about the requested insertion operation.
        ///     <see cref="InsertionParameters"/> </param>
        /// <returns>Returns the result of the insertion operation.
        ///     <see cref="MoveManipulationResult.isValid"/> and <see cref="MoveManipulationResult.needsPreview"/></returns>
        protected virtual MoveManipulationResult TryInsert(InsertionParameters insertionParameters) => default;

        /// <summary>
        /// Finishes the insertion operation. The items should be moved to `destination`.
        /// This method will only be called when the user accepts the insertion operation.
        /// </summary>
        /// <param name="insertionParameters"> Provides information about the requested insertion operation.
        ///     <see cref="InsertionParameters"/> </param>
        protected virtual void FinishInsert(InsertionParameters insertionParameters) { }

        /// <summary>
        /// Reverts the insertion operation to its initial state (ie the state after the `BeginInsert` invocation).
        /// </summary>
        /// <param name="targetsToRevert">Tracks that should have their insertion operation reverted. </param>
        protected virtual void RevertInsert(IReadOnlyList<Track> targetsToRevert) { }

        /// <summary>
        /// Get a snapshot of the state of the manipulated objects for the current manipulation.
        /// </summary>
        /// <param name="updateState">When true, the item and marker states will be updated before returning</param>
        /// <returns> Returns a MoveStateBundle of the current manipulation. </returns>
        protected virtual MoveStateBundle GetCurrentMoveStateBundle(bool updateState = false) { return default; }

        /// <summary>
        /// Applies the current state of the manipulated objects to the current manipulation.
        /// </summary>
        /// <param name="bundle"> MoveStateBundle containing the state of manipulated objects. </param>
        protected virtual void ApplyMoveStateBundle(MoveStateBundle bundle) { }

        /// <summary>
        /// Returns a list of all items that are affected by the manipulation.
        /// Those items' edges will not be part of snapping, if snapping is enabled.
        /// </summary>
        public virtual IReadOnlyList<Item> GetManipulatedItems()
        {
            return context.allItems;
        }

        protected void UpdateItemAndMarkerStatesFromContext()
        {
            m_MoveItemsState = new MoveItemsState();
            if (context.ManipulatingClips())
                m_MoveItemsState = ItemManipulator.ItemMoveStateFromContext(context, handler);

            m_MarkersState = new MoveMarkersState();
            if (context.ManipulatingMarkers())
                m_MarkersState = MarkerManipulator.MarkersMoveStateFromContext(context);
        }

        protected ISequenceViewModel viewModel { get; private set; }
        protected IManipulationHandler handler { get; private set; }
        // internal for tests
        protected internal ManipulationContext context { get; private set; }

        public IReadOnlyList<Track> targets { get; private set; }
        DiscreteTime m_AtTime;
        DiscreteTime m_StartTime;
        bool m_SupportsMoveToTrack;

        bool m_FirstManipulation;

        public MoveManipulationResult BeginManipulation(ISequenceViewModel vm, IManipulationHandler manipulationHandler, ManipulationContext manipulationContext)
        {
            handler = manipulationHandler;
            viewModel = vm;
            context = manipulationContext;
            targets = context.allTracks;
            m_StartTime = m_AtTime = context.totalRange.start;
            m_SupportsMoveToTrack = context.SupportsMoveToTrack(handler);

            m_FirstManipulation = true;
            viewModel?.Dispatch(new SetCurrentManipulation(ManipulationState.Move));

            return BeginInsert();
        }

        public void TransferManipulationFrom(MoveBehaviour other, ManipulationContext newContext)
        {
            other.CommitManipulation();
            handler = other.handler;
            viewModel = other.viewModel;
            context = newContext;
            m_StartTime = other.m_AtTime;
            m_SupportsMoveToTrack = other.m_SupportsMoveToTrack;
            m_AtTime = other.m_AtTime;
            targets = other.targets;

            ApplyMoveStateBundle(other.GetCurrentMoveStateBundle(true));
        }

        public MoveManipulationResult DoInsertManipulation(DiscreteTime atTime)
        {
            if (m_FirstManipulation)
            {
                handler?.Begin();
                m_FirstManipulation = false;
            }

            ManipulateTracks(targets);
            MoveManipulationResult result = TryInsert(BuildInsertionParameters(atTime));

            m_AtTime = atTime;
            return result;
        }

        public void RevertInsertManipulation()
        {
            ManipulateTracks(targets);
            RevertInsert(targets);
        }

        public bool ChangeManipulatedTrack(Track newTarget)
        {
            if (!SupportsMoveToTrack())
                throw new InvalidOperationException("Changing target track is not supported.");

            if (!context.CanMoveToTrack(handler, newTarget))
                return false;

            ChangeTrackTarget(targets[0], newTarget);
            targets = new[] { newTarget };
            return true;
        }

        public void CommitManipulation()
        {
            if (!m_FirstManipulation)
            {
                ManipulateTracks(targets);
                FinishInsert(BuildInsertionParameters(m_AtTime));
                handler?.Commit();
            }

            viewModel?.Dispatch(new SetCurrentManipulation(ManipulationState.None));
        }

        public void CancelManipulation()
        {
            handler?.Cancel();
            viewModel?.Dispatch(new SetCurrentManipulation(ManipulationState.None));
        }

        public bool SupportsMoveToTrack() => m_SupportsMoveToTrack && targets.Count == 1;

        void ManipulateTracks(IEnumerable<Track> tracks) => handler.Manipulate(tracks);

        InsertionParameters BuildInsertionParameters(DiscreteTime atTime)
        {
            DiscreteTime delta = atTime - m_AtTime;
            DiscreteTime totalDelta = atTime - m_StartTime;
            return new InsertionParameters(atTime, delta, totalDelta, targets);
        }
    }
}
