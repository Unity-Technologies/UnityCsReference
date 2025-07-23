// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

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
        [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal HierarchyNodeTypeHandler(IntPtr nativePtr, Hierarchy hierarchy, HierarchyCommandList cmdList) : base(nativePtr, hierarchy, cmdList)
        {
            m_ViewItemPool = new Lazy<UnityEngine.Pool.ObjectPool<HierarchyViewItem>>(() =>
                new UnityEngine.Pool.ObjectPool<HierarchyViewItem>(() => new HierarchyViewItem(), defaultCapacity: 0));
        }

        /// <summary>
        /// Determines if a node type handler can accept a specified node as a parent.
        /// </summary>
        /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
        /// <param name="parent">The parent <see cref="HierarchyNode"/>.</param>
        /// <returns><see langword="true"/> if the node can be set as a parent, <see langword="false"/> otherwise.</returns>
        public virtual bool AcceptParent(HierarchyView view, in HierarchyNode parent)
        {
            return true;
        }

        /// <summary>
        /// Determines if a node type handler can accept a specified node as a child.
        /// </summary>
        /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
        /// <param name="child">The child <see cref="HierarchyNode"/>.</param>
        /// <returns><see langword="true"/> if the node can be set as a child, <see langword="false"/> otherwise.</returns>
        public virtual bool AcceptChild(HierarchyView view, in HierarchyNode child)
        {
            return true;
        }

        /// <summary>
        /// Determine if the node type handler accept the naming action.
        /// </summary>
        /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <returns><see langword="true"/> if the node can be renamed, <see langword="false"/> otherwise.</returns>
        public virtual bool CanSetName(HierarchyView view, in HierarchyNode node)
        {
            return true;
        }

        /// <summary>
        /// Action to execute when renaming a node.
        /// </summary>
        /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <param name="name">The given name.</param>
        /// <returns><see langword="true"/> if the node is renamed successfully, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnSetName(HierarchyView view, in HierarchyNode node, string name) => false;

        /// <summary>
        /// Get a node display name. Default is the node name property.
        /// </summary>
        /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <returns>Display name</returns>
        public virtual string GetDisplayName(HierarchyView view, in HierarchyNode node) => Hierarchy.GetName(node);

        /// <summary>
        /// Copy selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool Copy(HierarchyView view) => CanCopy(view) ? OnCopy(view) : false;

        /// <summary>
        /// Determines if selected nodes can be copied.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanCopy(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when copying selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnCopy(HierarchyView view) => true;

        /// <summary>
        /// Cut selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool Cut(HierarchyView view) => CanCut(view) ? OnCut(view) : false;

        /// <summary>
        /// Determines if selected nodes can be cut.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanCut(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when cutting selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnCut(HierarchyView view) => true;

        /// <summary>
        /// Delete selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool Delete(HierarchyView view) => CanDelete(view) ? OnDelete(view) : false;

        /// <summary>
        /// Determines if selected nodes can be deleted.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanDelete(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when deleting selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnDelete(HierarchyView view) => true;

        /// <summary>
        /// Duplicate selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool Duplicate(HierarchyView view) => CanDuplicate(view) ? OnDuplicate(view) : false;

        /// <summary>
        /// Determines if selected nodes can be duplicated.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanDuplicate(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when duplicating selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnDuplicate(HierarchyView view) => true;

        /// <summary>
        /// Find references of selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool FindReferences(HierarchyView view) => CanFindReferences(view) ? OnFindReferences(view) : false;

        /// <summary>
        /// Determines if finding references of selected nodes is supported.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanFindReferences(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when finding references of selected nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnFindReferences(HierarchyView view) => true;

        /// <summary>
        /// Paste copied nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool Paste(HierarchyView view) => CanPaste(view) ? OnPaste(view) : false;

        /// <summary>
        /// Determines if copied nodes can be pasted.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanPaste(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when pasting copied nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnPaste(HierarchyView view) => true;

        /// <summary>
        /// Paste copied nodes as child.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool PasteAsChild(HierarchyView view) => CanPasteAsChild(view) ? OnPasteAsChild(view) : false;

        /// <summary>
        /// Determines if copied nodes can be pasted as child.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanPasteAsChild(HierarchyView view) => true;

        /// <summary>
        /// Action to execute when pasting copied nodes as child.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnPasteAsChild(HierarchyView view) => true;

        /// <summary>
        /// Double click operation on the node.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/>.</param>
        /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
        public bool DoubleClick(HierarchyView view, in HierarchyNode node) => CanDoubleClick(view, in node) ? OnDoubleClick(view, in node) : false;

        /// <summary>
        /// Determines if a double click operation can be performed on the <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/> to perform double click on.</param>
        /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
        public virtual bool CanDoubleClick(HierarchyView view, in HierarchyNode node) => true;

        /// <summary>
        /// Action to execute when double clicking on the <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="node">The <see cref="HierarchyNode"/> to perform double click on.</param>
        /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
        protected virtual bool OnDoubleClick(HierarchyView view, in HierarchyNode node) => true;

        /// <summary>
        /// Determines if a drag operation can be started with the specified nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        /// <param name="nodes">The dragged nodes.</param>
        /// <returns><see langword="true"/> if the dragging operation can be started, <see langword="false"/> otherwise.</returns>
        protected virtual bool CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes) => true;

        /// <summary>
        /// Action to execute when starting a drag operation.
        /// Used to setup a drag operation with the specified nodes by populating the <see cref="HierarchyViewDragAndDropSetupData"/> container.
        /// The <see cref="HierarchyViewDragAndDropSetupData"/> container contains lists of <see cref="UnityEngine.Object"/> references and paths that can be populated to store information about the drag operation.
        /// </summary>
        /// <param name="data">Container holding the data needed to start a drag and drop operation. <see cref="HierarchyNodeTypeHandler"/>s can populate this container.</param>
        protected virtual void OnStartDrag(in HierarchyViewDragAndDropSetupData data) { }

        /// <summary>
        /// Determines if a drop operation can be performed based on the <see cref="HierarchyViewDragAndDropHandlingData"/>.
        /// </summary>
        /// <param name="data">Data relative to the current drag and drop operation.</param>
        /// <returns>The status of the drag and drop operation.</returns>
        protected virtual DragVisualMode CanDrop(in HierarchyViewDragAndDropHandlingData data) => DragVisualMode.None;

        /// <summary>
        /// Action to execute when handling a drop operation based on the <see cref="HierarchyViewDragAndDropHandlingData"/>.
        /// </summary>
        /// <param name="data">Data relative to the current drag and drop operation.</param>
        /// <returns>The status of the drag and drop operation.</returns>
        protected virtual DragVisualMode OnDrop(in HierarchyViewDragAndDropHandlingData data) => DragVisualMode.None;

        /// <summary>
        /// Callback to initialize <see cref="HierarchyView"/>'s. Typically to add stylesheets or classes to <see cref="HierarchyView.StyleContainer"/> that are going to be used by nodes.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/>.</param>
        protected virtual void OnInitializingView(HierarchyView view) { }

        /// <summary>
        /// Called when a hierarchy view item is bound to a hierarchy view, allowing customization of the view item.
        /// </summary>
        /// <param name="item">The hierarchy view item.</param>
        protected virtual void Bind(HierarchyViewItem item) { }

        /// <summary>
        /// Called when a hierarchy view item is unbound from a hierarchy view, allowing cleanup of the view item.
        /// </summary>
        /// <param name="item">The hierarchy view item.</param>
        protected virtual void Unbind(HierarchyViewItem item) { }

        /// <summary>
        /// Customize the tooltip displayed when the mouse hovers the node name label.
        /// </summary>
        /// <param name="item"><see cref="HierarchyViewItem"/> that is hovered.</param>
        /// <param name="isFiltering">Is the view filtering results according to a search query? Note: When filtering the view displays its results as a flat list.</param>
        /// <param name="tooltip">The tooltip to customize.</param>
        /// <returns>Returns the computed tooltip.</returns>
        protected virtual void GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip)
        {
            // By default only show tooltip when filtering
            if (!isFiltering)
                return;

            tooltip.Append(Hierarchy.GetPath(in item.Node));
        }

        /// <summary>
        /// Append context menu for a given hierarchy node.
        /// </summary>
        /// <param name="view">The selected <see cref="HierarchyView"/>.</param>
        /// <param name="item">The hierarchy view item triggering the context menu. Can be null when the background of the view is the source of the context menu request.</param>
        /// <param name="menu">The <see cref="DropdownMenu"/> to populate with.</param>
        protected virtual void PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu) { }

        #region Expose protected methods to internal
        internal bool Internal_OnSetName(HierarchyView view, in HierarchyNode node, string name) => OnSetName(view, in node, name);
        internal DragVisualMode Internal_CanDrop(in HierarchyViewDragAndDropHandlingData data) => CanDrop(data);
        internal bool Internal_CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes) => CanStartDrag(view, nodes);
        internal void Internal_OnStartDrag(in HierarchyViewDragAndDropSetupData data) => OnStartDrag(data);
        internal DragVisualMode Internal_OnDrop(in HierarchyViewDragAndDropHandlingData data) => OnDrop(data);
        internal void Internal_OnInitializingView(HierarchyView view) => OnInitializingView(view);
        internal void Internal_PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu) => PopulateContextMenu(view, item, menu);
        internal void Internal_Bind(HierarchyViewItem item) => Bind(item);
        internal void Internal_Unbind(HierarchyViewItem item) => Unbind(item);
        internal void Internal_GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip) => GetTooltip(item, isFiltering, tooltip);
        #endregion
    }
}
