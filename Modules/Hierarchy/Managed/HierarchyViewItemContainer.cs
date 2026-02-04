// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a container for a hierarchy view item, used for pooling.
    /// </summary>
    sealed class HierarchyViewItemContainer : VisualElement
    {
        // The pool for the typeless nodes. This is used when there isn't a node type handler.
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
        static readonly UnityEngine.Pool.ObjectPool<HierarchyViewItem> s_ViewItemPool =
            new UnityEngine.Pool.ObjectPool<HierarchyViewItem>(() => new HierarchyViewItem());

        HierarchyView m_View;
        HierarchyViewItem m_ViewItem;
        HierarchyNodeTypeHandler m_ViewItemNodeTypeHandler;

        public HierarchyView View => m_View;
        public HierarchyViewItem ViewItem => m_ViewItem;
        public HierarchyNodeTypeHandler ViewItemNodeTypeHandler => m_ViewItemNodeTypeHandler;

        public void Bind(in HierarchyNode node, HierarchyView view)
        {
            if (node == HierarchyNode.Null)
                throw new ArgumentNullException(nameof(node));
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            var handler = view.Source.GetNodeTypeHandler(in node);
            if (m_ViewItem == null || m_ViewItemNodeTypeHandler != handler)
            {
                ReleaseViewItem();
                m_ViewItem = handler != null ? handler.ViewItemPool.Get() : s_ViewItemPool.Get();
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

        public void ReleaseViewItem()
        {
            if (m_ViewItem != null)
            {
                if (m_ViewItem.Bound)
                    m_ViewItem.Unbind();
                Remove(m_ViewItem);

                if (m_ViewItemNodeTypeHandler != null)
                    m_ViewItemNodeTypeHandler.ViewItemPool.Release(m_ViewItem);
                else
                    s_ViewItemPool.Release(m_ViewItem);

                m_ViewItem = null;
            }
            m_View = null;
            m_ViewItemNodeTypeHandler = null;
        }
    }
}
