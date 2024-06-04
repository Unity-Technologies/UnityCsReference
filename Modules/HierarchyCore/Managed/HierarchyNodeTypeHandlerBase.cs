// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides a base class for hierarchy node type handlers.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyNodeTypeHandlerBase.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyNodeTypeHandlerBaseBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public abstract class HierarchyNodeTypeHandlerBase : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyNodeTypeHandlerBase handler) => handler.m_Ptr;
        }

        /// <summary>
        /// Struct used to temporarily store the constructor parameters. Main purpose is to avoid
        /// passing them as parameters to the constructor, preserving the default constructor signature.
        /// </summary>
        struct ConstructorScope : IDisposable
        {
            [ThreadStatic] static IntPtr m_Ptr;
            [ThreadStatic] static Hierarchy m_Hierarchy;
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
        /// Called when a new search query begins.
        /// </summary>
        /// <param name="query">The search query descriptor.</param>
        [FreeFunction("HierarchyNodeTypeHandlerBaseBindings::SearchBegin", HasExplicitThis = true, IsThreadSafe = true)]
        protected extern virtual void SearchBegin(HierarchySearchQueryDescriptor query);

        /// <summary>
        /// Determines if a node matches the search query.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node matches the search query, <see langword="false"/> otherwise.</returns>
        [FreeFunction("HierarchyNodeTypeHandlerBaseBindings::SearchMatch", HasExplicitThis = true, IsThreadSafe = true)]
        protected extern virtual bool SearchMatch(in HierarchyNode node);

        /// <summary>
        /// Called when a search query ends.
        /// </summary>
        [FreeFunction("HierarchyNodeTypeHandlerBaseBindings::SearchEnd", HasExplicitThis = true, IsThreadSafe = true)]
        protected extern virtual void SearchEnd();

        [VisibleToOtherModules("UnityEngine.HierarchyModule")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static HierarchyNodeTypeHandlerBase FromIntPtr(IntPtr handlePtr) => handlePtr != IntPtr.Zero ? (HierarchyNodeTypeHandlerBase)GCHandle.FromIntPtr(handlePtr).Target : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Internal_SearchBegin(HierarchySearchQueryDescriptor query) => SearchBegin(query);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Internal_SearchMatch(in HierarchyNode node) => SearchMatch(in node);

        [FreeFunction("HierarchyNodeTypeHandlerManager::Get().GetNodeType", IsThreadSafe = true, ThrowsException = true)]
        static extern int GetNodeTypeFromType(Type type);

        #region Called From Native
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

                handler.Initialize();
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

        [RequiredByNativeCode]
        static HierarchyNodeFlags InvokeGetDefaultNodeFlags(IntPtr handlePtr, in HierarchyNode node, HierarchyNodeFlags defaultFlags) => FromIntPtr(handlePtr).GetDefaultNodeFlags(in node, defaultFlags);

#pragma warning disable 618 // Remove this pragma once the corresponding public APIs below are removed
        [RequiredByNativeCode]
        static bool InvokeChangesPending(IntPtr handlePtr) => FromIntPtr(handlePtr).ChangesPending();

        [RequiredByNativeCode]
        static bool InvokeIntegrateChanges(IntPtr handlePtr, IntPtr cmdListPtr) => FromIntPtr(handlePtr).IntegrateChanges(HierarchyCommandList.FromIntPtr(cmdListPtr));
#pragma warning restore 618

        [RequiredByNativeCode]
        static bool InvokeSearchMatch(IntPtr handlePtr, in HierarchyNode node) => FromIntPtr(handlePtr).SearchMatch(in node);

        [RequiredByNativeCode]
        static void InvokeSearchEnd(IntPtr handlePtr) => FromIntPtr(handlePtr).SearchEnd();
        #endregion

        #region Obsolete public APIs to remove in 2024
        /// <summary>
        /// Constructs a new <see cref="HierarchyNodeTypeHandlerBase"/>.
        /// </summary>
        [Obsolete("The constructor with a hierarchy parameter is obsolete and is no longer used. Remove the hierarchy parameter from your constructor.")]
        protected HierarchyNodeTypeHandlerBase(Hierarchy hierarchy) : this() { }

        /// <summary>
        /// Disposes this hierarchy node type handler to free up resources.
        /// </summary>
        [Obsolete("The IDisposable interface is obsolete and no longer has any effect. Instances of handlers are owned and disposed by the hierarchy so they do not need to be disposed by user code.")]
        public void Dispose() { }

        /// <summary>
        /// Callback that determines if pending changes from a registered node type handler need to be applied to the hierarchy. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <returns><see langword="true"/> if changes are pending, <see langword="false"/> otherwise.</returns>
        [Obsolete("ChangesPending is obsolete, it is replaced by adding commands into the hierarchy node type handler's CommandList.", false)]
        [FreeFunction("HierarchyNodeTypeHandlerBaseBindings::ChangesPending", HasExplicitThis = true, IsThreadSafe = true)]
        protected extern virtual bool ChangesPending();

        /// <summary>
        /// Callback that determines if changes from an update need to be integrated into the hierarchy. `IntegrateChanges` is called after <see cref="ChangesPending"/> returns <see langword="true"/>. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <param name="cmdList">A hierarchy command list that can modify the hierarchy.</param>
        /// <returns><see langword="true"/> if more invocations are needed to complete integrating changes, and <see langword="false"/> if the handler is done integrating changes.</returns>
        [Obsolete("IntegrateChanges is obsolete, it is replaced by adding commands into the hierarchy node type handler's CommandList.", false)]
        [FreeFunction("HierarchyNodeTypeHandlerBaseBindings::IntegrateChanges", HasExplicitThis = true, IsThreadSafe = true)]
        protected extern virtual bool IntegrateChanges(HierarchyCommandList cmdList);
        #endregion
    }
}
