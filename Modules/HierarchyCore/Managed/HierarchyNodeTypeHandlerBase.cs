// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides a base class for hierarchy node type handlers.
    /// </summary>
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public abstract class HierarchyNodeTypeHandlerBase : IDisposable
    {
        [RequiredByNativeCode, VisibleToOtherModules("UnityEditor.HierarchyModule")] internal IntPtr m_Ptr;
        [RequiredByNativeCode, VisibleToOtherModules("UnityEditor.HierarchyModule")] internal bool m_IsWrapper;
        [RequiredByNativeCode] readonly Hierarchy m_Hierarchy;
        [RequiredByNativeCode] readonly HierarchyCommandList m_CommandList;

        static readonly Dictionary<Type, int> s_NodeTypes = new();

        /// <summary>
        /// Get the Hierarchy owning this handler.
        /// </summary>
        public Hierarchy Hierarchy => m_Hierarchy;

        /// <summary>
        /// Get the <see cref="HierarchyCommandList"/> associated with this handler.
        /// </summary>
        protected HierarchyCommandList CommandList => m_CommandList;

        internal bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// Creates a hierarchy node type handler.
        /// </summary>
        /// <param name="hierarchy">The hierarchy to associate with the node type handler.</param>
        protected HierarchyNodeTypeHandlerBase(Hierarchy hierarchy)
        {
            m_Hierarchy = hierarchy;
            m_CommandList = new HierarchyCommandList(hierarchy, GetNodeType());
        }

        ~HierarchyNodeTypeHandlerBase()
        {
            Dispose(false);
        }

        internal virtual void Initialize()
        {
        }

        /// <summary>
        /// Disposes this hierarchy node type handler to free up resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this hierarchy node type handler to free up resources in the derived class.
        /// </summary>
        /// <param name="disposing">Returns true if called from Dispose, false otherwise.</param>
        protected virtual void Dispose(bool disposing)
        {
            m_CommandList.Dispose();
        }

        /// <summary>
        /// Retrieves the hierarchy node type for this hierarchy node type handler.
        /// </summary>
        /// <returns>The type of the hierarchy node.</returns>
        public HierarchyNodeType GetNodeType() => new HierarchyNodeType(GetNodeType(GetType()));

        /// <summary>
        /// Get the type name of this hierarchy node type handler.
        /// </summary>
        /// <returns>The type name of the hierarchy node.</returns>
        public virtual string GetNodeTypeName()
        {
            return string.Empty;
        }

        /// <summary>
        /// Get the default value used to initialize a hierarchy node flags.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="defaultFlags">The default hierarchy node flags.</param>
        /// <returns>The default flags of the hierarchy node.</returns>
        public virtual HierarchyNodeFlags GetDefaultNodeFlags(in HierarchyNode node, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None)
        {
            return defaultFlags;
        }

        /// <summary>
        /// Called when a new search query begins.
        /// </summary>
        /// <param name="query"></param>
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

        [FreeFunction("HierarchyNodeTypeHandlerManager::Get().GetNodeType", IsThreadSafe = true)]
        static extern int GetNodeType(Type type);

        internal void Internal_SearchBegin(HierarchySearchQueryDescriptor query) => SearchBegin(query);
        internal bool Internal_SearchMatch(in HierarchyNode node) => SearchMatch(in node);

        #region Called From Native
        static HierarchyNodeTypeHandlerBase GetHandlerFromPtr(IntPtr ptr)
        {
            return (HierarchyNodeTypeHandlerBase)GCHandle.FromIntPtr(ptr).Target;
        }

        [UsedByNativeCode, RequiredMember]
        static HierarchyNodeTypeHandlerBase InvokeCreateInstance(Type type, Hierarchy hierarchy)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var parameters = new[] { hierarchy };
            return (HierarchyNodeTypeHandlerBase)Activator.CreateInstance(type, flags, null, parameters, null);
        }

        [UsedByNativeCode, RequiredMember]
        static bool InvokeTryGetNodeType(Type type, out int nodeType)
        {
            if (s_NodeTypes.TryGetValue(type, out nodeType))
                return true;

            var method = type.GetMethod("Internal_GetNodeType", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                nodeType = (int)method.Invoke(null, null);
                s_NodeTypes.Add(type, nodeType);
                return true;
            }

            nodeType = HierarchyNodeType.k_HierarchyNodeTypeNull;
            return false;
        }

        [UsedByNativeCode, RequiredMember]
        static void InvokeInitialize(IntPtr ptr) => GetHandlerFromPtr(ptr).Initialize();

        [UsedByNativeCode, RequiredMember]
        static void InvokeDispose(IntPtr ptr) => GetHandlerFromPtr(ptr).Dispose();

        [UsedByNativeCode, RequiredMember]
        static string InvokeGetNodeTypeName(IntPtr ptr) => GetHandlerFromPtr(ptr).GetNodeTypeName();

        [UsedByNativeCode, RequiredMember]
        static HierarchyNodeFlags InvokeGetDefaultNodeFlags(IntPtr ptr, in HierarchyNode node, HierarchyNodeFlags defaultFlags) => GetHandlerFromPtr(ptr).GetDefaultNodeFlags(in node, defaultFlags);

#pragma warning disable 618 // Remove this pragma once the corresponding public APIs below are removed
        [UsedByNativeCode, RequiredMember]
        static bool InvokeChangesPending(IntPtr ptr) => GetHandlerFromPtr(ptr).ChangesPending();

        [UsedByNativeCode, RequiredMember]
        static bool InvokeIntegrateChanges(IntPtr ptr, HierarchyCommandList cmdList) => GetHandlerFromPtr(ptr).IntegrateChanges(cmdList);
#pragma warning restore 618

        [UsedByNativeCode, RequiredMember]
        static bool InvokeSearchMatch(IntPtr ptr, in HierarchyNode node) => GetHandlerFromPtr(ptr).SearchMatch(in node);

        [UsedByNativeCode, RequiredMember]
        static void InvokeSearchEnd(IntPtr ptr) => GetHandlerFromPtr(ptr).SearchEnd();
        #endregion

        #region Obsolete public APIs to remove in 2024
        /// <summary>
        /// Callback that determines if pending changes from a registered node type handler need to be applied to the hierarchy. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <returns><see langword="true"/> if changes are pending, <see langword="false"/> otherwise.</returns>
        [Obsolete("ChangesPending is obsolete, it is replaced by adding commands into the hierarchy node type handler's CommandList.", false)]
        protected virtual bool ChangesPending() => false;

        /// <summary>
        /// Callback that determines if changes from an update need to be integrated into the hierarchy. `IntegrateChanges` is called after <see cref="ChangesPending"/> returns <see langword="true"/>. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <param name="cmdList">A hierarchy command list that can modify the hierarchy.</param>
        /// <returns><see langword="true"/> if more invocations are needed to complete integrating changes, and <see langword="false"/> if the handler is done integrating changes.</returns>
        [Obsolete("IntegrateChanges is obsolete, it is replaced by adding commands into the hierarchy node type handler's CommandList.", false)]
        protected virtual bool IntegrateChanges(HierarchyCommandList cmdList) => false;
        #endregion
    }
}
