// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// The type of update an observer should do.
    /// </summary>
    enum UpdateType
    {
        /// <summary>
        /// The <see cref="IStateComponent"/> has not changed since the last observation.
        /// </summary>
        None,

        /// <summary>
        /// The <see cref="IStateComponent"/> can provide a description of what changed since the last observation.
        /// </summary>
        Partial,

        /// <summary>
        /// The <see cref="IStateComponent"/> cannot provide a description of what changed.
        /// Any data held by the <see cref="IStateComponent"/> may have changed.
        /// </summary>
        Complete,
    }
}
