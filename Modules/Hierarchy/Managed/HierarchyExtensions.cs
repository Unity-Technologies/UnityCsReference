// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides extension methods for <see cref="Hierarchy"/> to access and enumerate node type handlers.
    /// </summary>
    public static partial class HierarchyExtensions
    {
        /// <summary>
        /// Gets a <see cref="HierarchyNodeTypeHandler"/> instance from this hierarchy.
        /// </summary>
        /// <remarks>
        /// Use this method to retrieve a specific <see cref="HierarchyNodeTypeHandler"/> when you know the exact type at compile time. 
        /// </remarks>
        /// <typeparam name="T">The type of the <see cref="HierarchyNodeTypeHandler"/>.</typeparam>
        /// <returns>The <see cref="HierarchyNodeTypeHandler"/>.</returns>
        public static T GetNodeTypeHandler<T>(this Hierarchy hierarchy) where T : HierarchyNodeTypeHandler => hierarchy.GetNodeTypeHandlerBase<T>();

        /// <summary>
        /// Gets the <see cref="HierarchyNodeTypeHandler"/> instance for the specified node from this hierarchy.
        /// </summary>
        /// <remarks>
        /// Use this method to retrieve a specific <see cref="HierarchyNodeTypeHandler"/> instance from a specific <see cref="HierarchyNode"/>. 
        /// </remarks>
        /// <param name="hierarchy">The <see cref="Hierarchy"/> to get the <see cref="HierarchyNodeTypeHandler"/> from.</param>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the <see cref="HierarchyNodeTypeHandler"/> for.</param>
        /// <returns>The <see cref="HierarchyNodeTypeHandler"/>.</returns>
        public static HierarchyNodeTypeHandler GetNodeTypeHandler(this Hierarchy hierarchy, in HierarchyNode node)
        {
            var handlerBase = hierarchy.GetNodeTypeHandlerBase(in node);
            return handlerBase as HierarchyNodeTypeHandler;
        }

        /// <summary>
        /// Get the node type handler instance for the specified node from this hierarchy.
        /// </summary>
        /// <param name="hierarchyViewModel">The hierarchy view model.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The hierarchy node type handler.</returns>
        public static HierarchyNodeTypeHandler GetNodeTypeHandler(this HierarchyViewModel hierarchyViewModel, in HierarchyNode node)
        {
            var handlerBase = hierarchyViewModel.GetNodeTypeHandlerBase(in node);
            return handlerBase as HierarchyNodeTypeHandler;
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNodeTypeHandler"/> instance for the specified node type name from this hierarchy.
        /// </summary>
        /// <remarks>
        /// Use this method to retrieve a specific <see cref="HierarchyNodeTypeHandler"/> instance from a node type name. 
        /// </remarks>
        /// <param name="hierarchy">The <see cref="Hierarchy"/> to get the <see cref="HierarchyNodeTypeHandler"/> from.</param>
        /// <param name="nodeTypeName">The node type name to get the <see cref="HierarchyNodeTypeHandler"/> for.</param>
        /// <returns>The <see cref="HierarchyNodeTypeHandler"/>.</returns>
        public static HierarchyNodeTypeHandler GetNodeTypeHandler(this Hierarchy hierarchy, string nodeTypeName)
        {
            var handlerBase = hierarchy.GetNodeTypeHandlerBase(nodeTypeName);
            return handlerBase is HierarchyNodeTypeHandler handler ? handler : null;
        }

        /// <summary>
        /// Enumerates all the <see cref="HierarchyNodeTypeHandler"/> instances used by this hierarchy.
        /// </summary>
        /// <remarks>
        /// Use this method to enumerate all the <see cref="HierarchyNodeTypeHandler"/> instances used by this hierarchy.
        /// </remarks>
        /// <returns>The enumerable of <see cref="HierarchyNodeTypeHandler"/> instances.</returns>
        public static HierarchyNodeTypeHandlerEnumerable EnumerateNodeTypeHandlers(this Hierarchy hierarchy) => new HierarchyNodeTypeHandlerEnumerable(hierarchy);
    }
}
