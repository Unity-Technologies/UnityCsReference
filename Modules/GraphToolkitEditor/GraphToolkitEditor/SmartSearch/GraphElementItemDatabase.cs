// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using Unity.GraphToolkit.ItemLibrary.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Factory class to create a <see cref="ItemLibraryDatabase"/> of <see cref="GraphNodeModelLibraryItem"/>.
    /// </summary>
    [PublicAPI]
    [UnityRestricted]
    internal class GraphElementItemDatabase
    {
        public const string ConstantsPath = "Constant";
        public const string StickyNotePath = "Sticky Note";
        public const string GraphVariablesPath = "Graph Variables";
        public const string SubgraphsPath = "Subgraphs";

        // TODO: our builder methods ("AddStack",...) all use this field. Users should be able to create similar methods. making it public until we find a better solution
        /// <summary>
        /// The items in the database.
        /// </summary>
        public readonly List<ItemLibraryItem> Items;

        /// <summary>
        /// The <see cref="GraphModel"/> associated with the database.
        /// </summary>
        protected GraphModel GraphModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElementItemDatabase"/> class.
        /// </summary>
        /// <param name="graphModel">GraphModel of the graph elements.</param>
        public GraphElementItemDatabase(GraphModel graphModel)
        {
            Items = new List<ItemLibraryItem>();
            GraphModel = graphModel;
        }

        /// <summary>
        /// Adds a <see cref="ItemLibraryItem"/> for each node marked with <see cref="LibraryItemAttribute"/> to the database.
        /// </summary>
        /// <remarks>Nodes marked with <see cref="LibraryHelpAttribute"/> will also display a description in the details panel.</remarks>
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
                    if (!attribute.GraphModelType.IsInstanceOfType(GraphModel))
                        continue;

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
                                var modeIndex = IEnumerableExtensions.IndexOf(nodeModel.Modes, name);
                                nodeModel.CurrentModeIndex = modeIndex;
                            }
                        }))
                    {
                        CategoryPath = categoryPath,
                        Help = nodeHelpAttribute?.HelpText,
                        StyleName = attribute.StyleName
                    };

                    Items.Add(node);

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

            Items.Add(new GraphNodeModelLibraryItem($"{TypeHelpers.GetFriendlyName(type).Nicify()} {ConstantsPath}",
                new TypeItemLibraryData(handle),
                data => data.CreateConstantNode("", handle))
            {
                CategoryPath = ConstantsPath,
                Help = $"Constant of type {TypeHelpers.GetFriendlyName(type).Nicify()}"
            }
            );
            return this;
        }

        /// <summary>
        /// Adds a <see cref="VariableLibraryItem"/> to create a variable of the data type last selected from the <see cref="Blackboard"/> dropdown to the database.
        /// </summary>
        /// <param name="blackboardContentModel">The content model of the blackboard.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddLastVariable(BlackboardContentModel blackboardContentModel)
        {
            if (blackboardContentModel == null || !blackboardContentModel.HasDefaultButton())
                return this;

            var lastVariable = blackboardContentModel.LastVariableInfos;

            const string itemName = "Create Variable";
            const string description = "Creates variable of the data type last selected from the Blackboard dropdown";

            Items.Add(
                new VariableLibraryItem(itemName, lastVariable.TypeHandle, lastVariable.VariableType)
                {
                    ModifierFlags = lastVariable.ModifierFlags,
                    Scope = lastVariable.Scope,
                    Priority = -1, // To be the first item to appear in the item library after the favorites
                    Help = description
                });

            return this;
        }

        /// <summary>
        /// Adds a <see cref="VariableLibraryItem"/> to create a variable of the same data type as the port it is created from.
        /// </summary>
        /// <param name="portModel">The <see cref="PortModel"/> from which the variable is created.</param>
        /// <param name="variableCreationInfos">Data to create the variable.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementItemDatabase AddVariableFromPort(PortModel portModel, VariableCreationInfos variableCreationInfos)
        {
            if (portModel == null)
                return this;

            const string itemName = "Create Variable (of port type)";
            const string description = "Creates variable of the port data type";

            // Make sure the type is the port's
            variableCreationInfos.TypeHandle = portModel.DataTypeHandle;

            Items.Add(
                new VariableLibraryItem(itemName, variableCreationInfos.TypeHandle, variableCreationInfos.VariableType)
                {
                    ModifierFlags = variableCreationInfos.ModifierFlags,
                    Scope = variableCreationInfos.Scope,
                    Priority = -1, // To be the first item to appear in the item library after the favorites
                    Help = description
                });

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
                Items.Add(new GraphNodeModelLibraryItem(declarationModel.Title,
                    new TypeItemLibraryData(declarationModel.DataType),
                    data => data.CreateVariableNode(declarationModel))
                { CategoryPath = "Variables" });
            }

            return this;
        }

        /// <summary>
        /// Gets subgraphs of a given graph object type.
        /// </summary>
        /// <param name="graphObjectType">The type of subgraphs to get.</param>
        /// <returns>The subgraph assets found.</returns>
        protected IEnumerable<GraphModel> GetSubgraphsOfType(Type graphObjectType)
        {
            if (graphObjectType != null)
            {
                List<GraphModel> subGraphModels = null;

                // Get Local sub-graphs
                if (GraphModel is not null && GraphModel.LocalSubgraphs.Count > 0)
                {
                    subGraphModels ??= new List<GraphModel>();
                    subGraphModels.AddRange(GraphModel.LocalSubgraphs);
                }

                // Get Asset sub-graphs
                var results = AssetDatabase.FindAssets($"t:{graphObjectType.Name}");
                if (results.Length > 0)
                {
                    subGraphModels ??= new List<GraphModel>();
                    foreach (var result in results)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(result);
                        var assetGraphModel = (GraphObject.LoadGraphObjectAtPath(assetPath, graphObjectType))?.GraphModel;
                        if (assetGraphModel is not null && !assetGraphModel.IsContainerGraph() &&
                            assetGraphModel.CanBeSubgraph())
                            subGraphModels.Add(assetGraphModel);
                    }
                }

                return subGraphModels;
            }

            return Enumerable.Empty<GraphModel>();
        }

        /// <summary>
        /// Gets subgraphs of the same type as the main graph object type.
        /// </summary>
        /// <returns>The subgraph assets found.</returns>
        protected virtual IEnumerable<GraphModel> GetSubgraphs()
        {
            var graphAssetType = GraphModel?.GraphObject == null ? null : GraphModel.GraphObject.GetType();
            return GetSubgraphsOfType(graphAssetType);
        }

        /// <summary>
        /// Gets a name for the subgraph suitable for use in the item library.
        /// </summary>
        /// <param name="subgraphModel">The subgraph.</param>
        /// <returns>The name of the subgraph.</returns>
        protected virtual string GetSubgraphName(GraphModel subgraphModel)
        {
            var name = subgraphModel.Name ?? "UnknownAssetGraphModel";

            if (subgraphModel.GraphObject != null && subgraphModel.GraphObject.FilePath != null)
            {
                var path = Path.GetDirectoryName(subgraphModel.GraphObject.FilePath);

                if (path is not (null or "Assets"))
                {
                    var directoryName = new DirectoryInfo(path.NormalizePath()).Name;
                    return $"{name} ({directoryName})";
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
        /// Adds a <see cref="GraphNodeModelLibraryItem"/> for a Subgraph to the database.
        /// </summary>
        /// <returns>The database with the elements.</returns>
        public virtual GraphElementItemDatabase AddSubgraphs()
        {
            var subgraphModels = GetSubgraphs();
            if (subgraphModels == null || !subgraphModels.Any())
                return this;

            foreach (var graphModel in subgraphModels)
            {
                IItemLibraryData libraryData;
                if (graphModel.GraphObject is not null)
                    libraryData = new NodeItemLibraryData(graphModel.GraphObject.GetType(), graphModel.GetGraphReference(true));
                else
                    libraryData = new TypeItemLibraryData(GraphModel.GetSubgraphTypeHandle());

                Items.Add(new GraphNodeModelLibraryItem(GetSubgraphName(graphModel),
                    libraryData,
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
