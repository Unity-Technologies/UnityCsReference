// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Describes changes made to a graph model.
    /// </summary>
    class GraphChangeDescription
    {
        HashSet<Hash128> m_NewModels;
        Dictionary<Hash128, List<ChangeHint>> m_ChangedModels;
        HashSet<Hash128> m_DeletedModels;

        /// <summary>
        /// The new models.
        /// </summary>
        public IEnumerable<Hash128> NewModels => m_NewModels;

        /// <summary>
        /// The changed models.
        /// </summary>
        public IReadOnlyDictionary<Hash128, IReadOnlyList<ChangeHint>> ChangedModels =>
            m_ChangedModels.ToDictionary<KeyValuePair<Hash128, List<ChangeHint>>, Hash128, IReadOnlyList<ChangeHint>>(
                kv => kv.Key, kv => kv.Value);

        /// <summary>
        /// The deleted models.
        /// </summary>
        public IEnumerable<Hash128> DeletedModels => m_DeletedModels;

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
            IEnumerable<Hash128> newModels,
            IReadOnlyDictionary<Hash128, IReadOnlyList<ChangeHint>> changedModels,
            IEnumerable<Hash128> deletedModels)
        {
            m_NewModels = newModels?.ToHashSet()
                ?? new HashSet<Hash128>();

            m_ChangedModels = changedModels?.ToDictionary(kv => kv.Key, kv => kv.Value.ToList())
                ?? new Dictionary<Hash128, List<ChangeHint>>();

            m_DeletedModels = deletedModels?.ToHashSet()
                ?? new HashSet<Hash128>();
        }

        /// <summary>
        /// Merges <paramref name="other"/> into this.
        /// </summary>
        /// <param name="other">The other change description to merge in this change description.</param>
        public void Union(GraphChangeDescription other)
        {
            Union(other.m_NewModels, other.m_ChangedModels, other.m_DeletedModels);
        }

        static void AddItems(HashSet<Hash128> hashSet, IEnumerable<Hash128> items)
        {
            foreach (var item in items)
            {
                hashSet.Add(item);
            }
        }

        static void RemoveItems(HashSet<Hash128> hashSet, IEnumerable<Hash128> items)
        {
            foreach (var item in items)
            {
                hashSet.Remove(item);
            }
        }

        /// <summary>
        /// Merges change descriptions into this.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models.</param>
        /// <param name="deletedModels">The deleted models.</param>
        internal void Union(
            IEnumerable<Hash128> newModels,
            IEnumerable<KeyValuePair<Hash128, List<ChangeHint>>> changedModels,
            IEnumerable<Hash128> deletedModels)
        {
            if (newModels != null)
                AddItems(m_NewModels, newModels);

            if (deletedModels != null)
                AddItems(m_DeletedModels, deletedModels);

            if (changedModels != null)
            {
                // Merge changes from changedModels into writableChangedModels.
                foreach (var changedModel in changedModels)
                {
                    if (!m_ChangedModels.TryGetValue(changedModel.Key, out var hints) || hints == null)
                    {
                        hints = new List<ChangeHint>();
                    }

                    foreach (var hint in changedModel.Value)
                    {
                        if (!hints.Contains(hint))
                        {
                            hints.Add(hint);
                        }
                    }

                    m_ChangedModels[changedModel.Key] = hints;
                }
            }
        }

        /// <summary>
        /// Adds new models to the changes.
        /// </summary>
        /// <param name="models">The new models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddNewModels(params GraphElementModel[] models)
        {
            return AddNewModels(models as IEnumerable<GraphElementModel>);
        }

        /// <summary>
        /// Adds new models to the changes.
        /// </summary>
        /// <param name="models">The new models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddNewModels(IEnumerable<GraphElementModel> models)
        {
            Union(models.Select(m => m.Guid), null, null);
            return this;
        }

        /// <summary>
        /// Adds deleted models to the changes.
        /// </summary>
        /// <param name="models">The deleted models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddDeletedModels(params GraphElementModel[] models)
        {
            return AddDeletedModels(models as IEnumerable<GraphElementModel>);
        }

        /// <summary>
        /// Adds deleted models to the changes.
        /// </summary>
        /// <param name="models">The deleted models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddDeletedModels(IEnumerable<GraphElementModel> models)
        {
            Union(null, null, models.Select(m => m.Guid));
            return this;
        }

        /// <summary>
        /// Removes deleted models from the changes.
        /// </summary>
        /// <param name="models">The deleted models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription RemoveDeletedModels(params GraphElementModel[] models)
        {
            RemoveItems(m_DeletedModels, models.Select(m => m.Guid));
            return this;
        }

        /// <summary>
        /// Adds a changed model to the changes.
        /// </summary>
        /// <param name="model">The changed model.</param>
        /// <param name="changeHint">The hint about what changed.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddChangedModel(GraphElementModel model, ChangeHint changeHint)
        {
            Union(null, new Dictionary<Hash128, List<ChangeHint>> { { model.Guid, new List<ChangeHint> { changeHint } } }, null);
            return this;
        }

        /// <summary>
        /// Adds multiple changed models to the changes.
        /// </summary>
        /// <param name="models">The changed models.</param>
        /// <param name="changeHint">The hint about what changed.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddChangedModels(IEnumerable<GraphElementModel> models, ChangeHint changeHint)
        {
            Union(null, models.ToDictionary(m => m.Guid, _ => new List<ChangeHint> { changeHint }), null);
            return this;
        }

        /// <summary>
        /// Adds multiple changed models to the changes.
        /// </summary>
        /// <param name="changedModels">The changed models guids and hints.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddChangedModels(IReadOnlyDictionary<Hash128, IReadOnlyList<ChangeHint>> changedModels)
        {
            Union(null, changedModels.Select(kv =>
            {
                var list = kv.Value as List<ChangeHint>;
                list ??= kv.Value.ToList();

                return new KeyValuePair<Hash128, List<ChangeHint>>(kv.Key, list);
            }), null);
            return this;
        }
    }

    /// <summary>
    /// Describes a scope to gather a <see cref="GraphChangeDescription"/>. <see cref="GraphChangeDescriptionScope"/>s can be nested
    /// and each scope provide the <see cref="GraphChangeDescription"/> related to their scope only. When a scope is disposed, their related <see cref="GraphChangeDescription"/>
    /// is merged back into the parent scope, if any.
    /// </summary>
    class GraphChangeDescriptionScope : IDisposable
    {
        readonly GraphModel m_GraphModel;
        readonly GraphChangeDescription m_CurrentChangeDescription;

        /// <summary>
        /// The current <see cref="GraphChangeDescription"/> for this scope.
        /// </summary>
        public GraphChangeDescription ChangeDescription => m_CurrentChangeDescription;

        /// <summary>
        /// Creates a <see cref="GraphChangeDescriptionScope"/> for the <param name="graphModel"></param>.
        /// </summary>
        /// <param name="graphModel">The <see cref="GraphModel"/> on which the scope is applied.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public GraphChangeDescriptionScope(GraphModel graphModel)
        {
            m_GraphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
            m_CurrentChangeDescription = graphModel.PushNewGraphChangeDescription_Internal();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GraphChangeDescriptionScope()
        {
            Dispose(false);
        }

        void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            m_GraphModel.PopGraphChangeDescription_Internal();
        }
    }
}
