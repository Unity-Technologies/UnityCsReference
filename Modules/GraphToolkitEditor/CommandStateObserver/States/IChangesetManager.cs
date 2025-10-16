// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Interface for changeset managers.
    /// </summary>
    [UnityRestricted]
    internal interface IChangesetManager
    {
        /// <summary>
        /// Pushes the current changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        void PushCurrentChangeset(uint version);

        /// <summary>
        /// Pushes a null changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        /// <remarks>A null changeset is used to indicate that changes cannot be tracked for this version.</remarks>
        void PushNullChangeset(uint version);

        /// <summary>
        /// Removes changesets tagged with a version number
        /// lower or equal to <paramref name="upToAndIncludingVersion"/> from the changeset list.
        /// </summary>
        /// <param name="upToAndIncludingVersion">Remove changesets up to and including this version.</param>
        /// <param name="currentVersion">The version associated with the current changeset.</param>
        void RemoveObsoleteChangesets(uint upToAndIncludingVersion, uint currentVersion);

        /// <summary>
        /// Removes all changesets.
        /// </summary>
        void RemoveAllChangesets();

        /// <summary>
        /// Returns whether the <see cref="ChangesetManager"/> can compute a valid changeset for versions greater than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>True if a valid changeset can be computed, false otherwise.</returns>
        bool HasValidChangesetForVersions(uint afterVersion, uint currentVersion);

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version greater than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset, or null if the changeset cannot be computed.</returns>
        IChangeset GetAggregatedChangeset(uint afterVersion, uint currentVersion);
    }
}
