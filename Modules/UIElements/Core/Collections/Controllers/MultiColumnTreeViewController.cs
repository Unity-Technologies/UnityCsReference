// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements.Internal;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Multi-column tree view controller. View controllers of this type are meant to take care of data virtualized by any <see cref="MultiColumnTreeView"/> inheritor.
    /// </summary>
    public abstract class MultiColumnTreeViewController : BaseTreeViewController
    {
        MultiColumnController m_ColumnController;

        /// <summary>
        /// The column controller, taking care of operations on the header.
        /// </summary>
        public MultiColumnController columnController => m_ColumnController;

        internal MultiColumnCollectionHeader header => m_ColumnController?.header;

        /// <summary>
        /// The constructor for MultiColumnTreeViewController.
        /// </summary>
        /// <param name="columns">The columns data used to initialize the header.</param>
        /// <param name="sortDescriptions">The sort data used to initialize the header.</param>
        /// <param name="sortedColumns">The sorted columns for the view.</param>
        protected MultiColumnTreeViewController(Columns columns, SortColumnDescriptions sortDescriptions, List<SortColumnDescription> sortedColumns)
        {
            m_ColumnController = new MultiColumnController(columns, sortDescriptions, sortedColumns);
        }

        private protected override void HierarchyChanged()
        {
            if (m_ColumnController.SortIfNeeded())
            {
                view.RefreshItems();
            }
        }

        internal override void InvokeMakeItem(ReusableCollectionItem reusableItem)
        {
            if (reusableItem is ReusableMultiColumnTreeViewItem treeItem)
            {
                treeItem.Init(MakeItem(), m_ColumnController.header.columns);
                PostInitRegistration(treeItem);
            }
            else
            {
                base.InvokeMakeItem(reusableItem);
            }
        }

        internal override void InvokeBindItem(ReusableCollectionItem reusableItem, int index)
        {
            var sortedIndex = m_ColumnController.GetSortedIndex(index);
            base.InvokeBindItem(reusableItem, sortedIndex);
        }

        internal override void InvokeUnbindItem(ReusableCollectionItem reusableItem, int index)
        {
            var sortedIndex = m_ColumnController.GetSortedIndex(index);
            base.InvokeUnbindItem(reusableItem, sortedIndex);
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
        }

        /// <summary>
        /// Unregisters events and removes the header from the hierarchy.
        /// </summary>
        public override void Dispose()
        {
            m_ColumnController.Dispose();
            m_ColumnController = null;
            base.Dispose();
        }
    }
}
