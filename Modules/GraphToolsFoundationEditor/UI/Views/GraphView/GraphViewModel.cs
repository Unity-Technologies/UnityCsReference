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
    class GraphViewModel : RootViewModel
    {
        /// <summary>
        /// The graph view state component.
        /// </summary>
        public GraphViewStateComponent GraphViewState { get; }

        /// <summary>
        /// The graph model state component.
        /// </summary>
        public GraphModelStateComponent GraphModelState { get; }

        /// <summary>
        /// The selection state component.
        /// </summary>
        public SelectionStateComponent SelectionState { get; }

        /// <summary>
        /// The automatic placement state component.
        /// </summary>
        public AutoPlacementStateComponent AutoPlacementState { get; }

        /// <summary>
        /// The state component that holds the graph processing state.
        /// </summary>
        public GraphProcessingStateComponent GraphProcessingState { get; }

        /// <summary>
        /// The errors returned by the <see cref="GraphProcessor"/>s.
        /// </summary>
        public GraphProcessingErrorsStateComponent ProcessingErrorsState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphViewModel"/> class.
        /// </summary>
        public GraphViewModel(string graphViewName, GraphModel graphModel)
        : base(new SerializableGUID(graphViewName))
        {
            var graphKey = PersistedState.MakeGraphKey(graphModel);

            GraphViewState = PersistedState.GetOrCreatePersistedStateComponent<GraphViewStateComponent>(default, Guid, graphKey);

            GraphModelState = new GraphModelStateComponent();

            SelectionState = PersistedState.GetOrCreatePersistedStateComponent<SelectionStateComponent>(default, Guid, graphKey);

            AutoPlacementState = new AutoPlacementStateComponent();

            GraphProcessingState = new GraphProcessingStateComponent();

            ProcessingErrorsState = new GraphProcessingErrorsStateComponent();
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(GraphViewState);
            state?.AddStateComponent(GraphModelState);
            state?.AddStateComponent(SelectionState);
            state?.AddStateComponent(AutoPlacementState);
            state?.AddStateComponent(GraphProcessingState);
            state?.AddStateComponent(ProcessingErrorsState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(GraphViewState);
            state?.RemoveStateComponent(GraphModelState);
            state?.RemoveStateComponent(SelectionState);
            state?.RemoveStateComponent(AutoPlacementState);
            state?.RemoveStateComponent(GraphProcessingState);
            state?.RemoveStateComponent(ProcessingErrorsState);
        }
    }
}
