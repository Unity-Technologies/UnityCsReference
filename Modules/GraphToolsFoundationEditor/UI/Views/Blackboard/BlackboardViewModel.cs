// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class BlackboardViewModel : RootViewModel
    {
        /// <summary>
        /// The blackboard state component.
        /// </summary>
        public BlackboardViewStateComponent ViewState { get; }

        /// <summary>
        /// The <see cref="SelectionStateComponent"/>. Holds the blackboard selection.
        /// </summary>
        public SelectionStateComponent SelectionState { get; }

        /// <summary>
        /// The <see cref="GraphModelStateComponent"/> of the <see cref="GraphView"/> linked to this blackboard.
        /// </summary>
        public GraphModelStateComponent GraphModelState => ParentGraphView?.GraphViewModel.GraphModelState;

        /// <summary>
        /// The highlighter state.
        /// </summary>
        public DeclarationHighlighterStateComponent HighlighterState { get; }

        /// <summary>
        /// The parent graph view.
        /// </summary>
        public GraphView ParentGraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewModel"/> class.
        /// </summary>
        public BlackboardViewModel(GraphView graphView, DeclarationHighlighterStateComponent highlighterState)
        : base(Hash128.Compute(typeof(BlackboardViewModel).FullName + graphView.GraphViewModel.Guid))
        {
            ParentGraphView = graphView;
            HighlighterState = highlighterState;

            var key = PersistedState.MakeGraphKey(GraphModelState?.GraphModel);
            ViewState = PersistedState.GetOrCreatePersistedStateComponent<BlackboardViewStateComponent>(default, Guid, key);
            SelectionState = PersistedState.GetOrCreatePersistedStateComponent<SelectionStateComponent>(default, Guid, key);
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(ViewState);
            state?.AddStateComponent(SelectionState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(ViewState);
            state?.RemoveStateComponent(SelectionState);
        }
    }
}
