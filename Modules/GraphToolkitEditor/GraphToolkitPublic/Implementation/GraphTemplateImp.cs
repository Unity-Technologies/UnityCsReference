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

        public GraphTemplateImp(Type graphType, string newAssetName = "New Graph")
            : base(newAssetName, GetGraphExtension(graphType))
        {
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
        Type m_GraphType;

        public SubgraphTemplateImp(Type graphType, string graphTypeName = "Graph")
            : base(graphType, graphTypeName)
        {
            m_GraphType = graphType;
        }

        internal override void InitLocalSubgraphsPreOnEnable(GraphModel graphModel)
        {
            base.InitLocalSubgraphsPreOnEnable(graphModel);
            var graphModelImp = graphModel as GraphModelImp;
            if (graphModelImp != null)
            {
                graphModelImp.InstantiateGraph(m_GraphType);
            }
        }
    }
}
