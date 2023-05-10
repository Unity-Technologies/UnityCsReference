// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Abstract base class to manage changeset lists in a <see cref="IStateComponent"/>.
    /// </summary>
    abstract class ChangesetManager
    {
        List<(uint Version, IChangeset Changeset)> m_Changesets = new();

        IChangeset m_AggregatedChangesetCache;
        uint m_AggregatedChangesetCacheVersionFrom;
        uint m_AggregatedChangesetCacheVersionTo;

        /// <summary>
        /// Creates a new, empty <see cref="IChangeset"/>.
        /// </summary>
        /// <returns>The changeset.</returns>
        protected abstract IChangeset CreateChangeset();

        /// <summary>
        /// The current changeset.
        /// </summary>
        protected IChangeset CurrentChangeset { get; set; }

        // Used by tests.
        internal IChangeset LastChangeset_Internal => m_Changesets.Count > 0 ? m_Changesets[^1].Changeset : null;

        /// <summary>
        /// Pushes the current changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        public void PushCurrentChangeset(uint version)
        {
            m_Changesets.Add((version, CurrentChangeset));
            CurrentChangeset = CreateChangeset();
        }

        /// <summary>
        /// Pushes a null changeset in the changeset list, tagging it with <paramref name="version"/>.
        /// </summary>
        /// <param name="version">The version number used to tag the current changeset.</param>
        /// <remarks>A null changeset is used to indicate that changes cannot be tracked for this version.</remarks>
        public void PushNullChangeset(uint version)
        {
            m_Changesets.Add((version, null));
            CurrentChangeset.Clear();
        }

        /// <summary>
        /// Removes changesets tagged with a version number
        /// lower or equal to <paramref name="upToAndIncludingVersion"/> from the changeset list.
        /// </summary>
        /// <param name="upToAndIncludingVersion">Remove changesets up to and including this version.</param>
        /// <param name="currentVersion">The version associated with the current changeset.</param>
        public void RemoveObsoleteChangesets(uint upToAndIncludingVersion, uint currentVersion)
        {
            int countToRemove = 0;
            while (countToRemove < m_Changesets.Count && m_Changesets[countToRemove].Version <= upToAndIncludingVersion)
            {
                countToRemove++;
            }

            if (countToRemove > 0)
                m_Changesets.RemoveRange(0, countToRemove);

            if (upToAndIncludingVersion >= currentVersion)
            {
                CurrentChangeset.Clear();
            }
        }

        /// <summary>
        /// Removes all changesets.
        /// </summary>
        public void RemoveAllChangesets()
        {
            m_Changesets.Clear();
            CurrentChangeset.Clear();
        }

        /// <summary>
        /// Returns whether the <see cref="ChangesetManager"/> can compute a valid changeset for versions greater than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>True if a valid changeset can be computed, false otherwise.</returns>
        public bool HasValidChangesetForVersions(uint afterVersion, uint currentVersion)
        {
            if (m_Changesets.Count == 0 || afterVersion < m_Changesets[0].Version - 1)
                return false;

            foreach (var (version, changeset) in m_Changesets)
            {
                if (version > afterVersion && changeset == null)
                {
                    return false;
                }
            }

            return currentVersion > afterVersion;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version greater than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset, or null if the changeset cannot be computed.</returns>
        public IChangeset GetAggregatedChangeset(uint afterVersion, uint currentVersion)
        {
            if (m_AggregatedChangesetCacheVersionFrom != afterVersion || m_AggregatedChangesetCacheVersionTo != currentVersion)
                m_AggregatedChangesetCache = null;

            if (m_AggregatedChangesetCache != null)
                return m_AggregatedChangesetCache;

            var changesetList = new List<IChangeset>(m_Changesets.Count);
            foreach (var (version, changeset) in m_Changesets)
            {
                if (version > afterVersion)
                {
                    // A null changeset means a complete update was done at version, so we cannot aggregate changesets.
                    if (changeset == null)
                        return null;

                    changesetList.Add(changeset);
                }
            }

            if (changesetList.Count == 0)
            {
                return currentVersion > afterVersion ? CurrentChangeset : CreateChangeset();
            }

            if (currentVersion > afterVersion)
            {
                changesetList.Add(CurrentChangeset);
            }

            m_AggregatedChangesetCache = CreateChangeset();
            m_AggregatedChangesetCache.AggregateFrom(changesetList);
            m_AggregatedChangesetCacheVersionFrom = afterVersion;
            m_AggregatedChangesetCacheVersionTo = currentVersion;

            return m_AggregatedChangesetCache;
        }
    }

    /// <summary>
    /// A class to manage changeset lists in a <see cref="IStateComponent"/>.
    /// </summary>
    /// <typeparam name="TChangeset">The type of changesets.</typeparam>
    class ChangesetManager<TChangeset> : ChangesetManager where TChangeset : class, IChangeset, new()
    {
        /// <summary>
        /// The current changeset.
        /// </summary>
        public new TChangeset CurrentChangeset
        {
            get => (TChangeset)base.CurrentChangeset;
            private set => base.CurrentChangeset = value;
        }

        // Used by tests.
        internal new TChangeset LastChangeset_Internal => (TChangeset)base.LastChangeset_Internal;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangesetManager"/> class.
        /// </summary>
        public ChangesetManager()
        {
            CurrentChangeset = AllocateChangeset();
        }

        static TChangeset AllocateChangeset()
        {
            return new TChangeset();
        }

        /// <inheritdoc />
        protected override IChangeset CreateChangeset()
        {
            return AllocateChangeset();
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version greater than <paramref name="afterVersion"/>.
        /// </summary>
        /// <param name="afterVersion">The version from which to consider changesets.</param>
        /// <param name="currentVersion">The version associated with the current changeset. This should be the current version of the state component.</param>
        /// <returns>The aggregated changeset.</returns>
        public new TChangeset GetAggregatedChangeset(uint afterVersion, uint currentVersion) => (TChangeset)base.GetAggregatedChangeset(afterVersion, currentVersion);
    }
}
