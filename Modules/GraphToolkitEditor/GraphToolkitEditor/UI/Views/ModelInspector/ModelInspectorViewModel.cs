// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The model backing a <see cref="ModelInspectorView"/>.
    /// </summary>
    /// <remarks>
    /// It manages state components related to the inspector. This model coordinates the data that is displayed in the inspector view, keeping it in sync
    /// with the underlying graph components. This class is created in <see cref="GraphViewEditorWindow.CreateModelInspectorViewModel"/> and is then associated
    /// with the <see cref="ModelInspectorView"/>.
    /// </remarks>
    [Serializable]
    [UnityRestricted]
    internal class ModelInspectorViewModel : RootViewModel
    {
        /// <summary>
        /// The transition inspector state component.
        /// </summary>
        public TransitionInspectorStateComponent TransitionInspectorState { get; private set; }

        /// <summary>
        /// The model inspector state component.
        /// </summary>
        public ModelInspectorStateComponent ModelInspectorState { get; }

        /// <summary>
        /// The graph model state from the parent graph view.
        /// </summary>
        public GraphModelStateComponent GraphModelState { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorViewModel"/> class.
        /// </summary>
        public ModelInspectorViewModel(GraphModelStateComponent graphModelState, IReadOnlyList<GraphElementModel> selectedModels, Hash128 graphViewGuid)
            : base(Hash128.Compute(typeof(ModelInspectorViewModel).FullName + graphViewGuid))
        {
            GraphModelState = graphModelState;
            var graphModel = GraphModelState?.GraphModel;

            var key = PersistedState.MakeGraphKey(graphModel);
            ModelInspectorState = PersistedState.GetOrCreatePersistedStateComponent(default, Guid, key,
                () => new ModelInspectorStateComponent(selectedModels, graphModel));

            TransitionInspectorState = PersistedState.GetOrCreatePersistedStateComponent(default, Guid, key,
                () => new TransitionInspectorStateComponent());
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
            state?.AddStateComponent(ModelInspectorState);
            state?.AddStateComponent(TransitionInspectorState);
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
            state?.RemoveStateComponent(TransitionInspectorState);
            state?.RemoveStateComponent(ModelInspectorState);
        }
    }
}
