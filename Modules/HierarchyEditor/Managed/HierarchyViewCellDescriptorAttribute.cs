// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Attribute used to register a new Cell Descriptor for a specific Column.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HierarchyViewCellDescriptorAttribute : Attribute
    {
        /// <summary>
        /// Column Id or Column header title to register the Cell to.
        /// </summary>
        public string ColumnHint { get; }

        /// <summary>
        /// NodeHandler used to manage the Nodes corresponding to this Cell. Only a single Cell per NodeHandler can be registered in a Column.
        /// </summary>
        public Type Handler { get; }

        /// <summary>
        /// Attribute constructor.
        /// </summary>
        /// <param name="columnHint">Column Id or Column header title to register the Cell to.</param>
        /// <param name="handler">NodeHandler used to manage the Nodes corresponding to this Cell. If Null we assume the Cell will manipulate pure HierarchyNode.</param>
        public HierarchyViewCellDescriptorAttribute(string columnHint, Type handler = null)
        {
            ColumnHint = columnHint;
            Handler = handler;
        }

        /// <summary>
        /// Required signature for the function decorated by [HierarchyViewCellDescriptor]. This function can be used to populate the CellDescriptor.
        /// </summary>
        /// <param name="cell">Cell Descriptor to populate.</param>
        [RequiredSignature] public static void CustomizeDescriptor(HierarchyViewCellDescriptor cell) { }
    }
}
