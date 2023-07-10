// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a tree-like container of nodes.
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/Hierarchy.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyBindings.h")]
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyNodeTypeHandlerBase.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class Hierarchy : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(Hierarchy hierarchy) => hierarchy.m_Ptr;
        }

        class ExcludeFromBindings
        {
            internal HierarchyPropertyString m_UssItemClassListProperty;
        }

        [RequiredByNativeCode] IntPtr m_Ptr;

#pragma warning disable CS0649
        [RequiredByNativeCode] readonly bool m_IsWrapper;
#pragma warning restore CS0649

        ExcludeFromBindings m_State;

        [FreeFunction("HierarchyBindings::Create")]
        static extern IntPtr Internal_Create();

        [FreeFunction("HierarchyBindings::Destroy")]
        static extern void Internal_Destroy(IntPtr ptr);

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
        [NativeProperty("Root", TargetType.Field)]
        public extern HierarchyNode Root { get; }

        /// <summary>
        /// The total number of nodes.
        /// </summary>
        /// <remarks>
        /// This total count does not include the <see cref="Root"/> node.
        /// </remarks>
        public extern int Count { [NativeMethod("Count")] get; }

        /// <summary>
        /// Whether the hierarchy is currently updating.
        /// </summary>
        /// <remarks>
        /// Updating happens during the use of <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/>.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating")] get; }

        /// <summary>
        /// Whether the hierarchy requires an update.
        /// </summary>
        /// <remarks>
        /// An update is required when changes in registered hierarchy node handlers are pending.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded")] get; }

        internal HierarchyPropertyString UssItemClassListProperty
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            get
            {
                if (!State.m_UssItemClassListProperty.IsCreated)
                    State.m_UssItemClassListProperty = HierarchyProperties.GetItemClassListProperty(this);

                return State.m_UssItemClassListProperty;
            }
        }

        ExcludeFromBindings State => m_State ??= new ExcludeFromBindings();

        /// <summary>
        /// Constructs a new <see cref="Hierarchy"/>.
        /// </summary>
        public Hierarchy()
        {
            m_Ptr = Internal_Create();
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
                if (!m_IsWrapper)
                    Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Registers a hierarchy node type handler for this hierarchy.
        /// </summary>
        public void RegisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandlerBase => RegisterNodeTypeHandler(typeof(T));

        /// <summary>
        /// Removes a hierarchy node type handler from this hierarchy.
        /// </summary>
        public void UnregisterNodeTypeHandler<T>() where T : HierarchyNodeTypeHandlerBase => UnregisterNodeTypeHandler(typeof(T));

        /// <summary>
        /// Gets a hierarchy node type handler instance from this hierarchy.
        /// </summary>
        /// <returns>The hierarchy node type handler.</returns>
        public T GetNodeTypeHandlerBase<T>() where T : HierarchyNodeTypeHandlerBase => (T)GetNodeTypeHandlerFromType(typeof(T));

        /// <summary>
        /// Gets the node type handler instance for the specified node from this hierarchy.
        /// </summary>
        /// <returns>The hierarchy node type handler.</returns>
        public HierarchyNodeTypeHandlerBase GetNodeTypeHandlerBase(in HierarchyNode node) => GetNodeTypeHandlerFromNode(in node);

        /// <summary>
        /// Gets the node type handler instance for the specified node type name from this hierarchy.
        /// </summary>
        /// <param name="nodeTypeName">The node type name.</param>
        /// <returns>The hierarchy node type handler.</returns>
        public HierarchyNodeTypeHandlerBase GetNodeTypeHandlerBase(string nodeTypeName) => GetNodeTypeHandlerFromName(nodeTypeName);

        /// <summary>
        /// Gets all the node type handlers that this hierarchy uses.
        /// </summary>
        /// <param name="handlers">The list of node type handlers to populate.</param>
        [FreeFunction("HierarchyBindings::GetAllNodeTypeHandlersBase", HasExplicitThis = true)]
        public extern void GetAllNodeTypeHandlersBase(List<HierarchyNodeTypeHandlerBase> handlers);

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
        public extern HierarchyNodeType GetNodeType(in HierarchyNode node);

        /// <summary>
        /// Reserves memory for nodes to use. Use this to avoid memory allocation hits when you add batches of nodes.    
        /// </summary>
        /// <param name="count">The number of nodes to reserve memory for.</param>
        [NativeThrows]
        public extern void Reserve(int count);

        /// <summary>
        /// Determines whether a node exists or not.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node exists, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Exists(in HierarchyNode node);

        /// <summary>
        /// Gets the next sibling of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The next sibling of the hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Determines the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node. A value of -1 indicates the root node. A value of 0 indicates direct child nodes of the root node. A value of 1 indicates child nodes of the root node's direct children, and then their children have a value of 2 and so on. </returns>
        [NativeThrows]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Adds a new node that has <see cref="Root"/> as its parent to the hierarchy.
        /// </summary>
        /// <returns>A hierarchy node.</returns>
        public HierarchyNode Add() => AddNode(Root);

        /// <summary>
        /// Adds a new node that has a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the hierarchy node to add.</param>
        /// <returns>A hierarchy node.</returns>
        public HierarchyNode Add(in HierarchyNode parent) => AddNode(in parent);

        /// <summary>
        /// Adds multiple nodes that have <see cref="Root"/> as their parent to the hierarchy.
        /// </summary>
        /// <param name="count">The number of nodes to create.</param>
        /// <returns>An array of hierarchy nodes.</returns>
        public HierarchyNode[] Add(int count) => Add(count, Root);

        /// <summary>
        /// Adds multiple new nodes that have a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="count">The number of nodes to create.</param>
        /// <param name="parent">The parent of the hierarchy nodes.</param>
        /// <returns>An array of hierarchy nodes.</returns>
        public HierarchyNode[] Add(int count, in HierarchyNode parent)
        {
            var nodes = new HierarchyNode[count];
            AddNodeSpan(in parent, nodes);
            return nodes;
        }

        /// <summary>
        /// Adds multiple nodes that have <see cref="Root"/> as their parent to the hierarchy.
        /// </summary>
        /// <param name="outNodes">The span of nodes to fill with new nodes.</param>
        public void Add(Span<HierarchyNode> outNodes) => AddNodeSpan(Root, outNodes);

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
        public bool Remove(in HierarchyNode node) => RemoveNode(in node);

        /// <summary>
        /// Removes multiple nodes from the hierarchy.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes to remove from the hierarchy.</param>
        /// <returns>The number of removed nodes.</returns>
        public int Remove(Span<HierarchyNode> nodes) => RemoveNodeSpan(nodes);

        /// <summary>
        /// Removes all nodes from the hierarchy.
        /// </summary>
        public extern void Clear();

        /// <summary>
        /// Sets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="parent">The hierarchy node to set as a parent.</param>
        public void SetParent(in HierarchyNode node, in HierarchyNode parent) => SetNodeParent(in node, in parent);

        /// <summary>
        /// Sets the parent of multiple hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="parent">The hierarchy node to set as a parent.</param>
        public void SetParent(Span<HierarchyNode> nodes, in HierarchyNode parent) => SetNodeParentSpan(nodes, in parent);

        /// <summary>
        /// Gets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Gets the child nodes of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>An array of hierarchy nodes.</returns>
        [NativeThrows]
        public extern HierarchyNode[] GetChildren(in HierarchyNode node);

        /// <summary>
        /// Gets the child nodes of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="children">The span of nodes to fill with child nodes.</param>
        public void GetChildren(in HierarchyNode node, Span<HierarchyNode> children) => GetNodeChildrenSpan(in node, children);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of child nodes.</returns>
        [NativeThrows]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Sets the sorting index of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="sortIndex">The sorting index.</param>
        [NativeThrows]
        public extern void SetSortIndex(in HierarchyNode node, int sortIndex);

        /// <summary>
        /// Gets the sorting index of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The sorting index.</returns>
        [NativeThrows]
        public extern int GetSortIndex(in HierarchyNode node);

        /// <summary>
        /// Sorts the child nodes of a hierarchy node according to their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        [NativeThrows]
        public extern void SortChildren(in HierarchyNode node);

        /// <summary>
        /// Creates an unmanaged property with a specified name.
        /// </summary>
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
        /// <param name="node">The hierarchy node.</param>
        /// <param name="name">The name of the node.</param>
        [NativeThrows]
        public extern void SetName(in HierarchyNode node, string name);

        /// <summary>
        /// Gets the name of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The name of the node.</returns>
        [NativeThrows]
        public extern string GetName(in HierarchyNode node);

        /// <summary>
        /// Updates the hierarchy and requests that every registered hierarchy node type handler integrates their changes into the hierarchy.
        /// </summary>
        public extern void Update();

        /// <summary>
        /// Updates the hierarchy incrementally.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncremental();

        /// <summary>
        /// Incrementally updates the hierarchy until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time period in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncrementalTimed(double milliseconds);

        [NativeThrows, FreeFunction("HierarchyBindings::RegisterNodeTypeHandler", HasExplicitThis = true)]
        extern void RegisterNodeTypeHandler(Type type);

        [FreeFunction("HierarchyBindings::UnregisterNodeTypeHandler", HasExplicitThis = true)]
        extern void UnregisterNodeTypeHandler(Type type);

        [return: Unmarshalled]
        [FreeFunction("HierarchyBindings::GetNodeTypeHandlerFromType", HasExplicitThis = true)]
        extern HierarchyNodeTypeHandlerBase GetNodeTypeHandlerFromType(Type type);

        [return: Unmarshalled]
        [NativeThrows, FreeFunction("HierarchyBindings::GetNodeTypeHandlerFromNode", HasExplicitThis = true)]
        extern HierarchyNodeTypeHandlerBase GetNodeTypeHandlerFromNode(in HierarchyNode node);

        [return: Unmarshalled]
        [NativeThrows, FreeFunction("HierarchyBindings::GetNodeTypeHandlerFromName", HasExplicitThis = true)]
        extern HierarchyNodeTypeHandlerBase GetNodeTypeHandlerFromName(string nodeTypeName);

        [FreeFunction("HierarchyBindings::GetNodeTypeFromType", HasExplicitThis = true)]
        extern HierarchyNodeType GetNodeTypeFromType(Type type);

        [NativeThrows, FreeFunction("HierarchyBindings::AddNode", HasExplicitThis = true)]
        extern HierarchyNode AddNode(in HierarchyNode parent);

        [NativeThrows, FreeFunction("HierarchyBindings::AddNodeSpan", HasExplicitThis = true)]
        extern void AddNodeSpan(in HierarchyNode parent, Span<HierarchyNode> nodes);

        [NativeThrows, FreeFunction("HierarchyBindings::RemoveNode", HasExplicitThis = true)]
        extern bool RemoveNode(in HierarchyNode node);

        [NativeThrows, FreeFunction("HierarchyBindings::RemoveNodeSpan", HasExplicitThis = true)]
        extern int RemoveNodeSpan(Span<HierarchyNode> nodes);

        [NativeThrows, FreeFunction("HierarchyBindings::SetNodeParent", HasExplicitThis = true)]
        extern void SetNodeParent(in HierarchyNode node, in HierarchyNode parent);

        [NativeThrows, FreeFunction("HierarchyBindings::SetNodeParentSpan", HasExplicitThis = true)]
        extern void SetNodeParentSpan(Span<HierarchyNode> nodes, in HierarchyNode parent);

        [NativeThrows, FreeFunction("HierarchyBindings::GetNodeChildrenSpan", HasExplicitThis = true)]
        extern void GetNodeChildrenSpan(in HierarchyNode node, Span<HierarchyNode> children);

        [NativeThrows, FreeFunction("HierarchyBindings::GetOrCreateProperty", HasExplicitThis = true)]
        extern HierarchyPropertyId GetOrCreateProperty(string name, in HierarchyPropertyDescriptor descriptor);

        [NativeThrows, FreeFunction("HierarchyBindings::SetPropertyRaw", HasExplicitThis = true)]
        internal extern unsafe void SetPropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, void* ptr, int size);

        [NativeThrows, FreeFunction("HierarchyBindings::GetPropertyRaw", HasExplicitThis = true)]
        internal extern unsafe void* GetPropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, out int size);

        [NativeThrows, FreeFunction("HierarchyBindings::SetPropertyString", HasExplicitThis = true)]
        internal extern void SetPropertyString(in HierarchyPropertyId property, in HierarchyNode node, string value);

        [NativeThrows, FreeFunction("HierarchyBindings::GetPropertyString", HasExplicitThis = true)]
        internal extern string GetPropertyString(in HierarchyPropertyId property, in HierarchyNode node);

        [NativeThrows, FreeFunction("HierarchyBindings::ClearProperty", HasExplicitThis = true)]
        internal extern void ClearProperty(in HierarchyPropertyId property, in HierarchyNode node);
    }
}
