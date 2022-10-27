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
        /// Creates a new <see cref="Observation"/> instance that will update the observer's last observed version.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponent">The observed state component.</param>
        /// <returns>An <see cref="Observation"/> object.</returns>
        public static Observation ObserveState(this IStateObserver observer, IStateComponent stateComponent)
        {
            return Observation.Create_Internal(observer, stateComponent);
        }

        /// <summary>
        /// Creates new <see cref="Observation"/> instances that will update the observer's last observed version.
        /// </summary>
        /// <param name="observer">The observers.</param>
        /// <param name="stateComponents">The observed state components.</param>
        /// <returns>An <see cref="IEnumerable{Observation}"/> object.</returns>
        public static IEnumerable<Observation> ObserveStates(this IStateObserver observer, IEnumerable<IStateComponent> stateComponents)
        {
            return stateComponents.Select(s => Observation.Create_Internal(observer, s));
        }

        /// <summary>
        /// Creates a new <see cref="Observation"/> instance that will not update the observer's last observed version.
        /// </summary>
        /// <param name="observer">The observer.</param>
        /// <param name="stateComponent">The observed state component.</param>
        /// <returns>An <see cref="Observation"/> object.</returns>
        public static Observation PeekAtState(this IStateObserver observer, IStateComponent stateComponent)
        {
            return Observation.Create_Internal(observer, stateComponent, false);
        }
    }
}
