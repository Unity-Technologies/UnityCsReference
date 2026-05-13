// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleFloat through a percent slider.
    /// </summary>
    [UxmlElement]
    internal partial class StylePercentSliderField : StylePropertyField<StyleFloat, PercentSlider, float>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-percent-slider-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StylePercentSliderField() : this(null) {}

        public StylePercentSliderField(string label) : base(label, new PercentSlider())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override PercentSlider CreateValueField()
        {
            return new PercentSlider();
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
