// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;
using HierarchyViewItemPool = UnityEngine.Pool.ObjectPool<Unity.Hierarchy.HierarchyViewItem>;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a container for a hierarchy view item, used for pooling.
    /// </summary>
    sealed class HierarchyViewItemContainer : VisualElement
    {
        // The pool for the typeless nodes. This is used when there isn't a node type handler.
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
        static readonly HierarchyViewItemPool s_ViewItemPool = new (() => new HierarchyViewItem(), defaultCapacity: 0, maxSize: 512);

        HierarchyView m_View;
        HierarchyViewItem m_ViewItem;
        HierarchyNodeTypeHandler m_ViewItemNodeTypeHandler;

        public HierarchyView View => m_View;
        public HierarchyViewItem ViewItem => m_ViewItem;
        public HierarchyNodeTypeHandler ViewItemNodeTypeHandler => m_ViewItemNodeTypeHandler;

        static HierarchyViewItem GetViewItem(HierarchyNodeTypeHandler handler)
        {
            if (handler != null)
                return handler.ViewItemPool.Get();
            else
                return s_ViewItemPool.Get();
        }

        static void ReleaseViewItem(HierarchyViewItem viewItem, HierarchyNodeTypeHandler currentHandler)
        {
            if (viewItem == null)
                throw new ArgumentNullException(nameof(viewItem));

            if (currentHandler != null)
                currentHandler.ViewItemPool.Release(viewItem);
            else
                s_ViewItemPool.Release(viewItem);
        }

        public void Bind(in HierarchyNode node, HierarchyView view)
        {
            if (node == HierarchyNode.Null)
                throw new ArgumentNullException(nameof(node));
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var handler = view.Source.GetNodeTypeHandler(in node);
            if (m_ViewItem == null || m_ViewItemNodeTypeHandler != handler)
            {
                Release();

                m_ViewItem = GetViewItem(handler);
                if (m_ViewItem == null)
                    throw new NullReferenceException("Failed to get a view item from the pool");

                // Add the view item to the container before binding it to
                // the node since the bind expects the parent to be set
                Add(m_ViewItem);

                m_ViewItemNodeTypeHandler = handler;
            }

            m_View = view;
            m_ViewItem.Bind(in node, m_View);
        }

        public void Unbind()
        {
            m_ViewItem?.Unbind();
        }

        public void Release()
        {
            if (m_ViewItem != null)
            {
                if (m_ViewItem.Bound)
                    m_ViewItem.Unbind();

                Remove(m_ViewItem);
                ReleaseViewItem(m_ViewItem, m_ViewItemNodeTypeHandler);
                m_ViewItem = null;
            }
            m_View = null;
            m_ViewItemNodeTypeHandler = null;
        }
    }
}
