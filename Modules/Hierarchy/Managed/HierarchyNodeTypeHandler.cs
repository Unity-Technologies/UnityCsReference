// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using HierarchyViewItemPool = UnityEngine.Pool.ObjectPool<Unity.Hierarchy.HierarchyViewItem>;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides a base class for hierarchy node type handlers that manage how specific node types are displayed and interact with <see cref="HierarchyView"/> instances.
    /// </summary>
    [RequiredByNativeCode(Optional = true), StructLayout(LayoutKind.Sequential)]
    public abstract class HierarchyNodeTypeHandler : HierarchyNodeTypeHandlerBase
    {
        readonly HierarchyViewItemPool m_ViewItemPool = new(() => new HierarchyViewItem(), defaultCapacity: 256, maxSize: 512);

        /// <summary>
        /// The object pool used to store <see cref="HierarchyViewItem"/> instances specific to this handler.
        /// </summary>
        internal HierarchyViewItemPool ViewItemPool => m_ViewItemPool;

        /// <summary>
        /// Creates a new <see cref="HierarchyNodeTypeHandler"/>.
        /// </summary>
        protected HierarchyNodeTypeHandler()
        {
        }

        /// <summary>
        /// Creates a new <see cref="HierarchyNodeTypeHandler"/> from a pointer.
        /// </summary>
        /// <param name="nativePtr">The pointer to the native <see cref="HierarchyNodeTypeHandler"/>.</param>
        /// <param name="hierarchy">The <see cref="Hierarchy"/> this <see cref="HierarchyNodeTypeHandler"/> is associated with.</param>
        /// <param name="cmdList">The command list used for <see cref="Hierarchy"/> operations.</param>
        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal HierarchyNodeTypeHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
        }

        /// <summary>
        /// Called when the <see cref="HierarchyNodeTypeHandler"/> is bound to a <see cref="HierarchyView"/>.
        /// </summary>
        /// <remarks>
        /// Typically used to add stylesheets or classes to the <see cref="HierarchyView.StyleContainer"/>.
        /// </remarks>
        /// <param name="view">The <see cref="HierarchyView"/> being bound to.</param>
        protected virtual void OnBindView(HierarchyView view) { }

        /// <summary>
        /// Called when the <see cref="HierarchyNodeTypeHandler"/> is unbound from a <see cref="HierarchyView"/>.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/> being unbound from.</param>
        protected virtual void OnUnbindView(HierarchyView view) { }

        /// <summary>
        /// Called whenever a <see cref="HierarchyViewItem"/> is bound to a <see cref="HierarchyView"/>.
        /// Typically used to set up the item with the necessary data and styles.
        /// </summary>
        /// <param name="item">The <see cref="HierarchyViewItem"/> being bound.</param>
        protected virtual void OnBindItem(HierarchyViewItem item) { }

        /// <summary>
        /// Called whenever a <see cref="HierarchyViewItem"/> is unbound from a <see cref="HierarchyView"/>.
        /// </summary>
        /// <param name="item">The <see cref="HierarchyViewItem"/> being unbound.</param>
        protected virtual void OnUnbindItem(HierarchyViewItem item) { }

        #region Expose protected methods to internal
        internal void Internal_BindView(HierarchyView view) => OnBindView(view);
        internal void Internal_UnbindView(HierarchyView view) => OnUnbindView(view);
        internal void Internal_BindItem(HierarchyViewItem item) => OnBindItem(item);
        internal void Internal_UnbindItem(HierarchyViewItem item) => OnUnbindItem(item);
        #endregion
    }
}
