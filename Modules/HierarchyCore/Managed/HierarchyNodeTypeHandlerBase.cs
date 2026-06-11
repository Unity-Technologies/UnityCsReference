// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides a base class for hierarchy node type handlers.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyNodeTypeHandlerBase.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyNodeTypeHandlerBaseBindings.h")]
    [RequiredByNativeCode, StructLayout(LayoutKind.Sequential)]
    public abstract class HierarchyNodeTypeHandlerBase
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(HierarchyNodeTypeHandlerBase handler) => handler.m_Ptr;
        }

        /// <summary>
        /// Struct used to temporarily store the constructor parameters. Main purpose is to avoid
        /// passing them as parameters to the constructor, preserving the default constructor signature.
        /// </summary>
        struct ConstructorScope : IDisposable
        {
            // those are set and cleaned up in using blocks boundaries
            [NoAutoStaticsCleanup]
            [ThreadStatic] static IntPtr m_Ptr;
            [NoAutoStaticsCleanup]
            [ThreadStatic] static Hierarchy m_Hierarchy;
            [NoAutoStaticsCleanup]
            [ThreadStatic] static HierarchyCommandList m_CommandList;

            public static IntPtr Ptr { get => m_Ptr; private set => m_Ptr = value; }
            public static Hierarchy Hierarchy { get => m_Hierarchy; private set => m_Hierarchy = value; }
            public static HierarchyCommandList CommandList { get => m_CommandList; private set => m_CommandList = value; }

            public ConstructorScope(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList)
            {
                Ptr = nativePtr;
                Hierarchy = hierarchy;
                CommandList = cmdList;
            }

            public void Dispose()
            {
                Ptr = IntPtr.Zero;
                Hierarchy = null;
                CommandList = null;
            }
        }

        internal readonly IntPtr m_Ptr;
        readonly Hierarchy m_Hierarchy;
        readonly HierarchyCommandList m_CommandList;

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<Type, int> s_NodeTypes = new();

        /// <summary>
        /// Get the <see cref="Unity.Hierarchy.Hierarchy"/> owning this handler.
        /// </summary>
        public Hierarchy Hierarchy => m_Hierarchy;

        /// <summary>
        /// Get the <see cref="HierarchyCommandList"/> associated with this handler.
        /// </summary>
        protected HierarchyCommandList CommandList => m_CommandList;

        /// <summary>
        /// Constructs a new <see cref="HierarchyNodeTypeHandlerBase"/>.
        /// </summary>
        protected HierarchyNodeTypeHandlerBase()
        {
            // Note: this constructor is only used by handlers written in managed
            m_Ptr = ConstructorScope.Ptr;
            m_Hierarchy = ConstructorScope.Hierarchy;
            m_CommandList = ConstructorScope.CommandList;
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyNodeTypeHandlerBase"/> from a pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="cmdList">The command list.</param>
        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        internal HierarchyNodeTypeHandlerBase(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList)
        {
            // Note: this constructor is only used by handlers written in native
            m_Ptr = nativePtr;
            m_Hierarchy = hierarchy;
            m_CommandList = cmdList;
        }

        ~HierarchyNodeTypeHandlerBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initializes this hierarchy node type handler.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Disposes this hierarchy node type handler to free up resources in the derived class.
        /// </summary>
        /// <param name="disposing">Returns true if called from Dispose, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Retrieves the hierarchy node type for this hierarchy node type handler.
        /// </summary>
        /// <returns>The type of the hierarchy node.</returns>
        public HierarchyNodeType GetNodeType() => new HierarchyNodeType(GetNodeTypeFromType(GetType()));

        /// <summary>
        /// Get the type name of this hierarchy node type handler.
        /// </summary>
        /// <remarks>
        /// The returned type name is expected to never change during the lifetime of the handler.
        /// This is important for serialization and other purposes.
        /// </remarks>
        /// <returns>The type name of the hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern virtual string GetNodeTypeName();

        /// <summary>
        /// Get the default value used to initialize a hierarchy node flags.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="defaultFlags">The default hierarchy node flags.</param>
        /// <returns>The default flags of the hierarchy node.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern virtual HierarchyNodeFlags GetDefaultNodeFlags(in HierarchyNode node, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None);

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> corresponding to the given <see cref="EntityId"/>.
        /// </summary>
        /// <param name="entityId">The <see cref="EntityId"/> to look up.</param>
        /// <returns>The matching <see cref="HierarchyNode"/>, or <see cref="HierarchyNode.Null"/> if not found.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern virtual HierarchyNode GetNodeFromEntityId(EntityId entityId);

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> corresponding to each <see cref="EntityId"/> in <paramref name="entityIds"/>.
        /// Slots already set to a non-null value must be skipped.
        /// </summary>
        /// <param name="entityIds">The <see cref="EntityId"/> values to look up.</param>
        /// <param name="outNodes">Output buffer to fill; must be the same length as <paramref name="entityIds"/>.</param>
        /// <returns>The number of slots in <paramref name="outNodes"/> still set to <see cref="HierarchyNode.Null"/> after this handler runs.
        /// A return value of 0 means all entity ids were resolved; callers may skip subsequent handlers.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern virtual int GetNodesFromEntityIds(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes);

        /// <summary>
        /// Gets the <see cref="EntityId"/> corresponding to the given <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to look up.</param>
        /// <returns>The matching <see cref="EntityId"/>, or <see cref="EntityId.None"/> if not found.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public extern virtual EntityId GetEntityIdFromNode(in HierarchyNode node);

        /// <summary>
        /// Gets the <see cref="EntityId"/> corresponding to each <see cref="HierarchyNode"/> in <paramref name="nodes"/>.
        /// Slots already set to a non-<see cref="EntityId.None"/> value must be skipped.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/> values to look up.</param>
        /// <param name="outEntityIds">Output buffer to fill; must be the same length as <paramref name="nodes"/>.</param>
        /// <returns>The number of slots in <paramref name="outEntityIds"/> still set to <see cref="EntityId.None"/> after this handler runs.
        /// A return value of 0 means all nodes were resolved; callers may skip subsequent handlers.</returns>
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern virtual int GetEntityIdsFromNodes(ReadOnlySpan<HierarchyNode> nodes, Span<EntityId> outEntityIds);

        /// <summary>
        /// Called when a new search query begins.
        /// </summary>
        /// <param name="query">The search query descriptor.</param>
        protected virtual void SearchBegin(HierarchySearchQueryDescriptor query)
        {
        }

        /// <summary>
        /// Determines if a node matches the search query.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node matches the search query, <see langword="false"/> otherwise.</returns>
        protected virtual bool SearchMatch(in HierarchyNode node)
        {
            return false;
        }

        /// <summary>
        /// Called when a search query ends.
        /// </summary>
        protected virtual void SearchEnd()
        {
        }

        /// <summary>
        /// Called when the hierarchy update begins.
        /// </summary>
        protected virtual void UpdateBegin()
        {
        }

        /// <summary>
        /// Called when the hierarchy update ends.
        /// </summary>
        protected virtual void UpdateEnd()
        {
        }

        /// <summary>
        /// Called after the HierarchyViewModel finishes being updated.
        /// </summary>
        /// <param name="viewModel">The hierarchy view model.</param>
        protected virtual void ViewModelPostUpdate(HierarchyViewModel viewModel)
        {
        }

        /// <summary>
        /// Called after the <see cref="HierarchyViewModel"/> finishes setting its state.
        /// </summary>
        /// <param name="viewModel">The <see cref="HierarchyViewModel"/> for which the state was set.</param>
        protected virtual void ViewModelPostSetState(HierarchyViewModel viewModel)
        {
        }

        /// <summary>
        /// Returns the UID serialization info for this handler.
        /// A <see cref="HierarchyUIDInfo.Size"/> of 0 means this handler does not support UID serialization and its nodes will be skipped.
        /// </summary>
        /// <param name="info">The UID info containing the format version and per-node byte size.</param>
        protected virtual void GetUIDInfo(out HierarchyUIDInfo info) { info = default; }

        /// <summary>
        /// Serializes stable identifiers for the given nodes into <paramref name="outUIDs"/>.
        /// The buffer is pre-zeroed; write only slots that have a valid identity. Unwritten slots are treated as unresolvable during restore.
        /// </summary>
        /// <param name="nodes">Nodes to serialize, one per output slot.</param>
        /// <param name="outUIDs">Packed UID bytes, <c>GetUIDInfo().Size</c> bytes per node.</param>
        protected virtual void WriteUIDs(ReadOnlySpan<HierarchyNode> nodes, Span<byte> outUIDs) { }

        /// <summary>
        /// Restores nodes from the identifiers written by <see cref="WriteUIDs"/>.
        /// <paramref name="outNodes"/> is pre-initialized to <see cref="HierarchyNode.Null"/>; write only slots that resolve to a live node.
        /// </summary>
        /// <param name="info">UID format version and per-node byte size as stored; validate before reading.</param>
        /// <param name="uids">Packed UID bytes, <c>info.Size</c> bytes per node, as written by <see cref="WriteUIDs"/>.</param>
        /// <param name="outNodes">Resolved nodes, one per entry; leave as <see cref="HierarchyNode.Null"/> when a node cannot be found.</param>
        protected virtual void ReadUIDs(in HierarchyUIDInfo info, ReadOnlySpan<byte> uids, Span<HierarchyNode> outNodes) { }

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyNodeTypeHandlerBase FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyNodeTypeHandlerBase)GCHandle.FromIntPtr(handlePtr).Target : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Internal_SearchBegin(HierarchySearchQueryDescriptor query) => SearchBegin(query);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Internal_SearchMatch(in HierarchyNode node) => SearchMatch(in node);

        [FreeFunction("HierarchyNodeTypeHandlerManager::Get().GetNodeType", IsThreadSafe = true, ThrowsException = true)]
        static extern int GetNodeTypeFromType(Type type);

        #region Called from native
        [RequiredByNativeCode]
        static IntPtr CreateNodeTypeHandlerFromType(IntPtr nativePtr, Type handlerType, IntPtr hierarchyPtr, IntPtr cmdListPtr)
        {
            if (nativePtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(nativePtr));
            if (hierarchyPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hierarchyPtr));
            if (cmdListPtr == IntPtr.Zero)
                throw new ArgumentNullException(nameof(cmdListPtr));

            var hierarchy = Hierarchy.FromIntPtr(hierarchyPtr);
            var cmdList = HierarchyCommandList.FromIntPtr(cmdListPtr);
            using (var scope = new ConstructorScope(nativePtr, hierarchy, cmdList))
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var handler = (HierarchyNodeTypeHandlerBase)Activator.CreateInstance(handlerType, flags, null, null, null);
                if (handler == null)
                    return IntPtr.Zero;

                return GCHandle.ToIntPtr(GCHandle.Alloc(handler));
            }
        }

        [RequiredByNativeCode]
        static bool TryGetStaticNodeType(Type handlerType, out int nodeType)
        {
            if (s_NodeTypes.TryGetValue(handlerType, out nodeType))
                return true;

            var method = handlerType.GetMethod("GetStaticNodeType", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                nodeType = (int)method.Invoke(null, null);
                s_NodeTypes.Add(handlerType, nodeType);
                return true;
            }

            nodeType = HierarchyNodeType.k_HierarchyNodeTypeNull;
            return false;
        }

        [RequiredByNativeCode]
        static void InvokeInitialize(IntPtr handlePtr) => FromIntPtr(handlePtr).Initialize();

        [RequiredByNativeCode]
        static void InvokeDispose(IntPtr handlePtr)
        {
            var handler = FromIntPtr(handlePtr);
            handler.Dispose(true);
            GC.SuppressFinalize(handler);
        }

        [RequiredByNativeCode]
        static string InvokeGetNodeTypeName(IntPtr handlePtr) => FromIntPtr(handlePtr).GetNodeTypeName();

