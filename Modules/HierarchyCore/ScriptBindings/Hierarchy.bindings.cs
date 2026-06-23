// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a tree-like container of nodes.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/Hierarchy.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyBindings.h")]
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyNodeTypeHandlerBase.h")]
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
    public sealed class Hierarchy : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(Hierarchy hierarchy) => hierarchy.m_Ptr;
        }

        IntPtr m_Ptr;
        readonly IntPtr m_RootPtr;
        readonly IntPtr m_VersionPtr;
        readonly bool m_IsOwner;

        /// <summary>
        /// Whether or not this object is valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The root node.
        /// </summary>
        /// <remarks>
        /// The root node does not need to be created, and it cannot be modified.
        /// </remarks>
        public ref readonly HierarchyNode Root
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return ref *(HierarchyNode*)m_RootPtr;
                }
            }
        }

        /// <summary>
        /// The total number of nodes.
        /// </summary>
        /// <remarks>
        /// This total count does not include the <see cref="Root"/> node.
        /// </remarks>
        public extern int Count { [NativeMethod("Count", IsThreadSafe = true)] get; }

        /// <summary>
        /// Whether the hierarchy is currently updating.
        /// </summary>
        /// <remarks>
        /// Updating happens during the use of <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/>.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating", IsThreadSafe = true)] get; }

        /// <summary>
        /// Whether the hierarchy requires an update.
        /// </summary>
        /// <remarks>
        /// An update is required when changes in registered hierarchy node handlers are pending.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded", IsThreadSafe = true)] get; }

        /// <summary>
        /// The version of the hierarchy.
        /// </summary>
        internal int Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return *(int*)m_VersionPtr;
                }
            }
        }

        /// <summary>
        /// Delegate that is invoked when a new hierarchy node type handler is created.
        /// </summary>
        /// <param name="handler">The hierarchy node type handler that was created.</param>
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal delegate void HandlerCreatedEventHandler(HierarchyNodeTypeHandlerBase handler);

        /// <summary>
        /// Event that is invoked when a new hierarchy node type handler is created.
        /// </summary>
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal event HandlerCreatedEventHandler HandlerCreated;

        /// <summary>
        /// Constructs a new <see cref="Hierarchy"/>.
        /// </summary>
        public Hierarchy()
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), out var rootPtr, out var versionPtr);
            m_RootPtr = rootPtr;
            m_VersionPtr = versionPtr;
            m_IsOwner = true;
        }

        /// <summary>
        /// Constructs a new <see cref="Hierarchy"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="rootPtr">The root node pointer.</param>
        /// <param name="versionPtr">The version pointer.</param>
        Hierarchy(IntPtr nativePtr, IntPtr rootPtr, IntPtr versionPtr)
        {
            m_Ptr = nativePtr;
            m_RootPtr = rootPtr;
            m_VersionPtr = versionPtr;
            m_IsOwner = false;
        }

        ~Hierarchy()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose this object to release its memory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                if (m_IsOwner)
                    Destroy(m_Ptr);

                m_Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Get or create a hierarchy node type handler instance for this hierarchy.
        /// </summary>
        /// <remarks>
        /// If a hierarchy node type handler with that type is already created, the same instance is returned.
        /// </remarks>
        /// <returns>The hierarchy node type handler instance for that type.</returns>
        public T GetOrCreateNodeTypeHandler<T>() where T : HierarchyNodeTypeHandlerBase => (T)HierarchyNodeTypeHandlerBase.FromIntPtr(GetOrCreateNodeTypeHandler(typeof(T)));

        /// <summary>
        /// Gets a hierarchy node type handler instance from this hierarchy.
        /// </summary>
        /// <returns>The hierarchy node type handler instance for that type if already created, <see langword="null"/> otherwise.</returns>
        public T GetNodeTypeHandlerBase<T>() where T : HierarchyNodeTypeHandlerBase => (T)HierarchyNodeTypeHandlerBase.FromIntPtr(GetNodeTypeHandlerFromType(typeof(T)));

        /// <summary>
        /// Gets the node type handler instance for the specified node from this hierarchy.
        /// </summary>
        /// <returns>If the node has a type, the hierarchy node type handler base instance, <see langword="null"/> otherwise.</returns>
        public HierarchyNodeTypeHandlerBase GetNodeTypeHandlerBase(in HierarchyNode node) => HierarchyNodeTypeHandlerBase.FromIntPtr(GetNodeTypeHandlerFromNode(in node));

        /// <summary>
        /// Gets the node type handler instance for the specified node type name from this hierarchy.
        /// </summary>
        /// <param name="nodeTypeName">The node type name.</param>
        /// <returns>If the node type name matches a registered node type handler, the hierarchy node type handler base instance, <see langword="null"/> otherwise.</returns>
        public HierarchyNodeTypeHandlerBase GetNodeTypeHandlerBase(string nodeTypeName) => HierarchyNodeTypeHandlerBase.FromIntPtr(GetNodeTypeHandlerFromName(nodeTypeName));

        /// <summary>
        /// Enumerates all the <see cref="HierarchyNodeTypeHandlerBase"/> instances that this hierarchy uses.
        /// </summary>
        /// <returns>A <see cref="HierarchyNodeTypeHandlerBaseEnumerable"/> of <see cref="HierarchyNodeTypeHandlerBase"/> instances.</returns>
        public HierarchyNodeTypeHandlerBaseEnumerable EnumerateNodeTypeHandlersBase() => new HierarchyNodeTypeHandlerBaseEnumerable(this);

        /// <summary>
        /// Gets the type of the specified hierarchy node.
        /// </summary>
        /// <returns>The hierarchy node type.</returns>
        public HierarchyNodeType GetNodeType<T>() where T : HierarchyNodeTypeHandlerBase => GetNodeTypeFromType(typeof(T));

        /// <summary>
        /// Retrieve the hierarchy node type for the specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The hierarchy node type.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNodeType GetNodeType(in HierarchyNode node);

        /// <summary>
        /// Ensures that the hierarchy has enough memory reserved for storing the specified number of nodes.
        /// </summary>
        /// <param name="count">The number of nodes to reserve memory for.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void Reserve(int count);

        /// <summary>
        /// Ensures that the hierarchy node has enough memory reserved for storing the specified number of children nodes.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="count">The number of children nodes to reserve memory for.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void ReserveChildren(in HierarchyNode node, int count);

        /// <summary>
        /// Determines whether a node exists or not.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node exists, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool Exists(in HierarchyNode node);

        /// <summary>
        /// Determines whether multiple nodes exist or not.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes to check.</param>
        /// <param name="exists">The span to fill with existence results. Must have the same length as <paramref name="nodes"/>.</param>
        /// <returns><see langword="true"/> if all nodes exist, <see langword="false"/> otherwise.</returns>
        internal bool Exists(ReadOnlySpan<HierarchyNode> nodes, Span<bool> exists) => ExistsSpan(nodes, exists);

        /// <summary>
        /// Gets the next sibling of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The next sibling of the hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Determines the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node. A value of -1 indicates the root node. A value of 0 indicates direct child nodes of the root node. A value of 1 indicates child nodes of the root node's direct children, and then their children have a value of 2 and so on. </returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Adds a new node that has a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the hierarchy node to add.</param>
        /// <returns>A hierarchy node.</returns>
        public HierarchyNode Add(in HierarchyNode parent) => AddNode(in parent);

        /// <summary>
        /// Adds multiple new nodes that have a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the hierarchy nodes.</param>
        /// <param name="count">The number of nodes to create.</param>
        /// <returns>An array of hierarchy nodes.</returns>
        public HierarchyNode[] Add(in HierarchyNode parent, int count)
        {
            if (count < 0)
                throw new ArgumentException($"{nameof(count)} must be positive, but was {count}");

            var nodes = new HierarchyNode[count];
            AddNodeSpan(in parent, nodes);
            return nodes;
        }

        /// <summary>
        /// Adds multiple new nodes that have a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the hierarchy nodes.</param>
        /// <param name="outNodes">The span of nodes to fill with new nodes.</param>
        public void Add(in HierarchyNode parent, Span<HierarchyNode> outNodes) => AddNodeSpan(in parent, outNodes);

        /// <summary>
        /// Removes a node from the hierarchy.
        /// </summary>
        /// <param name="node">The hierarchy node to remove from the hierarchy.</param>
        /// <returns><see langword="true"/> if the node was removed, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool Remove(in HierarchyNode node);

        /// <summary>
        /// Recursively removes all children of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void RemoveChildren(in HierarchyNode node);

        /// <summary>
        /// Removes all nodes from the hierarchy.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Clear();

        /// <summary>
        /// Sets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="parent">The hierarchy node to set as a parent.</param>
        public void SetParent(in HierarchyNode node, in HierarchyNode parent) => SetNodeParent(in node, in parent);

        /// <summary>
        /// Sets the parent of a hierarchy node.
        /// </summary>
        /// <remarks>
        /// The index maximum value is the parent's child count, or child count minus one if moving within the same parent.
        /// </remarks>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="parent">The hierarchy node to set as a parent.</param>
        /// <param name="index">The index at which to insert the node in the parent's children list.</param>
        public void SetParent(in HierarchyNode node, in HierarchyNode parent, int index) => SetNodeParentAt(in node, in parent, index);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal void SetParent(ReadOnlySpan<HierarchyNode> nodes, in HierarchyNode parent, int index) => SetNodesParentAt(nodes, in parent, index);

        /// <summary>
        /// Gets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Gets the child node at the specified index of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="index">The child index.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode GetChild(in HierarchyNode node, int index);

        /// <summary>
        /// Gets the index of a hierarchy node in its parent's children list.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The node index, or -1 if invalid.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildIndex(in HierarchyNode node);

        /// <summary>
        /// Gets the child nodes of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>An array of <see cref="HierarchyNode"/> instances.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern HierarchyNode[] GetChildren(in HierarchyNode node);

        /// <summary>
        /// Gets the child nodes of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="outChildren">The span of nodes to fill with child nodes.</param>
        /// <returns>The number of <see cref="HierarchyNode"/> instances written to the <paramref name="outChildren"/> span.</returns>
        public int GetChildren(in HierarchyNode node, Span<HierarchyNode> outChildren) => GetNodeChildrenSpan(in node, outChildren);

        /// <summary>
        /// Gets the child nodes of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>An enumerable of hierarchy node children.</returns>
        public HierarchyNodeChildren EnumerateChildren(in HierarchyNode node) => new HierarchyNodeChildren(this, EnumerateChildrenPtr(in node));

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of child nodes.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has, including children of children.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of child nodes, including children of children.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetChildrenCountRecursive(in HierarchyNode node);

        /// <summary>
        /// Sets the sort index of a hierarchy node.
        /// </summary>
        /// <remarks>
        /// After setting sort indexes, you must call <see cref="SortChildren(in HierarchyNode)"/> or <see cref="SortChildrenRecursive"/> on the parent node to sort the child nodes.
        /// </remarks>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="sortIndex">The sort index.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SetSortIndex(in HierarchyNode node, int sortIndex);

        /// <summary>
        /// Gets the sort index of a hierarchy node. Default is 0.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The sort index.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetSortIndex(in HierarchyNode node);

        /// <summary>
        /// Sorts the child nodes of a hierarchy node according to their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SortChildren(in HierarchyNode node);

        /// <summary>
        /// Sorts the child nodes of a hierarchy node according to their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SortChildrenRecursive(in HierarchyNode node);

        /// <summary>
        /// Marks a hierarchy node as requiring sorting of its children in the next sorting operation.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SetChildrenNeedsSorting(in HierarchyNode node);

        /// <summary>
        /// Gets whether the child nodes of a hierarchy node need to be sorted.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the child nodes of a hierarchy node need to be sorted, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool DoesChildrenNeedsSorting(in HierarchyNode node);

        /// <summary>
        /// Creates an unmanaged property with a specified name.
        /// </summary>
        /// <remarks>
        /// The result of this method should be stored and reused to avoid the costly lookup by name.
        /// </remarks>
        /// <param name="name">The property name.</param>
        /// <param name="type">The storage type for the property.</param>
        /// <returns>The property accessor.</returns>
        public HierarchyPropertyUnmanaged<T> GetOrCreatePropertyUnmanaged<T>(string name, HierarchyPropertyStorageType type = HierarchyPropertyStorageType.Default) where T : unmanaged
        {
            var property = GetOrCreateProperty(name, new HierarchyPropertyDescriptor
            {
                Size = UnsafeUtility.SizeOf<T>(),
                Type = type,
            });
            return new HierarchyPropertyUnmanaged<T>(this, property);
        }

        /// <summary>
        /// Creates a string property with a specified name.
        /// </summary>
        /// <remarks>
        /// The result of this method should be stored and reused to avoid the costly lookup by name.
        /// </remarks>
        /// <param name="name">The property name.</param>
        /// <returns>The property accessor.</returns>
        public HierarchyPropertyString GetOrCreatePropertyString(string name)
        {
            var property = GetOrCreateProperty(name, new HierarchyPropertyDescriptor
            {
                Size = 0,
                Type = HierarchyPropertyStorageType.Blob
            });
            return new HierarchyPropertyString(this, property);
        }

        /// <summary>
        /// Sets the name of a hierarchy node.
        /// </summary>
        /// <remarks>
        /// This operation fails if the node has a handler that does not allow name changes.
        /// </remarks>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="name">The name of the node.</param>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void SetName(in HierarchyNode node, string name);

        /// <summary>
        /// Gets the name of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The name of the node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern string GetName(in HierarchyNode node);

        /// <summary>
        /// Gets the path of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The path of the node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern string GetPath(in HierarchyNode node);

        /// <summary>
        /// Gets the hierarchy node corresponding to the given entity id.
        /// </summary>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The hierarchy node, or <see cref="HierarchyNode.Null"/> if not found.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern HierarchyNode GetNodeFromEntityId(EntityId entityId);

        /// <summary>
        /// Gets the hierarchy nodes corresponding to the given entity ids.
        /// </summary>
        /// <param name="entityIds">The entity ids to look up.</param>
        /// <param name="outNodes">The span to fill with hierarchy nodes. Must have the same length as <paramref name="entityIds"/>.</param>
        /// <returns>The number of slots that could not be resolved by any handler.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetNodesFromEntityIds(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes);

        /// <summary>
        /// Gets the entity id corresponding to the given hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The entity id, or <see cref="EntityId.None"/> if not found.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern EntityId GetEntityIdFromNode(in HierarchyNode node);

        /// <summary>
        /// Gets the entity ids corresponding to the given hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes to look up.</param>
        /// <param name="outEntityIds">The span to fill with entity ids. Must have the same length as <paramref name="nodes"/>.</param>
        /// <returns>The number of slots that could not be resolved by any handler.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetEntityIdsFromNodes(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds);

        /// <summary>
        /// Force an update of the hierarchy, even if no changes are pending.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void SetDirty();

        /// <summary>
        /// Updates the hierarchy, executing all pending changes in the command list.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Update();

        /// <summary>
        /// Updates the hierarchy incrementally, executing one pending change in the command list.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncremental();

        /// <summary>
        /// Updates the hierarchy incrementally, executing pending changes in the command list until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time period in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern bool UpdateIncrementalTimed(double milliseconds);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Hierarchy FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (Hierarchy)GCHandle.FromIntPtr(handlePtr).Target : null;

        [FreeFunction("HierarchyBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, out IntPtr rootPtr, out IntPtr versionPtr);

        [FreeFunction("HierarchyBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [FreeFunction("HierarchyBindings::GetOrCreateNodeTypeHandler", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr GetOrCreateNodeTypeHandler(Type type);

        [FreeFunction("HierarchyBindings::GetNodeTypeHandlerFromType", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr GetNodeTypeHandlerFromType(Type type);

        [FreeFunction("HierarchyBindings::GetNodeTypeHandlerFromNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr GetNodeTypeHandlerFromNode(in HierarchyNode node);

        [FreeFunction("HierarchyBindings::GetNodeTypeHandlerFromName", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr GetNodeTypeHandlerFromName(string nodeTypeName);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [FreeFunction("HierarchyBindings::GetNodeTypeHandlersBaseCount", HasExplicitThis = true, IsThreadSafe = true)]
        internal extern int GetNodeTypeHandlersBaseCount();

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [FreeFunction("HierarchyBindings::GetNodeTypeHandlersBaseSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern int GetNodeTypeHandlersBaseSpan(Span<IntPtr> outHandlers);

        [FreeFunction("HierarchyBindings::GetNodeTypeFromType", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern HierarchyNodeType GetNodeTypeFromType(Type type);

        [FreeFunction("HierarchyBindings::AddNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern HierarchyNode AddNode(in HierarchyNode parent);

        [FreeFunction("HierarchyBindings::AddNodeSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void AddNodeSpan(in HierarchyNode parent, Span<HierarchyNode> nodes);

        [VisibleToOtherModules]
        [FreeFunction("HierarchyBindings::GetCommandList", HasExplicitThis = true, IsThreadSafe = true)]
        internal extern HierarchyCommandList GetCommandList();

        [FreeFunction("HierarchyBindings::SetNodeParent", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetNodeParent(in HierarchyNode node, in HierarchyNode parent);

        [FreeFunction("HierarchyBindings::SetNodeParentAt", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern void SetNodeParentAt(in HierarchyNode node, in HierarchyNode parent, int index);

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [FreeFunction("HierarchyBindings::SetNodesParentAt", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern void SetNodesParentAt(ReadOnlySpan<HierarchyNode> nodes, in HierarchyNode parent, int index);

        [FreeFunction("HierarchyBindings::GetNodeChildrenSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern int GetNodeChildrenSpan(in HierarchyNode node, Span<HierarchyNode> outChildren);

        [FreeFunction("HierarchyBindings::EnumerateChildrenPtr", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern IntPtr EnumerateChildrenPtr(in HierarchyNode node);

        [FreeFunction("HierarchyBindings::GetOrCreateProperty", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern HierarchyPropertyId GetOrCreateProperty(string name, in HierarchyPropertyDescriptor descriptor);

        [FreeFunction("HierarchyBindings::SetPropertyRaw", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern unsafe void SetPropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, void* ptr, int size);

        [FreeFunction("HierarchyBindings::GetPropertyRaw", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern unsafe void* GetPropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, out int size);

        [FreeFunction("HierarchyBindings::SetPropertyString", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern void SetPropertyString(in HierarchyPropertyId property, in HierarchyNode node, string value);

        [FreeFunction("HierarchyBindings::GetPropertyString", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern string GetPropertyString(in HierarchyPropertyId property, in HierarchyNode node);

        [FreeFunction("HierarchyBindings::ClearProperty", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        internal extern void ClearProperty(in HierarchyPropertyId property, in HierarchyNode node);

        [FreeFunction("HierarchyBindings::ExistsSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool ExistsSpan(ReadOnlySpan<HierarchyNode> nodes, Span<bool> exists);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateHierarchy(IntPtr nativePtr, IntPtr rootPtr, IntPtr versionPtr) => GCHandle.ToIntPtr(GCHandle.Alloc(new Hierarchy(nativePtr, rootPtr, versionPtr)));

        [RequiredByNativeCode]
        static void InvokeHandlerCreated(IntPtr hierarchyPtr, IntPtr handlerPtr)
        {
            var hierarchy = FromIntPtr(hierarchyPtr);
            var handler = HierarchyNodeTypeHandlerBase.FromIntPtr(handlerPtr);
            hierarchy.HandlerCreated?.Invoke(handler);
        }
        #endregion

        #region Marked as obsolete error in 6.6
        /// <summary>
        /// Registers a hierarchy node type handler for this hierarchy.
        /// </summary>
        /// <remarks>
        /// If a hierarchy node type handler with that type is already registered, the same instance is returned.
        /// </remarks>
        /// <returns>The hierarchy node type handler instance for that type.</returns>
        [Obsolete("RegisterNodeTypeHandler has been renamed GetOrCreateNodeTypeHandler (UnityUpgradable) -> GetOrCreateNodeTypeHandler<T>()", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public T RegisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandlerBase => throw null;

        /// <summary>
        /// Removes a hierarchy node type handler from this hierarchy.
        /// </summary>
        [Obsolete("UnregisterNodeTypeHandler no longer has any effect and will be removed in a future release.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void UnregisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandlerBase => throw null;

        /// <summary>
        /// Gets the number of node type handlers that this hierarchy uses.
        /// </summary>
        /// <returns>Number of node type handlers.</returns>
        [Obsolete("GetAllNodeTypeHandlersBaseCount is obsolete, please use EnumerateNodeTypeHandlersBase instead.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int GetAllNodeTypeHandlersBaseCount() => throw null;

        /// <summary>
        /// Gets all the node type handlers that this hierarchy uses.
        /// </summary>
        /// <param name="handlers">The list of node type handlers to populate.</param>
        [Obsolete("GetAllNodeTypeHandlersBase is obsolete, please use EnumerateNodeTypeHandlersBase instead.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetAllNodeTypeHandlersBase(List<HierarchyNodeTypeHandlerBase> handlers) => throw null;

        /// <summary>
        /// Sorts the child nodes of a hierarchy node according to their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="recurse">Whether to sort the child nodes recursively.</param>
        [Obsolete("SortChildren(node, recurse) with a bool parameter is obsolete, please use SortChildren(node) or SortChildrenRecursive(node) instead.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SortChildren(in HierarchyNode node, bool recurse) => throw null;
        #endregion

        #region Marked as obsolete warning in 6.6
        /// <summary>
        /// Gets the hash code for the specified hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The node hash code.</returns>
        [Obsolete("GetHashCode is no longer used by HierarchyViewModelState serialization. Use HierarchyNodeTypeHandlerBase.GetUIDInfo/WriteUIDs/ReadUIDs instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetHashCode(in HierarchyNode node);
        #endregion
    }
}
