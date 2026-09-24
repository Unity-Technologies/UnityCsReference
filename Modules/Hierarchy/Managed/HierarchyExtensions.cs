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
        /// <example>
        /// The following example adds context menu actions to the Hierarchy window to add or remove a specific component from a GameObject. The actions appear in the **Hierarchy Samples** submenu of the context menu. The example uses `GetNodeTypeHandler` to check whether each selected node is handled by a `HierarchyGameObjectHandler` and filters out nodes of other types. 
        ///
        /// The example requires a custom MonoBehaviour script called `Enemy.cs`.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/CustomContextMenuAction`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Save the `Enemy.cs` script outside of an `Editor` folder, because MonoBehaviour scripts in an `Editor` folder can't be attached to GameObjects.
        ///3. In the Hierarchy window, right-click one or more GameObjects and open the **Hierarchy Samples** submenu. Select **Turn into Enemy** to add the `Enemy` component, or **Remove Enemy component** to remove it.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CustomContextMenuAction/CustomContextMenuAction.cs"/>
        /// </example>
        /// <example>
        /// The following example shows the `Enemy` component that the CustomContextMenuAction example uses.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Runtime/Enemy.cs"/>
        /// </example>
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
