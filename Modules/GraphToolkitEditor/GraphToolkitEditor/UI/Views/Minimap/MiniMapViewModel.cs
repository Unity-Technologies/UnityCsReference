// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    [Serializable]
    [UnityRestricted]
    internal class MiniMapViewModel : RootViewModel
    {
        public GraphViewStateComponent GraphViewState => ParentGraphView?.GraphViewModel.GraphViewState;
        public GraphModelStateComponent GraphModelState => ParentGraphView?.GraphViewModel.GraphModelState;
        public SelectionStateComponent SelectionState => ParentGraphView?.GraphViewModel.SelectionState;

        /// <summary>
        /// The <see cref="GraphModel"/> displayed by the MiniMapView.
        /// </summary>
        public GraphModel GraphModel => ParentGraphView?.GraphViewModel.GraphModelState.GraphModel;

        /// <summary>
        /// The GraphView linked to this MiniMapView.
        /// </summary>
        public GraphView ParentGraphView { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMapViewModel"/> class.
        /// </summary>
        public MiniMapViewModel(GraphView graphView)
        {
            ParentGraphView = graphView;
        }

        /// <inheritdoc />
        public override void AddToState(IState state)
        {
        }

        /// <inheritdoc />
        public override void RemoveFromState(IState state)
        {
        }
    }
}
