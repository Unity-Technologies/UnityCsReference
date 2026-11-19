// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class SubgraphStateView : StateView, ISubgraphNodeView
    {
        public SubgraphStateView()
        {
            var clickable = new Clickable(OpenSubgraph);
            clickable.activators.Clear();
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);
        }

        public virtual void OpenSubgraph()
        {
            if (Model is not ISubgraphNodeInternal subgraphNodeModel || subgraphNodeModel.GetSubgraphModel() == null)
                return;

            GraphView.Dispatch(new LoadGraphCommand(subgraphNodeModel.GetSubgraphModel(), LoadGraphCommand.LoadStrategies.PushOnStack, title: (subgraphNodeModel as IHasTitle)?.Title, subgraphNodeGuid: Model.Guid));
            if (GraphView.Window is GraphViewEditorWindow graphViewWindow)
                graphViewWindow.UpdateWindowsWithSameCurrentGraph(false);
        }
    }
}
