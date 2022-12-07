// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Extension methods for <see cref="IStateObserver"/>.
    /// </summary>
    static class StateObserverExtensions
    {
        /// <summary>
        /// Creates a new <see cref="Observation"/> instance.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponent">The observed state component.</param>
        /// <returns>An <see cref="Observation"/> object.</returns>
        public static Observation ObserveState(this IStateObserver observer, IStateComponent stateComponent)
        {
            return Observation.Create_Internal(observer, stateComponent);
        }

        /// <summary>
        /// Creates new <see cref="Observation"/> instances for a single observer observing multiple state components.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponents">The observed state components.</param>
        /// <returns>The observations as an <see cref="IEnumerable{Observation}"/> object.</returns>
        public static IEnumerable<Observation> ObserveStates(this IStateObserver observer, IEnumerable<IStateComponent> stateComponents)
        {
            return stateComponents.Select(s => Observation.Create_Internal(observer, s));
        }

        /// <summary>
        /// Creates a new <see cref="Observation"/> instance.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponent">The observed state component.</param>
        /// <returns>An <see cref="Observation"/> object.</returns>
        /// <remarks>The observation returned by this method will not change the observer's
        /// last observed version of the state component.
        /// Thus, this method can be used to find out if an observer needs to schedule
        /// an <see cref="ObserveState"/> to be done at a later time.</remarks>
        public static Observation PeekAtState(this IStateObserver observer, IStateComponent stateComponent)
        {
            return Observation.Create_Internal(observer, stateComponent, false);
        }
    }
}
