// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class GraphTemplateImp : GraphTemplate
    {
        /// <inheritdoc />
        public override Type GraphModelType { get; }

        public override string NewAssetName { get; }

        public Type GraphType { get; }

        public GraphTemplateImp(Type graphType, string newAssetName = "New Graph")
            : base(newAssetName, GetGraphExtension(graphType))
        {
            GraphType = graphType;
            GraphModelType = typeof(GraphModelImp);
            NewAssetName = newAssetName;
        }

        static string GetGraphExtension(Type graphType)
        {
            return graphType.GetCustomAttribute<GraphAttribute>(false)?.Extension;
        }
    }
    class SubgraphTemplateImp : GraphTemplateImp
    {
        public SubgraphTemplateImp(Type graphType, string graphTypeName = "Graph")
            : base(graphType, graphTypeName) { }

        public override void InitBasicGraph(GraphModel graphModel)
        {
            base.InitBasicGraph(graphModel);

            // the GraphModel will first use the graphtype from its GraphObjectImp, we need to change it to the subgraph type.
            if (graphModel is GraphModelImp graphModelImp && !GraphType.IsInstanceOfType(graphModelImp.Graph))
            {
                graphModelImp.RecreateGraph(GraphType);
            }
        }
    }
}
