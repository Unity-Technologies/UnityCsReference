// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Interface for changeset managers.
    /// </summary>
    interface IChangesetManager
    {
        /// <summary>
        /// The current changeset.
        /// </summary>
        IChangeset CurrentChangeset { get; }

        /// <summary>
        /// Pushes the current changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        void PushChangeset(uint version);

        /// <summary>
        /// Removes obsolete changesets from the changeset list. Changesets tagged with a version number
        /// lower or equal to <paramref name="untilVersion"/> are considered obsolete.
        /// </summary>
        /// <param name="untilVersion">Purge changeset up to and including this version.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The version number of the earliest changeset.</returns>
        uint PurgeOldChangesets(uint untilVersion, uint currentVersion);
    }
}
