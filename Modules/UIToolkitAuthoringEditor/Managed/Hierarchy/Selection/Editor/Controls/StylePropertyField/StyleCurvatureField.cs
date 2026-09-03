// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleCurvature.
    /// </summary>
    [UxmlElement]
    internal partial class StyleCurvatureField : StylePropertyField<StyleCurvature, CurvatureStyleField, Curvature>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-curvature-field";
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
        public StyleCurvatureField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleCurvatureField(string label) : base(label, new CurvatureStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override CurvatureStyleField CreateValueField()
        {
            return new CurvatureStyleField();
        }

        protected override StyleCurvature CreateStyleValue(Curvature v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleCurvature v)
        {
            return value == v;
        }
    }
}
