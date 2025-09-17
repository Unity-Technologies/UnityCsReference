// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Extension to HierarchyView adding a bunch of useful operation methods.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.HierarchyModule")]
    static class HierarchyViewOperationExtension
    {
        /// <summary>
        /// Cut selected nodes.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        public static void OnCut(this HierarchyView view)
        {
            foreach (var handler in view.Source.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler && editorHandler.CanCut(view))
                    editorHandler.OnCut(view);
            }
        }

        /// <summary>
        /// Copy selected nodes.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        public static void OnCopy(this HierarchyView view)
        {
            foreach (var handler in view.Source.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler && editorHandler.CanCopy(view))
                    editorHandler.OnCopy(view);
            }
        }

        /// <summary>
        /// Paste copied nodes.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        public static void OnPaste(this HierarchyView view)
        {
            foreach (var handler in view.Source.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler && editorHandler.CanPaste(view))
                    editorHandler.OnPaste(view);
            }
        }

        /// <summary>
        /// Paste copied nodes as child.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        /// <param name="keepWorldPos">If true, the pasted nodes will keep their world position, otherwise they will be placed at the local position of the parent.</param>
        public static void OnPasteAsChild(this HierarchyView view, bool keepWorldPos)
        {
            foreach (var handler in view.Source.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler && editorHandler.CanPasteAsChild(view))
                    editorHandler.OnPasteAsChild(view, keepWorldPos);
            }
        }

        /// <summary>
        /// Duplicate selected nodes.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        public static void OnDuplicate(this HierarchyView view)
        {
            foreach (var handler in view.Source.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler && editorHandler.CanDuplicate(view))
                    editorHandler.OnDuplicate(view);
            }
        }

        /// <summary>
        /// Delete selected nodes.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        public static void OnDelete(this HierarchyView view)
        {
            foreach (var handler in view.Source.EnumerateNodeTypeHandlers())
            {
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler && editorHandler.CanDelete(view))
                    editorHandler.OnDelete(view);
            }
        }

        /// <summary>
        /// Rename a given node.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        /// <param name="node">Hierarchy node to be renamed</param>
        public static void OnSetName(this HierarchyView view, in HierarchyNode node)
        {
            view.BeginRename(in node);
        }

        /// <summary>
        /// Rename a given node.
        /// </summary>
        /// <param name="view">View accepting the operation</param>
        /// <param name="node">Hierarchy node to get corresponding view item</param>
        public static HierarchyViewItem GetHierarchyViewItemForNode(this HierarchyView view, in HierarchyNode node)
        {
            if (node == HierarchyNode.Null)
                return null;

            var index = view.ViewModel.IndexOf(in node);
            if (index < 0)
                return null;

            var root = view.ListView.GetRootElementForIndex(index);
            if (root == null)
                return null;

            return root.Q<HierarchyViewItem>();
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal static bool DoesSelectedNodesHaveChildren(this HierarchyView view)
        {
            var viewModel = view.ViewModel;
            foreach (ref readonly var node in viewModel.EnumerateNodesWithAllFlags(HierarchyNodeFlags.Selected))
            {
                if (viewModel.GetChildrenCount(in node) > 0)
                    return true;
            }
            return false;
        }
    }
}