#pragma warning disable 618 // Remove this pragma once the corresponding public APIs below are removed
        [RequiredByNativeCode]
        static int InvokeGetNodeHashCode(IntPtr handlePtr, in HierarchyNode node) => FromIntPtr(handlePtr).GetNodeHashCode(in node);
#pragma warning restore 618

        [RequiredByNativeCode]
        static int InvokeGetDefaultNodeFlags(IntPtr handlePtr, in HierarchyNode node, HierarchyNodeFlags defaultFlags) => (int)FromIntPtr(handlePtr).GetDefaultNodeFlags(in node, defaultFlags);

        [RequiredByNativeCode]
        static bool InvokeSearchMatch(IntPtr handlePtr, in HierarchyNode node) => FromIntPtr(handlePtr).SearchMatch(in node);

        [RequiredByNativeCode]
        static void InvokeSearchEnd(IntPtr handlePtr) => FromIntPtr(handlePtr).SearchEnd();

        [RequiredByNativeCode]
        static void InvokeUpdateBegin(IntPtr handlePtr) => FromIntPtr(handlePtr).UpdateBegin();

        [RequiredByNativeCode]
        static void InvokeUpdateEnd(IntPtr handlePtr) => FromIntPtr(handlePtr).UpdateEnd();

        [RequiredByNativeCode]
        static void InvokeViewModelPostUpdate(IntPtr handlePtr, IntPtr viewModelPtr) => FromIntPtr(handlePtr).ViewModelPostUpdate(HierarchyViewModel.FromIntPtr(viewModelPtr));

        [RequiredByNativeCode]
        static void InvokeViewModelPostSetState(IntPtr handlePtr, IntPtr viewModelPtr) => FromIntPtr(handlePtr).ViewModelPostSetState(HierarchyViewModel.FromIntPtr(viewModelPtr));

        [RequiredByNativeCode]
        static void InvokeGetNodeFromEntityId(IntPtr handlePtr, in EntityId entityId, out HierarchyNode result)
            => result = FromIntPtr(handlePtr).GetNodeFromEntityId(entityId);

        [RequiredByNativeCode]
        static unsafe void InvokeGetNodesFromEntityIds(IntPtr handlePtr, IntPtr entityIds, int count, IntPtr outNodes, out int remaining)
            => remaining = FromIntPtr(handlePtr).GetNodesFromEntityIds(
                new ReadOnlySpan<EntityId>((void*)entityIds, count),
                new Span<HierarchyNode>((void*)outNodes, count));

        [RequiredByNativeCode]
        static void InvokeGetEntityIdFromNode(IntPtr handlePtr, in HierarchyNode node, out EntityId result)
            => result = FromIntPtr(handlePtr).GetEntityIdFromNode(in node);

        [RequiredByNativeCode]
        static unsafe void InvokeGetEntityIdsFromNodes(IntPtr handlePtr, IntPtr nodes, int count, IntPtr outEntityIds, out int remaining)
            => remaining = FromIntPtr(handlePtr).GetEntityIdsFromNodes(
                new ReadOnlySpan<HierarchyNode>((void*)nodes, count),
                new Span<EntityId>((void*)outEntityIds, count));

        [RequiredByNativeCode]
        static void InvokeGetUIDInfo(IntPtr handlePtr, out HierarchyUIDInfo info)
        {
            FromIntPtr(handlePtr).GetUIDInfo(out info);
        }

        [RequiredByNativeCode]
        static unsafe void InvokeWriteUIDs(IntPtr handlePtr, IntPtr nodes, int count, IntPtr outUIDs, in HierarchyUIDInfo info)
            => FromIntPtr(handlePtr).WriteUIDs(
                new ReadOnlySpan<HierarchyNode>((void*)nodes, count),
                new Span<byte>((void*)outUIDs, count * info.Size));

        [RequiredByNativeCode]
        static unsafe void InvokeReadUIDs(IntPtr handlePtr, IntPtr uids, in HierarchyUIDInfo info, IntPtr outNodes, int count)
            => FromIntPtr(handlePtr).ReadUIDs(
                in info,
                new ReadOnlySpan<byte>((void*)uids, count * info.Size),
                new Span<HierarchyNode>((void*)outNodes, count));
        #endregion

        #region Marked as obsolete error in 6.6
        /// <summary>
        /// Constructs a new <see cref="HierarchyNodeTypeHandlerBase"/>.
        /// </summary>
        [Obsolete("The constructor with a hierarchy parameter is obsolete and is no longer used. Remove the hierarchy parameter from your constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected HierarchyNodeTypeHandlerBase(Hierarchy hierarchy) => throw null;

        /// <summary>
        /// Disposes this hierarchy node type handler to free up resources.
        /// </summary>
        [Obsolete("The IDisposable interface is obsolete and no longer has any effect. Instances of handlers are owned and disposed by the hierarchy so they do not need to be disposed by user code.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Dispose() => throw null;

        /// <summary>
        /// Callback that determines if pending changes from a registered node type handler need to be applied to the hierarchy. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <returns><see langword="true"/> if changes are pending, <see langword="false"/> otherwise.</returns>
        [Obsolete("ChangesPending is obsolete, it is replaced by adding commands into the hierarchy node type handler's CommandList.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual bool ChangesPending() => throw null;

        /// <summary>
        /// Callback that determines if changes from an update need to be integrated into the hierarchy. `IntegrateChanges` is called after <see cref="ChangesPending"/> returns <see langword="true"/>. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <param name="cmdList">A hierarchy command list that can modify the hierarchy.</param>
        /// <returns><see langword="true"/> if more invocations are needed to complete integrating changes, and <see langword="false"/> if the handler is done integrating changes.</returns>
        [Obsolete("IntegrateChanges is obsolete, it is replaced by adding commands into the hierarchy node type handler's CommandList.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual bool IntegrateChanges(HierarchyCommandList cmdList) => throw null;
        #endregion

        #region Marked as obsolete warning in 6.6
        /// <summary>
        /// Gets the hash code for the specified hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The node hash code.</returns>
        [Obsolete("GetNodeHashCode is no longer used by HierarchyViewModelState serialization. Override GetUIDInfo/WriteUIDs/ReadUIDs instead.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern virtual int GetNodeHashCode(in HierarchyNode node);
        #endregion
    }
}
