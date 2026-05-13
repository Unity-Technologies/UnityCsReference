// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

    /// <summary>
    /// Makes a field for entering TextShadow Offset.
    /// </summary>
    [UxmlElement]
    internal partial class TextShadowOffsetField : BaseField<Vector2>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-text-shadow-offset-field";

        StyleLengthField m_OffsetXField;
        StyleLengthField m_OffsetYField;

        public StyleLengthField offsetXField => m_OffsetXField;
        public StyleLengthField offsetYField => m_OffsetYField;

        public TextShadowOffsetField() : this(null) { }

        public TextShadowOffsetField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);

            m_OffsetXField = new StyleLengthField("Horizontal") { tooltip = "Move text shadow horizontally (left and right)", containsAffordance = false };
            m_OffsetYField = new StyleLengthField("Vertical") { tooltip= "Move text shadow vertically (up and down)", containsAffordance = false };

            Add(m_OffsetXField);
            Add(m_OffsetYField);

            m_OffsetXField.AddValidation(new Syntax("length"));
            m_OffsetYField.AddValidation(new Syntax("length"));

            m_OffsetXField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.x)
                {
                    var newVal = value;
                    newVal.x = e.newValue.value.value;
                    value = newVal;
                }
            });

            m_OffsetYField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.y)
                {
                    var newVal = value;
                    newVal.y = e.newValue.value.value;
                    value = newVal;
                }
            });
        }

        public override void SetValueWithoutNotify(Vector2 value)
        {
            base.SetValueWithoutNotify(value);
            m_OffsetXField.SetValueWithoutNotify(value.x);
            m_OffsetYField.SetValueWithoutNotify(value.y);
        }
    }

