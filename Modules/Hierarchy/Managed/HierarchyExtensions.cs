// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Hierarchy
{
    /// <summary>
    /// Extension methods on <see cref="Hierarchy"/>.
    /// </summary>
    public static partial class HierarchyExtensions
    {
        /// <summary>
        /// Get an hierarchy node type handler instance from this hierarchy.
        /// </summary>
        /// <typeparam name="T">The type of the hierarchy node type handler.</typeparam>
        /// <returns>The hierarchy node the handler.</returns>
        public static T GetNodeTypeHandler<T>(this Hierarchy hierarchy) where T : HierarchyNodeTypeHandler => hierarchy.GetNodeTypeHandlerBase<T>();

        /// <summary>
        /// Get the node type handler instance for the specified node from this hierarchy.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The hierarchy node the handler.</returns>
        public static HierarchyNodeTypeHandler GetNodeTypeHandler(this Hierarchy hierarchy, in HierarchyNode node)
        {
            var handlerBase = hierarchy.GetNodeTypeHandlerBase(in node);
            return handlerBase is HierarchyNodeTypeHandler handler ? handler : null;
        }

        /// <summary>
        /// Get the node type handler instance for the specified node type name from this hierarchy.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="nodeTypeName">The node type name.</param>
        /// <returns>The hierarchy node the handler.</returns>
        public static HierarchyNodeTypeHandler GetNodeTypeHandler(this Hierarchy hierarchy, string nodeTypeName)
        {
            var handlerBase = hierarchy.GetNodeTypeHandlerBase(nodeTypeName);
            return handlerBase is HierarchyNodeTypeHandler handler ? handler : null;
        }

        /// <summary>
        /// Enumerate all the node type handlers used by this hierarchy.
        /// </summary>
        /// <returns>An enumerable of hierarchy node type handler.</returns>
        public static HierarchyNodeTypeHandlerEnumerable EnumerateNodeTypeHandlers(this Hierarchy hierarchy) => new HierarchyNodeTypeHandlerEnumerable(hierarchy);
    }
}
