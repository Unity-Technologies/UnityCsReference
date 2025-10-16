// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Hierarchy Column
    /// </summary>
    internal sealed class HierarchyViewColumn : Column
    {
        internal const string k_NonDefaultValue = "non-default-value";

        readonly HierarchyView m_View;
        readonly List<HierarchyViewCellDescriptor> m_CellDescriptors = new();

        internal HierarchyView View => m_View;

        /// <summary>
        /// Column Descriptor used to build this Column.
        /// </summary>
        public readonly HierarchyViewColumnDescriptor Descriptor;

        /// <summary>
        /// Available Collection of CellDescriptors for this column
        /// </summary>
        public IReadOnlyCollection<HierarchyViewCellDescriptor> CellDescriptors => m_CellDescriptors;

        /// <summary>
        /// Create a new Column from a ColumnDescriptor and set the View it belongs to.
        /// </summary>
        /// <param name="view">HierarchyView that will host the column</param>
        /// <param name="descriptor">ColumnDescriptor used to provide all the data to create the column and binds it.</param>
        public HierarchyViewColumn(HierarchyView view, HierarchyViewColumnDescriptor descriptor)
        {
            m_View = view;
            Descriptor = descriptor;

            name = Descriptor.Id;
            resizable = true;
            stretchable = false;
            sortable = false;

            title = Descriptor.Title;

            if (Descriptor.MakeHeader != null)
            {
                makeHeader += MakeHeader;
                bindHeader += BindHeader;
                unbindHeader += UnbindHeader;
                destroyHeader += DestroyHeader;
            }
            else
            {
                if (Descriptor.Icon)
                    icon = Background.FromTexture2D(Descriptor.Icon);
            }

            if (descriptor.DefaultWidth > 0)
            {
                this.width = descriptor.DefaultWidth;
            }

            makeCell += MakeCell;
            bindCell += BindCell;
            unbindCell += UnbindCell;
        }

        /// <summary>
        /// Add a CellDescriptor to this Column. The cell descriptor must match Id of the ColumnDescriptor and there must be no other CellDescriptor with the same NodeType.
        /// </summary>
        /// <param name="desc">CellDescriptor to register.</param>
        public void AddCell(HierarchyViewCellDescriptor desc)
        {
            if (!desc.ValidForColumn(Descriptor))
            {
                Debug.LogError($"Cannot register Cell: {desc.ColumnId} with Column: {Descriptor.Id}");
                return;
            }

            foreach (var d in CellDescriptors)
            {
                if (d.HandlerType == desc.HandlerType)
                {
                    Debug.LogError($"Cell: for NodeType {desc.HandlerType} is already registered.");
                    return;
                }
            }

            m_CellDescriptors.Add(desc);
        }

        internal void ApplyDefaultColumnProperties()
        {
            if (Descriptor.DefaultWidth > 0)
            {
                SetWidth(this, Descriptor.DefaultWidth);
            }

            visible = Descriptor.DefaultVisibility;
        }

        internal static void SetWidth(Column col, float newWidth)
        {
            if (newWidth <= 0)
                return;
            col.width = newWidth;
            if (col.minWidth.value > newWidth)
            {
                col.minWidth = newWidth;
            }
        }

        VisualElement MakeHeader()
        {
            return Descriptor.MakeHeader();
        }

        void BindHeader(VisualElement header)
        {
            Descriptor?.BindHeader(header, m_View);
        }

        void UnbindHeader(VisualElement header)
        {
            Descriptor?.UnbindHeader(header, m_View);
        }

        void DestroyHeader(VisualElement header)
        {
            Descriptor?.DestroyHeader(header, m_View);
        }

        VisualElement MakeCell()
        {
            var cell = new HierarchyViewCell(m_View, this);
            return cell;
        }

        internal void BindColumn(HierarchyView view)
        {
            Descriptor.InvokeBindColumn(this, view);
            foreach (var cellDesc in CellDescriptors)
            {
                cellDesc.InvokeBindColumn(Descriptor, view);
            }
        }

        internal void UnbindColumn(HierarchyView view)
        {
            foreach (var cellDesc in CellDescriptors)
            {
                cellDesc.InvokeUnbindColumn(Descriptor, view);
            }

            if (Descriptor.UnbindHeader != null || Descriptor.DestroyHeader != null)
            {
                var multiColumnHeaders = view.ListView.Query<VisualElement>(null, "unity-multi-column-header__column").ToList();

                foreach (var header in multiColumnHeaders)
                {
                    if (header.name == Descriptor.Id)
                    {
                        var content = header.Q(null, "unity-multi-column-header__column__content");
                        Descriptor.UnbindHeader?.Invoke(content, view);
                        Descriptor.DestroyHeader?.Invoke(content, view);
                        break;
                    }
                }
            }
            Descriptor.InvokeUnbindColumn(this, view);
        }

        void BindCell(VisualElement cellElement, int index)
        {
            var cell = cellElement as HierarchyViewCell;
            if (cell == null)
                return;

            var node = m_View.ViewModel[index];
            if (node == HierarchyNode.Null)
                return;

            // Patch: normally unnecessary, but since BindCell can be called whenever UI is repaint
            // outside the regular update loop, it is possible the node no longer exists in the hierarchy
            // even though it still exists in the view model.
            if (!m_View.Source.Exists(node))
                return;

            cell.Node = node;
            cell.NodeIndex = index;
            cell.Handler = m_View.Source.GetNodeTypeHandler(node);
            if (cell.Handler == null)
                return;

            foreach (var desc in CellDescriptors)
            {
                if (desc.HandlerType == null || desc.HandlerType == cell.Handler.GetType())
                {
                    cell.Descriptor = desc;
                    break;
                }
            }

            if (cell.Descriptor == null)
            {
                return;
            }
            cell.BindCell();
        }

        void UnbindCell(VisualElement cellElement, int index)
        {
            var cell = cellElement as HierarchyViewCell;
            if (cell == null)
                return;
            cell.UnbindCell();
        }

        /// <summary>
        /// Return Column string representation
        /// </summary>
        /// <returns>Return Column string representation</returns>
        public override string ToString()
        {
            return Descriptor.ToString();
        }
    }
}
