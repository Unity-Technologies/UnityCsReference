// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A model for the BlackboardView. Holds state components.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class BlackboardRootViewModel : RootViewModel
    {
        /// <summary>
        /// The blackboard view state.
        /// </summary>
        public BlackboardViewStateComponent ViewState { get; }

        /// <summary>
        /// The <see cref="SelectionStateComponent"/>. Holds the blackboard selection.
        /// </summary>
        public SelectionStateComponent SelectionState { get; }

        /// <summary>
        /// The blackboard content.
        /// </summary>
        public BlackboardContentStateComponent BlackboardContentState { get; }

        /// <summary>
        /// The graph model state.
        /// </summary>
        public GraphModelStateComponent GraphModelState { get; }

        /// <summary>
        /// The highlighter state.
        /// </summary>
        public DeclarationHighlighterStateComponent HighlighterState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardRootViewModel"/> class.
        /// </summary>
        /// <param name="highlighterState">The highlighter state.</param>
        /// <param name="graphModelState">The graph model state.</param>
        /// <param name="blackboardContentModel">The blackboard content model.</param>
        /// <param name="graphViewGuid">The highlighter state.</param>
        public BlackboardRootViewModel(DeclarationHighlighterStateComponent highlighterState,
                                       GraphModelStateComponent graphModelState,
                                       BlackboardContentModel blackboardContentModel,
                                       Hash128 graphViewGuid)
            : base(Hash128.Compute(typeof(BlackboardRootViewModel).FullName + graphViewGuid))
        {
            HighlighterState = highlighterState;
            GraphModelState = graphModelState;

            BlackboardContentState = new BlackboardContentStateComponent(blackboardContentModel);

            var key = PersistedState.MakeGraphKey(graphModelState.GraphModel);
            ViewState = PersistedState.GetOrCreatePersistedStateComponent<BlackboardViewStateComponent>(default, Guid, key);
            SelectionState = PersistedState.GetOrCreatePersistedStateComponent<SelectionStateComponent>(default, Guid, key);
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(BlackboardContentState);
            state?.AddStateComponent(ViewState);
            state?.AddStateComponent(SelectionState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(BlackboardContentState);
            state?.RemoveStateComponent(ViewState);
            state?.RemoveStateComponent(SelectionState);
        }
    }
}
