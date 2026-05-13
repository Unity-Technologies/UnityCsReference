// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using BackgroundPosition = UnityEngine.UIElements.BackgroundPosition;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackgroundPosition.
    /// </summary>
    [UxmlElement]
    internal partial class StyleBackgroundPositionField : StylePropertyField<StyleBackgroundPosition, BackgroundPositionStyleField, BackgroundPosition>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-position-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBackgroundPositionField() : this(null) {}

        public StyleBackgroundPositionField(string label) : base(label, new BackgroundPositionStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override BackgroundPositionStyleField CreateValueField()
        {
            return new BackgroundPositionStyleField();
        }

        protected override StyleBackgroundPosition CreateStyleValue(BackgroundPosition v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleBackgroundPosition v)
        {
            return value == v;
        }
    }
}
