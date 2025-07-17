// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Attribute used to register a new Hierarchy column. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class HierarchyViewColumnDescriptorAttribute : Attribute
    {
        /// <summary>
        /// Column Id. Must be unique in the HiearchyWindow.
        /// </summary>
        public readonly string ColumnId;

        /// <summary>
        /// Attribute constructor.
        /// </summary>
        /// <param name="columnId">Column Id. Must be unique in the HiearchyWindow.</param>
        public HierarchyViewColumnDescriptorAttribute(string columnId)
        {
            ColumnId = columnId;
        }

        /// <summary>
        /// Required signature for the function decorated by [HierarchyColumnDescriptor]. This function can be used to populate the columnDescriptor
        /// </summary>
        /// <param name="columnDesc"></param>
        [RequiredSignature] public static void CreateDescriptor(HierarchyViewColumnDescriptor columnDesc) { }
    }
}
