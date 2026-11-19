// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Specifies that a method creates a <see cref="HierarchyViewCellDescriptor"/> for a specific <see cref="HierarchyViewColumn"/>. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HierarchyViewCellDescriptorAttribute : Attribute
    {
        /// <summary>
        /// The column identifier or column header title to register this cell to.
        /// </summary>
        public string ColumnHint { get; }

        /// <summary>
        /// Gets the <see cref="HierarchyNodeTypeHandler"/> type used to manage the nodes for this cell. Only one cell descriptor per handler type can be registered in a column.
        /// </summary>
        public Type Handler { get; }

        /// <summary>
        /// Creates a new <see cref="HierarchyViewCellDescriptorAttribute"/>.
        /// </summary>
        /// <param name="columnHint">The column identifier or column header title to register the cell descriptor to.</param>
        /// <param name="handler">The <see cref="HierarchyNodeTypeHandler"/> type used to manage the nodes for this cell. If null, the cell manipulates <see cref="HierarchyNode"/> instances directly.</param>
        public HierarchyViewCellDescriptorAttribute(string columnHint, Type handler = null)
        {
            ColumnHint = columnHint;
            Handler = handler;
        }

        /// <summary>
        /// Defines the required signature for methods decorated with <see cref="HierarchyViewCellDescriptorAttribute"/>. Implement this method to populate the <see cref="HierarchyViewCellDescriptor"/>.
        /// </summary>
        /// <param name="cell">The <see cref="HierarchyViewCellDescriptor"/> to populate.</param>
        [RequiredSignature] public static void CustomizeDescriptor(HierarchyViewCellDescriptor cell) { }
    }
}
