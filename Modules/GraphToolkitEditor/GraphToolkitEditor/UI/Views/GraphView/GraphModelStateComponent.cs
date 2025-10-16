// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A component to hold the editor state of the <see cref="GraphModel"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class GraphModelStateComponent : StateComponent<GraphModelStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for the <see cref="GraphModelStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<GraphModelStateComponent>, IOnGraphLoaded
        {
            GraphElementModel[] m_Single = new GraphElementModel[1];
            Hash128[] m_SingleGuid = new Hash128[1];

            /// <summary>
            /// Saves the current state and loads the state associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph model for which to load the state component.</param>
            public void OnGraphLoaded(GraphModel graphModel)
            {
                m_State.m_CurrentGraph = graphModel?.GetGraphReference() ?? default;
                m_State.SetUpdateType(UpdateType.Complete);
            }

            void MarkForUpdate(bool somethingChanged)
            {
                if (somethingChanged)
                {
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Marks graph element models as newly created.
            /// </summary>
            /// <param name="models">The newly created models.</param>
            public void MarkNew(IReadOnlyList<GraphElementModel> models)
            {
                var somethingChanged = m_State.CurrentChangeset.AddNewModels(models);
                MarkForUpdate(somethingChanged);
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
            public void MarkChanged(IReadOnlyList<KeyValuePair<GraphElementModel, ChangeHintList>> changes)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(changes);
                MarkForUpdate(somethingChanged);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="models">The changed models.</param>
            /// <param name="changeHint">A hint about what changed on the models. The hint applies to all models.
            /// If null, <see cref="ChangeHint.Unspecified"/> is used internally.</param>
            public void MarkChanged(IReadOnlyList<GraphElementModel> models, ChangeHint changeHint)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(models, changeHint);
                MarkForUpdate(somethingChanged);
            }

            /// <summary>
            /// Marks models as changed through Guids.
            /// </summary>
            /// <param name="guids">The changed guids.</param>
            /// <param name="changeHint">A hint about what changed on the models. The hint applies to all models.
            /// If null, <see cref="ChangeHint.Unspecified"/> is used internally.</param>
            public void MarkChanged(IReadOnlyList<Hash128> guids, ChangeHint changeHint)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(guids, changeHint);
                MarkForUpdate(somethingChanged);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="models">The changed models.</param>
            /// <param name="changeHints">Hints about what changed on the models. Hints apply to all models.
            /// If null, <see cref="ChangeHintList.Unspecified"/> is used internally.</param>
            public void MarkChanged(IReadOnlyList<GraphElementModel> models, ChangeHintList changeHints = null)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(models, changeHints);
                MarkForUpdate(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="model">The changed model.</param>
            /// <param name="changeHint">A hint about what changed on the model.
            /// If null, <see cref="ChangeHint.Unspecified"/> is used internally.</param>
            public void MarkChanged(GraphElementModel model, ChangeHint changeHint)
            {
                m_Single[0] = model;
                MarkChanged(m_Single, changeHint);
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="guid">The guid of the changed model.</param>
            /// <param name="changeHint">A hint about what changed on the model.
            /// If null, <see cref="ChangeHint.Unspecified"/> is used internally.</param>
            public void MarkChanged(Hash128 guid, ChangeHint changeHint)
            {
                m_SingleGuid[0] = guid;
                MarkChanged(m_SingleGuid, changeHint);
            }

            /// <summary>
            /// Marks a graph element model to be renamed.
            /// </summary>
            /// <param name="model">The model to be renamed.</param>
            public void MarkForRename(GraphElementModel model)
            {
                if (model != null)
                {
                    m_State.CurrentChangeset.RenamedModel = model;

                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Marks graph element models to be expanded.
            /// </summary>
            /// <param name="models">The models to be expanded.</param>
            public void MarkForExpand(IReadOnlyList<GraphElementModel> models)
            {
                if (models != null)
                {
                    var expandedModels = new List<Hash128>();
                    for (var i = 0; i < models.Count; i++)
                        expandedModels.Add(models[i].Guid);

                    m_State.CurrentChangeset.ExpandedModels = expandedModels;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="model">The changed model.</param>
            /// <param name="changeHints">Hints about what changed on the model.
            /// If null, <see cref="ChangeHintList.Unspecified"/> is used internally.</param>
            public void MarkChanged(GraphElementModel model, ChangeHintList changeHints = null)
            {
                m_Single[0] = model;
                MarkChanged(m_Single, changeHints);
            }

            /// <summary>
            /// Marks graph element models as deleted.
            /// </summary>
            /// <param name="models">The deleted models.</param>
            public void MarkDeleted(IReadOnlyList<GraphElementModel> models)
            {
                var somethingChanged = m_State.CurrentChangeset.AddDeletedModels(models);
                MarkForUpdate(somethingChanged);
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
            /// Tells the state component that the graph object asset was modified externally.
            /// </summary>
            public void AssetChangedOnDisk()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Marks all the models as added, deleted, or changed.
            /// </summary>
            /// <param name="changes">The <see cref="GraphChangeDescription"/> that contains the updated models.</param>
            /// <remarks>
            /// This method updates the current changeset by marking models as added, deleted, or changed based on the provided <see cref="GraphChangeDescription"/>.
            /// It ensures that all modifications to the graph are properly registered.
            /// </remarks>
            public void MarkUpdated(GraphChangeDescription changes)
            {
                var somethingChanged = m_State.CurrentChangeset.AddNewModels(new List<Hash128>(changes.NewModels));
                somethingChanged |= m_State.CurrentChangeset.AddChangedModels(changes.ChangedModels);
                somethingChanged |= m_State.CurrentChangeset.AddDeletedModels(new List<Hash128>(changes.DeletedModels));
                MarkForUpdate(somethingChanged);
            }

            /// <summary>
            /// Marks the graph as changed.
            /// </summary>
            /// <remarks>
            /// 'MarkGraphPropertiesChanged' marks the graph as changed when its properties are modified, which ensures that updates are tracked and properly processed.
            /// This method registers the state's <see cref="GraphModel"/> in the current changeset so that the necessary updates can be applied.
            /// It should be called when the changed models added to the changeset may not fully represent the modification. Some changes might not be explicitly
            /// captured by the changed models.
            /// </remarks>
            public void MarkGraphPropertiesChanged()
            {
                m_State.SetUpdateType(UpdateType.Partial);
                m_State.CurrentChangeset.AddChangedModels(new[] { m_State.GraphModel }, ChangeHint.Data);
            }
        }

        /// <summary>
        /// The class that describes what changed in the <see cref="GraphModelStateComponent"/>.
        /// </summary>
        [Serializable]
        [UnityRestricted]
        internal class Changeset : IChangeset, ISerializationCallbackReceiver
        {
            [SerializeField]
            List<Hash128> m_NewModelList;

            [SerializeField]
            List<Hash128> m_ChangedModelsList;

            [SerializeField]
            List<int> m_ChangedModelsHintList;

            [SerializeField]
            List<Hash128> m_DeletedModelList;

            HashSet<Hash128> m_NewModels;
            Dictionary<Hash128, ChangeHintList> m_ChangedModelsAndHints;
            HashSet<Hash128> m_DeletedModels;

            /// <summary>
            /// The new models.
            /// </summary>
            public IReadOnlyCollection<Hash128> NewModels => m_NewModels;

            /// <summary>
            /// The changed models and the hints about what changed.
            /// </summary>
            public IReadOnlyDictionary<Hash128, ChangeHintList> ChangedModelsAndHints => m_ChangedModelsAndHints;

            /// <summary>
            /// The changed models.
            /// </summary>
            public IReadOnlyCollection<Hash128> ChangedModels => m_ChangedModelsAndHints.Keys;

            /// <summary>
            /// The deleted models.
            /// </summary>
            public IReadOnlyCollection<Hash128> DeletedModels => m_DeletedModels;

            /// <summary>
            /// The models whose title will be focused for rename.
            /// </summary>
            public GraphElementModel RenamedModel { get; set; }

            /// <summary>
            /// The models that will be expanded.
            /// </summary>
            public IReadOnlyList<Hash128> ExpandedModels { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Changeset" /> class.
            /// </summary>
            public Changeset()
            {
                m_NewModels = new HashSet<Hash128>();
                m_ChangedModelsAndHints = new Dictionary<Hash128, ChangeHintList>();
                m_DeletedModels = new HashSet<Hash128>();
            }

            /// <summary>
            /// Adds models to the list of new models.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <returns>True if at least one model was added to the list of new models, false otherwise.</returns>
            public bool AddNewModels(IReadOnlyList<Model> models)
            {
                if (models == null)
                    return false;

                var somethingChanged = false;

                for (var i = 0; i < models.Count; i++)
                {
                    if (models[i] != null)
                    {
                        if (m_DeletedModels.Contains(models[i].Guid))
                            continue;

                        m_ChangedModelsAndHints.Remove(models[i].Guid);
                        m_NewModels.Add(models[i].Guid);

                        somethingChanged = true;
                    }
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of new models.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <returns>True if at least one model was added to the list of new models, false otherwise.</returns>
            public bool AddNewModels(IReadOnlyList<Hash128> modelGuids)
            {
                if (modelGuids == null)
                    return false;

                var somethingChanged = false;

                for (var i = 0; i < modelGuids.Count; i++)
                {
                    if (modelGuids[i] == null || m_DeletedModels.Contains(modelGuids[i]))
                        continue;

                    m_ChangedModelsAndHints.Remove(modelGuids[i]);
                    m_NewModels.Add(modelGuids[i]);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="changes">The models to add.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyList<KeyValuePair<GraphElementModel, ChangeHintList>> changes)
            {
                if (changes == null)
                    return false;

                var somethingChanged = false;

                for (var i = 0; i < changes.Count; i++)
                {
                    var (model, changeHints) = changes[i];
                    if (model == null || m_NewModels.Contains(model.Guid) || m_DeletedModels.Contains(model.Guid))
                        continue;

                    AddChangedModel(model.Guid, changeHints);

                    if (!somethingChanged)
                    {
                        for (var j = 0; j < changeHints.Hints.Count; j++)
                        {
                            if (changeHints.Hints[j] != ChangeHint.NeedsRedraw)
                            {
                                somethingChanged = true;
                                break;
                            }
                        }
                    }
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="changes">The models to add.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyCollection<KeyValuePair<Hash128, ChangeHintList>> changes)
            {
                if (changes == null)
                    return false;

                var somethingChanged = false;

                foreach (var (modelGuid, changeHints) in changes)
                {
                    if (m_NewModels.Contains(modelGuid) || m_DeletedModels.Contains(modelGuid))
                        continue;

                    AddChangedModel(modelGuid, changeHints);

                    if (!somethingChanged)
                    {
                        for (var j = 0; j < changeHints.Hints.Count; j++)
                        {
                            if (changeHints.Hints[j] != ChangeHint.NeedsRedraw)
                            {
                                somethingChanged = true;
                                break;
                            }
                        }
                    }
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <param name="changeHint">A hint about what changed on the models.
            /// If null, <see cref="ChangeHint.Unspecified"/> is used internally.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyList<Model> models, ChangeHint changeHint = null)
            {
                if (models == null)
                    return false;

                var somethingChanged = false;
                changeHint ??= ChangeHint.Unspecified;

                for (var i = 0; i < models.Count; i++)
                {
                    var model = models[i];
                    if (model == null || m_NewModels.Contains(model.Guid) || m_DeletedModels.Contains(model.Guid))
                        continue;

                    AddChangedModel(model.Guid, changeHint);
                    somethingChanged |= changeHint != ChangeHint.NeedsRedraw;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <param name="changeHint">A hint about what changed on the models.
            /// If null, <see cref="ChangeHint.Unspecified"/> is used internally.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyList<Hash128> modelGuids, ChangeHint changeHint = null)
            {
                if (modelGuids == null)
                    return false;

                var somethingChanged = false;
                changeHint ??= ChangeHint.Unspecified;

                for (var i = 0; i < modelGuids.Count; i++)
                {
                    if (m_NewModels.Contains(modelGuids[i]) || m_DeletedModels.Contains(modelGuids[i]))
                        continue;

                    AddChangedModel(modelGuids[i], changeHint);
                    somethingChanged |= changeHint != ChangeHint.NeedsRedraw;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <param name="changeHints">Hints about what changed on the models. The hints apply to all models.
            /// If null, <see cref="ChangeHintList.Unspecified"/> is used internally.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyList<Model> models, ChangeHintList changeHints)
            {
                if (models == null)
                    return false;

                var somethingChanged = false;
                changeHints ??= ChangeHintList.Unspecified;

                for (var i = 0; i < models.Count; i++)
                {
                    var model = models[i];
                    if (model == null || m_NewModels.Contains(model.Guid) || m_DeletedModels.Contains(model.Guid))
                        continue;
                    AddChangedModel(model.Guid, changeHints);
                }

                if (models.Count > 0)
                {
                    for (var i = 0; i < changeHints.Hints.Count; i++)
                    {
                        if (changeHints.Hints[i] != ChangeHint.NeedsRedraw)
                        {
                            somethingChanged = true;
                            break;
                        }
                    }
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <param name="changeHints">Hints about what changed on the models. The hints apply to all models.
            /// If null, <see cref="ChangeHintList.Unspecified"/> is used internally.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyList<Hash128> modelGuids, ChangeHintList changeHints)
            {
                if (modelGuids == null)
                    return false;

                var somethingChanged = false;
                changeHints ??= ChangeHintList.Unspecified;

                for (var i = 0; i < modelGuids.Count; i++)
                {
                    if (m_NewModels.Contains(modelGuids[i]) || m_DeletedModels.Contains(modelGuids[i]))
                        continue;
                    AddChangedModel(modelGuids[i], changeHints);
                }

                if (modelGuids.Count > 0)
                {
                    for (var i = 0; i < changeHints.Hints.Count; i++)
                    {
                        if (changeHints.Hints[i] != ChangeHint.NeedsRedraw)
                        {
                            somethingChanged = true;
                            break;
                        }
                    }
                }

                return somethingChanged;
            }

            void AddChangedModel(Hash128 modelGuid, ChangeHint changeHint)
            {
                m_ChangedModelsAndHints.TryGetValue(modelGuid, out var currentHints);
                m_ChangedModelsAndHints[modelGuid] = ChangeHintList.Add(currentHints, changeHint);
            }

            void AddChangedModel(Hash128 modelGuid, ChangeHintList changeHints)
            {
                m_ChangedModelsAndHints.TryGetValue(modelGuid, out var currentHints);
                m_ChangedModelsAndHints[modelGuid] = ChangeHintList.AddRange(currentHints, changeHints);
            }

            /// <summary>
            /// Adds models to the list of deleted models.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <returns>True if at least one model was added to the list of deleted models, false otherwise.</returns>
            public bool AddDeletedModels(IReadOnlyList<Model> models)
            {
                if (models == null)
                    return false;

                var somethingChanged = false;

                for (var i = 0; i < models.Count; i++)
                {
                    if (models[i] == null)
                        continue;

                    var wasNew = m_NewModels.Remove(models[i].Guid);
                    m_ChangedModelsAndHints.Remove(models[i].Guid);

                    if (!wasNew)
                    {
                        m_DeletedModels.Add(models[i].Guid);
                        somethingChanged = true;
                    }
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of deleted models.
            /// </summary>
            /// <param name="modelGuids">The guids of the models to add.</param>
            /// <returns>True if at least one model was added to the list of deleted models, false otherwise.</returns>
            public bool AddDeletedModels(IReadOnlyList<Hash128> modelGuids)
            {
                if (modelGuids == null)
                    return false;

                var somethingChanged = false;

                for (var i = 0; i < modelGuids.Count; i++)
                {
                    var wasNew = m_NewModels.Remove(modelGuids[i]);
                    m_ChangedModelsAndHints.Remove(modelGuids[i]);

                    if (!wasNew)
                    {
                        m_DeletedModels.Add(modelGuids[i]);
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
            public void AggregateFrom(IReadOnlyList<IChangeset> changesets)
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

                for (var index = changesets.Count - 1; index >= 0; index--)
                {
                    if (changesets[index] is not Changeset changeset)
                        continue;

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

                foreach (var cs in changesets)
                {
                    if (cs is Changeset { RenamedModel: not null } changeset && !m_DeletedModels.Contains(changeset.RenamedModel.Guid))
                    {
                        RenamedModel = changeset.RenamedModel;
                        break;
                    }
                }

                foreach (var cs in changesets)
                {
                    if (cs is Changeset { ExpandedModels: not null } changeset)
                    {
                        var expandedModels = new List<Hash128>();
                        for (var i = 0; i < changeset.ExpandedModels.Count; i++)
                        {
                            var modelToExpand = changeset.ExpandedModels[i];
                            if (!m_DeletedModels.Contains(modelToExpand))
                                expandedModels.Add(modelToExpand);
                        }
                        ExpandedModels = expandedModels;
                    }
                }
            }

            /// <inheritdoc />
            public void OnBeforeSerialize()
            {
                m_NewModelList = new List<Hash128>(m_NewModels);
                m_ChangedModelsList = new List<Hash128>();
                m_ChangedModelsHintList = new List<int>();

                foreach (var kv in m_ChangedModelsAndHints)
                {
                    m_ChangedModelsList.Add(kv.Key);

                    m_ChangedModelsHintList.Add(kv.Value.Hints.Count);
                    for (var i = 0; i < kv.Value.Hints.Count; i++)
                    {
                        var changeHint = kv.Value.Hints[i];
                        m_ChangedModelsHintList.Add(changeHint.Id);
                    }
                }
                m_DeletedModelList = new List<Hash128>(m_DeletedModels);
            }

            /// <inheritdoc />
            public void OnAfterDeserialize()
            {
                m_NewModels = new HashSet<Hash128>(m_NewModelList);
                m_DeletedModels = new HashSet<Hash128>(m_DeletedModelList);

                var declaredHints = new List<ChangeHint>(Enumeration.GetDeclared<ChangeHint>());

                int hintIndex = 0;
                m_ChangedModelsAndHints = new Dictionary<Hash128, ChangeHintList>();
                for (var modelIndex = 0; modelIndex < m_ChangedModelsList.Count; modelIndex++)
                {
                    var hintCount = m_ChangedModelsHintList[hintIndex++];
                    var firstHint = hintIndex;
                    hintIndex += hintCount;

                    if (hintCount == 0)
                    {
                        m_ChangedModelsAndHints[m_ChangedModelsList[modelIndex]] = ChangeHintList.Unspecified;
                    }
                    else if (hintCount == 1)
                    {
                        var declaredHintIndex = declaredHints.FindIndex(h => h.Id == m_ChangedModelsHintList[firstHint]);
                        if (declaredHintIndex >= 0)
                        {
                            m_ChangedModelsAndHints[m_ChangedModelsList[modelIndex]] = ChangeHintList.ToSharedList(declaredHints[declaredHintIndex]);
                        }
                        else
                        {
                            m_ChangedModelsAndHints[m_ChangedModelsList[modelIndex]] = ChangeHintList.Unspecified;
                        }
                    }
                    else
                    {
                        ChangeHintList changeHintList = null;
                        for (var i = firstHint; i < firstHint + hintCount; i++)
                        {
                            var declaredHintIndex = declaredHints.FindIndex(h => h.Id == m_ChangedModelsHintList[i]);
                            if (declaredHintIndex >= 0)
                            {
                                changeHintList = ChangeHintList.Add(changeHintList, declaredHints[declaredHintIndex]);
                            }
                        }

                        if (changeHintList == null)
                        {
                            m_ChangedModelsAndHints[m_ChangedModelsList[modelIndex]] = ChangeHintList.Unspecified;
                        }
                        else
                        {
                            m_ChangedModelsAndHints[m_ChangedModelsList[modelIndex]] = changeHintList;
                        }
                    }
                }
            }
        }

        ChangesetManager<Changeset> m_ChangesetManager = new ChangesetManager<Changeset>();
        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        [SerializeField]
        GraphReference m_CurrentGraph;

        /// <summary>
        /// The <see cref="GraphModel"/>.
        /// </summary>
        /// <remarks>This method is virtual for tests.</remarks>
        public virtual GraphModel GraphModel => ResolveGraphModelFromReference(m_CurrentGraph);


        public GraphTool GraphTool { get; internal set; }

        GraphModel ResolveGraphModelFromReference(in GraphReference reference)
        {
            return GraphTool != null ? GraphTool.ResolveGraphModelFromReference(reference) : GraphReference.ResolveGraphModel(reference);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphModelStateComponent"/> class.
        /// </summary>
        /// <param name="graphTool">The <see cref="GraphTool"/>. </param>
        /// <param name="guid">The unique identifier of the new instance of <see cref="GraphModelStateComponent"/>.</param>
        /// <remarks>
        /// This constructor initializes a new instance of the <see cref="GraphModelStateComponent"/> class. It assigns a unique
        /// identifier to the instance, which ensures it can be tracked and referenced within the state components.
        /// </remarks>
        public GraphModelStateComponent(GraphTool graphTool, Hash128 guid)
            : base(guid)
        {
            GraphTool = graphTool;
        }

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
                    (CurrentChangeset as IChangeset).Copy(changeset);
                    SetUpdateType(UpdateType.Partial);
                }

                graphModelStateComponent.m_CurrentGraph = default;
            }
        }

        /// <inheritdoc />
        public override void WillPushOnUndoStack(string undoString)
        {
            base.WillPushOnUndoStack(undoString);

            var obj = ResolveGraphModelFromReference(m_CurrentGraph).GraphObject;
            if (obj != null)
            {
                Undo.RegisterCompleteObjectUndo(new Object[] { obj }, undoString);
            }
        }

        /// <inheritdoc />
        public override void UndoRedoPerformed(bool isRedo)
        {
            base.UndoRedoPerformed(isRedo);
            GraphModel?.UndoRedoPerformed();
        }

        /// <inheritdoc />
        public override bool CanBeUndoDataSource(IUndoableStateComponent newStateComponent)
        {
            if (newStateComponent.Guid == Guid)
                return true;

            return newStateComponent is GraphModelStateComponent gmsc && m_CurrentGraph.Equals(gmsc.m_CurrentGraph);
        }
    }
}
