// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// State component for the <see cref="ModelInspectorView"/>.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class ModelInspectorStateComponent : PersistedStateComponent<ModelInspectorStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for the component.
        /// </summary>
        public class StateUpdater : BaseUpdater<ModelInspectorStateComponent>
        {
            /// <summary>
            /// Sets the models being inspected.
            /// </summary>
            /// <param name="models">The models being inspected.</param>
            /// <param name="graphModel">The graph model to which the inspected model belongs.</param>
            public void SetInspectedModels(IEnumerable<Model> models, GraphModel graphModel)
            {
                m_State.SetInspectedModel(models, graphModel);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Sets a section as collapsed or expanded.
            /// </summary>
            /// <param name="sectionModel">The section to modify.</param>
            /// <param name="collapsed">True if the section should be collapsed, false if it should be expanded.</param>
            public void SetSectionCollapsed(InspectorSectionModel sectionModel, bool collapsed)
            {
                sectionModel.Collapsed = collapsed;
                m_State.CurrentChangeset.ChangedModels.Add(sectionModel.Guid);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Sets the inspector ScrollView scroll offset.
            /// </summary>
            /// <param name="scrollOffset">The horizontal and vertical offsets for the ScrollView.</param>
            public void SetScrollOffset(Vector2 scrollOffset)
            {
                m_State.GetInspectorModel().ScrollOffset = scrollOffset;
                m_State.CurrentChangeset.AdditionalChanges |= Changeset.AdditionalChangesEnum.ScrollOffset;
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph for which we want to load a state component.</param>
            public void SaveAndLoadStateForGraph(GraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }
        }

        /// <summary>
        /// Description of changes for the <see cref="ModelInspectorStateComponent"/>.
        /// </summary>
        public class Changeset : SimpleChangeset
        {
            /// <summary>
            /// Describes additional changes to the state.
            /// </summary>
            [Flags]
            public enum AdditionalChangesEnum
            {
                /// <summary>
                /// No changes.
                /// </summary>
                None = 0,

                /// <summary>
                /// The scroll offset of the view changed.
                /// </summary>
                ScrollOffset = 1,
            }

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
        }

        ChangesetManager<Changeset> m_ChangesetManager;

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        GraphModel m_GraphModel;

        List<Model> m_InspectedModels;

        /// <summary>
        /// The models being inspected.
        /// </summary>
        public IReadOnlyList<Model> InspectedModels => m_InspectedModels;

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
        public ModelInspectorStateComponent(IEnumerable<Model> models, GraphModel graphModel)
            : this()
        {
            SetInspectedModel(models, graphModel);
        }

        void SetInspectedModel(IEnumerable<Model> models, GraphModel graphModel)
        {
            m_InspectedModels = new List<Model>(models.Where(m => m != null));
            m_GraphModel = graphModel;

            if (m_InspectedModels.Count == 0)
            {
                m_InspectedModels.Add(graphModel);
            }
        }

        /// <summary>
        /// Gets the inspector model.
        /// </summary>
        /// <returns>The inspector model.</returns>
        public InspectorModel GetInspectorModel()
        {
            if (m_InspectedModels.Count == 0 || m_GraphModel?.Stencil == null)
                return null;

            if (m_InspectedModels.Count >= 1)
            {
                var inspectorModel = m_GraphModel.Stencil.CreateInspectorModel(m_InspectedModels);

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

            if (other is ModelInspectorStateComponent)
            {
                SetUpdateType(UpdateType.Complete);

                m_GraphModel = null;
                m_InspectedModels = new List<Model>();
            }
        }
    }
}
