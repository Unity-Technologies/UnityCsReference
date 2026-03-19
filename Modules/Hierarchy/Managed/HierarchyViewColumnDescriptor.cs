// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides a column descriptor used to register a new column in the <see cref="HierarchyView"/> and to control the display and customization of this column.
    /// </summary>
    public sealed class HierarchyViewColumnDescriptor
    {
        bool m_IsBound;

        /// <summary>
        /// Gets the column identifier. <see cref="HierarchyViewCellDescriptor"/> instances use this identifier to register themselves with the column.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets or sets the display name of the column. If null, the header is empty unless it contains an icon.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the icon displayed in the header of the column.
        /// </summary>
        public Texture2D Icon { get; set; }

        /// <summary>
        /// Gets or sets the tooltip shown when the user hovers over the header of this column.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// Gets or sets the priority used to sort column order.
        /// A priority of 0 corresponds to the Name column in the <see cref="Hierarchy"/>.
        /// A negative priority places the column to the left of the Name column.
        /// A positive priority places the column to the right of the default Name column.
        /// </summary>
        public int DefaultPriority { get; set; }

        /// <summary>
        /// Gets or sets the default width when first instantiating the column. If you set it to a negative value, the column is assigned an arbitrary width.
        /// </summary>
        public int DefaultWidth { get; set; } = -1;

        /// <summary>
        /// Gets or sets whether the column is initially visible.
        /// </summary>
        public bool DefaultVisibility { get; set; }

        /// <summary>
        /// Gets or sets user data associated with this descriptor.
        /// </summary>
        public object UserData { get; set; }

        /// <summary>
        /// Callback that creates a custom header. If null, the default header displays both the icon and text of the Name field.
        /// </summary>
        public Func<VisualElement> MakeHeader;

        /// <summary>
        /// Callback that binds a custom header.
        /// </summary>
        public Action<VisualElement, HierarchyView> BindHeader;

        /// <summary>
        /// Callback triggered when the header is about to be destroyed or after domain reload.
        /// </summary>
        public Action<VisualElement, HierarchyView> UnbindHeader;

        /// <summary>
        /// Callback triggered when the window is closed.
        /// </summary>
        public Action<VisualElement, HierarchyView> DestroyHeader;

        /// <summary>
        /// Callback triggered when the column is built and shown in the <see cref="HierarchyView"/>.
        /// </summary>
        public Action<HierarchyViewColumn, HierarchyView> BindColumn;

        /// <summary>
        /// Callback triggered when the column is about to be destroyed.
        /// </summary>
        public Action<HierarchyViewColumn, HierarchyView> UnbindColumn;

        internal void InvokeBindColumn(HierarchyViewColumn column, HierarchyView view)
        {
            if (m_IsBound)
                return;
            BindColumn?.Invoke(column, view);
            m_IsBound = true;
        }

        internal void InvokeUnbindColumn(HierarchyViewColumn column, HierarchyView view)
        {
            if (!m_IsBound)
                return;
            UnbindColumn?.Invoke(column, view);
            m_IsBound = false;
        }

        /// <summary>
        /// Creates a new <see cref="HierarchyViewColumnDescriptor"/>.
        /// </summary>
        /// <param name="columnId">The unique identifier for this column descriptor.</param>
        public HierarchyViewColumnDescriptor(string columnId)
        {
            Id = columnId;
        }

        public override string ToString()
        {
            return Id;
        }
    }
}
