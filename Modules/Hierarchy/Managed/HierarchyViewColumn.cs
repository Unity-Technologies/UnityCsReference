// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a column in a <see cref="HierarchyView"/>.
    /// </summary>
    public sealed class HierarchyViewColumn : Column
    {
        internal static readonly UniqueStyleString k_NonDefaultValue = new("non-default-value");

        readonly HierarchyView m_View;
        readonly List<HierarchyViewCellDescriptor> m_CellDescriptors = new();

        internal HierarchyView View => m_View;

        /// <summary>
        /// Gets the <see cref="HierarchyViewColumnDescriptor"/> used to build this column.
        /// </summary>
        public HierarchyViewColumnDescriptor Descriptor { get; }

        /// <summary>
        /// Gets the collection of <see cref="HierarchyViewCellDescriptor"/> instances available for this column.
        /// </summary>
        public IReadOnlyCollection<HierarchyViewCellDescriptor> CellDescriptors => m_CellDescriptors;

        /// <summary>
        /// Creates a new <see cref="HierarchyViewColumn"/> from a <see cref="HierarchyViewColumnDescriptor"/> and sets the <see cref="HierarchyView"/> it belongs to.
        /// </summary>
        /// <param name="view">The <see cref="HierarchyView"/> that hosts this column.</param>
        /// <param name="descriptor">The <see cref="HierarchyViewColumnDescriptor"/> that provides the data to create the column and bind it.</param>
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
        /// Adds a <see cref="HierarchyViewCellDescriptor"/> to this column. The cell descriptor must match the identifier of the <see cref="HierarchyViewColumnDescriptor"/>, and there must be no other cell descriptor with the same node type.
        /// </summary>
        /// <param name="desc">The <see cref="HierarchyViewCellDescriptor"/> to register.</param>
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

            cell.Node = node;
            cell.NodeIndex = index;
            cell.Handler = m_View.ViewModel.GetNodeTypeHandler(node);
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
        /// Returns a string representation of this <see cref="HierarchyViewColumn"/>.
        /// </summary>
        /// <returns>A string representation of this column.</returns>
        public override string ToString()
        {
            return Descriptor.ToString();
        }
    }
}
