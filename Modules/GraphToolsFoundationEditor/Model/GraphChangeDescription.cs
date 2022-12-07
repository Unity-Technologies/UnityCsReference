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
            Union(models, null, null);
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
            Union(null, null, models);
            return this;
        }

        /// <summary>
        /// Removes deleted models from the changes.
        /// </summary>
        /// <param name="models">The deleted models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription RemoveDeletedModels(params GraphElementModel[] models)
        {
            DeletedModels = DeletedModels.Except(models);
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
            Union(null, new Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>>() { { model, new[] { changeHint } } }, null);
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
            Union(null, models.ToDictionary<GraphElementModel, GraphElementModel, IReadOnlyList<ChangeHint>>(m => m, m => new[] { changeHint }), null);
            return this;
        }

        /// <summary>
        /// Adds multiple changed models to the changes.
        /// </summary>
        /// <param name="changedModels">The changed models and hints.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddChangedModels(IReadOnlyDictionary<GraphElementModel, IReadOnlyList<ChangeHint>> changedModels)
        {
            Union(null, changedModels, null);
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
