// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

    /// <summary>
    /// Makes a field for entering TextShadow.
    /// </summary>
    [UxmlElement]
    internal partial class TextShadowField : BaseField<TextShadow>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-text-shadow-field";

        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/TextShadowField.uxml";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/Controls/TextShadowField.uss";

        ColorField m_ColorField;
        StyleLengthField m_BlurRadiusField;
        TextShadowOffsetField m_OffsetField;

        public ColorField colorField => m_ColorField;
        public StyleLengthField blurRadiusField => m_BlurRadiusField;
        public TextShadowOffsetField offsetField => m_OffsetField;

        public TextShadowField() : this(null) { }

        public TextShadowField(string label)
            : base(label)
        {
            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);
            var template = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            AddToClassList(ussClassName);

            m_ColorField = this.Q<ColorField>("text-shadow-field-color");
            m_BlurRadiusField = this.Q<StyleLengthField>("text-shadow-field-blur-radius");
            m_OffsetField = this.Q<TextShadowOffsetField>("text-shadow-field-offset");

            m_ColorField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.color)
                {
                    var newVal = value;
                    newVal.color = e.newValue;
                    value = newVal;
                }
            });

            m_BlurRadiusField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.blurRadius)
                {
                    var newVal = value;
                    newVal.blurRadius = e.newValue.value.value;
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
        }

        public override void SetValueWithoutNotify(TextShadow textShadow)
        {
            base.SetValueWithoutNotify(textShadow);
            m_ColorField.SetValueWithoutNotify(value.color);
            m_BlurRadiusField.SetValueWithoutNotify(value.blurRadius);
            m_OffsetField.SetValueWithoutNotify(value.offset);
        }
    }

