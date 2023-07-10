// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Multi-column list view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="MultiColumnListView"/> inheritor.
    /// </summary>
    public class MultiColumnListViewController : BaseListViewController
    {
        MultiColumnController m_ColumnController;

        /// <summary>
        /// The column controller, taking care of operations on the header.
        /// </summary>
        public MultiColumnController columnController => m_ColumnController;

        internal MultiColumnCollectionHeader header => m_ColumnController?.header;

        /// <summary>
        /// The constructor for MultiColumnListViewController.
        /// </summary>
        /// <param name="columns">The columns data used to initialize the header.</param>
        /// <param name="sortDescriptions">The sort data used to initialize the header.</param>
        /// <param name="sortedColumns">The sorted columns for the view.</param>
        public MultiColumnListViewController(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
        {
            m_ColumnController = new MultiColumnController(columns, sortDescriptions, sortedColumns);
        }

        internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableMultiColumnListViewItem listItem)
            {
                listItem.Init(MakeItem(), m_ColumnController.header.columns, baseListView.reorderMode == ListViewReorderMode.Animated);
                PostInitRegistration(listItem);
            }
            else
            {
                base.InvokeMakeItem(reusableItem);
            }
        }

        /// <inheritdoc />
        protected override VisualElement MakeItem()
        {
            return m_ColumnController.MakeItem();
        }

        /// <inheritdoc />
        protected override void BindItem(VisualElement element, int index)
        {
            m_ColumnController.BindItem(element, index, GetItemForIndex(index));
        }

        /// <inheritdoc />
        protected override void UnbindItem(VisualElement element, int index)
        {
            m_ColumnController.UnbindItem(element, index);
        }

        /// <inheritdoc />
        protected override void DestroyItem(VisualElement element)
        {
            m_ColumnController.DestroyItem(element);
        }

        /// <inheritdoc />
        protected override void PrepareView()
        {
            m_ColumnController.PrepareView(view);
            baseListView.reorderModeChanged += UpdateReorderClassList;
        }

        /// <summary>
        /// Unregisters events and removes the header from the hierarchy.
        /// </summary>
        public override void Dispose()
        {
            baseListView.reorderModeChanged -= UpdateReorderClassList;
            m_ColumnController.Dispose();
            m_ColumnController = null;
            base.Dispose();
        }

        void UpdateReorderClassList()
        {
            m_ColumnController.header.EnableInClassList(MultiColumnCollectionHeader.reorderableUssClassName,
                baseListView.reorderable && baseListView.reorderMode == ListViewReorderMode.Animated);
        }
    }
}
