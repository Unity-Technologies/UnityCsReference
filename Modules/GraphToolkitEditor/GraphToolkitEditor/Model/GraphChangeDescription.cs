// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Describes changes made to a graph model.
    /// </summary>
    [UnityRestricted]
    internal class GraphChangeDescription
    {
        HashSet<Hash128> m_NewModels;
        Dictionary<Hash128, ChangeHintList> m_ChangedModels;
        HashSet<Hash128> m_DeletedModels;

        /// <summary>
        /// The new models.
        /// </summary>
        public IReadOnlyCollection<Hash128> NewModels
        {
            get
            {
                if (m_NewModels == null)
                    return Array.Empty<Hash128>();
                return m_NewModels;
            }
        }

        /// <summary>
        /// The changed models.
        /// </summary>
        public IReadOnlyCollection<KeyValuePair<Hash128, ChangeHintList>> ChangedModels
        {
            get
            {
                if (m_ChangedModels == null)
                    return Array.Empty<KeyValuePair<Hash128, ChangeHintList>>();
                return m_ChangedModels;
            }
        }

        /// <summary>
        /// The deleted models.
        /// </summary>
        public IReadOnlyCollection<Hash128> DeletedModels
        {
            get
            {
                if (m_DeletedModels == null)
                    return Array.Empty<Hash128>();
                return m_DeletedModels;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        public GraphChangeDescription()
            : this(Array.Empty<Hash128>(), Array.Empty<KeyValuePair<Hash128, ChangeHintList>>(), Array.Empty<Hash128>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models, with hints about what changed.</param>
        /// <param name="deletedModels">The deleted models.</param>
        protected GraphChangeDescription(
            IEnumerable<Hash128> newModels,
            IReadOnlyCollection<KeyValuePair<Hash128, ChangeHintList>> changedModels,
            IEnumerable<Hash128> deletedModels)
        {
            m_NewModels = newModels != null ? new HashSet<Hash128>(newModels) : null;
            m_ChangedModels = changedModels != null ? new Dictionary<Hash128, ChangeHintList>(changedModels) : null;
            m_DeletedModels = deletedModels != null ? new HashSet<Hash128>(deletedModels) : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphChangeDescription"/> class.
        /// </summary>
        /// <param name="newModels">The new models.</param>
        /// <param name="changedModels">The changed models, with hints about what changed.</param>
        /// <param name="deletedModels">The deleted models.</param>
        public virtual void Initialize(
            IEnumerable<Hash128> newModels,
            IReadOnlyCollection<KeyValuePair<Hash128, ChangeHintList>> changedModels,
            IEnumerable<Hash128> deletedModels)
        {
            m_NewModels = newModels != null ? new HashSet<Hash128>(newModels) : new HashSet<Hash128>();
            m_ChangedModels = changedModels != null ? new Dictionary<Hash128, ChangeHintList>(changedModels) : new Dictionary<Hash128, ChangeHintList>();
            m_DeletedModels = deletedModels != null ? new HashSet<Hash128>(deletedModels) : new HashSet<Hash128>();
        }

        /// <summary>
        /// Merges <paramref name="other"/> into this.
        /// </summary>
        /// <param name="other">The other change description to merge in this change description.</param>
        public void Union(GraphChangeDescription other)
        {
            if (other.m_NewModels != null && m_NewModels != null)
            {
                foreach (var item in other.m_NewModels)
                {
                    m_NewModels.Add(item);
                }
            }

            if (other.m_DeletedModels != null && m_DeletedModels != null)
            {
                foreach (var item in other.m_DeletedModels)
                {
                    m_DeletedModels.Add(item);
                }
            }

            if (other.m_ChangedModels != null && m_ChangedModels != null)
            {
                foreach (var (changedModel, hintList) in other.m_ChangedModels)
                {
                    AddChangedModel(changedModel, hintList);
                }
            }
        }

        protected virtual void AddChangedModel(Hash128 changedModelGuid, ChangeHint hint)
        {
            if (m_ChangedModels == null)
                return;

            if (!m_ChangedModels.TryGetValue(changedModelGuid, out var hintList) || hintList == null)
            {
                m_ChangedModels[changedModelGuid] = ChangeHintList.ToSharedList(hint);
            }
            else
            {
                m_ChangedModels[changedModelGuid] = ChangeHintList.Add(hintList, hint);
            }
        }

        protected virtual void AddChangedModel(Hash128 changedModelGuid, ChangeHintList hints)
        {
            if (m_ChangedModels == null)
                return;

            m_ChangedModels.TryGetValue(changedModelGuid, out var hintList);
            m_ChangedModels[changedModelGuid] = ChangeHintList.AddRange(hintList, hints);
        }

        /// <summary>
        /// Adds a new model to the changes and sets the graph object dirty.
        /// </summary>
        /// <param name="model">The new model.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public virtual GraphChangeDescription AddNewModel(GraphElementModel model)
        {
            if (model != null)
            {
                if (model is not PortModel)
                {
                    model.GraphModel?.SetGraphObjectDirty();
                }

                m_NewModels?.Add(model.Guid);
            }

            return this;
        }

        /// <summary>
        /// Adds new models to the changes and sets the graph object dirty. This assumes all models are from the same graph.
        /// </summary>
        /// <param name="models">The new models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public virtual GraphChangeDescription AddNewModels(IReadOnlyList<GraphElementModel> models)
        {
            if (models == null || models.Count == 0)
                return this;

            var shouldSetDirty = false;
            foreach (var model in models)
            {
                if (model is not PortModel)
                {
                    shouldSetDirty = true;
                }

                m_NewModels?.Add(model.Guid);
            }

            if (shouldSetDirty)
            {
                models[0].GraphModel?.SetGraphObjectDirty();
            }

            return this;
        }

        /// <summary>
        /// Adds a deleted model to the changes and sets the graph object dirty.
        /// </summary>
        /// <param name="model">The deleted model.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddDeletedModel(GraphElementModel model)
        {
            if (model != null)
            {
                if (model is not PortModel)
                {
                    model.GraphModel?.SetGraphObjectDirty();
                }

                m_DeletedModels?.Add(model.Guid);
            }

            return this;
        }

        /// <summary>
        /// Adds deleted models to the changes and sets the graph object dirty. This assumes all models are from the same graph.
        /// </summary>
        /// <param name="models">The deleted models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddDeletedModels(IReadOnlyList<GraphElementModel> models)
        {
            if (models == null || models.Count == 0)
                return this;

            var shouldSetDirty = false;
            foreach (var model in models)
            {
                if (model is not PortModel)
                {
                    shouldSetDirty = true;
                }

                m_DeletedModels?.Add(model.Guid);
            }

            if (shouldSetDirty)
            {
                models[0].GraphModel?.SetGraphObjectDirty();
            }

            return this;
        }

        /// <summary>
        /// Removes a deleted model from the changes.
        /// </summary>
        /// <param name="model">The deleted model.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription RemoveDeletedModel(GraphElementModel model)
        {
            m_DeletedModels?.Remove(model.Guid);
            return this;
        }

        /// <summary>
        /// Removes deleted models from the changes.
        /// </summary>
        /// <param name="models">The deleted models.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription RemoveDeletedModels(IReadOnlyList<GraphElementModel> models)
        {
            if (models == null || models.Count == 0)
                return this;

            foreach (var item in models)
            {
                m_DeletedModels?.Remove(item.Guid);
            }

            return this;
        }

        /// <summary>
        /// Adds a changed model to the changes and sets the graph object dirty.
        /// </summary>
        /// <param name="model">The changed model.</param>
        /// <param name="changeHint">The hint about what changed.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddChangedModel(GraphElementModel model, ChangeHint changeHint)
        {
            if (model is not PortModel)
            {
                model.GraphModel?.SetGraphObjectDirty();
            }

            AddChangedModel(model.Guid, changeHint);
            return this;
        }

        /// <summary>
        /// Adds multiple changed models to the changes and sets the graph object dirty. This assumes all models are from the same graph.
        /// </summary>
        /// <param name="models">The changed models.</param>
        /// <param name="changeHint">The hint about what changed.</param>
        /// <returns>The modified <see cref="GraphChangeDescription"/>.</returns>
        public GraphChangeDescription AddChangedModels(IReadOnlyList<GraphElementModel> models, ChangeHint changeHint)
        {
            if (models == null || models.Count == 0)
                return this;

            var shouldSetDirty = false;
            foreach (var model in models)
            {
                if (model is not PortModel)
                {
                    shouldSetDirty = true;
                }

                AddChangedModel(model.Guid, changeHint);
            }

            if (shouldSetDirty)
            {
                models[0].GraphModel?.SetGraphObjectDirty();
            }

            return this;
        }
    }

    /// <summary>
    /// Describes a scope to gather a <see cref="GraphChangeDescription"/>. <see cref="GraphChangeDescriptionScope"/>s can be nested
    /// and each scope provide the <see cref="GraphChangeDescription"/> related to their scope only. When a scope is disposed, their related <see cref="GraphChangeDescription"/>
    /// is merged back into the parent scope, if any.
    /// </summary>
    [UnityRestricted]
    internal class GraphChangeDescriptionScope : IDisposable
    {
        readonly GraphModel m_GraphModel;
        readonly GraphChangeDescription m_CurrentChangeDescription;

        /// <summary>
        /// The current <see cref="GraphChangeDescription"/> for this scope.
        /// </summary>
        public GraphChangeDescription ChangeDescription => m_CurrentChangeDescription;

        /// <summary>
        /// Creates a <see cref="GraphChangeDescriptionScope"/> for the <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="graphModel">The <see cref="GraphModel"/> on which the scope is applied.</param>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="GraphModel"/> is null.</exception>
        public GraphChangeDescriptionScope(GraphModel graphModel)
        {
            m_GraphModel = graphModel ?? throw new ArgumentNullException(nameof(graphModel));
            m_CurrentChangeDescription = graphModel.PushNewGraphChangeDescription();
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
            m_GraphModel.PopGraphChangeDescription();
        }
    }
}
