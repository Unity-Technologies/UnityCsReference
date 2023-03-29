// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Holds the graph elements that need to be repositioned in the current view.
    /// </summary>
    class AutoPlacementStateComponent : StateComponent<AutoPlacementStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for <see cref="AutoPlacementStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<AutoPlacementStateComponent>
        {
            /// <summary>
            /// Marks a model as needing to be aligned.
            /// </summary>
            /// <param name="model">The model to align.</param>
            public void MarkModelToAutoAlign(GraphElementModel model)
            {
                m_State.CurrentChangeset.AddModelToAutoAlign(model);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Marks a model as needing to be repositioned after its creation.
            /// </summary>
            /// <param name="model">The model to reposition.</param>
            /// <remarks>Nodes created from wires need to recompute their position after their creation to make sure
            /// that the last hovered position corresponds to the connected port. In the case of an incompatible connection,
            /// the last hovered position will correspond to the nodes' middle height or width, depending on the orientation.</remarks>
            public void MarkModelToRepositionAtCreation((GraphElementModel, WireModel, WireSide) model)
            {
                m_State.CurrentChangeset.AddModelToRepositionAtCreation(model);
                m_State.SetUpdateType(UpdateType.Partial);
            }
        }

        /// <summary>
        /// The class that describes what changed in the <see cref="AutoPlacementStateComponent"/>.
        /// </summary>
        [Serializable]
        public class Changeset : IChangeset, ISerializationCallbackReceiver
        {
            /// <summary>
            /// Holds information about a model that needs to be repositioned immediately after creation.
            /// </summary>
            [Serializable]
            public struct ModelToReposition
            {
                [SerializeField]
                SerializableGUID m_Model;
                [SerializeField]
                SerializableGUID m_WireModel;
                [SerializeField]
                WireSide m_Side;

                /// <summary>
                /// The model to reposition.
                /// </summary>
                public SerializableGUID Model
                {
                    get => m_Model;
                    set => m_Model = value;
                }

                /// <summary>
                /// A wire attached to the node.
                /// </summary>
                public SerializableGUID WireModel
                {
                    get => m_WireModel;
                    set => m_WireModel = value;
                }

                /// <summary>
                /// The side on which the wire is attached to the node.
                /// </summary>
                public WireSide WireSide
                {
                    get => m_Side;
                    set => m_Side = value;
                }
            }

            [SerializeField]
            List<SerializableGUID> m_ModelsToAutoAlignList;

            [SerializeField]
            List<ModelToReposition> m_ModelsToRepositionAtCreationList;

            protected HashSet<SerializableGUID> m_ModelsToAutoAlign;
            protected HashSet<ModelToReposition> m_ModelsToRepositionAtCreation;

            /// <summary>
            /// The models that need to be aligned.
            /// </summary>
            public IEnumerable<SerializableGUID> ModelsToAutoAlign => m_ModelsToAutoAlign;

            /// <summary>
            /// The models that need to be repositioned at their creation.
            /// </summary>
            public IEnumerable<ModelToReposition> ModelsToRepositionAtCreation => m_ModelsToRepositionAtCreation;

            /// <summary>
            /// Initializes a new instance of the <see cref="BlackboardViewStateComponent.Changeset" /> class.
            /// </summary>
            public Changeset()
            {
                m_ModelsToAutoAlign = new HashSet<SerializableGUID>();
                m_ModelsToRepositionAtCreation = new HashSet<ModelToReposition>();
            }

            /// <summary>
            /// Adds a model to the list of models to auto-align.
            /// </summary>
            /// <param name="model">The model to add.</param>
            public virtual void AddModelToAutoAlign(GraphElementModel model)
            {
                m_ModelsToAutoAlign.Add(model.Guid);
                m_ModelsToRepositionAtCreation.RemoveWhere(e => e.Model == model.Guid);
            }

            /// <summary>
            /// Adds a model to the list of models to reposition at their creation.
            /// </summary>
            /// <param name="model">The model to add.</param>
            public virtual void AddModelToRepositionAtCreation((GraphElementModel, WireModel, WireSide) model)
            {
                if (!m_ModelsToAutoAlign.Contains(model.Item1.Guid))
                    m_ModelsToRepositionAtCreation.Add(new ModelToReposition
                    {
                        Model = model.Item1.Guid,
                        WireModel = model.Item2.Guid,
                        WireSide = model.Item3
                    }
                    );
            }

            /// <inheritdoc />
            public virtual bool Reverse()
            {
                return true;
            }

            /// <inheritdoc/>
            public virtual void Clear()
            {
                m_ModelsToAutoAlign.Clear();
                m_ModelsToRepositionAtCreation.Clear();
            }

            /// <inheritdoc/>
            public virtual void AggregateFrom(IEnumerable<IChangeset> changesets)
            {
                foreach (var changeset in changesets.OfType<Changeset>())
                {
                    m_ModelsToAutoAlign.UnionWith(changeset.m_ModelsToAutoAlign);
                    m_ModelsToRepositionAtCreation.UnionWith(changeset.m_ModelsToRepositionAtCreation);
                }

                m_ModelsToRepositionAtCreation.RemoveWhere(m => m_ModelsToAutoAlign.Contains(m.Model));
            }

            /// <inheritdoc />
            public virtual void OnBeforeSerialize()
            {
                m_ModelsToAutoAlignList = m_ModelsToAutoAlign.ToList();
                m_ModelsToRepositionAtCreationList = m_ModelsToRepositionAtCreation.ToList();
            }

            /// <inheritdoc />
            public virtual void OnAfterDeserialize()
            {
                m_ModelsToAutoAlign = new HashSet<SerializableGUID>(m_ModelsToAutoAlignList);
                m_ModelsToRepositionAtCreation = new HashSet<ModelToReposition>(m_ModelsToRepositionAtCreationList);
            }
        }

        ChangesetManager<Changeset> m_ChangesetManager = new ChangesetManager<Changeset>();
        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public Changeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is AutoPlacementStateComponent)
            {
                SetUpdateType(UpdateType.Partial);

                CurrentChangeset.AggregateFrom(new[] { changeset });
            }
        }
    }
}
