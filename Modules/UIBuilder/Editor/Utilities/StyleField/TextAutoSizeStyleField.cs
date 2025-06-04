// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class TextAutoSizeStyleField : BaseField<TextAutoSize>
    {
        public class TextAutoSizeStyleFieldConverter : UxmlAttributeConverter<TextAutoSize>
        {
            public override TextAutoSize FromString(string value) => throw new NotImplementedException();
            public override string ToString(TextAutoSize value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<TextAutoSize>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TextAutoSize>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextAutoSizeStyleField();
        }

        static readonly string s_FieldClassName = "unity-text-auto-size-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/TextAutoSizeStyleField.uxml";
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/TextAutoSizeStyleField.uss";

        static readonly string s_TextAutoSizeFieldName = "text-auto-size-field";
        static readonly string s_ToggleFieldName = "text-auto-size-toggle";
        static readonly string s_MinSizeFieldName = s_TextAutoSizeFieldName + "-min-size";
        static readonly string s_MaxSizeFieldName = s_TextAutoSizeFieldName + "-max-size";

        Toggle m_AutoSizeToggle;
        DimensionStyleField m_MinSizeField;
        DimensionStyleField m_MaxSizeField;

        public TextAutoSizeStyleField() : this(null) {}

        public TextAutoSizeStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_AutoSizeToggle = this.Q<Toggle>(s_ToggleFieldName);
            m_MinSizeField = this.Q<DimensionStyleField>(s_MinSizeFieldName);
            m_MaxSizeField = this.Q<DimensionStyleField>(s_MaxSizeFieldName);

            m_AutoSizeToggle.RegisterValueChangedCallback(e =>
            {
                UpdateTextAutoSizeField();
                UpdateIntegerFieldsVisibility(e.newValue ? TextAutoSizeMode.BestFit : TextAutoSizeMode.None);
                e.StopPropagation();
            });
            m_MinSizeField.RegisterValueChangedCallback(e =>
            {
                UpdateTextAutoSizeField();
                e.StopPropagation();
            });
            m_MaxSizeField.RegisterValueChangedCallback(e =>
            {
                UpdateTextAutoSizeField();
                e.StopPropagation();
            });

            // Initialize visibility based on initial mode
            UpdateIntegerFieldsVisibility(TextAutoSizeMode.None);
        }

        public override void SetValueWithoutNotify(TextAutoSize newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_AutoSizeToggle.SetValueWithoutNotify(value.mode != TextAutoSizeMode.None);
            m_MinSizeField.SetValueWithoutNotify(value.minSize.ToString());
            m_MaxSizeField.SetValueWithoutNotify(value.maxSize.ToString());
            UpdateIntegerFieldsVisibility(value.mode);
        }

        void UpdateIntegerFieldsVisibility(TextAutoSizeMode mode)
        {
            var displayStyle = mode == TextAutoSizeMode.None ? DisplayStyle.None : DisplayStyle.Flex;
            m_MinSizeField.style.display = displayStyle;
            m_MaxSizeField.style.display = displayStyle;
        }

        void UpdateTextAutoSizeField()
        {
            TextAutoSizeMode newMode = m_AutoSizeToggle.value ? TextAutoSizeMode.BestFit : TextAutoSizeMode.None;

            value = new TextAutoSize
            {
                mode = newMode,
                minSize = m_MinSizeField.length,
                maxSize = m_MaxSizeField.length,
            };
        }

        internal void UpdateSubFieldVisualInputTooltips(string minSizeTooltip, string maxSizeTooltip)
        {
            m_MinSizeField.visualInput.tooltip = minSizeTooltip;
            m_MaxSizeField.visualInput.tooltip = maxSizeTooltip;
        }
    }
}
