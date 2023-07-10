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
    /// Provides a base class for hierarchy node type handlers.
    /// </summary>
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public abstract class HierarchyNodeTypeHandlerBase : IDisposable
    {
        [RequiredByNativeCode, VisibleToOtherModules("UnityEditor.HierarchyModule")] internal IntPtr m_Ptr;
        [RequiredByNativeCode, VisibleToOtherModules("UnityEditor.HierarchyModule")] internal bool m_IsWrapper;
        [RequiredByNativeCode] readonly Hierarchy m_Hierarchy;

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
        }

        ~HierarchyNodeTypeHandlerBase()
        {
            Dispose(false);
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
        public HierarchyNodeType GetNodeType() => GetNodeType(GetType());

        /// <summary>
        /// Retrieves the hierarchy node type name for this hierarchy node type handler.
        /// </summary>
        /// <returns>The type name of the hierarchy node.</returns>
        public virtual string GetNodeTypeName()
        {
            return string.Empty;
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
        /// Determines if a node type handler can accept a specified node as a parent.
        /// </summary>
        /// <param name="node">The hierarchy parent node.</param>
        /// <returns><see langword="true"/> if the node can be set as a parent, <see langword="false"/> otherwise.</returns>
        public virtual bool AcceptParent(in HierarchyNode node)
        {
            return true;
        }

        /// <summary>
        /// Determines if a node type handler can accept a specified node as a child.
        /// </summary>
        /// <param name="node">The hierarchy child node.</param>
        /// <returns><see langword="true"/> if the node can be set as a child, <see langword="false"/> otherwise.</returns>
        public virtual bool AcceptChild(in HierarchyNode node)
        {
            return true;
        }

        /// <summary>
        /// Called when a new search query begins.
        /// </summary>
        /// <param name="query"></param>
        protected internal virtual void SearchBegin(HierarchySearchQueryDescriptor query)
        {
        }

        /// <summary>
        /// Determines if a node matches the search query.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node matches the search query, <see langword="false"/> otherwise.</returns>
        protected internal virtual bool SearchMatch(in HierarchyNode node)
        {
            return false;
        }

        /// <summary>
        /// Called when a search query ends.
        /// </summary>
        protected internal virtual void SearchEnd()
        {
        }

        [FreeFunction("HierarchyNodeTypeHandlerManager::Get().GetNodeType")]
        static extern HierarchyNodeType GetNodeType(Type type);
    }
}
