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
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset.</returns>
        IChangeset GetAggregatedChangeset(uint afterVersion, uint currentVersion);

        /// <summary>
        /// Pushes the current changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        void PushChangeset(uint version);

        /// <summary>
        /// Removes changesets tagged with a version number
        /// lower or equal to <paramref name="upToAndIncludingVersion"/> from the changeset list.
        /// </summary>
        /// <param name="upToAndIncludingVersion">Remove changesets up to and including this version.</param>
        /// <param name="currentVersion">The version associated with the current changeset.</param>
        void PurgeChangesets(uint upToAndIncludingVersion, uint currentVersion);

        /// <summary>
        /// Gets the version number of the earliest changeset.
        /// </summary>
        /// <returns>The version number of the earliest changeset, or uint.MaxValue if there is no changeset.</returns>
        uint GetEarliestChangesetVersion();
    }
}
