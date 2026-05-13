// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleFloat.
    /// </summary>
    [UxmlElement]
    internal partial class StyleFloatField : StylePropertyField<StyleFloat, FloatField, float>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-float-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleFloatField() : this(null) {}

        public StyleFloatField(string label) : base(label, new FloatField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override FloatField CreateValueField()
        {
            return new FloatField();
        }

        protected override StyleFloat CreateStyleValue(float v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleFloat v)
        {
            return value == v;
        }
    }
}
