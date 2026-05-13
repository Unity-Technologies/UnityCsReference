// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleColor.
    /// </summary>
    [UxmlElement]
    internal partial class StyleColorField : StylePropertyField<StyleColor, ColorField, Color>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-color-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleColorField() : this(null) {}

        public StyleColorField(string label) : base(label, new ColorField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override ColorField CreateValueField()
        {
            return new ColorField();
        }

        protected override StyleColor CreateStyleValue(Color v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleColor v)
        {
            return value == v;
        }
    }
}
