// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A hierarchy view model is a read-only filtering view of a <see cref="HierarchyFlattened"/>.
    /// </summary>
    /// <seealso cref="Hierarchy"/>
    /// <seealso cref="HierarchyNode"/>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyViewModel.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyViewModelBindings.h")]
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyViewModel : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyViewModel viewModel) => viewModel.m_Ptr;
        }

        IntPtr m_Ptr;
        internal readonly Hierarchy m_Hierarchy;
        internal readonly HierarchyFlattened m_HierarchyFlattened;
        ReadOnlyNativeVector<HierarchyFlattenedNode> m_FlattenedNodes;
        ReadOnlyNativeVector<HierarchyNode> m_Nodes;
        uint m_Version;
        readonly bool m_IsOwner;
        Dictionary<int, IHierarchyNodeTypeHandlerViewModelState> m_HandlerStates;

        /// <summary>
        /// Delegate that is invoked when flags on hierarchy nodes are changed.
        /// </summary>
        /// <param name="flags">The flags that were changed on hierarchy nodes.</param>
        /// <seealso cref="FlagsChanged"/>
        public delegate void FlagsChangedEventHandler(HierarchyNodeFlags flags);

        /// <summary>
        /// Event that is invoked when flags on hierarchy nodes are changed.
        /// </summary>
        /// <seealso cref="FlagsChangedEventHandler"/>
        /// <seealso cref="BeginFlagsChange"/>
        /// <seealso cref="EndFlagsChange"/>
        public event FlagsChangedEventHandler FlagsChanged;

        /// <summary>
        /// Whether this object is valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The total number of hierarchy nodes in the hierarchy view model.
        /// </summary>
        /// <remarks>
        /// The total does not include the <see cref="Hierarchy.Root"/> node.
        /// </remarks>
        public int Count => m_Nodes.Count;

        /// <summary>
        /// Whether the hierarchy view model is currently updating.
        /// </summary>
        /// <remarks>
        /// This happens when <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/> is used.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating", IsThreadSafe = true)] get; }

        /// <summary>
        /// Whether the hierarchy view model requires an update.
        /// </summary>
        /// <remarks>
        /// This happens when the underlying hierarchy changes topology.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded", IsThreadSafe = true)] get; }

        /// <summary>
        /// Whether the hierarchy view model is currently filtering nodes.
        /// </summary>
        /// <remarks>
        /// This happens when there is a non empty <see cref="HierarchySearchQueryDescriptor"/> set.
        /// </remarks>
        public extern bool Filtering { [NativeMethod("Filtering", IsThreadSafe = true)] get; }

        /// <summary>
        /// The number of nodes that directly matched the search query.
        /// </summary>
        /// <remarks>
        /// This count excludes container nodes (such as scenes) that are visible only because they contain matching descendants.
        /// The count is computed during filtering and is available after the view model finishes updating.
        /// </remarks>
        internal extern int SearchMatchCount { [NativeMethod("GetSearchMatchCount")] get; }

        internal ReadOnlyNativeVector<HierarchyFlattenedNode> FlattenedNodes
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_FlattenedNodes;
        }

        internal ReadOnlyNativeVector<HierarchyNode> Nodes
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Nodes;
        }

        internal uint Version
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Version;
        }

        internal extern float UpdateProgress
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod("UpdateProgress", IsThreadSafe = true)]
            get;
        }

        internal IHierarchySearchQueryParser QueryParser
        {
            [VisibleToOtherModules("UnityEditor.HierarchyModule")]
            get;
            [VisibleToOtherModules("UnityEditor.HierarchyModule")]
            set;
        }

        internal extern HierarchySearchQueryDescriptor Query
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod(IsThreadSafe = true)]
            get;
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod(IsThreadSafe = true)]
            set;
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyViewModel"/> from a flattened hierarchy.
        /// </summary>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        public HierarchyViewModel(HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None)
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), hierarchyFlattened, defaultFlags, out var flattenedNodesPtr, out var flattenedNodesCount, out var nodesPtr, out var nodesCount, out var version);
            m_Hierarchy = hierarchyFlattened.m_Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            m_Nodes = new ReadOnlyNativeVector<HierarchyNode>(nodesPtr, nodesCount);
            m_Version = version;
            m_IsOwner = true;

            QueryParser = new DefaultHierarchySearchQueryParser();
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyViewModel"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        /// <param name="flattenedNodesPtr">The native pointer to the flattened nodes.</param>
        /// <param name="flattenedNodesCount">The number of flattened nodes.</param>
        /// <param name="nodesPtr">The native pointer to the nodes.</param>
        /// <param name="nodesCount">The number of nodes.</param>
        /// <param name="version">The hierarchy view model version.</param>
        HierarchyViewModel(IntPtr nativePtr, HierarchyFlattened hierarchyFlattened, IntPtr flattenedNodesPtr, int flattenedNodesCount, IntPtr nodesPtr, int nodesCount, uint version)
        {
            m_Ptr = nativePtr;
            m_Hierarchy = hierarchyFlattened.m_Hierarchy;
            m_HierarchyFlattened = hierarchyFlattened;
            m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            m_Nodes = new ReadOnlyNativeVector<HierarchyNode>(nodesPtr, nodesCount);
            m_Version = version;
            m_IsOwner = false;

            QueryParser = new DefaultHierarchySearchQueryParser();
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~HierarchyViewModel()
        {
            Dispose(false);
        }
#pragma warning restore UA5000

        /// <summary>
        /// Disposes this object and releases its memory.
        /// </summary>
        public void Dispose()
        {
            DestroyHandlerStates();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the state a node type handler keeps for this view model.
        /// </summary>
        /// <param name="nodeType">The node type of the handler owning the state.</param>
        /// <param name="state">When this method returns, the state, or <see langword="null"/> when there is none.</param>
        /// <typeparam name="T">The type of state the handler stores.</typeparam>
        /// <returns><see langword="true"/> if the handler has state of that type, <see langword="false"/> otherwise.</returns>
        public bool TryGetHandlerState<T>(HierarchyNodeType nodeType, out T state)
            where T : class, IHierarchyNodeTypeHandlerViewModelState
        {
            state = m_HandlerStates != null && m_HandlerStates.TryGetValue(nodeType.Id, out var existing) ? existing as T : null;
            return state != null;
        }

        /// <summary>
        /// Gets the state a node type handler keeps for this view model, creating it when there is none.
        /// </summary>
        /// <param name="nodeType">The node type of the handler owning the state.</param>
        /// <typeparam name="T">The type of state the handler stores.</typeparam>
        /// <returns>The state.</returns>
        public T GetOrCreateHandlerState<T>(HierarchyNodeType nodeType)
            where T : class, IHierarchyNodeTypeHandlerViewModelState, new()
        {
            if (TryGetHandlerState<T>(nodeType, out var existing))
                return existing;

            var state = new T();
            m_HandlerStates ??= new Dictionary<int, IHierarchyNodeTypeHandlerViewModelState>();
            if (m_HandlerStates.TryGetValue(nodeType.Id, out var previous))
                previous?.Dispose();

            m_HandlerStates[nodeType.Id] = state;
            return state;
        }

        /// <summary>
        /// Disposes and forgets the state a node type handler keeps for this view model.
        /// </summary>
        /// <param name="nodeType">The node type of the handler owning the state.</param>
        /// <returns><see langword="true"/> if there was state to destroy, <see langword="false"/> otherwise.</returns>
        public bool DestroyHandlerState(HierarchyNodeType nodeType)
        {
            if (m_HandlerStates == null || !m_HandlerStates.TryGetValue(nodeType.Id, out var state))
                return false;

            state?.Dispose();
            return m_HandlerStates.Remove(nodeType.Id);
        }

        // Handler state belongs to this view model, so nothing else is going to release it
        internal void DestroyHandlerStates()
        {
            if (m_HandlerStates == null)
                return;

            foreach (var state in m_HandlerStates.Values)
                state?.Dispose();

            m_HandlerStates.Clear();
        }

        void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                if (m_IsOwner)
                    Destroy(m_Ptr);

                m_Ptr = IntPtr.Zero;
            }

            m_FlattenedNodes = default;
            m_Nodes = default;
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> at a specified index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A hierarchy node.</returns>
        public ref readonly HierarchyNode this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Nodes[index];
        }

        /// <summary>
        /// Gets the index of a specified node.
        /// </summary>
        /// <param name="node">The hierarchy node to find the index of in the view model.</param>
        /// <returns>An index of the node if found, -1 otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int IndexOf(in HierarchyNode node);

        /// <summary>
        /// Determines if a specified node is in the hierarchy view model.
        /// </summary>
        /// <param name="node">The hierarchy node to search for in the view model.</param>
        /// <returns><see langword="true"/> if the node is found, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool Contains(in HierarchyNode node);

        /// <summary>
        /// Sets the root of the hierarchy view model.
        /// </summary>
        /// <remarks>
        /// This is purely visual and does not affect the underlying hierarchy data.
        /// </remarks>
        /// <param name="node">The hierarchy node to use as the visual root of the view model.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SetRoot(in HierarchyNode node);

        /// <summary>
        /// Gets the root node of the hierarchy view model.
        /// </summary>
        /// <returns>The root <see cref="HierarchyNode"/> of the hierarchy view model.</returns>
        /// <example>
        /// The following example adds an action to a context menu that you can use to select the nearest common ancestor of the GameObjects you have selected in the Hierarchy window. The action appears in the **Hierarchy Samples** submenu of the context menu. The example uses `GetRoot` to exclude the root node from being selected as a common ancestor in the Hierarchy window.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/SelectCommonAncestor`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Select two or more GameObjects.
        ///3. In the Hierarchy window, right-click and select **Hierarchy Samples**, then **Select Common Ancestor**.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/SelectCommonAncestor/SelectCommonAncestor.cs"/>
        /// </example>
        [NativeMethod(IsThreadSafe = true)]
        public extern HierarchyNode GetRoot();

        /// <summary>
        /// Gets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to get the parent for.</param>
        /// <returns>The parent <see cref="HierarchyNode"/> of the specified node.</returns>
        /// <example>
        /// The following example adds an action to a context menu that you can use to select the nearest common ancestor of the GameObjects you have selected in the Hierarchy window. The action appears in the **Hierarchy Samples** submenu of the context menu. The example uses `GetParent` to advance two nodes up the hierarchy in a single loop to find their closest common ancestor.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/SelectCommonAncestor`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Select two or more GameObjects.
        ///3. In the Hierarchy window, right-click and select **Hierarchy Samples**, then **Select Common Ancestor**.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/SelectCommonAncestor/SelectCommonAncestor.cs"/>
        /// </example>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Gets the next sibling of a hierarchy node in the view model.
        /// </summary>
        /// <param name="node">The hierarchy node to get the next sibling for.</param>
        /// <returns>The next sibling <see cref="HierarchyNode"/>.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has.
        /// </summary>
        /// <param name="node">The hierarchy node to count children for.</param>
        /// <returns>The number of direct child nodes of the specified hierarchy node.</returns>
        /// <example>
        /// The following example displays how many children each collapsed item has in the Hierarchy window. It uses `GetChildrenCount` to check whether a collapsed item has children before displaying the label.
        /// To use this example, save the script in a folder called `Assets/Editor/CountWhenCollapsed`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CountWhenCollapsed/CountWhenCollapsed.cs"/>
        /// </example>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Determines whether a hierarchy node has any visible direct children (children without the Hidden flag).
        /// </summary>
        /// <param name="node">The hierarchy node to check for visible children.</param>
        /// <returns><see langword="true"/> if the node has at least one visible direct child, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool HasVisibleChildren(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has, including children of children.
        /// </summary>
        /// <param name="node">The hierarchy node to count descendants for.</param>
        /// <returns>The number of child nodes, including children of children.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildrenCountRecursive(in HierarchyNode node);

        /// <summary>
        /// Gets the child node at the specified index of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to get the child from.</param>
        /// <param name="index">The index of the child to retrieve.</param>
        /// <returns>The child <see cref="HierarchyNode"/> at the specified index.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetChild(in HierarchyNode node, int index);

        /// <summary>
        /// Gets the index of a hierarchy node in its parent's children list.
        /// </summary>
        /// <param name="node">The hierarchy node to get the child index for.</param>
        /// <returns>The index of the node within its parent's children list, or -1 if the node is invalid.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildIndex(in HierarchyNode node);

        /// <summary>
        /// Determines the depth level of a hierarchy node within the view model.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to get the depth for.</param>
        /// <returns>The depth level of the <see cref="HierarchyNode"/>.</returns>
        /// <example>
        /// The following example adds an action to a context menu that you can use to select the nearest common ancestor of the GameObjects you have selected in the Hierarchy window. The action appears in the **Hierarchy Samples** submenu of the context menu. The example uses `GetDepth` to compare two nodes and advance the deeper one upward when finding the nearest common ancestor of all selected nodes.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/SelectCommonAncestor`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Select two or more GameObjects.
        ///3. In the Hierarchy window, right-click and select **Hierarchy Samples**, then **Select Common Ancestor**.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/SelectCommonAncestor/SelectCommonAncestor.cs"/>
        /// </example>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Gets the node type handler instance for the specified node from this hierarchy view model.
        /// </summary>
        /// <returns>If the node has a type, the hierarchy node type handler base instance, <see langword="null"/> otherwise.</returns>
        public HierarchyNodeTypeHandlerBase GetNodeTypeHandlerBase(in HierarchyNode node) => HierarchyNodeTypeHandlerBase.FromIntPtr(GetNodeTypeHandlerFromNode(in node));

        /// <summary>
        /// Gets the hierarchy node type for the specified node.
        /// </summary>
        /// <param name="node">The hierarchy node to get the type for.</param>
        /// <returns>The <see cref="HierarchyNodeType"/> assigned to the specified hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNodeType GetNodeType(in HierarchyNode node);

        /// <summary>
        /// Gets all the flags set on a given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to retrieve the flags from.</param>
        /// <returns>The flags set on the hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNodeFlags GetFlags(in HierarchyNode node);

        /// <summary>
        /// Sets the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The flags to set on all hierarchy nodes in the view model.</param>
        public void SetFlags(HierarchyNodeFlags flags) => SetFlagsAll(flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to set the flags on.</param>
        /// <param name="flags">The flags to set on the hierarchy node.</param>
        public void SetFlags(in HierarchyNode node, HierarchyNodeFlags flags) => SetFlagsNode(in node, flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes to set the flags on.</param>
        /// <param name="flags">The flags to set on the specified hierarchy nodes.</param>
        /// <returns>The number of nodes that had their flags set.</returns>
        public int SetFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => SetFlagsNodes(nodes, flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices to set the flags on.</param>
        /// <param name="flags">The flags to set on the hierarchy nodes at the specified indices.</param>
        /// <returns>The number of nodes that had their flags set.</returns>
        public int SetFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => SetFlagsIndices(indices, flags);

        /// <summary>
        /// Sets the specified flags recursively on the hierarchy node.
        /// </summary>
        /// <param name="node">The root hierarchy node to set flags on recursively.</param>
        /// <param name="flags">The flags to set on the hierarchy node and its descendants.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        /// <example>
        /// The following example draws visual connector lines in the Hierarchy window to show the parent and child relationships between GameObjects. It uses `SetFlagsRecursive` to add a hover flag to a parent node and all of its child nodes when your mouse enters a connector line in the Hierarchy window. 
        ///
        /// The example requires three USS files: `Connectors.uss` for the base styles, `Connectors_dark.uss` for the Dark theme, and `Connectors_light.uss` for the Light theme.
        ///
        /// To use this example, save the script and USS files in a folder called `Assets/Editor/Connectors`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_dark.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_dark.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_light.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_light.uss"/>
        /// </example>
        /// <example>
        /// The following example creates a context menu item that collapses all nodes in the Hierarchy window except the paths to the selected items. The action appears in the **Hierarchy Samples** submenu of the context menu. It uses `SetFlagsRecursive` to expand the ancestors of the selected nodes.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CollapseOthers/CollapseOthers.cs"/>
        /// </example>
        /// <seealso cref="ClearFlagsRecursive(in HierarchyNode, HierarchyNodeFlags, HierarchyTraversalDirection)"/>
        public void SetFlagsRecursive(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => SetFlagsRecursiveNode(in node, flags, direction);

        /// <summary>
        /// Sets the specified flags recursively on the hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes to set flags on recursively.</param>
        /// <param name="flags">The flags to set on the hierarchy nodes and their descendants.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void SetFlagsRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => SetFlagsRecursiveNodes(nodes, flags, direction);

        /// <summary>
        /// Gets whether or not all of the specified flags are set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The flags to check across all hierarchy nodes.</param>
        /// <returns><see langword="true"/> if any node has all of the flags set, <see langword="false"/> otherwise.</returns>
        public bool HasFlags(HierarchyNodeFlags flags) => HasFlagsAny(flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to check for the specified flags.</param>
        /// <param name="flags">The flags to check on the hierarchy node.</param>
        /// <returns><see langword="true"/> if all of the flags are set, <see langword="false"/> otherwise.</returns>
        /// <example>
        /// The following example adds an action to a context menu that you can use to select the nearest common ancestor of the GameObjects you have selected in the Hierarchy window. The action appears in the **Hierarchy Samples** submenu of the context menu. The example uses `HasFlags` to check whether the common ancestor is itself selected, and if so, returns the ancestor's parent instead.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/SelectCommonAncestor`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Select two or more GameObjects.
        ///3. In the Hierarchy window, right-click and select **Hierarchy Samples**, then **Select Common Ancestor**.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/SelectCommonAncestor/SelectCommonAncestor.cs"/>
        /// </example>
        public bool HasFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to count matching hierarchy nodes for.</param>
        /// <returns>The number of nodes that have all of the flags set.</returns>
        /// <example>
        /// The following example creates a context menu item that collapses all nodes in the Hierarchy window except the paths to the selected items. The action appears in the **Hierarchy Samples** submenu of the context menu. It uses `HasFlagsCount` to count the number of selected and expanded nodes in the Hierarchy window.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CollapseOthers/CollapseOthers.cs"/>
        /// </example>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int HasFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are not set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The flags to check across all hierarchy nodes.</param>
        /// <returns><see langword="true"/> if none of the nodes have all of the flags set, <see langword="false"/> otherwise.</returns>
        public bool DoesNotHaveFlags(HierarchyNodeFlags flags) => DoesNotHaveFlagsAny(flags);

        /// <summary>
        /// Gets whether or not all of the specified flags are not set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to check for the absence of the specified flags.</param>
        /// <param name="flags">The flags to verify are absent on the hierarchy node.</param>
        /// <returns><see langword="true"/> if all of the flags are not set, <see langword="false"/> otherwise.</returns>
        /// <example>
        /// The following example displays how many children each collapsed item has in the Hierarchy window. It uses `DoesNotHaveFlags` to check whether a node is collapsed and has children before adding the label to the left of the item name.
        /// To use this example, save the script in a folder called `Assets/Editor/CountWhenCollapsed`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CountWhenCollapsed/CountWhenCollapsed.cs"/>
        /// </example>
        public bool DoesNotHaveFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to count non-matching hierarchy nodes for.</param>
        /// <returns>The number of nodes that do not have all of the flags set.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int DoesNotHaveFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets the first index of a node that has the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The first index with the flags set, or -1 if no node found.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal extern int GetFirstIndexWithFlags(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets the last index of a node that has the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The last index with the flags set, or -1 if no node found.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal extern int GetLastIndexWithFlags(HierarchyNodeFlags flags);

        /// <summary>
        /// Clears the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The flags to clear on all hierarchy nodes in the view model.</param>
        /// <example>
        /// The following example creates a context menu item that collapses all nodes in the Hierarchy window except the paths to the selected items. The action appears in the **Hierarchy Samples** submenu of the context menu. It uses `ClearFlags` to perform the initial collapse.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CollapseOthers/CollapseOthers.cs"/>
        /// </example>
        public void ClearFlags(HierarchyNodeFlags flags) => ClearFlagsAll(flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to clear the flags on.</param>
        /// <param name="flags">The flags to clear on the hierarchy node.</param>
        public void ClearFlags(in HierarchyNode node, HierarchyNodeFlags flags) => ClearFlagsNode(in node, flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes to clear the flags on.</param>
        /// <param name="flags">The flags to clear on the specified hierarchy nodes.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ClearFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => ClearFlagsNodes(nodes, flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices to clear the flags on.</param>
        /// <param name="flags">The flags to clear on the hierarchy nodes at the specified indices.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ClearFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => ClearFlagsIndices(indices, flags);

        /// <summary>
        /// Clears the specified flags recursively on the hierarchy node.
        /// </summary>
        /// <param name="node">The root hierarchy node to clear flags on recursively.</param>
        /// <param name="flags">The flags to clear on the hierarchy node and its descendants.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        /// <example>
        /// The following example draws visual connector lines in the Hierarchy window to show the parent and child relationships between GameObjects. It uses `ClearFlagsRecursive` to remove a hover flag set on a parent node and all of its child nodes when your mouse leaves a connector line in the Hierarchy window. 
        ///
        /// The example requires three USS files: `Connectors.uss` for the base styles, `Connectors_dark.uss` for the Dark theme, and `Connectors_light.uss` for the Light theme.
        ///
        /// To use this example, save the script and USS files in a folder called `Assets/Editor/Connectors`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_dark.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_dark.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `Connectors_light.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/Connectors/Connectors_light.uss"/>
        /// </example>
        /// <seealso cref="SetFlagsRecursive(in HierarchyNode, HierarchyNodeFlags, HierarchyTraversalDirection)"/>
        public void ClearFlagsRecursive(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ClearFlagsRecursiveNode(in node, flags, direction);

        /// <summary>
        /// Clears the specified flags recursively on the hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes to clear flags on recursively.</param>
        /// <param name="flags">The flags to clear on the hierarchy nodes and their descendants.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ClearFlagsRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ClearFlagsRecursiveNodes(nodes, flags, direction);

        /// <summary>
        /// Toggles the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The flags to toggle on all hierarchy nodes in the view model.</param>
        public void ToggleFlags(HierarchyNodeFlags flags) => ToggleFlagsAll(flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to toggle the flags on.</param>
        /// <param name="flags">The flags to toggle on the hierarchy node.</param>
        public void ToggleFlags(in HierarchyNode node, HierarchyNodeFlags flags) => ToggleFlagsNode(in node, flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes to toggle the flags on.</param>
        /// <param name="flags">The flags to toggle on the specified hierarchy nodes.</param>
        /// <returns>The number of nodes that had their flags toggled.</returns>
        public int ToggleFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => ToggleFlagsNodes(nodes, flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices to toggle the flags on.</param>
        /// <param name="flags">The flags to toggle on the hierarchy nodes at the specified indices.</param>
        /// <returns>The number of nodes that had their flags toggled.</returns>
        public int ToggleFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => ToggleFlagsIndices(indices, flags);

        /// <summary>
        /// Toggles the specified flags recursively on the hierarchy node.
        /// </summary>
        /// <param name="node">The root hierarchy node to toggle flags on recursively.</param>
        /// <param name="flags">The flags to toggle on the hierarchy node and its descendants.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ToggleFlagsRecursive(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ToggleFlagsRecursiveNode(in node, flags, direction);

        /// <summary>
        /// Toggles the specified flags recursively on the hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes to toggle flags on recursively.</param>
        /// <param name="flags">The flags to toggle on the hierarchy nodes and their descendants.</param>
        /// <param name="direction">The direction of the recursion operation.</param>
        public void ToggleFlagsRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction) => ToggleFlagsRecursiveNodes(nodes, flags, direction);

        /// <summary>
        /// Begins a batch of flags changes.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void BeginFlagsChange();

        /// <summary>
        /// Ends a batch of flags changes.
        /// </summary>
        /// <returns>The flags that were changed during the batch.</returns>
        public HierarchyNodeFlags EndFlagsChange() => EndFlagsChange(true);

        /// <summary>
        /// Ends a batch of flags changes without notifying listeners.
        /// </summary>
        /// <returns>The flags that were changed during the batch.</returns>
        public HierarchyNodeFlags EndFlagsChangeWithoutNotify() => EndFlagsChange(false);

        /// <summary>
        /// Gets all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to match against hierarchy nodes.</param>
        /// <param name="outNodes">The output span to write matching hierarchy nodes into.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to match against hierarchy nodes.</param>
        /// <returns>An array containing all hierarchy nodes that have all of the specified flags set.</returns>
        public HierarchyNode[] GetNodesWithFlags(HierarchyNodeFlags flags)
        {
            var count = HasFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to match when enumerating hierarchy nodes.</param>
        /// <returns>An enumerable that iterates over all nodes with all of the specified flags set.</returns>
        /// <example>
        /// The following example creates a context menu item that collapses all nodes in the Hierarchy window except the paths to the selected items. The action appears in the **Hierarchy Samples** submenu of the context menu. It uses `EnumerateNodesWithFlags` to iterate the selected nodes and expand the path to each one.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CollapseOthers/CollapseOthers.cs"/>
        /// </example>
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithFlags(HierarchyNodeFlags flags) => new HierarchyViewModelNodesEnumerable(this, flags, HasFlagsNode);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to match against hierarchy nodes.</param>
        /// <param name="outIndices">The output span to write matching hierarchy node indices into.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices for all hierarchy nodes that have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to match against hierarchy nodes.</param>
        /// <returns>An array that contains the indices of all hierarchy nodes that have all of the specified flags set.</returns>
        public int[] GetIndicesWithFlags(HierarchyNodeFlags flags)
        {
            var count = HasFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Gets all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to exclude when matching hierarchy nodes.</param>
        /// <param name="outNodes">The output span to write matching hierarchy nodes into.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithoutFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to exclude when matching hierarchy nodes.</param>
        /// <returns>An array that contains all hierarchy nodes that do not have all of the specified flags set.</returns>
        public HierarchyNode[] GetNodesWithoutFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveFlagsCount(flags);
            if (count == 0)
                return Array.Empty<HierarchyNode>();

            var nodes = new HierarchyNode[count];
            GetNodesWithoutFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to exclude when enumerating hierarchy nodes.</param>
        /// <returns>An enumerable that iterates over all nodes that do not have all of the specified flags set.</returns>
        public HierarchyViewModelNodesEnumerable EnumerateNodesWithoutFlags(HierarchyNodeFlags flags) => new HierarchyViewModelNodesEnumerable(this, flags, DoesNotHaveFlagsNode);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to exclude when matching hierarchy nodes.</param>
        /// <param name="outIndices">The output span to write matching hierarchy node indices into.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithoutFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets the indices of all hierarchy nodes that do not have all of the specified flags set.
        /// </summary>
        /// <param name="flags">The flags to exclude when matching hierarchy nodes.</param>
        /// <returns>An array that contains the indices of all hierarchy nodes that do not have all of the specified flags set.</returns>
        public int[] GetIndicesWithoutFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveFlagsCount(flags);
            if (count == 0)
                return Array.Empty<int>();

            var indices = new int[count];
            GetIndicesWithoutFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Sets the search query to filter the hierarchy nodes displayed in the view model.
        /// </summary>
        /// <param name="query">The search query string used to filter hierarchy nodes.</param>
        public void SetQuery(string query)
        {
            var newQuery = QueryParser.ParseQuery(query);
            if (newQuery == Query)
                return;
            Query = newQuery;
        }

        /// <summary>
        /// Updates the hierarchy view model and requests a rebuild of the list of <see cref="HierarchyNode"/> that filters the <see cref="HierarchyFlattened"/>.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Update();

        /// <summary>
        /// Updates the hierarchy view model incrementally.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncremental();

        /// <summary>
        /// Updates the hierarchy view model incrementally until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The maximum duration in milliseconds to spend updating the hierarchy view model.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncrementalTimed(double milliseconds);

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator for iterating over all hierarchy nodes in the view model.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>. Enumerates and filters items at the same time.
        /// </summary>
        /// <seealso cref="HierarchyViewModel.GetEnumerator"/>
        public struct Enumerator
        {
            readonly HierarchyViewModel m_ViewModel;
            readonly ReadOnlyNativeVector<HierarchyNode> m_Nodes;
            readonly uint m_Version;
            int m_Index;

            internal Enumerator(HierarchyViewModel hierarchyViewModel)
            {
                m_ViewModel = hierarchyViewModel;
                m_Nodes = hierarchyViewModel.m_Nodes;
                m_Version = hierarchyViewModel.m_Version;
                m_Index = -1;
            }

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public ref readonly HierarchyNode Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if (m_Version != m_ViewModel.m_Version)
                        throw new InvalidOperationException("HierarchyViewModel was modified.");

                    return ref m_Nodes[m_Index];
                }
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns>Returns true if Current item is valid</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Nodes.Count;
        }

        /// <summary>
        /// Returns a read-only span of all hierarchy nodes in the view model.
        /// </summary>
        /// <returns>A read-only span of hierarchy nodes.</returns>
        public ReadOnlySpan<HierarchyNode> AsReadOnlySpan() => m_Nodes.AsReadOnlySpan();

        // Currently required to feed UI Toolkit containers itemsSource property, which requires the collection to
        // be an IList. We do not want HierarchyViewModel to be an IList, so we provide a read-only list wrapper.
        [VisibleToOtherModules]
        internal sealed class ReadOnlyList : IList
        {
            readonly HierarchyViewModel m_ViewModel;

            internal ReadOnlyList(HierarchyViewModel viewModel)
            {
                m_ViewModel = viewModel;
            }

            public bool IsFixedSize => true;
            public bool IsReadOnly => true;

            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_ViewModel.IsCreated ? m_ViewModel.Count : throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
            }

            public object this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_ViewModel.IsCreated ? m_ViewModel[index] : throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
                set => throw new NotSupportedException();
            }

            public bool Contains(object value)
            {
                if (value is HierarchyNode node)
                {
                    return m_ViewModel.IsCreated ?
                        m_ViewModel.Contains(in node) :
                        throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
                }
                return false;
            }

            public int IndexOf(object value)
            {
                if (value is HierarchyNode node)
                {
                    return m_ViewModel.IsCreated ?
                        m_ViewModel.IndexOf(in node) :
                        throw new NullReferenceException($"{nameof(HierarchyViewModel)} has been disposed.");
                }
                return -1;
            }

            public void CopyTo(Array array, int index)
            {
                for (var i = index; i < m_ViewModel.Count; ++i)
                    array.SetValue(m_ViewModel[i], i - index);
            }

            public Enumerator GetEnumerator() => new HierarchyViewModel.Enumerator(m_ViewModel);

            int IList.Add(object value) => throw new NotSupportedException();
            void IList.Clear() => throw new NotSupportedException();
            void IList.Insert(int index, object value) => throw new NotSupportedException();
            void IList.Remove(object value) => throw new NotSupportedException();
            void IList.RemoveAt(int index) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();
            bool ICollection.IsSynchronized => throw new NotImplementedException();
            object ICollection.SyncRoot => throw new NotImplementedException();
        }

        [VisibleToOtherModules]
        internal ReadOnlyList AsReadOnlyList() => new ReadOnlyList(this);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [NativeMethod(IsThreadSafe = true)]
        internal extern byte[] GetState();

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [NativeMethod(IsThreadSafe = true)]
        internal extern void SetState(ReadOnlySpan<byte> bytes);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal void SetParentOfSelection(in HierarchyNode parentNode) => SelectionSetParent(in parentNode);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal void SetParentOfSelection(in HierarchyNode parentNode, int index) => SelectionSetParentAt(in parentNode, index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyViewModel FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyViewModel)GCHandle.FromIntPtr(handlePtr).Target : null;

        [FreeFunction("HierarchyViewModelBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, HierarchyFlattened hierarchyFlattened, HierarchyNodeFlags defaultFlags, out IntPtr nodesPtr, out int nodesCount, out IntPtr indicesPtr, out int indicesCount, out uint version);

        [FreeFunction("HierarchyViewModelBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [FreeFunction("HierarchyViewModelBindings::GetNodeTypeHandlerFromNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr GetNodeTypeHandlerFromNode(in HierarchyNode node);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int SetFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsRecursiveNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetFlagsRecursiveNode(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsRecursiveNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern void SetFlagsRecursiveNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int SetFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::HasFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool HasFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveFlagsAny", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveFlagsAny(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::DoesNotHaveFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool DoesNotHaveFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ClearFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ClearFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ClearFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ClearFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsRecursiveNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ClearFlagsRecursiveNode(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsRecursiveNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern void ClearFlagsRecursiveNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsAll", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsAll(HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ToggleFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsIndices", HasExplicitThis = true, IsThreadSafe = true)]
        extern int ToggleFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsRecursiveNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void ToggleFlagsRecursiveNode(in HierarchyNode node, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsRecursiveNodes", HasExplicitThis = true, IsThreadSafe = true)]
        extern void ToggleFlagsRecursiveNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags, HierarchyTraversalDirection direction);

        [FreeFunction("HierarchyViewModelBindings::EndFlagsChange", HasExplicitThis = true, IsThreadSafe = true)]
        extern HierarchyNodeFlags EndFlagsChange(bool notify);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [FreeFunction("HierarchyViewModelBindings::GetNodesWithoutFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodesWithoutFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyViewModelBindings::GetIndicesWithoutFlagsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetIndicesWithoutFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [FreeFunction("HierarchyViewModelBindings::SelectionSetParent", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SelectionSetParent(in HierarchyNode parentNode);

        [FreeFunction("HierarchyViewModelBindings::SelectionSetParentAt", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SelectionSetParentAt(in HierarchyNode parentNode, int index);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateHierarchyViewModel(IntPtr nativePtr, IntPtr flattenedPtr, IntPtr flattenedNodesPtr, int flattenedNodesCount, IntPtr nodesPtr, int nodesCount, uint version) =>
            GCHandle.ToIntPtr(GCHandle.Alloc(new HierarchyViewModel(nativePtr, HierarchyFlattened.FromIntPtr(flattenedPtr), flattenedNodesPtr, flattenedNodesCount, nodesPtr, nodesCount, version)));

        [RequiredByNativeCode]
        static void UpdateHierarchyViewModel(IntPtr handlePtr, IntPtr flattenedNodesPtr, int flattenedNodesCount, IntPtr nodesPtr, int nodesCount, uint version)
        {
            var viewModel = FromIntPtr(handlePtr);
            viewModel.m_FlattenedNodes = new ReadOnlyNativeVector<HierarchyFlattenedNode>(flattenedNodesPtr, flattenedNodesCount);
            viewModel.m_Nodes = new ReadOnlyNativeVector<HierarchyNode>(nodesPtr, nodesCount);
            viewModel.m_Version = version;
        }

        [RequiredByNativeCode]
        static void InvokeFlagsChanged(IntPtr handlePtr, HierarchyNodeFlags flags)
        {
            var viewModel = FromIntPtr(handlePtr);
            viewModel.FlagsChanged?.Invoke(flags);
        }

        [RequiredByNativeCode]
        static void SearchBegin(IntPtr handlePtr)
        {
            var viewModel = FromIntPtr(handlePtr);
            foreach (var handler in viewModel.m_Hierarchy.EnumerateNodeTypeHandlersBase())
                handler.Internal_SearchBegin(viewModel.Query, viewModel);
        }
        #endregion
    }
}
