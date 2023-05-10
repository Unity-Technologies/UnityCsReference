// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// State component holding blackboard view related data.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class BlackboardViewStateComponent : PersistedStateComponent<BlackboardViewStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="BlackboardViewStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<BlackboardViewStateComponent>
        {
            /// <summary>
            /// Sets the expanded state of the variable declaration model in the blackboard.
            /// </summary>
            /// <param name="model">The model for which to set the state.</param>
            /// <param name="expanded">True if the variable should be expanded, false otherwise.</param>
            public void SetVariableDeclarationModelExpanded(VariableDeclarationModel model, bool expanded)
            {
                bool isExpanded = m_State.GetVariableDeclarationModelExpanded(model);
                if (isExpanded && !expanded)
                {
                    m_State.m_BlackboardExpandedRowHashStates?.Remove(model.Guid);
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
                else if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardExpandedRowHashStates?.Add(model.Guid);
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Sets the expanded state of the group model in the blackboard.
            /// </summary>
            /// <param name="model">The model for which to set the state.</param>
            /// <param name="expanded">True if the group should be expanded, false otherwise.</param>
            public void SetGroupModelExpanded(GroupModel model, bool expanded)
            {
                bool isExpanded = m_State.GetGroupExpanded(model);
                if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardCollapsedGroupHashStates?.Remove(model.Guid);
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
                else if (isExpanded && !expanded)
                {
                    m_State.m_BlackboardCollapsedGroupHashStates?.Add(model.Guid);
                    m_State.CurrentChangeset.ChangedModels.Add(model.Guid);
                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Sets the Blackboard ScrollView scroll offset.
            /// </summary>
            /// <param name="scrollOffset">The horizontal and vertical offsets for the ScrollView.</param>
            public void SetScrollOffset(Vector2 scrollOffset)
            {
                m_State.m_ScrollOffset = scrollOffset;
                m_State.CurrentChangeset.AdditionalChanges |= Changeset.AdditionalChangesEnum.ScrollOffset;
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The asset for which we want to load a state component.</param>
            public void SaveAndLoadStateForGraph(GraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }
        }

        /// <summary>
        /// Description of changes for the <see cref="BlackboardViewStateComponent"/>.
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

        ChangesetManager<Changeset> m_ChangesetManager = new ChangesetManager<Changeset>();

        /// <inheritdoc />
        public override ChangesetManager ChangesetManager => m_ChangesetManager;

        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        [SerializeField, Obsolete]
#pragma warning disable CS0618
        List<SerializableGUID> m_BlackboardExpandedRowStates;
#pragma warning restore CS0618

        [SerializeField, Obsolete]
#pragma warning disable CS0618
        List<SerializableGUID> m_BlackboardCollapsedGroupStates;
#pragma warning restore CS0618

        [SerializeField]
        List<Hash128> m_BlackboardExpandedRowHashStates;

        [SerializeField]
        List<Hash128> m_BlackboardCollapsedGroupHashStates;

        [SerializeField]
        Vector2 m_ScrollOffset;

        /// <summary>
        /// The scroll offset of the blackboard scroll view.
        /// </summary>
        public Vector2 ScrollOffset => m_ScrollOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewStateComponent" /> class.
        /// </summary>
        public BlackboardViewStateComponent()
        {
            m_BlackboardExpandedRowHashStates = new List<Hash128>();
            m_BlackboardCollapsedGroupHashStates = new List<Hash128>();
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

        /// <summary>
        /// Gets the expanded state of a variable declaration model.
        /// </summary>
        /// <param name="model">The variable declaration model.</param>
        /// <returns>True is the UI for the model should be expanded. False otherwise.</returns>
        public bool GetVariableDeclarationModelExpanded(VariableDeclarationModel model)
        {
            return m_BlackboardExpandedRowHashStates?.Contains(model.Guid) ?? false;
        }

        /// <summary>
        /// Gets the expanded state of a variable declaration model.
        /// </summary>
        /// <param name="model">The variable declaration model.</param>
        /// <returns>True is the UI for the model should be expanded. False otherwise.</returns>
        public bool GetGroupExpanded(GroupModel model)
        {
            return !(m_BlackboardCollapsedGroupHashStates?.Contains(model.Guid) ?? false);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is BlackboardViewStateComponent blackboardViewStateComponent)
            {
                var changedRows = new HashSet<Hash128>(m_BlackboardExpandedRowHashStates);
                changedRows.SymmetricExceptWith(blackboardViewStateComponent.m_BlackboardExpandedRowHashStates);
                if (changedRows.Count != 0)
                {
                    CurrentChangeset.ChangedModels.UnionWith(changedRows);
                    SetUpdateType(UpdateType.Partial);

                    m_BlackboardExpandedRowHashStates = blackboardViewStateComponent.m_BlackboardExpandedRowHashStates;
                }
                blackboardViewStateComponent.m_BlackboardExpandedRowHashStates = null;

                var changedGroups = new HashSet<Hash128>(m_BlackboardCollapsedGroupHashStates);
                changedGroups.SymmetricExceptWith(blackboardViewStateComponent.m_BlackboardCollapsedGroupHashStates);
                if (changedGroups.Count != 0)
                {
                    CurrentChangeset.ChangedModels.UnionWith(changedGroups);
                    SetUpdateType(UpdateType.Partial);

                    m_BlackboardCollapsedGroupHashStates = blackboardViewStateComponent.m_BlackboardCollapsedGroupHashStates;
                }
                blackboardViewStateComponent.m_BlackboardCollapsedGroupHashStates = null;

                if (m_ScrollOffset != blackboardViewStateComponent.m_ScrollOffset)
                {
                    CurrentChangeset.AdditionalChanges |= Changeset.AdditionalChangesEnum.ScrollOffset;
                    SetUpdateType(UpdateType.Partial);

                    m_ScrollOffset = blackboardViewStateComponent.m_ScrollOffset;
                }
            }
        }

        /// <inheritdoc />
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

#pragma warning disable CS0612
#pragma warning disable CS0618
            m_BlackboardExpandedRowStates = new List<SerializableGUID>(m_BlackboardExpandedRowHashStates.Count);
            foreach (var guid in m_BlackboardExpandedRowHashStates)
            {
                m_BlackboardExpandedRowStates.Add(guid);
            }

            m_BlackboardCollapsedGroupStates = new List<SerializableGUID>(m_BlackboardCollapsedGroupHashStates.Count);
            foreach (var guid in m_BlackboardCollapsedGroupHashStates)
            {
                m_BlackboardCollapsedGroupStates.Add(guid);
            }
#pragma warning restore CS0618
#pragma warning restore CS0612
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

#pragma warning disable CS0612
            if (m_BlackboardExpandedRowStates != null)
            {
                m_BlackboardExpandedRowHashStates = new List<Hash128>(m_BlackboardExpandedRowStates.Count);
                foreach (var guid in m_BlackboardExpandedRowStates)
                {
                    m_BlackboardExpandedRowHashStates.Add(guid);
                }

                m_BlackboardExpandedRowStates = null;
            }

            if (m_BlackboardCollapsedGroupStates != null)
            {
                m_BlackboardCollapsedGroupHashStates = new List<Hash128>(m_BlackboardCollapsedGroupStates.Count);
                foreach (var guid in m_BlackboardCollapsedGroupStates)
                {
                    m_BlackboardCollapsedGroupHashStates.Add(guid);
                }

                m_BlackboardCollapsedGroupStates = null;
            }
#pragma warning restore CS0612
        }
    }
}
