// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Abstract base class to manage changeset lists in a <see cref="StateComponent{TUpdater}"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class ChangesetManager : IChangesetManager
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

        // Used by tests. Throws if no changeset is available (instead of returning null) because m_Changesets can contain null changesets.
        internal IChangeset LastChangeset => m_Changesets.Count > 0 ? m_Changesets[^1].Changeset : throw new InvalidOperationException("No changeset available.");

        /// <inheritdoc />
        public void PushCurrentChangeset(uint version)
        {
            m_Changesets.Add((version, CurrentChangeset));
            CurrentChangeset = CreateChangeset();
        }

        /// <inheritdoc />
        public void PushNullChangeset(uint version)
        {
            m_Changesets.Add((version, null));
            CurrentChangeset.Clear();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void RemoveAllChangesets()
        {
            m_Changesets.Clear();
            CurrentChangeset.Clear();
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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
    [UnityRestricted]
    internal sealed class ChangesetManager<TChangeset> : ChangesetManager where TChangeset : class, IChangeset, new()
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
        internal new TChangeset LastChangeset => (TChangeset)base.LastChangeset;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangesetManager"/> class.
        /// </summary>
        public ChangesetManager()
        {
            CurrentChangeset = new TChangeset();
        }

        /// <inheritdoc />
        protected override IChangeset CreateChangeset()
        {
            return new TChangeset();
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
