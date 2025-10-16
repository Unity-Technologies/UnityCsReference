// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The model backing a <see cref="GraphView"/>.
    /// </summary>
    /// <remarks>
    /// 'GraphRootViewModel' is the model backing a <see cref="GraphView"/>. It manages all state components associated with the graph view.
    /// The <see cref="GraphViewEditorWindow.CreateGraphRootViewModel"/> method creates and initializes 'GraphRootViewModel'.
    /// </remarks>
    [Serializable]
    [UnityRestricted]
    internal class GraphRootViewModel : RootViewModel
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
        /// The space partitioning state component.
        /// </summary>
        public SpacePartitioningStateComponent SpacePartitioningState { get; }

        /// <summary>
        /// The culling state component.
        /// </summary>
        public GraphViewCullingStateComponent GraphViewCullingState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphRootViewModel"/> class.
        /// </summary>
        /// <param name="graphViewName">A unique name for the graphView.</param>
        /// <param name="graphModel">The current <see cref="GraphModel"/>.</param>
        /// <param name="graphTool">The <see cref="GraphTool"/>.</param>
        /// <param name="graphGUID">A GUID unique to this graph.</param>
        public GraphRootViewModel(string graphViewName, GraphModel graphModel, GraphTool graphTool, Hash128 graphGUID)
            : base(Hash128.Compute(graphViewName))
        {
            var graphKey = PersistedState.MakeGraphKey(graphModel);

            GraphViewState = PersistedState.GetOrCreatePersistedStateComponent<GraphViewStateComponent>(default, Guid, graphKey);

            // The GraphModelStateComponent is initialized with a GUID so that it is the same across domain reloads.
            GraphModelState = new GraphModelStateComponent(graphTool, graphGUID);

            SelectionState = PersistedState.GetOrCreatePersistedStateComponent<SelectionStateComponent>(default, Guid, graphKey);

            AutoPlacementState = new AutoPlacementStateComponent();

            GraphProcessingState = new GraphProcessingStateComponent();

            ProcessingErrorsState = new GraphProcessingErrorsStateComponent();

            SpacePartitioningState = new SpacePartitioningStateComponent();

            GraphViewCullingState = new GraphViewCullingStateComponent();
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
            state?.AddStateComponent(SpacePartitioningState);
            state?.AddStateComponent(GraphViewCullingState);
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
            state?.RemoveStateComponent(SpacePartitioningState);
            state?.RemoveStateComponent(GraphViewCullingState);
        }
    }
}
