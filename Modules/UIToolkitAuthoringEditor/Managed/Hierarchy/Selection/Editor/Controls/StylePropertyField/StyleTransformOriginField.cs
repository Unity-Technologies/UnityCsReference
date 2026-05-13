// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleTransformOrigin.
    /// </summary>
    [UxmlElement]
    internal partial class StyleTransformOriginField : StylePropertyField<StyleTransformOrigin, TransformOriginStyleField, TransformOrigin>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-transform-origin-field";
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
        public StyleTransformOriginField()
            : base(null, new TransformOriginStyleField()) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleTransformOriginField(string label)
            : base(label, new TransformOriginStyleField(label))
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override TransformOriginStyleField CreateValueField()
        {
            return new TransformOriginStyleField();
        }

        protected override StyleTransformOrigin CreateStyleValue(TransformOrigin v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleTransformOrigin v)
        {
            return value == v;
        }
    }
}
