// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleTranslate.
    /// </summary>
    [UxmlElement]
    internal partial class StyleTranslateField : StylePropertyField<StyleTranslate, TranslateField, Translate>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-translate-field";
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
        public StyleTranslateField() : this(null) {}

        TranslateField m_TranslateField;

        public TranslateField translateField => m_TranslateField;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public StyleTranslateField(string label) : base(label, new TranslateField())
        {
            m_TranslateField = visualInput as TranslateField;
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override TranslateField CreateValueField()
        {
            return new TranslateField();
        }

        protected override StyleTranslate CreateStyleValue(Translate v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleTranslate v)
        {
            return value == v;
        }
    }
}
