// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a list of commands that modify a hierarchy.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyCommandList.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyCommandListBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyCommandList : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyCommandList cmdList) => cmdList.m_Ptr;
        }

        IntPtr m_Ptr;
        readonly bool m_IsOwner;

        /// <summary>
        /// Determines if this object is valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The current size in bytes used by commands in the command list.
        /// </summary>
        public extern int Size { [NativeMethod("Size", IsThreadSafe = true)] get; }

        /// <summary>
        /// The capacity in bytes for storing commands in the command list.
        /// </summary>
        public extern int Capacity { [NativeMethod("Capacity", IsThreadSafe = true)] get; }

        /// <summary>
        /// Determines if the command list is empty.
        /// </summary>
        public extern bool IsEmpty { [NativeMethod("IsEmpty", IsThreadSafe = true)] get; }

        /// <summary>
        /// Determines if the command list is currently executing.
        /// </summary>
        public extern bool IsExecuting { [NativeMethod("IsExecuting", IsThreadSafe = true)] get; }

        /// <summary>
        /// Constructs a new <see cref="HierarchyCommandList"/>.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="initialCapacity">The initial required capacity in bytes.</param>
        public HierarchyCommandList(Hierarchy hierarchy, int initialCapacity = 64 * 1024) : this(hierarchy, HierarchyNodeType.Null, initialCapacity)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyCommandList"/>.
        /// </summary>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="nodeType">The hierarchy node type.</param>
        /// <param name="initialCapacity">The initial required capacity in bytes.</param>
        internal HierarchyCommandList(Hierarchy hierarchy, HierarchyNodeType nodeType, int initialCapacity = 64 * 1024)
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)), hierarchy, nodeType, initialCapacity);
            m_IsOwner = true;
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyCommandList"/> from a native pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        HierarchyCommandList(IntPtr nativePtr)
        {
            m_Ptr = nativePtr;
            m_IsOwner = false;
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
                if (m_IsOwner)
                    Destroy(m_Ptr);

                m_Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Clears all commands from the command list.
        /// </summary>
        [NativeMethod(IsThreadSafe = true)]
        public extern void Clear();

        /// <summary>
        /// Reserves memory for nodes to use. Use this to avoid memory allocation hits when you add batches of nodes.
        /// </summary>
        /// <param name="count">The number of nodes to reserve memory for.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
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
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool Remove(in HierarchyNode node);

        /// <summary>
        /// Recursively removes all children of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool RemoveChildren(in HierarchyNode node);

        /// <summary>
        /// Sets the parent node of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to set a parent for.</param>
        /// <param name="parent">The hierarchy node to set as the parent node.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool SetParent(in HierarchyNode node, in HierarchyNode parent);

        /// <summary>
        /// Sets the sorting index for a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node to set a sorting index for.</param>
        /// <param name="sortIndex">The sorting index.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool SetSortIndex(in HierarchyNode node, int sortIndex);

        /// <summary>
        /// Sorts the child nodes of a hierarchy node by their sort index.
        /// </summary>
        /// <param name="node">The hierarchy node with child nodes to sort by their index.</param>
        /// <param name="recurse">Whether to sort the child nodes recursively.</param>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool SortChildren(in HierarchyNode node, bool recurse = false);

        /// <summary>
        /// Sets a value for a property of a hierarchy node.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
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
        /// <param name="property">The property.</param>
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
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool SetName(in HierarchyNode node, string name);

        /// <summary>
        /// Executes all the commands in the hierarchy command list.
        /// </summary>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void Execute();

        /// <summary>
        /// Executes one command from the hierarchy command list.
        /// </summary>
        /// <returns><see langword="true"/> if the command was appended to the list, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool ExecuteIncremental();

        /// <summary>
        /// Executes commands from the hierarchy command list until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time limit in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the execution, <see langword="false"/> otherwise.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool ExecuteIncrementalTimed(double milliseconds);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyCommandList FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyCommandList)GCHandle.FromIntPtr(handlePtr).Target : null;

        [FreeFunction("HierarchyCommandListBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr, Hierarchy hierarchy, HierarchyNodeType nodeType, int initialCapacity);

        [FreeFunction("HierarchyCommandListBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [FreeFunction("HierarchyCommandListBindings::AddNode", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool AddNode(in HierarchyNode parent, out HierarchyNode node);

        [FreeFunction("HierarchyCommandListBindings::AddNodeSpan", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool AddNodeSpan(in HierarchyNode parent, Span<HierarchyNode> outNodes);

        [FreeFunction("HierarchyCommandListBindings::SetNodePropertyRaw", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern unsafe bool SetNodePropertyRaw(in HierarchyPropertyId property, in HierarchyNode node, void* ptr, int size);

        [FreeFunction("HierarchyCommandListBindings::SetNodePropertyString", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool SetNodePropertyString(in HierarchyPropertyId property, in HierarchyNode node, string value);

        [FreeFunction("HierarchyCommandListBindings::ClearNodeProperty", HasExplicitThis = true, IsThreadSafe = true, ThrowsException = true)]
        extern bool ClearNodeProperty(in HierarchyPropertyId property, in HierarchyNode node);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateCommandList(IntPtr nativePtr) => GCHandle.ToIntPtr(GCHandle.Alloc(new HierarchyCommandList(nativePtr)));
        #endregion
    }
}
