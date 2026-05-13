// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// A field for editing <see cref="TransformOrigin"/> values with X, Y, and Z components,
    /// including a visual selector for picking preset origin positions.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UxmlElement]
    internal partial class TransformOriginStyleField : BaseField<TransformOrigin>
    {
        static readonly string s_FieldClassName = "unity-transform-origin-style-field";
        static readonly string s_UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/TransformOriginStyleField";

        public static readonly string TransformOriginXFieldName = "x-field";
        public static readonly string TransformOriginYFieldName = "y-field";
        public static readonly string TransformOriginZFieldName = "z-field";
        public static readonly string TransformOriginSelectorFieldName = "selector";

        LengthField m_XField;
        LengthField m_YField;
        FloatField m_ZField;
        TransformOriginSelector m_Selector;

        /// <summary>
        /// Returns the <see cref="LengthField"/> for the X component.
        /// </summary>
        public LengthField xField => m_XField;

        /// <summary>
        /// Returns the <see cref="LengthField"/> for the Y component.
        /// </summary>
        public LengthField yField => m_YField;

        /// <summary>
        /// Returns the <see cref="FloatField"/> for the Z component.
        /// </summary>
        public FloatField zField => m_ZField;

        public TransformOriginStyleField() : this(null) { }

        public TransformOriginStyleField(string label) : base(label, null)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(EditorGUIUtility.Load(s_UssPathNoExt + ".uss") as StyleSheet);
            styleSheets.Add(
                EditorGUIUtility.Load(s_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss") as
                    StyleSheet);

            var selectorContainer = new VisualElement { name = "selector-container" };
            m_Selector = new TransformOriginSelector { name = TransformOriginSelectorFieldName };
            selectorContainer.Add(m_Selector);

            var valueContent = new VisualElement { name = "value-content" };

            m_XField = new LengthField("X") { name = TransformOriginXFieldName };
            m_YField = new LengthField("Y") { name = TransformOriginYFieldName };
            m_ZField = new FloatField("Z") { name = TransformOriginZFieldName };

            valueContent.Add(m_XField);
            valueContent.Add(m_YField);
            valueContent.Add(m_ZField);

            visualInput.Add(selectorContainer);
            visualInput.Add(valueContent);

            m_XField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.x)
                    value = new TransformOrigin(e.newValue, value.y, value.z);
            });

            m_YField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.y)
                    value = new TransformOrigin(value.x, e.newValue, value.z);
            });

            m_ZField.RegisterValueChangedCallback(e =>
            {
                if (e.newValue != value.z)
                    value = new TransformOrigin(value.x, value.y, e.newValue);
            });

            m_Selector.pointSelected = OnPointClicked;

            value = new TransformOrigin(Length.Pixels(0), Length.Pixels(0), 0);
        }

        public override void SetValueWithoutNotify(TransformOrigin newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_XField.SetValueWithoutNotify(value.x);
            m_YField.SetValueWithoutNotify(value.y);
            m_ZField.SetValueWithoutNotify(value.z);
            UpdateSelector();
        }

        void OnPointClicked(float x, float y)
        {
            value = new TransformOrigin(Length.Percent(x * 100), Length.Percent(y * 100), value.z);
        }

        void UpdateSelector()
        {
            m_Selector.originX = GetSelectorPosition(value.x);
            m_Selector.originY = GetSelectorPosition(value.y);
        }

        float GetSelectorPosition(Length length)
        {
            if (length.unit == LengthUnit.Pixel)
                return Mathf.Approximately(length.value, 0) ? 0 : float.NaN;

            return length.value / 100f;
        }
    }
}
