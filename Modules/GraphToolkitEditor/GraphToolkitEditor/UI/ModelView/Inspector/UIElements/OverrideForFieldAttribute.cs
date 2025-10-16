// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Links a bool field to another field. They will appear in the <see cref="SerializedFieldsInspector"/> in the same row.
    /// The semantic is that the field given by <see cref="FieldName"/> is used only if the field with this attribute is true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UnityRestricted]
    internal class OverrideForFieldAttribute : Attribute
    {
        /// <summary>
        /// The name of the field that this field overrides.
        /// </summary>
        public readonly string FieldName;

        /// <summary>
        /// Creates a new instance of the <see cref="OverrideForFieldAttribute"/> class.
        /// </summary>
        /// <param name="fieldName">The name of the field that this field overrides.</param>
        public OverrideForFieldAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
