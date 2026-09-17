// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class GraphViewImp : GraphView
    {
        public GraphViewImp(EditorWindow window, GraphTool graphTool, string graphViewName, GraphRootViewModel graphViewModel, ViewSelection viewSelection, GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive, TypeHandleInfos typeHandleInfos = null)
            : base(window, graphTool, graphViewName, graphViewModel, viewSelection, displayMode, typeHandleInfos) { }

        protected override IDragAndDropHandler GraphAssetDragAndDropHandler
        {
            get
            {
                if (GraphModel is GraphModelImp { AllowSubgraphCreation: true })
                {
                    return m_SubgraphAssetDragAndDropHandler ??= new SubgraphDragAndDropHandler(this);
                }

                return null;
            }
        }

        protected override ItemLibraryHelper CreateItemLibraryHelper()
        {
            return (Window as GraphViewEditorWindow)?.CreateItemLibraryHelper(GraphModel);
        }

        public override GraphView CreateSimplePreview()
        {
            return new PreviewGraphViewImp(null, null, "",  null, null, GraphViewDisplayMode.NonInteractive);
        }

        protected override void AppendConvertToAssetSubgraphMenuItem(ContextualMenuPopulateEvent evt)
        {
            AppendConvertSubgraphMenuItem(evt, true, IsSameGraphType);
        }

        protected override void AppendUnpackToLocalSubgraphMenuItem(ContextualMenuPopulateEvent evt)
        {
            AppendConvertSubgraphMenuItem(evt, false, IsSameGraphType);
        }

        protected override void AppendCreateLocalSubgraphFromSelectionMenuItem(ContextualMenuPopulateEvent evt)
        {
            // In state machines, only allow creating a local subgraph from selection if the subgraph is also a state machine.
            AppendCreateLocalSubgraphFromSelectionMenuItem(evt, template =>
            {
                var subgraphIsStateMachine = (template as GraphTemplateImp)?.GraphType.IsSubclassOf(typeof(StateMachine)) ?? false;
                return GraphModel.IsStateMachineGraph == subgraphIsStateMachine;
            });
        }

        bool IsSameGraphType(ISubgraphNodeInternal subgraphNode, GraphTemplate template)
        {
            if (subgraphNode == null)
                return false;

            var graphType = (template as GraphTemplateImp)?.GraphType ?? GraphModel.GetType();
            var subgraphModel = subgraphNode.GetSubgraphModel();
            var subgraphImp = subgraphModel != null ? subgraphModel.IsStateMachineGraph
                ? subgraphModel as StateMachineImp
                : subgraphModel as GraphModelImp : null;

            var subgraph = subgraphImp?.Graph;
            if (subgraph != null)
                return graphType.IsInstanceOfType(subgraph);

            var subgraphStateMachine = (subgraphNode as SubgraphStateModelImp)?.GetSubgraphAsStateMachine();
            return subgraphStateMachine != null && graphType.IsInstanceOfType(subgraphStateMachine);
        }

        internal void CallBuildContextualMenuForTests(ContextualMenuPopulateEvent evt)
        {
            BuildContextualMenu(evt);
            evt.menu.PrepareForDisplay(evt.triggerEvent);
        }
    }
}
