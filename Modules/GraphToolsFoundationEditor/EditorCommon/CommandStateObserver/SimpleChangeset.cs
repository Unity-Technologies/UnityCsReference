// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Simple change tracking class.
    /// </summary>
    class SimpleChangeset : IChangeset
    {
        /// <summary>
        /// The changed models.
        /// </summary>
        public HashSet<Hash128> ChangedModels { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleChangeset" /> class.
        /// </summary>
        public SimpleChangeset()
        {
            ChangedModels = new HashSet<Hash128>();
        }

        /// <inheritdoc/>
        public void Clear()
        {
            ChangedModels.Clear();
        }

        /// <inheritdoc/>
        public void AggregateFrom(IEnumerable<IChangeset> changesets)
        {
            Clear();
            foreach (var cs in changesets)
            {
                if (cs is SimpleChangeset changeset)
                {
                    ChangedModels.UnionWith(changeset.ChangedModels);
                }
            }
        }

        /// <inheritdoc />
        public bool Reverse()
        {
            // Nothing to do.
            return true;
        }
    }
}
