// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleRotate.
    /// </summary>
    [UxmlElement]
    internal partial class StyleRotateField : StylePropertyField<StyleRotate, RotateField, Rotate>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-rotate-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Constructor.
        /// </summary>
        public StyleRotateField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleRotateField(string label) : base(label, new RotateField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            var dragger = new FieldMouseDragger<Angle>(valueField.angleField);
            dragger.SetDragZone(labelElement);
            labelElement.EnableInClassList(labelDraggerVariantUssClassName, true);
        }

        protected override RotateField CreateValueField()
        {
            return new RotateField();
        }

        protected override StyleRotate CreateStyleValue(Rotate v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleRotate v)
        {
            return value == v;
        }
    }
}
