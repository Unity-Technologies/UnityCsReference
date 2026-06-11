// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a cell container for a value displayed in a <see cref="HierarchyViewColumn"/>. By default, all cells that display a default value don't display their
    /// UI or editor unless their corresponding row is selected or the mouse hovers over the row.
    /// </summary>
    public sealed class HierarchyViewCell : VisualElement
    {
        static readonly UniqueStyleString k_HierarchyViewCellName = new("HierarchyViewCell");

        bool m_IsCellBound;
        bool m_IsDefaultValue;

        /// <summary>
        /// Gets the parent column of this <see cref="HierarchyViewCell"/>.
        /// </summary>
        public HierarchyViewColumn Column { get; }

        /// <summary>
        /// Gets the <see cref="HierarchyView"/> that owns this cell.
        /// </summary>
        public HierarchyView View { get; }

        internal void BindCell()
        {
            if (m_IsCellBound)
                return;

            Descriptor?.BindCell?.Invoke(this);
            m_IsCellBound = true;
        }

        internal void UnbindCell()
        {
            if (!m_IsCellBound)
                return;

            if (Descriptor != null)
            {
                Descriptor?.UnbindCell?.Invoke(this);
                if (Descriptor.ClearCellContent)
                {
                    // Clear any custom ui that might have been added by the Cell Handler.
                    Clear();
                }
            }

            BoundObject = null;
            Node = HierarchyNode.Null;
            NodeIndex = -1;
            Handler = null;
            Descriptor = null;
            m_IsCellBound = false;
        }

        #region Set During Bind
        /// <summary>
        /// Gets the <see cref="HierarchyNodeTypeHandler"/> of the node displayed in this cell.
        /// </summary>
        public HierarchyNodeTypeHandler Handler { get; internal set; }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> that corresponds to this cell.
        /// </summary>
        public HierarchyNode Node { get; internal set; }

        /// <summary>
        /// Gets the index of the<see cref="HierarchyNode"/> of this cell in the <see cref="HierarchyViewModel"/>.
        /// </summary>
        public int NodeIndex { get; internal set; }

        /// <summary>
        /// Gets the <see cref="HierarchyViewCellDescriptor"/> used to create this cell.
        /// </summary>
        public HierarchyViewCellDescriptor Descriptor { get; internal set; }
        #endregion

        /// <summary>
        /// Gets or sets user data bound to this cell. Typically a serialized object that acts as the model for the cell.
        /// </summary>
        public object BoundObject { get; set; }

        /// <summary>
        /// Whether the cell shows a default value.
        /// </summary>
        public bool IsDefaultValue
        {
            get => m_IsDefaultValue;
            set
            {
                if (value)
                {
                    RemoveFromClassList(HierarchyViewColumn.k_NonDefaultValue);
                }
                else
                {
                    AddToClassList(HierarchyViewColumn.k_NonDefaultValue);
                }
                m_IsDefaultValue = value;
            }
        }

        internal HierarchyViewCell(HierarchyView view, HierarchyViewColumn column)
        {
            View = view;
            Column = column;
            SetName(k_HierarchyViewCellName);
        }

        public override string ToString()
        {
            var s = Column.Descriptor.Id;
            if (Descriptor != null)
            {
                return Descriptor.ToString();
            }
            else
            {
                return $"{Column.Descriptor} - NoCellDesc";
            }
        }
    }
}
