// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Editor")]
    class ModelInspectorViewModel : RootViewModel
    {
        /// <summary>
        /// The model inspector state component.
        /// </summary>
        public ModelInspectorStateComponent ModelInspectorState { get; }

        /// <summary>
        /// The graph model state from the parent graph view.
        /// </summary>
        public GraphModelStateComponent GraphModelState => ParentGraphView?.GraphViewModel.GraphModelState;

        /// <summary>
        /// The parent graph view.
        /// </summary>
        public GraphView ParentGraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorViewModel"/> class.
        /// </summary>
        public ModelInspectorViewModel(GraphView graphView)
        : base(Hash128.Compute(typeof(ModelInspectorViewModel).FullName + graphView.GraphViewModel.Guid))
        {
            ParentGraphView = graphView;

            var graphModel = GraphModelState?.GraphModel;
            var lastSelectedNode = graphView.GraphViewModel.SelectionState.GetSelection(graphModel).LastOrDefault(t => t is AbstractNodeModel || t is VariableDeclarationModel);

            var key = PersistedState.MakeGraphKey(graphModel);
            ModelInspectorState = PersistedState.GetOrCreatePersistedStateComponent(default, Guid, key,
                () => new ModelInspectorStateComponent(new[] { lastSelectedNode }, graphModel));
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(ModelInspectorState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(ModelInspectorState);
        }
    }
}
