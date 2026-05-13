// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UsedImplicitly]
    [MovedFrom("Unity.UI.Builder")]
    [UxmlElement]
    internal partial class TextAutoSizeStyleField : BaseField<TextAutoSize>
    {
        static readonly string s_FieldClassName = "unity-text-auto-size-style-field";
        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/TextAutoSizeStyleField.uxml";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/Controls/TextAutoSizeStyleField.uss";

        static readonly string s_TextAutoSizeFieldName = "text-auto-size-field";
        static readonly string s_ToggleFieldName = "text-auto-size-toggle";
        static readonly string s_MinSizeFieldName = s_TextAutoSizeFieldName + "-min-size";
        static readonly string s_MaxSizeFieldName = s_TextAutoSizeFieldName + "-max-size";

        public bool IsBestFit => value.mode == TextAutoSizeMode.BestFit;

        Toggle m_AutoSizeToggle;
        StyleLengthField m_MinSizeField;
        StyleLengthField m_MaxSizeField;

        public TextAutoSizeStyleField() : this(null) {}

        public TextAutoSizeStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            var vta = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            vta.CloneTree(this);

            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);

            m_AutoSizeToggle = this.Q<Toggle>(s_ToggleFieldName);
            m_MinSizeField = this.Q<StyleLengthField>(s_MinSizeFieldName);
            m_MaxSizeField = this.Q<StyleLengthField>(s_MaxSizeFieldName);

            // Remove "initial" keyword from the options popup
            m_MinSizeField.RegisterCallback<AttachToPanelEvent>(e =>
            {
                m_MinSizeField.schedule.Execute(() => RemoveInitialKeywordFromField(m_MinSizeField));
            });
            m_MaxSizeField.RegisterCallback<AttachToPanelEvent>(e =>
            {
                m_MaxSizeField.schedule.Execute(() => RemoveInitialKeywordFromField(m_MaxSizeField));
            });

            m_AutoSizeToggle.RegisterValueChangedCallback(e =>
            {
                UpdateTextAutoSizeField();
                UpdateIntegerFieldsVisibility(e.newValue ? TextAutoSizeMode.BestFit : TextAutoSizeMode.None);
                e.StopPropagation();
            });
            m_MinSizeField.RegisterValueChangedCallback(e =>
            {
                m_MinSizeField.value = Mathf.Clamp(m_MinSizeField.value.value.value, 0, m_MaxSizeField.value.value.value);
                UpdateTextAutoSizeField();
                e.StopPropagation();
            });
            m_MaxSizeField.RegisterValueChangedCallback(e =>
            {
                m_MaxSizeField.value = Mathf.Max(m_MinSizeField.valueField.value.value, m_MaxSizeField.value.value.value);
                UpdateTextAutoSizeField();
                e.StopPropagation();
            });

            // Initialize visibility based on initial mode
            UpdateIntegerFieldsVisibility(TextAutoSizeMode.None);
        }

        void RemoveInitialKeywordFromField(StyleLengthField field)
        {
            var lengthField = field.valueField;
            var popup = lengthField.optionsPopup;
            if (popup != null && popup.choices.Contains("initial"))
            {
                popup.choices.Remove("initial");
            }
        }

        public override void SetValueWithoutNotify(TextAutoSize newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            m_AutoSizeToggle.SetValueWithoutNotify(value.mode != TextAutoSizeMode.None);
            m_MinSizeField.SetValueWithoutNotify(value.minSize);
            m_MaxSizeField.SetValueWithoutNotify(value.maxSize);
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
                minSize = m_MinSizeField.value.value,
                maxSize = m_MaxSizeField.value.value,
            };
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void UpdateSubFieldVisualInputTooltips(string minSizeTooltip, string maxSizeTooltip)
        {
            m_MinSizeField.visualInput.tooltip = minSizeTooltip;
            m_MaxSizeField.visualInput.tooltip = maxSizeTooltip;
        }
    }
}
