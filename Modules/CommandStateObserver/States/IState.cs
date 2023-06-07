// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Interface for states.
    /// </summary>
    interface IState
    {
        /// <summary>
        /// All the state components.
        /// </summary>
        IReadOnlyList<IStateComponent> AllStateComponents { get; }

        /// <summary>
        /// Adds a state component to the state.
        /// </summary>
        /// <param name="stateComponent">The state component to add.</param>
        void AddStateComponent(IStateComponent stateComponent);

        /// <summary>
        /// Removes a state component from the state.
        /// </summary>
        /// <param name="stateComponent">The state component to remove.</param>
        void RemoveStateComponent(IStateComponent stateComponent);

        /// <summary>
        /// Delegate called when state components are added to the state or removed from the state.
        /// </summary>
        Action<IState, IStateComponent> OnStateComponentListModified { get; set; }
    }
}
