// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Base interface for state observers.
    /// </summary>
    [UnityRestricted]
    internal interface IStateObserver
    {
        /// <summary>
        /// The state components observed by the observer.
        /// </summary>
        IReadOnlyList<IStateComponent> ObservedStateComponents { get; }

        /// <summary>
        /// The state components that can be modified by the observer.
        /// </summary>
        IReadOnlyList<IStateComponent> ModifiedStateComponents { get; }

        /// <summary>
        /// Observes the <see cref="IStateObserver.ObservedStateComponents"/> and modifies the <see cref="IStateObserver.ModifiedStateComponents"/>.
        /// </summary>
        void Observe();

        /// <summary>
        /// Gets the last observed component version of <paramref name="stateComponent"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to get the last observed version.</param>
        /// <returns>The last observed component version of <paramref name="stateComponent"/>.</returns>
        StateComponentVersion GetLastObservedComponentVersion(IStateComponent stateComponent);

        /// <summary>
        /// Updates the observed version for component <paramref name="stateComponent"/> to <paramref name="newVersion"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to update the version.</param>
        /// <param name="newVersion">The new version.</param>
        void UpdateObservedVersion(IStateComponent stateComponent, StateComponentVersion newVersion);
    }
}
