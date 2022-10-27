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

        IChangeset IChangesetManager.CurrentChangeset => CurrentChangeset;

        /// <inheritdoc />
        public void PushChangeset(uint version)
        {
            m_Changesets.Add((version, CurrentChangeset));
            CurrentChangeset = new TChangeset();
        }

        /// <inheritdoc />
        public uint PurgeOldChangesets(uint untilVersion, uint currentVersion)
        {
            int countToRemove = 0;
            while (countToRemove < m_Changesets.Count && m_Changesets[countToRemove].Version <= untilVersion)
            {
                countToRemove++;
            }

            if (countToRemove > 0)
                m_Changesets.RemoveRange(0, countToRemove);

            if (untilVersion >= currentVersion)
            {
                CurrentChangeset.Clear();
                return currentVersion;
            }

            return m_Changesets.Count > 0 ? m_Changesets[0].Version : currentVersion;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset.</returns>
        public TChangeset GetAggregatedChangeset(uint sinceVersion, uint currentVersion)
        {
            if (m_AggregatedChangesetCacheVersionFrom != sinceVersion || m_AggregatedChangesetCacheVersionTo != currentVersion)
                m_AggregatedChangesetCache = null;

            if (m_AggregatedChangesetCache != null)
                return m_AggregatedChangesetCache;

            var changesetList = m_Changesets
                .Where(c => c.Version > sinceVersion)
                .Select(c => c.Changeset)
                .ToList();

            if (changesetList.Count == 0)
            {
                return currentVersion > sinceVersion ? CurrentChangeset : null;
            }

            if (currentVersion > sinceVersion)
            {
                changesetList.Add(CurrentChangeset);
            }

            m_AggregatedChangesetCache = new TChangeset();
            m_AggregatedChangesetCache.AggregateFrom(changesetList);
            m_AggregatedChangesetCacheVersionFrom = sinceVersion;
            m_AggregatedChangesetCacheVersionTo = currentVersion;

            return m_AggregatedChangesetCache;
        }
    }
}
