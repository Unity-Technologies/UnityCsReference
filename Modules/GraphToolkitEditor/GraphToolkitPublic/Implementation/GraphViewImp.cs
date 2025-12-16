// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
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
                if (GraphModel is GraphModelImp graphModelImp)
                {
                    var graphAttribute = graphModelImp.Graph.GetType().GetCustomAttribute<GraphAttribute>();
                    if (graphAttribute != null && graphAttribute.Options.HasFlag(GraphOptions.SupportsSubgraphs))
                    {
                        // Only add subgraph drag and drop handler if the graph supports subgraphs
                        return m_SubgraphAssetDragAndDropHandler ??= new SubgraphDragAndDropHandler(this);
                    }
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

        internal void CallBuildContextualMenuForTests(ContextualMenuPopulateEvent evt)
        {
            BuildContextualMenu(evt);
            evt.menu.PrepareForDisplay(evt.triggerEvent);
        }
    }
}
