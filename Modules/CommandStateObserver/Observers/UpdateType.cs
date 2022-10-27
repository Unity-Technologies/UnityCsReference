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
        /// The observer is up-to-date with the state and no update is necessary.
        /// </summary>
        None,

        /// <summary>
        /// The state component maintains changesets and the observer is sufficiently up-to-date
        /// to use them to incrementally update itself.
        /// </summary>
        Partial,

        /// <summary>
        /// The observer should do a complete update.
        /// </summary>
        Complete,
    }
}
