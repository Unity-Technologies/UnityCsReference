// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleBoxShadow.
    /// </summary>
    [UxmlElement]
    internal partial class StyleBoxShadowField : StylePropertyField<StyleBoxShadow, BoxShadowField, BoxShadow>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-box-shadow-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleBoxShadowField()
            : this(null) { }

        public StyleBoxShadowField(string label)
            : base(label, new BoxShadowField())
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

        protected override BoxShadowField CreateValueField()
        {
            return new BoxShadowField();
        }

        protected override StyleBoxShadow CreateStyleValue(BoxShadow v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleBoxShadow v)
        {
            return value == v;
        }
    }
}
