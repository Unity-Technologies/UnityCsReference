// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{
    class PublicDatabaseProviderImp : DefaultDatabaseProvider
    {
        Dictionary<Type, ItemLibraryDatabaseBase[]> m_ContextDatabases = new();
        ItemLibraryDatabaseBase[] m_GraphDatabases;

        public override IReadOnlyList<Type> SupportedTypes => ((GraphModelImp)GraphModel).SupportedTypes;

        public PublicDatabaseProviderImp(GraphModel graphModel)
            : base(graphModel)
        {
        }
        public override IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementsDatabases(BlackboardContentModel blackboardModel = null)
        {
            m_GraphDatabases ??= new ItemLibraryDatabaseBase[2];
            if (GraphModel is GraphModelImp graphModelImp)
            {
                var graphAttribute = graphModelImp.Graph.GetType().GetCustomAttribute<GraphAttribute>();
                if (graphAttribute != null && graphAttribute.Options.HasFlag(GraphOptions.SupportsSubgraphs))
                {
                    // Only add subgraph items if the graph supports subgraphs
                    m_GraphDatabases[0] = InitialGraphElementDatabase().AddSubgraphs().Build();
                }
                else
                {
                    m_GraphDatabases[0] = InitialGraphElementDatabase().Build();
                }
            }
            m_GraphDatabases[1] ??= ConstantDatabase().Build();

            return m_GraphDatabases;
        }

        protected override GraphElementItemDatabase InitialGraphElementDatabase()
        {
            var db = new PublicGraphElementItemDatabase(GraphModel);

            AddNodes(db);

            return db;
        }

        public override IReadOnlyList<ItemLibraryDatabaseBase> GetGraphElementContainerDatabases(IGraphElementContainer container)
        {
            if (container is UserContextNodeModelImp imp)
            {
                if (!m_ContextDatabases.TryGetValue(imp.Node.GetType(), out var dbs))
                {
                    dbs = new ItemLibraryDatabaseBase[1];

                    var db = new GraphElementItemDatabase(GraphModel);

                    AddBlocks(imp, db);

                    dbs[0] = db.Build();

                    m_ContextDatabases[container.GetType()] = dbs;
                }

                return dbs;
            }

            return null;
        }

        GraphElementItemDatabase ConstantDatabase()
        {
            var db = new GraphElementItemDatabase(GraphModel);

            db.AddConstants(((GraphModelImp)GraphModel).SupportedTypes);

            return db;
        }
        void AddNodes(GraphElementItemDatabase db)
        {
            foreach (var nodeType in ((GraphModelImp)GraphModel).SupportedNodes)
            {
                bool isContextNode = typeof(ContextNode).IsAssignableFrom(nodeType);
                var nodeAttribute = nodeType.GetCustomAttribute<NodeAttribute>();
                var nodeDef = new GraphNodeModelLibraryItem(
                    nodeType.Name,
                    new NodeItemLibraryData(nodeType),
                    d => isContextNode ? GraphModelImp.CreateContextNodeFromData(d, nodeType) : GraphModelImp.CreateNodeFromData(d, nodeType))
                {
                    CategoryPath = isContextNode ? "Contexts" : "Nodes",
                    IconPath = nodeAttribute?.IconPath ?? ""
                };

                db.Items.Add(nodeDef);
            }
        }
        static void AddBlocks(UserContextNodeModelImp imp, GraphElementItemDatabase db)
        {
            var graphType = ((GraphModelImp)imp.GraphModel).Graph.GetType();
            var contextType = imp.Node.GetType();

            foreach (var blockType in PublicGraphFactory.GetBlockTypes(graphType, contextType))
            {
                var nodeAttribute = blockType.GetCustomAttribute<NodeAttribute>();
                var nodeDef = new GraphNodeModelLibraryItem(
                    blockType.Name,
                    new NodeItemLibraryData(blockType),
                    d => GraphModelImp.CreateContextFromBlockData(d, blockType, contextType))
                {
                    CategoryPath = "Blocks",
                    IconPath = nodeAttribute?.IconPath ?? ""
                };

                db.Items.Add(nodeDef);
            }
        }
    }
}
