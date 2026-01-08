// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// State component for the <see cref="ModelInspectorView"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class ModelInspectorStateComponent : PersistedStateComponent<ModelInspectorStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for the component.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<ModelInspectorStateComponent>, IOnGraphLoaded
        {
            /// <summary>
            /// Sets the models being inspected.
            /// </summary>
            /// <param name="models">The models being inspected.</param>
            /// <param name="graphModel">The graph model to which the inspected model belongs.</param>
            public void SetInspectedModels(IReadOnlyList<Model> models, GraphModel graphModel)
            {
                if (m_State.SetInspectedModel(models, graphModel))
                {
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }

            /// <summary>
            /// Sets a section as collapsed or expanded.
            /// </summary>
            /// <param name="sectionModel">The section to modify.</param>
            /// <param name="collapsed">True if the section should be collapsed, false if it should be expanded.</param>
            public void SetSectionCollapsed(InspectorSectionModel sectionModel, bool collapsed)
            {
                if (sectionModel.Collapsed != collapsed)
                {
                    sectionModel.Collapsed = collapsed;
                    m_State.CurrentChangeset.ChangedModels.Add(sectionModel.Guid);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Set expandable ports as collapsed or expanded.
            /// </summary>
            /// <param name="uniqueName">The UniqueName of ports which state to change.</param>
            /// <param name="expanded">True if the ports should be expanded, false if they should be collapsed.</param>
            public void SetExpandablePortsExpanded(string uniqueName, bool expanded)
            {
                m_State.SetExpandablePortsExpanded(uniqueName, expanded);
                m_State.CurrentChangeset.PortUniqueNameChanged.Add(uniqueName);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Sets the inspector ScrollView scroll offset.
            /// </summary>
            /// <param name="scrollOffset">The horizontal and vertical offsets for the ScrollView.</param>
            public void SetScrollOffset(Vector2 scrollOffset)
            {
                if (m_State.ScrollOffset != scrollOffset)
                {
                    m_State.ScrollOffset = scrollOffset;
                    m_State.CurrentChangeset.AdditionalChanges |= Changeset.AdditionalChangesEnum.ScrollOffset;
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph for which we want to load a state component.</param>
            public void OnGraphLoaded(GraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }
        }

        /// <summary>
        /// Description of changes for the <see cref="ModelInspectorStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class Changeset : SimpleChangeset
        {
            /// <summary>
            /// Describes additional changes to the state.
            /// </summary>
            [Flags]
            [UnityRestricted]
            internal enum AdditionalChangesEnum
            {
                /// <summary>
                /// No changes.
                /// </summary>
                None = 0,

                /// <summary>
                /// The scroll offset of the view changed.
                /// </summary>
                ScrollOffset = 1
            }

            /// <summary>
            /// The unique names of the ports that have been expanded or collapsed.
            /// </summary>
            public HashSet<string> PortUniqueNameChanged { get; } = new();

            /// <summary>
            /// Additional changes to the states.
            /// </summary>
            public AdditionalChangesEnum AdditionalChanges { get; set; }

            /// <summary>
            /// Checks whether some change was done to the state.
            /// </summary>
            /// <param name="flags">The change to check.</param>
            /// <returns>True if the change is present, false otherwise.</returns>
            public bool HasAdditionalChange(AdditionalChangesEnum flags)
            {
                return (AdditionalChanges & flags) == flags;
            }

            public override void AggregateFrom(IReadOnlyList<IChangeset> changesets)
            {
                base.AggregateFrom(changesets);
                foreach (var cs in changesets)
                {
                    if (cs is Changeset changeset)
                    {
                        PortUniqueNameChanged.UnionWith(changeset.PortUniqueNameChanged);
                        AdditionalChanges |= changeset.AdditionalChanges;
                    }
                }
            }
        }

        ChangesetManager<Changeset> m_ChangesetManager;

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        GraphModel m_GraphModel;

        [SerializeField]
        List<Hash128> m_InspectedModelGuids;

        [SerializeField]
        Vector2 m_ScrollOffset;

        [SerializeField]
        List<string> m_CollapsedExpandablePorts = new();

        List<Model> m_InspectedModels;

        public virtual Vector2 ScrollOffset
        {
            get => m_ScrollOffset;
            internal set => m_ScrollOffset = value;
        }

        /// <summary>
        /// The models being inspected.
        /// </summary>
        public IReadOnlyList<Model> InspectedModels
        {
            get
            {
                m_InspectedModelGuids ??= new List<Hash128>();
                if (m_InspectedModels.Count == 0 && m_InspectedModelGuids.Count > 0 && m_GraphModel != null)
                {
                    m_InspectedModels.Clear();
                    foreach (var guid in m_InspectedModelGuids)
                    {
                        var model = m_GraphModel.GetModel(guid);
                        if (model != null)
                        {
                            m_InspectedModels.Add(model);
                        }
                    }
                }
                return m_InspectedModels;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorStateComponent" /> class.
        /// </summary>
        public ModelInspectorStateComponent()
        {
            m_ChangesetManager = new ChangesetManager<Changeset>();
            m_InspectedModels = new List<Model>(); // Needed because of serialization.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorStateComponent"/> class.
        /// </summary>
        /// <param name="models">The models being inspected.</param>
        /// <param name="graphModel">The graph model to which the inspected model belongs.</param>
        public ModelInspectorStateComponent(IReadOnlyList<Model> models, GraphModel graphModel)
            : this()
        {
            SetInspectedModel(models, graphModel);
        }

        bool SetInspectedModel(IReadOnlyList<Model> models, GraphModel graphModel)
        {
            var hasChanges = (m_GraphModel != graphModel);

            m_GraphModel = graphModel;

            var inspectedModelCache = InspectedModels;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!inspectedModelCache.SequenceEqual(models.Where(m => m != null)))
#pragma warning restore RS0030
            {
                hasChanges = true;
                ScrollOffset = Vector2.zero;
            }


            if (!hasChanges)
                return false;

            m_InspectedModels.Clear();
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_InspectedModels.AddRange(models.Where(m => m != null));
#pragma warning restore RS0030

            m_InspectedModelGuids ??= new List<Hash128>();
            m_InspectedModelGuids.Clear();

            m_GraphModel = graphModel;

            if (m_InspectedModels.Count == 0 && graphModel != null)
            {
                m_InspectedModels.Add(graphModel);
            }
            foreach (var inspectedModel in m_InspectedModels)
            {
                m_InspectedModelGuids.Add(inspectedModel.Guid);
            }

            return true;
        }

        void SetExpandablePortsExpanded(string uniqueName, bool expanded)
        {
            if (expanded)
            {
                m_CollapsedExpandablePorts.Remove(uniqueName);
            }
            else
            {
                if (!m_CollapsedExpandablePorts.Contains(uniqueName))
                    m_CollapsedExpandablePorts.Add(uniqueName);
            }
        }

        /// <summary>
        /// Whether an expandable port is collapsed or expanded.
        /// </summary>
        /// <param name="uniqueName">The <see cref="PortModel.UniqueName"/> of the <see cref="PortModel"/></param>
        public bool IsExpandablePortExpanded(string uniqueName)
        {
            return !m_CollapsedExpandablePorts.Contains(uniqueName);
        }

        /// <summary>
        /// Gets the inspector model.
        /// </summary>
        /// <returns>The inspector model.</returns>
        public InspectorModel GetInspectorModel()
        {
            if (m_InspectedModels.Count == 0 || m_GraphModel == null)
                return null;

            if (m_InspectedModels.Count >= 1)
            {
                var inspectorModel = m_GraphModel.CreateInspectorModel(m_InspectedModels);

                return inspectorModel;
            }

            return null;
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

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is ModelInspectorStateComponent otherInspectorState)
            {
                SetUpdateType(UpdateType.Complete);

                m_ScrollOffset = otherInspectorState.m_ScrollOffset;
                m_GraphModel = null;
                if (otherInspectorState.m_InspectedModelGuids != null)
                    m_InspectedModelGuids = new List<Hash128>(otherInspectorState.m_InspectedModelGuids);
                if (otherInspectorState.m_CollapsedExpandablePorts != null)
                    m_CollapsedExpandablePorts = new List<string>(otherInspectorState.m_CollapsedExpandablePorts);
                m_InspectedModels.Clear();
            }
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_InspectedModels.Clear();
        }
    }
}
