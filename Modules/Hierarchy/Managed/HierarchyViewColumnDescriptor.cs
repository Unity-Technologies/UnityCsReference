// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Column Descriptor used to register a new Column in the Hierarchy and to control display and customization of this column.
    /// </summary>
    internal sealed class HierarchyViewColumnDescriptor
    {
        bool m_IsBound;

        /// <summary>
        /// Column Id. HierarchyViewCellDescriptor will use this Id to register themselves with the Column.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// Display name of the Column. If null the header will be empty (unless it contains an icon).
        /// </summary>
        public string Title;

        /// <summary>
        /// Icon displayed in the header of the column. 
        /// </summary>
        public Texture2D Icon;

        /// <summary>
        /// Tooltip shown when the user hover the header of this column.
        /// </summary>
        public string Tooltip;

        /// <summary>
        /// Priority used to sort column order.
        /// Priority of 0 corresponds to the Hierarchy Name column.
        /// Negative priority will put the column the the left of the Name column.
        /// Positive priority will put the column to the right of the Name default column.
        /// </summary>
        public int DefaultPriority;

        /// <summary>
        /// Default Width when first instantiating the column. If negative the columns will be assigned an arbitrary width.
        /// </summary>
        public int DefaultWidth = -1;

        /// <summary>
        /// Define if the column should initially be visible.
        /// </summary>
        public bool DefaultVisibility;

        /// <summary>
        /// User data
        /// </summary>
        public object UserData;

        /// <summary>
        /// Callback allowing the user to create a custom header. If callback is null, default header will be used to display both the icon and text of the Name field.
        /// </summary>
        public Func<VisualElement> MakeHeader;

        /// <summary>
        /// Callback allowing the user to bind a custom header.
        /// </summary>
        public Action<VisualElement, HierarchyView> BindHeader;

        /// <summary>
        /// Callback triggered when the header is about to be destroyed.
        /// </summary>
        public Action<VisualElement, HierarchyView> UnbindHeader;

        /// <summary>
        /// Callback triggered when the header is about to be destroyed.
        /// </summary>
        public Action<VisualElement, HierarchyView> DestroyHeader;

        /// <summary>
        /// Callback triggered when the column is built and shown in the View.
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
        /// Create a new Column Descriptor.
        /// </summary>
        /// <param name="columnId">Unique ID of this column descriptor.</param>
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
