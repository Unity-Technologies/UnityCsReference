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
    /// Represents a list of commands that modify a hierarchy.
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

        [FreeFunction("HierarchyCommandListBindings::BindScriptingObject", HasExplicitThis = true)]
        extern void Internal_BindScriptingObject([Unmarshalled] HierarchyCommandList self);

        /// <summary>
        /// Determines if this object is valid and uses memory.
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
        /// Determines if the command list is empty.
        /// </summary>
        public extern bool IsEmpty { [NativeMethod("IsEmpty")] get; }

        /// <summary>
        /// Determines if the command list is currently executing.
        /// </summary>
        public extern bool IsExecuting { [NativeMethod("IsExecuting")] get; }

        /// <summary>
        /// Creates a new command list.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="initialCapacity">The initial capacity in bytes.</param>
        public HierarchyCommandList(Hierarchy hierarchy, int initialCapacity = 64 * 1024)
        {
            m_Ptr = Internal_Create(hierarchy, initialCapacity);
            m_IsWrapper = false;
            m_Hierarchy = hierarchy;
            Internal_BindScriptingObject(this);
        }

        ~HierarchyCommandList()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the command list and releases its memory.
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
        /// Clears all commands from the command list.
        /// </summary>
        public extern void Clear();

        /// <summary>
        /// Reserves memory for nodes to use. Use this to avoid memory allocation hits when you add batches of nodes.
        /// </summary>
        /// <param name="count">The number of nodes to reserve memory for.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Reserve(int count);

        /// <summary>
        /// Adds a new node that has a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the new node.</param>
        /// <param name="node">The new node if the command succeeds.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(in HierarchyNode parent, out HierarchyNode node) => AddNode(in parent, out node);

        /// <summary>
        /// Adds multiple new nodes that have a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the new nodes.</param>
        /// <param name="count">The number of nodes to create.</param>
        /// <param name="nodes">The new nodes if the command succeeds.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(in HierarchyNode parent, int count, out HierarchyNode[] nodes)
        {
            nodes = new HierarchyNode[count];
            return AddNodeSpan(in parent, nodes);
        }

        /// <summary>
        /// Adds multiple new nodes that have a specified parent node to the hierarchy.
        /// </summary>
        /// <param name="parent">The parent of the new nodes.</param>
        /// <param name="outNodes">The span of nodes filled with new nodes if the command succeeds.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool Add(in HierarchyNode parent, Span<HierarchyNode> outNodes) => AddNodeSpan(in parent, outNodes);

        /// <summary>
        /// Removes a node from the hierarchy.
        /// </summary>
        /// <param name="node">The hierarchy node to remove.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Remove(in HierarchyNode node);

        /// <summary>
        /// Recursively removes all children of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool RemoveChildren(in HierarchyNode node);

        /// <summary>
        /// Sets the parent node of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to set a parent for.</param>
        /// <param name="parent">The hierarchy node to set as the parent node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SetParent(in HierarchyNode node, in HierarchyNode parent);

        /// <summary>
        /// Sets the sorting index for a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to set a sorting index for.</param>
        /// <param name="sortIndex">The sorting index.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SetSortIndex(in HierarchyNode node, int sortIndex);

        /// <summary>
        /// Sorts the child nodes of a hierarchy node by their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node with child nodes to sort by their index.</param>
        /// <param name="recurse">Whether to sort the child nodes recursively.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SortChildren(in HierarchyNode node, bool recurse = false);

        /// <summary>
        /// Sets a value for a property of a hierarchy node.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The property value.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool SetProperty<T>(in HierarchyPropertyUnmanaged<T> property, in HierarchyNode node, T value) where T : unmanaged
        {
            unsafe
            {
                return SetNodePropertyRaw(in property.m_Property, in node, &value, sizeof(T));
            }
        }

        /// <summary>
        /// Sets a value for a property of a hierarchy node
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The property value.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool SetProperty(in HierarchyPropertyString property, in HierarchyNode node, string value) => SetNodePropertyString(in property.m_Property, in node, value);

        /// <summary>
        /// Clears a property value for the specified hierarchy node.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The hierarchy property.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool ClearProperty<T>(in HierarchyPropertyUnmanaged<T> property, in HierarchyNode node) where T : unmanaged => ClearNodeProperty(in property.m_Property, in node);

        /// <summary>
        /// Clears a property value for the specified hierarchy node.
        /// </summary>
        /// <param name="property">The hierarchy property.</param>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        public bool ClearProperty(in HierarchyPropertyString property, in HierarchyNode node) => ClearNodeProperty(in property.m_Property, in node);

        /// <summary>
        /// Sets a name for a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="name">The name of the node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool SetName(in HierarchyNode node, string name);

        /// <summary>
        /// Executes all the commands in the hierarchy command list.
        /// </summary>
        [NativeThrows]
        public extern void Execute();

        /// <summary>
        /// Executes one command from the hierarchy command list.
        /// </summary>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool ExecuteIncremental();

        /// <summary>
        /// Executes commands from the hierarchy command list until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time limit in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the execution, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool ExecuteIncrementalTimed(double milliseconds);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::AddNode", HasExplicitThis = true)]
        extern bool AddNode(in HierarchyNode parent, out HierarchyNode node);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::AddNodeSpan", HasExplicitThis = true)]
        extern bool AddNodeSpan(in HierarchyNode parent, Span<HierarchyNode> outNodes);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::SetNodePropertyRaw", HasExplicitThis = true)]
        extern unsafe bool SetNodePropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, void* ptr, int size);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::SetNodePropertyString", HasExplicitThis = true)]
        extern bool SetNodePropertyString(in HierarchyPropertyId property, in HierarchyNode node, string value);

        [NativeThrows, FreeFunction("HierarchyCommandListBindings::ClearNodeProperty", HasExplicitThis = true)]
        extern bool ClearNodeProperty(in HierarchyPropertyId property, in HierarchyNode node);
    }
}
