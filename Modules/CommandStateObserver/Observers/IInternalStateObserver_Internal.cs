// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.CommandStateObserver
{
    interface IInternalStateObserver_Internal
    {
        /// <summary>
        /// Gets the last observed component version of <paramref name="stateComponent"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to get the last observed version.</param>
        /// <returns>Returns the last observed component version of <paramref name="stateComponent"/>.</returns>
        StateComponentVersion GetLastObservedComponentVersion_Internal(IStateComponent stateComponent);

        /// <summary>
        /// Updates the observed version for component <paramref name="stateComponent"/> to <paramref name="newVersion"/>.
        /// </summary>
        /// <param name="stateComponent">The state component for which to update the version.</param>
        /// <param name="newVersion">The new version.</param>
        void UpdateObservedVersion_Internal(IStateComponent stateComponent, StateComponentVersion newVersion);
    }

    static class StateObserverHelper_Internal
    {
        internal static IStateObserver CurrentObserver_Internal { get; set; }
    }
}
