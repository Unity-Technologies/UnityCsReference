// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// State component that holds a <see cref="BlackboardContentModel"/>.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class BlackboardContentStateComponent : StateComponent<BlackboardContentStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for <see cref="BlackboardContentStateComponent"/>.
        /// </summary>
        [UnityRestricted]
        internal class StateUpdater : BaseUpdater<BlackboardContentStateComponent>
        {

            /// <summary>
            /// Saves the state component and mark it for update.
            /// </summary>
            /// <remarks>
            /// 'SaveAndLoadStateForGraph' saves the current state component and replaces it with the state component associated with the specified <see cref="GraphModel"/>.
            /// This method ensures that the state is updated to reflect the current graph.
            /// </remarks>
            public void SaveAndLoadStateForGraph()
            {
                if (m_State.BlackboardModel != null)
                {
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }
        }

        [SerializeReference]
        BlackboardContentModel m_BlackboardModel;

        /// <summary>
        /// The blackboard content model.
        /// </summary>
        public BlackboardContentModel BlackboardModel
        {
            get => m_BlackboardModel;
            protected set => m_BlackboardModel = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardContentStateComponent"/> class.
        /// </summary>
        /// <param name="blackboardContentModel">The model to use.</param>
        public BlackboardContentStateComponent(BlackboardContentModel blackboardContentModel)
        {
            BlackboardModel = blackboardContentModel;
        }

        ChangesetManager<GraphModelStateComponent.Changeset> m_ChangesetManager = new();

        // Uses the same changeset type as GraphModelStateComponent.
        GraphModelStateComponent.Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <inheritdoc />
        public override IChangesetManager ChangesetManager => m_ChangesetManager;

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public GraphModelStateComponent.Changeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc/>
        protected override void Move(IStateComponent other, IChangeset changeset)
        {
            base.Move(other, changeset);

            if (other is BlackboardContentStateComponent blackboardGraphStateComponent)
            {
                if (!BlackboardModel.Equals(blackboardGraphStateComponent.BlackboardModel) || changeset == null)
                {
                    BlackboardModel = blackboardGraphStateComponent.BlackboardModel;
                    SetUpdateType(UpdateType.Complete);
                }
                else
                {
                    (CurrentChangeset as IChangeset).Copy(changeset);
                    SetUpdateType(UpdateType.Partial);
                }

                blackboardGraphStateComponent.BlackboardModel = null;
            }
        }

        public override bool CanBeUndoDataSource(IUndoableStateComponent newStateComponent)
        {
            if (newStateComponent.Guid == Guid)
                return true;

            return newStateComponent is BlackboardContentStateComponent gmsc && m_BlackboardModel.Equals(gmsc.m_BlackboardModel);
        }
    }
}
