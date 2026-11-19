// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Commands.Manipulations;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.ViewModel
{
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal abstract class TrimBehaviour
    {
        [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
        internal enum Location
        {
            Start,
            End
        }

        protected ISequenceViewModel viewModel { get; private set; }
        protected IManipulationHandler handler { get; private set; }
        protected Item itemToTrim { get; private set; }
        public Location location { get; private set; }

        /// <summary>
        /// Use this method to prepare a trim operation.
        /// </summary>
        /// <remarks>
        /// The location of the trim is available with the <see cref="location"/> property.
        /// The viewmodel is available with the <see cref="viewModel"/> property.
        /// The manipulation handler is available with the <see cref="handler"/> property.
        /// </remarks>
        /// <param name="item">Item to trim</param>
        protected virtual void Begin(Item item) { }

        /// <summary>
        /// Use this method to trim an item.
        /// </summary>
        /// <remarks>
        /// The item to trim is available with the <see cref="itemToTrim"/> property.
        /// The location of the trim is available with the <see cref="location"/> property.
        /// The viewmodel is available with the <see cref="viewModel"/> property.
        /// The manipulation handler is available with the <see cref="handler"/> property.
        /// </remarks>
        /// <param name="requestedTime">The time at which the item should be trimmed.</param>
        protected virtual void Trim(DiscreteTime requestedTime) { }

        /// <summary>
        /// Use this method to commit the trim operation.
        /// This method will only be called when the user accepts the trim operation.
        /// </summary>
        protected virtual void Finish() { }

        /// <summary>
        /// Returns a list of all items that are affected by the manipulation.
        /// Those items' edges will not be part of snapping, if snapping is enabled.
        /// </summary>
        public virtual IReadOnlyList<Item> GetManipulatedItems()
        {
            return new[] { itemToTrim };
        }

        bool m_FirstManipulation;

        public void BeginManipulation(ISequenceViewModel vm, IManipulationHandler manipulationHandler, Location alocation, Item item)
        {
            handler = manipulationHandler;
            viewModel = vm;
            itemToTrim = item;
            location = alocation;

            m_FirstManipulation = true;
            viewModel.Dispatch(new SetCurrentManipulation(ManipulationState.Trim));

            Begin(itemToTrim);
        }

        public void TrimManipulation(DiscreteTime requestedTime)
        {
            if (m_FirstManipulation)
            {
                handler?.Begin();
                m_FirstManipulation = false;
            }

            ManipulateTrack(itemToTrim.parent);
            Trim(requestedTime);
        }

        public void CommitManipulation()
        {
            if (!m_FirstManipulation)
            {
                ManipulateTrack(itemToTrim.parent);
                Finish();
                handler?.Commit();
            }

            viewModel.Dispatch(new SetCurrentManipulation(ManipulationState.None));
        }

        public void CancelManipulation()
        {
            handler?.Cancel();
            viewModel.Dispatch(new SetCurrentManipulation(ManipulationState.None));
        }

        void ManipulateTrack(Track track)
        {
            handler?.Manipulate(track);
        }

        public static Location InvertLocation(Location location) => location == Location.Start ? Location.End : Location.Start;
    }
}
