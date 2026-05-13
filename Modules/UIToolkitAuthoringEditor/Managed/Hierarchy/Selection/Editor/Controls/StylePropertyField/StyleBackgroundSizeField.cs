// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using BackgroundSize = UnityEngine.UIElements.BackgroundSize;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackgroundSize.
    /// </summary>
    [UxmlElement]
    internal partial class StyleBackgroundSizeField : StylePropertyField<StyleBackgroundSize, BackgroundSizeStyleField, BackgroundSize>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-size-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBackgroundSizeField() : this(null) {}

        public StyleBackgroundSizeField(string label) : base(label, new BackgroundSizeStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override BackgroundSizeStyleField CreateValueField()
        {
            return new BackgroundSizeStyleField();
        }

        protected override StyleBackgroundSize CreateStyleValue(BackgroundSize v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleBackgroundSize v)
        {
            return value == v;
        }
    }
}
