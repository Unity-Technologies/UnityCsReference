// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represent a list of commands used to modify an hierarchy when executed.
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyCommandList.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyCommandListBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyCommandList : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyCommandList cmdList) => cmdList.m_Ptr;
        }

        [RequiredByNativeCode] IntPtr m_Ptr;
        [RequiredByNativeCode] readonly bool m_IsWrapper;

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [RequiredByNativeCode] internal readonly Hierarchy m_Hierarchy;

        [FreeFunction("HierarchyCommandListBindings::Create")]
        static extern IntPtr Internal_Create(Hierarchy hierarchy, int initialCapacity);

        [FreeFunction("HierarchyCommandListBindings::Destroy")]
        static extern void Internal_Destroy(IntPtr ptr);

        /// <summary>
        /// Whether or not this object is still valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The current size in bytes used by commands in the command list.
        /// </summary>
        public extern int Size { [NativeMethod("Size")] get; }

        /// <summary>
        /// The capacity in bytes for storing commands in the command list.
        /// </summary>
        public extern int Capacity { [NativeMethod("Capacity")] get; }

        /// <summary>
        /// Determine if the command list is empty.
        /// </summary>
        public extern bool IsEmpty { [NativeMethod("IsEmpty")] get; }

        /// <summary>
        /// Determine if the command list is currently executing.
        /// </summary>
        public extern bool IsExecuting { [NativeMethod("IsExecuting")] get; }

        /// <summary>
        /// Construct a new command list.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="initialCapacity">The initial capacity in bytes.</param>
        public HierarchyCommandList(Hierarchy hierarchy, int initialCapacity = 64 * 1024)
        {
            m_Ptr = Internal_Create(hierarchy, initialCapacity);
            m_IsWrapper = false;
            m_Hierarchy = hierarchy;
        }

        ~HierarchyCommandList()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose the command list, releasing its memory.
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
        /// Clear all the commands from the command list.
        /// </summary>
        public extern void Clear();

        /// <summary>
        /// Pre-allocate memory if necessary, in anticipation of adding nodes.
        /// </summary>
        /// <param name="count">The number of nodes that are going to be added.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Reserve(int count);

        /// <summary>
        /// Add a new node to the hierarchy, with <see cref="Hierarchy.Root"/> as the parent.
        /// </summary>
        /// <param name="node">The new node if command is successful.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(out HierarchyNode node) => AddNode(out node);

        /// <summary>
        /// Add a new node to the hierarchy, with the specified parent node.
        /// </summary>
        /// <param name="parent">The hierarchy node parent.</param>
        /// <param name="node">The new node if command is successful.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(in HierarchyNode parent, out HierarchyNode node) => AddNodeWithParent(in parent, out node);

        /// <summary>
        /// Add multiple new nodes to the hierarchy, with <see cref="Hierarchy.Root"/> as parent.
        /// </summary>
        /// <param name="count">The number of node to create.</param>
        /// <param name="nodes">The new nodes if command is successful.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(int count, out HierarchyNode[] nodes)
        {
            nodes = new HierarchyNode[count];
            return AddNodeSpan(nodes);
        }

        /// <summary>
        /// Add multiple new nodes to the hierarchy, with the specified parent node.
        /// </summary>
        /// <param name="count">The number of node to create.</param>
        /// <param name="parent">The hierarchy nodes parent.</param>
        /// <param name="nodes">The new nodes if command is successful.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(int count, in HierarchyNode parent, out HierarchyNode[] nodes)
        {
            nodes = new HierarchyNode[count];
            return AddNodeSpanWithParent(in parent, nodes);
        }

        /// <summary>
        /// Add multiple new nodes to the hierarchy, with <see cref="Hierarchy.Root"/> as parent.
        /// </summary>
        /// <param name="outNodes">Span of nodes filled with new nodes if command is successful.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(Span<HierarchyNode> outNodes) => AddNodeSpan(outNodes);

        /// <summary>
        /// Add multiple new nodes to the hierarchy, with the specified parent node.
        /// </summary>
        /// <param name="parent">The hierarchy nodes parent.</param>
        /// <param name="outNodes">Span of nodes filled with new nodes if command is successful.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(in HierarchyNode parent, Span<HierarchyNode> outNodes) => AddNodeSpanWithParent(in parent, outNodes);

        /// <summary>
        /// Remove a node from the hierarchy.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Remove(in HierarchyNode node) => RemoveNode(in node);

        /// <summary>
        /// Remove multiple nodes from the hierarchy.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Remove(Span<HierarchyNode> nodes) => RemoveNodeSpan(nodes);

        /// <summary>
        /// Set the parent of an hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="parent">The hierarchy node parent.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool SetParent(in HierarchyNode node, in HierarchyNode parent) => SetNodeParent(in node, in parent);

        /// <summary>
        /// Set the parent of multiple hierarchy nodes.
        /// </summary>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="parent">The hierarchy nodes parent.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool SetParent(Span<HierarchyNode> nodes, in HierarchyNode parent) => SetNodeParentSpan(nodes, in parent);

        /// <summary>
        /// Set the sorting index of an hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="sortIndex">The sorting index.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SetSortIndex(in HierarchyNode node, int sortIndex);

        /// <summary>
        /// Sort the children of a hierarchy node by their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SortChildren(in HierarchyNode node);

        /// <summary>
        /// Set the value of an hierarchy node's property.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The property value.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool SetProperty<T>(in HierarchyPropertyUnmanaged<T> property, in HierarchyNode node, T value) where T : unmanaged
        {
            unsafe
            {
                return SetNodePropertyRaw(in property.m_Property, in node, &value, sizeof(T));
            }
        }

        /// <summary>
        /// Set the value of an hierarchy node's property.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The property value.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool SetProperty(in HierarchyPropertyString property, in HierarchyNode node, string value) => SetNodePropertyString(in property.m_Property, in node, value);

        /// <summary>
        /// Clear the property value for the specified hierarchy node.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The hierarchy property.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool ClearProperty<T>(in HierarchyPropertyUnmanaged<T> property, in HierarchyNode node) where T : unmanaged => ClearNodeProperty(in property.m_Property, in node);

        /// <summary>
        /// Clear the property value for the specified hierarchy node.
        /// </summary>
        /// <param name="property">The hierarchy property.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        public bool ClearProperty(in HierarchyPropertyString property, in HierarchyNode node) => ClearNodeProperty(in property.m_Property, in node);

        /// <summary>
        /// Set the name of an hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="name">The node's name.</param>
        /// <returns><see langword="true"/> if the command was successfully appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SetName(in HierarchyNode node, string name);

        /// <summary>
        /// Execute all the commands of the hierarchy command list.
        /// </summary>
        [NativeThrows]
        public extern void Execute();

        /// <summary>
        /// Execute one command of the hierarchy command list.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the execution, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool ExecuteIncremental();

        /// <summary>
        /// Execute commands of the hierarchy command list until the time limit is reached.
        /// </summary>
        /// <param name="milliseconds">Time limit in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the execution, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool ExecuteIncrementalTimed(double milliseconds);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::AddNode", HasExplicitThis = true)]
        extern bool AddNode(out HierarchyNode node);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::AddNodeWithParent", HasExplicitThis = true)]
        extern bool AddNodeWithParent(in HierarchyNode parent, out HierarchyNode node);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::AddNodeSpan", HasExplicitThis = true)]
        extern bool AddNodeSpan(Span<HierarchyNode> nodes);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::AddNodeSpanWithParent", HasExplicitThis = true)]
        extern bool AddNodeSpanWithParent(in HierarchyNode parent, Span<HierarchyNode> nodes);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::RemoveNode", HasExplicitThis = true)]
        extern bool RemoveNode(in HierarchyNode node);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::RemoveNodeSpan", HasExplicitThis = true)]
        extern bool RemoveNodeSpan(Span<HierarchyNode> nodes);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::SetNodeParent", HasExplicitThis = true)]
        extern bool SetNodeParent(in HierarchyNode node, in HierarchyNode parent);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::SetNodeParentSpan", HasExplicitThis = true)]
        extern bool SetNodeParentSpan(Span<HierarchyNode> nodes, in HierarchyNode parent);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::SetNodePropertyRaw", HasExplicitThis = true)]
        extern unsafe bool SetNodePropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, void* ptr, int size);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::SetNodePropertyString", HasExplicitThis = true)]
        extern bool SetNodePropertyString(in HierarchyPropertyId property, in HierarchyNode node, string value);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::ClearNodeProperty", HasExplicitThis = true)]
        extern bool ClearNodeProperty(in HierarchyPropertyId property, in HierarchyNode node);
    }
}
