// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.ItemLibrary.Editor;
using UnityEditor;
using UnityEditor.Utils;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Factory class to create a <see cref="ItemLibraryDatabase"/> of <see cref="GraphNodeModelLibraryItem"/>.
    /// </summary>
    [PublicAPI]
    class GraphElementItemDatabase
    {
        public const string ConstantsPath = "Constant";
        public const string StickyNotePath = "Sticky Note";
        public const string GraphVariablesPath = "Graph Variables";
        public const string SubgraphsPath = "Subgraphs";

        // TODO: our builder methods ("AddStack",...) all use this field. Users should be able to create similar methods. making it public until we find a better solution
        public readonly List<ItemLibraryItem> Items;
        public readonly Stencil Stencil;
        GraphModel m_GraphModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElementItemDatabase"/> class.
        /// </summary>
        /// <param name="stencil">Stencil of the graph elements.</param>
        /// <param name="graphModel">GraphModel of the graph elements.</param>
        public GraphElementItemDatabase(Stencil stencil, GraphModel graphModel)
        {
            Stencil = stencil;
            Items = new List<ItemLibraryItem>();
            m_GraphModel = graphModel;
        }

        /// <summary>
        /// Adds a <see cref="ItemLibraryItem"/> for each node marked with <see cref="LibraryItemAttribute"/> to the database.
        /// <remarks>Nodes marked with <see cref="LibraryHelpAttribute"/> will also display a description in the details panel.</remarks>
        /// </summary>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddNodesWithLibraryItemAttribute()
        {
            var types = TypeCache.GetTypesWithAttribute<LibraryItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<LibraryItemAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                //Blocks and Nodes share LibraryItemAttribute but blocks shouldn't be added to nodes lists.
                if (typeof(BlockNodeModel).IsAssignableFrom(type))
                    continue;

                var nodeHelpAttribute = type.GetCustomAttribute<LibraryHelpAttribute>();

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(Stencil))
                        continue;

                    switch (attribute.Context)
                    {
                        case SearchContext.Graph:
                        {
                            ItemLibraryItem.ExtractPathAndNameFromFullName(attribute.Path, out var categoryPath, out var name);
                            if (attribute.Mode != null && name != attribute.Mode)
                                name = attribute.Mode;

                            var node = new GraphNodeModelLibraryItem(
                                name,
                                new NodeItemLibraryData(type),
                                data => data.CreateNode(type, name, n =>
                                {
                                    if (attribute.Mode != null && n is NodeModel nodeModel)
                                    {
                                        var modeIndex = nodeModel.Modes.IndexOf_Internal(name);
                                        nodeModel.CurrentModeIndex = modeIndex;
                                    }
                                }))
                            {
                                CategoryPath = categoryPath,
                                Help = nodeHelpAttribute?.HelpText,
                                StyleName = attribute.StyleName
                            };

                            Items.Add(node);
                            break;
                        }

                        default:
                            Debug.LogWarning($"The node {type} is not a " +
                                $"{SearchContext.Graph} node, so it cannot be added to the library");
                            break;
                    }
                    if (attribute.Mode == null)
                        break;
                }
            }

            return this;
        }

        /// <summary>
        /// Adds <see cref="GraphNodeModelLibraryItem"/>s for constants to the database.
        /// </summary>
        /// <param name="types">The types of constants to add.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddConstants(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                AddConstant(type);
            }

            return this;
        }

        /// <summary>
        /// Adds a <see cref="GraphNodeModelLibraryItem"/> for a constant of a certain type to the database.
        /// </summary>
        /// <param name="type">The type of constant to add.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddConstant(Type type)
        {
            TypeHandle handle = type.GenerateTypeHandle();

            Items.Add(new GraphNodeModelLibraryItem($"{type.FriendlyName().Nicify()} {ConstantsPath}",
                new TypeItemLibraryData(handle),
                data => data.CreateConstantNode("", handle))
                {
                    CategoryPath = ConstantsPath,
                    Help = $"Constant of type {type.FriendlyName().Nicify()}"
                }
            );
            return this;
        }

        /// <summary>
        /// Adds <see cref="GraphNodeModelLibraryItem"/>s for every graph variable to the database.
        /// </summary>
        /// <param name="graphModel">The GraphModel containing the variables.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddGraphVariables(GraphModel graphModel)
        {
            foreach (var declarationModel in graphModel.VariableDeclarations)
            {
                Items.Add(new GraphNodeModelLibraryItem(declarationModel.DisplayTitle,
                    new TypeItemLibraryData(declarationModel.DataType),
                    data => data.CreateVariableNode(declarationModel)));
            }

            return this;
        }

        /// <summary>
        /// Gets subgraphs of a given asset type.
        /// </summary>
        /// <param name="graphAssetType">The type of subgraphs to get.</param>
        /// <returns>The subgraph assets found.</returns>
        protected IEnumerable<GraphModel> GetSubgraphs(Type graphAssetType)
        {
            if (graphAssetType != null)
            {
                var assetPaths = AssetDatabase.FindAssets($"t:{graphAssetType}").Select(AssetDatabase.GUIDToAssetPath).ToList();
                return assetPaths
                    .Select(p => (AssetDatabase.LoadAssetAtPath(p, graphAssetType) as GraphAsset)?.GraphModel)
                    .Where(g => g != null && !g.IsContainerGraph() && g.CanBeSubgraph());
            }

            return Enumerable.Empty<GraphModel>();
        }

        /// <summary>
        /// Gets subgraphs of the same type as the main graph asset type.
        /// </summary>
        /// <returns>The subgraph assets found.</returns>
        protected virtual IEnumerable<GraphModel> GetSubgraphs()
        {
            var graphAssetType = Stencil.GraphModel?.Asset == null ? null : Stencil.GraphModel.Asset.GetType();
            return GetSubgraphs(graphAssetType);
        }

        /// <summary>
        /// Gets a name for the subgraph suitable for use in the item library.
        /// </summary>
        /// <param name="subgraphModel">The subgraph.</param>
        /// <returns>The name of the subgraph.</returns>
        protected virtual string GetSubgraphName(GraphModel subgraphModel)
        {
            var name = subgraphModel.Name ?? "UnknownAssetGraphModel";

            if (subgraphModel.Asset != null && subgraphModel.Asset.FilePath != null)
            {
                var path = Path.GetDirectoryName(subgraphModel.Asset.FilePath);

                if (path is not (null or "Assets"))
                {
                    path = path.NormalizePath();
                    Path.GetRelativePath("Assets", path);

                    return $"{path}/{name}";
                }
            }

            return name;
        }

        /// <summary>
        /// Gets the category where to list the subgraph in the item library.
        /// </summary>
        /// <param name="subgraphModel">The subgraph.</param>
        /// <returns>The category.</returns>
        protected virtual string GetSubgraphCategoryPath(GraphModel subgraphModel)
        {
            return SubgraphsPath;
        }

        /// <summary>
        /// Gets a description for the subgraph in the item library.
        /// </summary>
        /// <param name="subgraphModel">The subgraph.</param>
        /// <returns>The description.</returns>
        protected virtual string GetSubgraphDescription(GraphModel subgraphModel)
        {
            return "";
        }

        /// <summary>
        /// Adds a <see cref="GraphNodeModelLibraryItem"/> for a Asset Graph Subgraph to the database.
        /// </summary>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddAssetGraphSubgraphs()
        {
            var graphModels = GetSubgraphs();

            var handle = Stencil.GetSubgraphNodeTypeHandle();

            foreach (var graphModel in graphModels)
            {
                Items.Add(new GraphNodeModelLibraryItem(GetSubgraphName(graphModel),
                    new TypeItemLibraryData(handle),
                    data => data.CreateSubgraphNode(graphModel))
                {
                    CategoryPath = GetSubgraphCategoryPath(graphModel),
                    Help = GetSubgraphDescription(graphModel)
                });
            }

            return this;
        }

        /// <summary>
        /// Gets a version of the database compatible with the library.
        /// </summary>
        /// <returns>A version of the database compatible with the library.</returns>
        public ItemLibraryDatabase Build()
        {
            return new ItemLibraryDatabase(Items);
        }
    }
}
