// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleInt.
    /// </summary>
    [UxmlElement]
    internal partial class StyleIntField : StylePropertyField<StyleInt, IntegerField, int>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-int-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleIntField() : this(null) {}

        public StyleIntField(string label) : base(label, new IntegerField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override IntegerField CreateValueField()
        {
            return new IntegerField();
        }

        protected override StyleInt CreateStyleValue(int v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleInt v)
        {
            return value == v;
        }
    }
}
