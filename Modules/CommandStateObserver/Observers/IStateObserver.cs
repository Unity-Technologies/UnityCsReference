// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Base interface for state observers.
    /// </summary>
    interface IStateObserver
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
    }
}
