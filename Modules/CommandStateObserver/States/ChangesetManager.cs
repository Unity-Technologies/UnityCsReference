// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// A class to manage changeset lists in a <see cref="IStateComponent"/>.
    /// </summary>
    /// <typeparam name="TChangeset">The type of changesets.</typeparam>
    class ChangesetManager<TChangeset> : IChangesetManager where TChangeset : class, IChangeset, new()
    {
        List<(uint Version, TChangeset Changeset)> m_Changesets = new List<(uint, TChangeset)>();

        TChangeset m_AggregatedChangesetCache;
        uint m_AggregatedChangesetCacheVersionFrom;
        uint m_AggregatedChangesetCacheVersionTo;

        /// <summary>
        /// The current changeset.
        /// </summary>
        public TChangeset CurrentChangeset { get; private set; } = new TChangeset();

        // Used by tests.
        internal TChangeset LastChangeset_Internal
        {
            get
            {
                if (m_Changesets.Count > 0)
                {
                    return m_Changesets[^1].Changeset;
                }

                return null;
            }
        }

        IChangeset IChangesetManager.CurrentChangeset => CurrentChangeset;

        /// <summary>
        /// Pushes the current changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        public void PushChangeset(uint version)
        {
            m_Changesets.Add((version, CurrentChangeset));
            CurrentChangeset = new TChangeset();
        }

        /// <summary>
        /// Removes changesets tagged with a version number
        /// lower or equal to <paramref name="upToAndIncludingVersion"/> from the changeset list.
        /// </summary>
        /// <param name="upToAndIncludingVersion">Remove changesets up to and including this version.</param>
        /// <param name="currentVersion">The version associated with the current changeset.</param>
        public void PurgeChangesets(uint upToAndIncludingVersion, uint currentVersion)
        {
            if (upToAndIncludingVersion == uint.MaxValue)
            {
                m_Changesets.Clear();
            }
            else
            {
                int countToRemove = 0;
                while (countToRemove < m_Changesets.Count && m_Changesets[countToRemove].Version <= upToAndIncludingVersion)
                {
                    countToRemove++;
                }

                if (countToRemove > 0)
                    m_Changesets.RemoveRange(0, countToRemove);
            }

            if (upToAndIncludingVersion > currentVersion)
            {
                CurrentChangeset.Clear();
            }
        }

        /// <summary>
        /// Gets the version number of the earliest changeset.
        /// </summary>
        /// <returns>The version number of the earliest changeset, or uint.MaxValue if there is no changeset.</returns>
        public uint GetEarliestChangesetVersion()
        {
            return m_Changesets.Count > 0 ? m_Changesets[0].Version : uint.MaxValue;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset.</returns>
        IChangeset IChangesetManager.GetAggregatedChangeset(uint afterVersion, uint currentVersion)
        {
            return GetAggregatedChangeset(afterVersion, currentVersion);
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version greater than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset.</returns>
        public TChangeset GetAggregatedChangeset(uint afterVersion, uint currentVersion)
        {
            if (m_AggregatedChangesetCacheVersionFrom != afterVersion || m_AggregatedChangesetCacheVersionTo != currentVersion)
                m_AggregatedChangesetCache = null;

            if (m_AggregatedChangesetCache != null)
                return m_AggregatedChangesetCache;

            var changesetList = m_Changesets
                .Where(c => c.Version > afterVersion)
                .Select(c => c.Changeset)
                .ToList();

            if (changesetList.Count == 0)
            {
                return currentVersion > afterVersion ? CurrentChangeset : null;
            }

            if (currentVersion > afterVersion)
            {
                changesetList.Add(CurrentChangeset);
            }

            m_AggregatedChangesetCache = new TChangeset();
            m_AggregatedChangesetCache.AggregateFrom(changesetList);
            m_AggregatedChangesetCacheVersionFrom = afterVersion;
            m_AggregatedChangesetCacheVersionTo = currentVersion;

            return m_AggregatedChangesetCache;
        }
    }
}
