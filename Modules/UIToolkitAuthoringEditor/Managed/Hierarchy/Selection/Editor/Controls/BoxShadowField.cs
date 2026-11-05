// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

    /// <summary>
    /// Makes a field for entering BoxShadow.
    /// </summary>
    [UxmlElement]
    internal partial class BoxShadowField : BaseField<BoxShadow>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-box-shadow-field";

        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/BoxShadowField.uxml";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/Controls/TextShadowField.uss";

        ColorField m_ColorField;
        TextShadowOffsetField m_OffsetField;
        StyleLengthField m_BlurRadiusField;
        StyleLengthField m_SpreadRadiusField;
        Toggle m_InsetField;

        public ColorField colorField => m_ColorField;
        public TextShadowOffsetField offsetField => m_OffsetField;
        public StyleLengthField blurRadiusField => m_BlurRadiusField;
        public StyleLengthField spreadRadiusField => m_SpreadRadiusField;
        public Toggle insetField => m_InsetField;

        public BoxShadowField() : this(null) { }

        public BoxShadowField(string label)
            : base(label)
        {
            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);
            var template = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            AddToClassList(ussClassName);

            m_ColorField = this.Q<ColorField>("box-shadow-field-color");
            m_OffsetField = this.Q<TextShadowOffsetField>("box-shadow-field-offset");
            m_BlurRadiusField = this.Q<StyleLengthField>("box-shadow-field-blur-radius");
            m_SpreadRadiusField = this.Q<StyleLengthField>("box-shadow-field-spread-radius");
            m_InsetField = this.Q<Toggle>("box-shadow-field-inset");

            m_OffsetField.offsetXField.tooltip = "Move box shadow horizontally (left and right).";
            m_OffsetField.offsetYField.tooltip = "Move box shadow vertically (up and down).";

            m_ColorField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.color)
                {
                    var newVal = value;
                    newVal.color = e.newValue;
                    value = newVal;
                }
            });

            m_OffsetField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.offset)
                {
                    var newVal = value;
                    newVal.offset = e.newValue;
                    value = newVal;
                }
            });

            m_BlurRadiusField.RegisterValueChangedCallback(e =>
            {
                if (!IsPixelLength(e.newValue))
                {
                    m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius);
                    return;
                }

                // The blur radius can't be negative. The field is reset even when the value doesn't change.
                var blurRadius = Mathf.Max(0, e.newValue.value.value);
                if (blurRadius != e.newValue.value.value)
                    m_BlurRadiusField.SetValueWithoutNotify(blurRadius);
                if (blurRadius != value.blurRadius)
                {
                    var newVal = value;
                    newVal.blurRadius = blurRadius;
                    value = newVal;
                }
            });

            m_SpreadRadiusField.RegisterValueChangedCallback(e =>
            {
                if (!IsPixelLength(e.newValue))
                {
                    m_SpreadRadiusField.SetValueWithoutNotify(value.spreadRadius);
                    return;
                }

                if (e.newValue != value.spreadRadius)
                {
                    var newVal = value;
                    newVal.spreadRadius = e.newValue.value.value;
                    value = newVal;
                }
            });

            m_InsetField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.inset)
                {
                    var newVal = value;
                    newVal.inset = e.newValue;
                    value = newVal;
                }
            });
        }

        // box-shadow radii are in pixels: percentages and keywords like auto aren't supported
        static bool IsPixelLength(StyleLength length)
        {
            return length.keyword == StyleKeyword.Undefined && length.value.unit == LengthUnit.Pixel;
        }

        public override void SetValueWithoutNotify(BoxShadow boxShadow)
        {
            base.SetValueWithoutNotify(boxShadow);
            m_ColorField.SetValueWithoutNotify(value.color);
            m_OffsetField.SetValueWithoutNotify(value.offset);
            m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius);
            m_SpreadRadiusField.SetValueWithoutNotify(value.spreadRadius);
            m_InsetField.SetValueWithoutNotify(value.inset);
        }
    }
