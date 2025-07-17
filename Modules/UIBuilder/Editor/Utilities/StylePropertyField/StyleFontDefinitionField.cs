// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.WSA.Cursor;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Makes a style field for editing a StyleFontDefinition.
    /// </summary>
    internal class StyleFontDefinitionField : StylePropertyField<StyleFontDefinition, FontDefinitionField, FontDefinition>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleFontDefinition, FontDefinitionField, FontDefinition>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleFontDefinition, FontDefinitionField, FontDefinition>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new StyleFontDefinitionField();
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-font-definition-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleFontDefinitionField() : this(null) {}

        public StyleFontDefinitionField(string label) : base(label, new FontDefinitionField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }
    }
}
