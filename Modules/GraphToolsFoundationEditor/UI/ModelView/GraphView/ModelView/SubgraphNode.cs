// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for a <see cref="SubgraphNodeModel"/>.
    /// </summary>
    class SubgraphNode : CollapsibleInOutNode
    {
        public SubgraphNode()
        {
            var clickable = new Clickable(OnOpenSubgraph);
            clickable.activators.Clear();
            clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });
            this.AddManipulator(clickable);
        }

        protected internal static Vector2 ComputeSubgraphNodePosition_Internal(List<GraphElementModel> elements, GraphView graphView)
        {
            var firstElement = elements.FirstOrDefault().GetView_Internal(graphView);

            if (firstElement != null)
            {
                var elementsUIWithoutWires = elements.Where(e => !(e is WireModel)).Select(n => n.GetView_Internal(graphView)).Where(e => e != null);
                var encompassingRect = elementsUIWithoutWires.Aggregate(firstElement.layout, (current, e) => RectUtils_Internal.Encompass(current, e.layout));

                return encompassingRect.center;
            }

            return Vector2.zero;
        }

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.ReplacePart(titleIconContainerPartName, SubgraphNodeTitlePart.Create(titleIconContainerPartName, Model, this, ussClassName));
        }

        void OnOpenSubgraph()
        {
            if (Model is SubgraphNodeModel subgraphNodeModel && subgraphNodeModel.SubgraphModel != null)
            {
                GraphView.Dispatch(new LoadGraphCommand(subgraphNodeModel.SubgraphModel, null, LoadGraphCommand.LoadStrategies.PushOnStack));
                if (GraphView.Window is GraphViewEditorWindow graphViewWindow)
                    graphViewWindow.UpdateWindowsWithSameCurrentGraph_Internal(false);
            }
        }

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }
    }
}
