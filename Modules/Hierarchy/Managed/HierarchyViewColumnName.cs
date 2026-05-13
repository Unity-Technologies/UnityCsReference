// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using HierarchyViewItemContainerPool = UnityEngine.Pool.ObjectPool<Unity.Hierarchy.HierarchyViewItemContainer>;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents the default column in the hierarchy view, which contains the icon and name of nodes.
    /// </summary>
    [VisibleToOtherModules]
    sealed class HierarchyViewColumnName : Column
    {
        internal const string k_HierarchyNameColumnName = "HierarchyViewColumn Name";

        [NoAutoStaticsCleanup]
        static readonly BindingId k_ColumnStretchableProperty = nameof(Column.stretchable);

        static readonly Length k_DefaultMinimumWidth = new Length(35f, LengthUnit.Pixel); // 35px is the default when unset
        static readonly Length k_MinimumWidth = new Length(200f, LengthUnit.Pixel);

        readonly HierarchyView m_View;
        readonly HierarchyViewItemContainerPool m_ViewItemContainerPool;

        public event Action<HierarchyViewItem> OnBindItem;
        public event Action<HierarchyViewItem> OnUnbindItem;

        public HierarchyViewColumnName(HierarchyView view)
        {
            m_View = view;
            m_ViewItemContainerPool = new(() => new HierarchyViewItemContainer(), defaultCapacity: 0, maxSize: 512);

            title = "Name";
            name = k_HierarchyNameColumnName;
            ApplyDefaultColumnProperties();

            makeCell = MakeCell;
            destroyCell = DestroyCell;
            bindCell = BindCell;
            unbindCell = UnbindCell;

            propertyChanged += (_, args) =>
            {
                if (args.propertyName == k_ColumnStretchableProperty)
                {
                    minWidth = stretchable ? k_MinimumWidth : k_DefaultMinimumWidth;

                    if (width.value < minWidth.value)
                        width = minWidth;
                }
            };
        }

        internal void ApplyDefaultColumnProperties()
        {
            minWidth = stretchable ? k_MinimumWidth : k_DefaultMinimumWidth;

            visible = true;
            optional = false;
            resizable = true;
            sortable = false;
        }

        VisualElement MakeCell()
        {
            return m_ViewItemContainerPool.Get();
        }

        void DestroyCell(VisualElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (element is not HierarchyViewItemContainer container)
                throw new ArgumentException($"Expected {nameof(element)} to be a {nameof(HierarchyViewItemContainer)}");

            container.Release();
            m_ViewItemContainerPool.Release(container);
        }

        void BindCell(VisualElement element, int index)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (element is not HierarchyViewItemContainer container)
                throw new ArgumentException($"Expected {nameof(element)} to be a {nameof(HierarchyViewItemContainer)}");

            var node = m_View.ViewModel[index];
            if (node == HierarchyNode.Null)
                throw new InvalidOperationException("Expected node to be valid");

            container.Bind(in node, m_View);
            OnBindItem?.Invoke(container.ViewItem);
        }

        void UnbindCell(VisualElement element, int index)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (element is not HierarchyViewItemContainer container)
                throw new ArgumentException($"Expected {nameof(element)} to be a {nameof(HierarchyViewItemContainer)}");

            // In the case the container was never bound, ViewItem will be null
            if (container.ViewItem != null)
                OnUnbindItem?.Invoke(container.ViewItem);

            container.Unbind();
        }
    }
}
