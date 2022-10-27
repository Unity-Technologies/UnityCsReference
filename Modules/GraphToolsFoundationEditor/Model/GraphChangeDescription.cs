// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Describes changes made to a graph model.
    /// </summary>
    class GraphChangeDescription
    {
        /// <summary>
        /// The new models.
        /// </summary>
        public IEnumerable<GraphElementModel> NewModels { get; private set; }

        /// <summary>
        /// The changed models.
        /// </summary>
        public IReadOnlyDictionary<GraphElementModel, IReadOnlyList<ChangeHint>> ChangedModels { get; private set; }

        /// <summary>
        /// The deleted models.
        /// </summary>
        public IEnumerable<GraphElementModel> DeletedModels { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        public GraphChangeDescription()
            : this(null, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models, with hints about what changed.</param>
        /// <param name="deletedModels">The deleted models.</param>
        public GraphChangeDescription(
            IEnumerable<GraphElementModel> newModels,
            IReadOnlyDictionary<GraphElementModel, IReadOnlyList<ChangeHint>> changedModels,
            IEnumerable<GraphElementModel> deletedModels)
        {
            NewModels = newModels ?? Enumerable.Empty<GraphElementModel>();
            ChangedModels = changedModels ?? new Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>>();
            DeletedModels = deletedModels ?? Enumerable.Empty<GraphElementModel>();
        }

        /// <summary>
        /// Merges <paramref name="other"/> into this.
        /// </summary>
        /// <param name="other">The other change description to merge in this change description.</param>
        public void Union(GraphChangeDescription other)
        {
            Union(other.NewModels, other.ChangedModels, other.DeletedModels);
        }

        /// <summary>
        /// Merges change descriptions into this.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models.</param>
        /// <param name="deletedModels">The deleted models.</param>
        public void Union(
            IEnumerable<GraphElementModel> newModels,
            IReadOnlyDictionary<GraphElementModel, IReadOnlyList<ChangeHint>> changedModels,
            IEnumerable<GraphElementModel> deletedModels)
        {
            if (newModels != null)
                NewModels = NewModels.Union(newModels);

            if (deletedModels != null)
                DeletedModels = DeletedModels.Union(deletedModels);

            if (changedModels != null)
            {
                // Convert ChangedModels to a writable dictionary.
                var writableChangedModels = ChangedModels as Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>>;
                writableChangedModels ??= ChangedModels.ToDictionary(kv => kv.Key, kv => kv.Value);

                // Merge changes from changedModels into writableChangedModels.
                foreach (var changedModel in changedModels)
                {
                    if (writableChangedModels.TryGetValue(changedModel.Key, out var hints))
                    {
                        // If writableChangedModels already contains changedModel, merge the hints.

                        // Convert hints to a writable list.
                        var writableHints = hints as List<ChangeHint> ?? hints.ToList();

                        // Add hints from changedModel to rwHint.
                        foreach (var hint in changedModel.Value)
                        {
                            if (!writableHints.Contains(hint))
                            {
                                writableHints.Add(hint);
                            }
                        }

                        writableChangedModels[changedModel.Key] = writableHints;
                    }
                    else
                    {
                        writableChangedModels[changedModel.Key] = changedModel.Value;
                    }

                    ChangedModels = writableChangedModels;
                }
            }
        }
    }
}
