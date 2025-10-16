// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Interface for state components.
    /// </summary>
    [UnityRestricted]
    internal interface IStateComponent
    {
        /// <summary>
        /// The current version of the state component.
        /// </summary>
        uint CurrentVersion { get; }

        /// <summary>
        /// The changeset manager. Should return non-null if the state components needs to track changes.
        /// </summary>
        public IChangesetManager ChangesetManager { get; }


        /// <summary>
        /// The state from which this state component is part of.
        /// </summary>
        public IState State { get; }

        /// <summary>
        /// Gets the type of update an observer should do.
        /// </summary>
        /// <param name="observerVersion">The last state component version observed by the observer.</param>
        /// <returns>Returns the type of update an observer should do.</returns>
        UpdateType GetObserverUpdateType(StateComponentVersion observerVersion);

        /// <summary>
        /// Called when the state component has been added to the state.
        /// </summary>
        /// <param name="state">The state to which the state component was added.</param>
        void OnAddedToState(IState state);

        /// <summary>
        /// Called when the state component has been removed from the state.
        /// </summary>
        /// <param name="state">The state from which the state component was removed.</param>
        void OnRemovedFromState(IState state);
    }
}
