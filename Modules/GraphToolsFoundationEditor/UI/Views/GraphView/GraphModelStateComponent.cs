// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A component to hold the editor state of the <see cref="GraphModel"/>.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class GraphModelStateComponent : StateComponent<GraphModelStateComponent.StateUpdater>
    {
        /// <summary>
        /// An observer that updates the <see cref="GraphModelStateComponent"/> when a graph is loaded.
        /// </summary>
        public class GraphAssetLoadedObserver : StateObserver
        {
            ToolStateComponent m_ToolStateComponent;
            GraphModelStateComponent m_GraphModelStateComponent;

            /// <summary>
            /// Initializes a new instance of the <see cref="GraphAssetLoadedObserver"/> class.
            /// </summary>
            public GraphAssetLoadedObserver(ToolStateComponent toolStateComponent, GraphModelStateComponent graphModelStateComponent)
                : base(new [] { toolStateComponent},
                    new IStateComponent[] { graphModelStateComponent })
            {
                m_ToolStateComponent = toolStateComponent;
                m_GraphModelStateComponent = graphModelStateComponent;
            }

            /// <inheritdoc />
            public override void Observe()
            {
                using (var obs = this.ObserveState(m_ToolStateComponent))
                {
                    if (obs.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_GraphModelStateComponent.UpdateScope)
                        {
                            updater.SaveAndLoadStateForGraph(m_ToolStateComponent.GraphModel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updater for the <see cref="GraphModelStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<GraphModelStateComponent>
        {
            GraphElementModel[] m_Single = new GraphElementModel[1];

            /// <summary>
            /// Saves the current state and loads the state associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph asset for which to load the state component.</param>
            public void SaveAndLoadStateForGraph(GraphModel graphModel)
            {
                m_State.m_CurrentGraph = new OpenedGraph(graphModel, null);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            void SetDirty(bool somethingChanged)
            {
                if (somethingChanged)
                {
                    m_State.SetUpdateType(UpdateType.Partial);

                    var graphAsset = m_State.m_CurrentGraph.GetGraphAsset();
                    if (graphAsset != null)
                    {
                        graphAsset.Dirty = true;
                    }
                }
            }

            /// <summary>
            /// Marks graph element models as newly created.
            /// </summary>
            /// <param name="models">The newly created models.</param>
            public void MarkNew(IEnumerable<GraphElementModel> models)
            {
                var somethingChanged = m_State.CurrentChangeset.AddNewModels(models);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as newly created.
            /// </summary>
            /// <param name="model">The newly created model.</param>
            public void MarkNew(GraphElementModel model)
            {
                m_Single[0] = model;
                MarkNew(m_Single);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="changes">The changed models.</param>
            public void MarkChanged(IEnumerable<KeyValuePair<GraphElementModel, IReadOnlyList<ChangeHint>>> changes)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(changes);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="models">The changed models.</param>
            /// <param name="changeHint">A hint about what changed on the models. The hint applies to all models.</param>
            public void MarkChanged(IEnumerable<GraphElementModel> models, ChangeHint changeHint)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(models, changeHint);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="models">The changed models.</param>
            /// <param name="changeHints">Hints about what changed on the models. Hints apply to all models.</param>
            public void MarkChanged(IEnumerable<GraphElementModel> models, List<ChangeHint> changeHints = null)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(models, changeHints);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="model">The changed model.</param>
            /// <param name="changeHint">A hint about what changed on the model.</param>
            public void MarkChanged(GraphElementModel model, ChangeHint changeHint)
            {
                m_Single[0] = model;
                MarkChanged(m_Single, changeHint);
            }

            public void MarkForRename(GraphElementModel model)
            {
                if (model != null)
                {
                    m_State.CurrentChangeset.RenamedModel = model;

                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="model">The changed model.</param>
            /// <param name="changeHints">Hints about what changed on the model.</param>
            public void MarkChanged(GraphElementModel model, List<ChangeHint> changeHints = null)
            {
                m_Single[0] = model;
                MarkChanged(m_Single, changeHints);
            }

            /// <summary>
            /// Marks graph element models as deleted.
            /// </summary>
            /// <param name="models">The deleted models.</param>
            public void MarkDeleted(IEnumerable<GraphElementModel> models)
            {
                var somethingChanged = m_State.CurrentChangeset.AddDeletedModels(models);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as deleted.
            /// </summary>
            /// <param name="model">The deleted model.</param>
            public void MarkDeleted(GraphElementModel model)
            {
                m_Single[0] = model;
                MarkDeleted(m_Single);
            }

            /// <summary>
            /// Tells the state component that the graph asset was modified externally.
            /// </summary>
            public void AssetChangedOnDisk()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Mark all the models as added, deleted or changed.
            /// </summary>
            /// <param name="changes">The <see cref="GraphChangeDescription"/> that contains the updated models.</param>
            public void MarkUpdated(GraphChangeDescription changes)
            {
                var somethingChanged = m_State.CurrentChangeset.AddNewModels(changes.NewModels);
                somethingChanged |= m_State.CurrentChangeset.AddChangedModels(changes.ChangedModels);
                somethingChanged |= m_State.CurrentChangeset.AddDeletedModels(changes.DeletedModels);
                SetDirty(somethingChanged);
            }

            public void MarkGraphPropertiesChanged()
            {
                m_State.SetUpdateType(UpdateType.Partial);
                m_State.CurrentChangeset.AddChangedModels(new[] { m_State.GraphModel }, ChangeHint.Data);
                SetDirty(true);
            }
        }

        /// <summary>
        /// The class that describes what changed in the <see cref="GraphModelStateComponent"/>.
        /// </summary>
        [Serializable]
        public class Changeset : IChangeset, ISerializationCallbackReceiver
        {
            static readonly List<ChangeHint> k_DefaultChangeHints = new List<ChangeHint> { ChangeHint.Unspecified };

            [SerializeField]
            List<Hash128> m_NewModelList;

            [SerializeField]
            List<Hash128> m_ChangedModelsList;

            [SerializeField]
            List<int> m_ChangedModelsHintList;

            [SerializeField]
            List<Hash128> m_DeletedModelList;

            HashSet<Hash128> m_NewModels;
            Dictionary<Hash128, List<ChangeHint>> m_ChangedModelsAndHints;
            HashSet<Hash128> m_DeletedModels;

            /// <summary>
            /// The new models.
            /// </summary>
            public IEnumerable<Hash128> NewModels => m_NewModels;

            /// <summary>
            /// The changed models and the hints about what changed.
            /// </summary>
            public IReadOnlyDictionary<Hash128, IReadOnlyList<ChangeHint>> ChangedModelsAndHints =>
                m_ChangedModelsAndHints.ToDictionary<KeyValuePair<Hash128, List<ChangeHint>>, Hash128, IReadOnlyList<ChangeHint>>(
                    kv => kv.Key, kv => kv.Value);

            /// <summary>
            /// The changed models.
            /// </summary>
            public IEnumerable<Hash128> ChangedModels => m_ChangedModelsAndHints.Keys;

            /// <summary>
            /// The deleted models.
            /// </summary>
            public IEnumerable<Hash128> DeletedModels => m_DeletedModels;

            /// <summary>
            /// The models whose title will be focused for rename.
            /// </summary>
            public GraphElementModel RenamedModel { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Changeset" /> class.
            /// </summary>
            public Changeset()
            {
                m_NewModels = new HashSet<Hash128>();
                m_ChangedModelsAndHints = new Dictionary<Hash128, List<ChangeHint>>();
                m_DeletedModels = new HashSet<Hash128>();
            }

            /// <summary>
            /// Adds models to the list of new models.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <returns>True if at least one model was added to the list of new models, false otherwise.</returns>
            public bool AddNewModels(IEnumerable<Model> models)
            {
                return AddNewModels(models.Where(m => m != null).Select(m => m.Guid));
            }

            /// <summary>
            /// Adds models to the list of new models.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <returns>True if at least one model was added to the list of new models, false otherwise.</returns>
            public bool AddNewModels(IEnumerable<Hash128> modelGuids)
            {
                var somethingChanged = false;

                foreach (var guid in modelGuids ?? Enumerable.Empty<Hash128>())
                {
                    if (m_DeletedModels.Contains(guid))
                        continue;

                    m_ChangedModelsAndHints.Remove(guid);
                    m_NewModels.Add(guid);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="changes">The models to add.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<KeyValuePair<GraphElementModel, IReadOnlyList<ChangeHint>>> changes)
            {
                return AddChangedModels(changes
                    .Where(kv => kv.Key != null)
                    .Select(kv => KeyValuePair.Create(kv.Key.Guid, kv.Value)));
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="changes">The models to add.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<KeyValuePair<Hash128, IReadOnlyList<ChangeHint>>> changes)
            {
                var somethingChanged = false;

                foreach (var change in changes)
                {
                    if (m_NewModels.Contains(change.Key) ||
                        m_DeletedModels.Contains(change.Key))
                        continue;

                    AddChangedModel(change.Key, change.Value);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <param name="changeHint">A hint about what changed on the models.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<Model> models, ChangeHint changeHint = null)
            {
                return AddChangedModels(models.Where(m => m != null).Select(m => m.Guid), changeHint);
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <param name="changeHint">A hint about what changed on the models.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<Hash128> modelGuids, ChangeHint changeHint = null)
            {
                var somethingChanged = false;
                changeHint ??= ChangeHint.Unspecified;

                foreach (var model in modelGuids ?? Enumerable.Empty<Hash128>())
                {
                    if (m_NewModels.Contains(model) ||
                        m_DeletedModels.Contains(model))
                        continue;

                    AddChangedModel(model, changeHint);

                    somethingChanged = true;
                }

                return somethingChanged;

            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <param name="changeHints">Hints about what changed on the models. The hints apply to all models.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<Model> models, IReadOnlyList<ChangeHint> changeHints)
            {
                return AddChangedModels(models.Where(m => m != null).Select(m => m.Guid), changeHints);
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <param name="changeHints">Hints about what changed on the models. The hints apply to all models.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<Hash128> modelGuids, IReadOnlyList<ChangeHint> changeHints)
            {
                var somethingChanged = false;
                changeHints ??= k_DefaultChangeHints;

                foreach (var guid in modelGuids ?? Enumerable.Empty<Hash128>())
                {
                    if (m_NewModels.Contains(guid) ||
                        m_DeletedModels.Contains(guid))
                        continue;

                    AddChangedModel(guid, changeHints);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            void AddChangedModel(Hash128 modelGuid, ChangeHint changeHint)
            {
                if (!m_ChangedModelsAndHints.TryGetValue(modelGuid, out var currentHints))
                {
                    m_ChangedModelsAndHints[modelGuid] = new List<ChangeHint> { changeHint };
                }
                else if (!currentHints.Contains(changeHint))
                {
                    currentHints.Add(changeHint);
                }
            }

            void AddChangedModel(Hash128 modelGuid, IReadOnlyList<ChangeHint> changeHints)
            {
                if (!m_ChangedModelsAndHints.TryGetValue(modelGuid, out var currentHints))
                {
                    if (ReferenceEquals(changeHints, k_DefaultChangeHints))
                    {
                        m_ChangedModelsAndHints[modelGuid] = k_DefaultChangeHints;
                    }
                    else
                    {
                        m_ChangedModelsAndHints[modelGuid] = changeHints.ToList();
                    }
                }
                else
                {
                    if (ReferenceEquals(changeHints, k_DefaultChangeHints))
                    {
                        m_ChangedModelsAndHints[modelGuid] = changeHints.ToList();
                        changeHints = m_ChangedModelsAndHints[modelGuid];
                    }

                    foreach (var hint in changeHints)
                    {
                        if (!currentHints.Contains(hint))
                        {
                            currentHints.Add(hint);
                        }
                    }
                }
            }

            /// <summary>
            /// Adds models to the list of deleted models.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <returns>True if at least one model was added to the list of deleted models, false otherwise.</returns>
            public bool AddDeletedModels(IEnumerable<Model> models)
            {
                return AddDeletedModels(models.Where(m => m != null).Select(m => m.Guid));
            }

            /// <summary>
            /// Adds models to the list of deleted models.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <returns>True if at least one model was added to the list of deleted models, false otherwise.</returns>
            public bool AddDeletedModels(IEnumerable<Hash128> modelGuids)
            {
                var somethingChanged = false;
                foreach (var guid in modelGuids ?? Enumerable.Empty<Hash128>())
                {
                    var wasNew = m_NewModels.Remove(guid);
                    m_ChangedModelsAndHints.Remove(guid);

                    if (!wasNew)
                    {
                        m_DeletedModels.Add(guid);
                        somethingChanged = true;
                    }
                }

                return somethingChanged;
            }

            /// <inheritdoc />
            public bool Reverse()
            {
                (m_NewModels, m_DeletedModels) = (m_DeletedModels, m_NewModels);
                (m_NewModelList, m_DeletedModelList) = (m_DeletedModelList, m_NewModelList);
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                m_NewModels.Clear();
                m_ChangedModelsAndHints.Clear();
                m_DeletedModels.Clear();
            }

            /// <inheritdoc/>
            public void AggregateFrom(IEnumerable<IChangeset> changesets)
            {
                Clear();

                // The pattern of how changes should be accumulated is the following:
                // Legend: N=New model, C=Changed model, D=Deleted model, 0=No change
                //
                // N + N = N
                // N + C = N
                // N + D = 0
                // C + N = N
                // C + C = C
                // C + D = D
                // D + N = N
                // D + C = D
                // D + D = D
                //
                // Notice D + N = N. This prevents any changes happening before hand to not get completely lost, for example C + D + N will become N instead
                // of 0 if D + N had equal 0. Furthermore, we are applying the pairwise comparison in reverse to handle some special cases, like D + N + D = D.
                // When doing a forward comparison, (D + N) + D => N + D => 0, but in reverse it becomes D + (N + D) => D + 0 => D.

                foreach (var changeset in changesets.OfType<Changeset>().Reverse())
                {
                    foreach (var newModel in changeset.m_NewModels)
                    {
                        // N + D = 0
                        if (m_DeletedModels.Contains(newModel))
                            m_DeletedModels.Remove(newModel);
                        else
                        {
                            // N + N/C = N
                            if (m_ChangedModelsAndHints.ContainsKey(newModel))
                                m_ChangedModelsAndHints.Remove(newModel);
                            m_NewModels.Add(newModel);
                        }
                    }

                    // C + N/D = N/D
                    foreach (var kv in changeset.m_ChangedModelsAndHints)
                    {
                        if (!m_NewModels.Contains(kv.Key) && !m_DeletedModels.Contains(kv.Key))
                            AddChangedModel(kv.Key, kv.Value);
                    }

                    foreach (var deletedModel in changeset.m_DeletedModels)
                    {
                        // D + N = N
                        if (m_NewModels.Contains(deletedModel))
                            continue;

                        // D + C/D = D
                        if (m_ChangedModelsAndHints.ContainsKey(deletedModel))
                            m_ChangedModelsAndHints.Remove(deletedModel);
                        m_DeletedModels.Add(deletedModel);
                    }
                }

                foreach (var changeset in changesets.OfType<Changeset>())
                {
                    if (changeset.RenamedModel != null && !m_DeletedModels.Contains(changeset.RenamedModel.Guid))
                    {
                        RenamedModel = changeset.RenamedModel;
                        break;
                    }
                }
            }

            /// <inheritdoc />
            public void OnBeforeSerialize()
            {
                m_NewModelList = m_NewModels.ToList();

                m_ChangedModelsList = new List<Hash128>();
                m_ChangedModelsHintList = new List<int>();
                foreach (var kv in m_ChangedModelsAndHints)
                {
                    m_ChangedModelsList.Add(kv.Key);

                    m_ChangedModelsHintList.Add(kv.Value.Count);
                    foreach (var changeHint in kv.Value)
                    {
                        m_ChangedModelsHintList.Add(changeHint.Id);
                    }
                }
                m_DeletedModelList = m_DeletedModels.ToList();
            }

            /// <inheritdoc />
            public void OnAfterDeserialize()
            {
                m_NewModels = new HashSet<Hash128>(m_NewModelList);
                m_DeletedModels = new HashSet<Hash128>(m_DeletedModelList);

                var declaredHints = Enumeration.GetDeclared<ChangeHint>().ToList();

                int hintIndex = 0;
                m_ChangedModelsAndHints = new Dictionary<Hash128, List<ChangeHint>>();
                for (var modelIndex = 0; modelIndex < m_ChangedModelsList.Count; modelIndex++)
                {
                    var hintCount = m_ChangedModelsHintList[hintIndex++];
                    var firstHint = hintIndex;
                    hintIndex += hintCount;

                    var hints = new List<ChangeHint>(hintCount);
                    for (var i = firstHint; i < firstHint + hintCount; i++)
                    {
                        var declaredHintIndex = declaredHints.FindIndex(h => h.Id == m_ChangedModelsHintList[i]);
                        if (declaredHintIndex >= 0)
                        {
                            hints.Add(declaredHints[declaredHintIndex]);
                        }
                    }

                    m_ChangedModelsAndHints[m_ChangedModelsList[modelIndex]] = hints;
                }
            }
        }

        ChangesetManager<Changeset> m_ChangesetManager = new ChangesetManager<Changeset>();
        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <inheritdoc />
        public override ChangesetManager ChangesetManager => m_ChangesetManager;

        [SerializeField]
        OpenedGraph m_CurrentGraph;

        /// <summary>
        /// The <see cref="GraphModel"/>.
        /// <remarks>This method is virtual for tests.</remarks>
        /// </summary>
        public virtual GraphModel GraphModel => m_CurrentGraph.GetGraphModel();

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public Changeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc/>
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is GraphModelStateComponent graphModelStateComponent)
            {
                if (!m_CurrentGraph.Equals(graphModelStateComponent.m_CurrentGraph) || changeset == null)
                {
                    m_CurrentGraph = graphModelStateComponent.m_CurrentGraph;
                    SetUpdateType(UpdateType.Complete);
                }
                else
                {
                    CurrentChangeset.AggregateFrom(new[] { changeset });
                    SetUpdateType(UpdateType.Partial);
                }

                graphModelStateComponent.m_CurrentGraph = default;
            }
        }

        /// <inheritdoc />
        public override void WillPushOnUndoStack(string undoString)
        {
            base.WillPushOnUndoStack(undoString);

            var obj = m_CurrentGraph.GetGraphAsset() as Object;
            if (obj != null)
            {
                Undo.RegisterCompleteObjectUndo(new[] { obj }, undoString);
            }
        }

        /// <inheritdoc />
        public override void UndoRedoPerformed(bool isRedo)
        {
            base.UndoRedoPerformed(isRedo);
            GraphModel?.UndoRedoPerformed();
        }
    }
}
