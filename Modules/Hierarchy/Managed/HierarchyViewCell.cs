// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Cell Container for a value displayed in a HierarchyColumn. By default all cells that are displaying a default value won't have their
    /// UI/editor be displayed except when their corresponding row is selected or mouse is hovered over the row.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal sealed class HierarchyViewCell : VisualElement
    {
        bool m_IsCellBound;
        bool m_IsDefaultValue;

        /// <summary>
        /// Parent Column of this Cell.
        /// </summary>
        public readonly HierarchyViewColumn Column;

        /// <summary>
        /// HierarchyView owning this cell.
        /// </summary>
        public readonly HierarchyView View;

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
        /// NodeHandler of the Node displayed in this column.
        /// </summary>
        public HierarchyNodeTypeHandler Handler { get; internal set; }

        /// <summary>
        /// Node corresponding to this Cell.
        /// </summary>
        public HierarchyNode Node { get; internal set; }

        /// <summary>
        /// Node Index in the HierarchyViewModel of the Node of this Cell.
        /// </summary>
        public int NodeIndex { get; internal set; }

        /// <summary>
        /// Cell descriptor that was used to create this Cell.
        /// </summary>
        public HierarchyViewCellDescriptor Descriptor { get; internal set; }
        #endregion

        /// <summary>
        /// User Data that can be bound on that Cell. Usually a SerializedObject that acts as the Model of the Cell.
        /// </summary>
        public object BoundObject { get; set; }

        /// <summary>
        /// Is the Cell showing a default value.
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
            name = "HierarchyViewCell";
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
