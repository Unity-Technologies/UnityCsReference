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

        static readonly Dictionary<Type, int> s_NodeTypes = new();

        /// <summary>
        /// Get the Hierarchy owning this handler.
        /// </summary>
        public Hierarchy Hierarchy => m_Hierarchy;

        internal bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// Creates a hierarchy node type handler.
        /// </summary>
        /// <param name="hierarchy">The hierarchy to associate with the node type handler.</param>
        protected HierarchyNodeTypeHandlerBase(Hierarchy hierarchy)
        {
            m_Hierarchy = hierarchy;
            Initialize();
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
        protected virtual void Dispose(bool disposing) { }

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
        /// Determines if a node type handler can accept a specified node as a parent.
        /// </summary>
        /// <param name="parent">The hierarchy parent node.</param>
        /// <returns><see langword="true"/> if the node can be set as a parent, <see langword="false"/> otherwise.</returns>
        public virtual bool AcceptParent(in HierarchyNode parent)
        {
            return true;
        }

        /// <summary>
        /// Determines if a node type handler can accept a specified node as a child.
        /// </summary>
        /// <param name="child">The hierarchy child node.</param>
        /// <returns><see langword="true"/> if the node can be set as a child, <see langword="false"/> otherwise.</returns>
        public virtual bool AcceptChild(in HierarchyNode child)
        {
            return true;
        }

        /// <summary>
        /// Determine if the node type handler accept the naming action.
        /// </summary>
        /// <returns><see langword="true"/> if the node can be renamed, <see langword="false"/> otherwise.</returns>
        public virtual bool CanSetName(in HierarchyNode node)
        {
            return true;
        }

        /// <summary>
        /// Callback that determines if pending changes from a registered node type handler need to be applied to the hierarchy. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <returns><see langword="true"/> if changes are pending, <see langword="false"/> otherwise.</returns>
        protected abstract bool ChangesPending();

        /// <summary>
        /// Callback that determines if changes from an update need to be integrated into the hierarchy. `IntegrateChanges` is called after <see cref="ChangesPending"/> returns <see langword="true"/>. When the hierarchy is updated, `ChangesPending` is called on all registered node ype handlers. If they return true, then `IntegrateChanges` is called on them. If they return false, then `IntegrateChanges` is not called on them.
        /// </summary>
        /// <param name="cmdList">A hierarchy command list that can modify the hierarchy.</param>
        /// <returns><see langword="true"/> if more invocations are needed to complete integrating changes, and <see langword="false"/> if the handler is done integrating changes.</returns>
        protected abstract bool IntegrateChanges(HierarchyCommandList cmdList);

        /// <summary>
        /// Called when a node is renamed in the hierarchy.
        /// </summary>
        /// <returns><see langword="true"/> if the node is renamed successfully, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnSetName(in HierarchyNode node, string name)
        {
            return false;
        }

        /// <summary>
        /// Called when a node is parented in the hierarchy.
        /// </summary>
        /// <param name="node">The node that is parented.</param>
        /// <param name="parent">The new parent of the node.</param>
        /// <returns><see langword="true"/> if the node is parented successfully, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnSetParent(in HierarchyNode node, in HierarchyNode parent)
        {
            return true;
        }

        /// <summary>
        /// Called when the sorting index of a node is changed in the hierarchy.
        /// </summary>
        /// <param name="node">The node that is sorted.</param>
        /// <param name="index">The new sorting index.</param>
        /// <returns><see langword="true"/> if the sorting index was applied successfully, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnSetSortIndex(in HierarchyNode node, int index)
        {
            return true;
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

        [FreeFunction("HierarchyNodeTypeHandlerManager::Get().GetNodeType")]
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
        static bool InvokeAcceptParent(IntPtr ptr, in HierarchyNode node) => GetHandlerFromPtr(ptr).AcceptParent(in node);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeAcceptChild(IntPtr ptr, in HierarchyNode node) => GetHandlerFromPtr(ptr).AcceptChild(in node);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeCanSetName(IntPtr ptr, in HierarchyNode node) => GetHandlerFromPtr(ptr).CanSetName(in node);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeChangesPending(IntPtr ptr) => GetHandlerFromPtr(ptr).ChangesPending();

        [UsedByNativeCode, RequiredMember]
        static bool InvokeIntegrateChanges(IntPtr ptr, HierarchyCommandList cmdList) => GetHandlerFromPtr(ptr).IntegrateChanges(cmdList);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeOnSetName(IntPtr ptr, in HierarchyNode node, string name) => GetHandlerFromPtr(ptr).OnSetName(in node, name);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeOnSetParent(IntPtr ptr, in HierarchyNode node, in HierarchyNode parent) => GetHandlerFromPtr(ptr).OnSetParent(in node, in parent);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeOnSetSortIndex(IntPtr ptr, in HierarchyNode node, int index) => GetHandlerFromPtr(ptr).OnSetSortIndex(in node, index);

        [UsedByNativeCode, RequiredMember]
        static bool InvokeSearchMatch(IntPtr ptr, in HierarchyNode node) => GetHandlerFromPtr(ptr).SearchMatch(in node);

        [UsedByNativeCode, RequiredMember]
        static void InvokeSearchEnd(IntPtr ptr) => GetHandlerFromPtr(ptr).SearchEnd();
        #endregion
    }
}
