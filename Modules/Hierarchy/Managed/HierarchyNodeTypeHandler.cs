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
    /// Base class for hierarchy node type handlers.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal abstract class HierarchyNodeTypeHandler : HierarchyNodeTypeHandlerBase
    {
        readonly Lazy<UnityEngine.Pool.ObjectPool<HierarchyViewItem>> m_ViewItemPool;

        /// <summary>
        /// The object pool used to store <see cref="HierarchyViewItem"/> instances specific to this handler.
        /// </summary>
        internal UnityEngine.Pool.ObjectPool<HierarchyViewItem> ViewItemPool => m_ViewItemPool.Value;

        /// <summary>
        /// Construct a new <see cref="HierarchyNodeTypeHandler"/>.
        /// </summary>
        protected HierarchyNodeTypeHandler()
        {
            m_ViewItemPool = new Lazy<UnityEngine.Pool.ObjectPool<HierarchyViewItem>>(() =>
                new UnityEngine.Pool.ObjectPool<HierarchyViewItem>(() => new HierarchyViewItem(), defaultCapacity: 0));
        }

        /// <summary>
        /// Construct a new <see cref="HierarchyNodeTypeHandler"/> from a pointer.
        /// </summary>
        /// <param name="nativePtr">The native pointer.</param>
        /// <param name="hierarchy">The hierarchy.</param>
        /// <param name="cmdList">The command list.</param>
        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal HierarchyNodeTypeHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
            m_ViewItemPool = new Lazy<UnityEngine.Pool.ObjectPool<HierarchyViewItem>>(() =>
                new UnityEngine.Pool.ObjectPool<HierarchyViewItem>(() => new HierarchyViewItem(), defaultCapacity: 0));
        }

        /// <summary>
        /// Called when the hierarchy node type handler is bound to a hierarchy view.
        /// Typically used to add stylesheets or classes to the <see cref="HierarchyView.StyleContainer"/>.
        /// </summary>
        /// <param name="view">The hierarchy view.</param>
        protected virtual void OnBindView(HierarchyView view) { }

        /// <summary>
        /// Called when the hierarchy node type handler is unbound from a hierarchy view.
        /// </summary>
        /// <param name="view">The hierarchy view.</param>
        protected virtual void OnUnbindView(HierarchyView view) { }

        /// <summary>
        /// Called whenever a hierarchy view item is bound to a hierarchy view.
        /// Typically used to set up the item with the necessary data and styles.
        /// </summary>
        /// <param name="item">The hierarchy view item.</param>
        protected virtual void OnBindItem(HierarchyViewItem item) { }

        /// <summary>
        /// Called whenever a hierarchy view item is unbound from a hierarchy view.
        /// </summary>
        /// <param name="item">The hierarchy view item.</param>
        protected virtual void OnUnbindItem(HierarchyViewItem item) { }

        #region Expose protected methods to internal
        internal void Internal_BindView(HierarchyView view) => OnBindView(view);
        internal void Internal_UnbindView(HierarchyView view) => OnUnbindView(view);
        internal void Internal_BindItem(HierarchyViewItem item) => OnBindItem(item);
        internal void Internal_UnbindItem(HierarchyViewItem item) => OnUnbindItem(item);
        #endregion
    }
}
