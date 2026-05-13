// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using BackgroundRepeat = UnityEngine.UIElements.BackgroundRepeat;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBackgroundRepeat.
    /// </summary>
    [UxmlElement]
    internal partial class StyleBackgroundRepeatField : StylePropertyField<StyleBackgroundRepeat, BackgroundRepeatStyleField, BackgroundRepeat>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-background-repeat-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBackgroundRepeatField() : this(null) {}

        public StyleBackgroundRepeatField(string label) : base(label, new BackgroundRepeatStyleField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            // If label is null, remove the labelElement added with the affordance
            if (Contains(labelElement) && string.IsNullOrEmpty(labelElement.text))
            {
                AddToClassList(noLabelVariantUssClassName);
                labelElement.RemoveFromHierarchy();
            }
        }

        protected override BackgroundRepeatStyleField CreateValueField()
        {
            return new BackgroundRepeatStyleField();
        }

        protected override StyleBackgroundRepeat CreateStyleValue(BackgroundRepeat v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleBackgroundRepeat v)
        {
            return value == v;
        }
    }
}
