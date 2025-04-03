// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class BackgroundRepeatStyleField : BaseField<BackgroundRepeat>
    {
        public class BackgroundRepeatConverter : UxmlAttributeConverter<BackgroundRepeat>
        {
            public override BackgroundRepeat FromString(string value) => throw new NotImplementedException();
            public override string ToString(BackgroundRepeat value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<BackgroundRepeat>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<BackgroundRepeat>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new BackgroundRepeatStyleField();
        }

        static readonly string s_FieldClassName = "unity-background-repeat-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/BackgroundRepeatStyleField.uxml";
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/BackgroundRepeatStyleField.uss";

        public static readonly string s_BackgroundRepeatXFieldName = "repeatx";
        public static readonly string s_BackgroundRepeatYFieldName = "repeaty";

        ToggleButtonGroup m_BackgroundRepeatXField;
        ToggleButtonGroup m_BackgroundRepeatYField;

        static readonly int[] k_RepeatButtonMapping = { 0, 3, 2, 1 };
        static readonly int[] k_ButtonRepeatMapping = { 0, 1, 2, 3 };

        public BackgroundRepeatStyleField() : this(null) { }

        public BackgroundRepeatStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_BackgroundRepeatXField = this.Q<ToggleButtonGroup>(s_BackgroundRepeatXFieldName);
            m_BackgroundRepeatYField = this.Q<ToggleButtonGroup>(s_BackgroundRepeatYFieldName);

            m_BackgroundRepeatXField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundRepeatField();
                e.StopPropagation();
            });
            m_BackgroundRepeatYField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundRepeatField();
                e.StopPropagation();
            });

            var fieldTooltip = "Controls how a background image is repeated within an element.";
            m_BackgroundRepeatXField.labelElement.tooltip = fieldTooltip;
            m_BackgroundRepeatYField.labelElement.tooltip = fieldTooltip;

            var iconImageNoRepeat = BuilderInspectorUtilities.LoadIcon("repeatBG_OFF", "Background/");
            var iconImageRepeat = BuilderInspectorUtilities.LoadIcon("repeatBG_ON", "Background/");
            var iconImageRound = BuilderInspectorUtilities.LoadIcon("repeatBG_ROUND", "Background/");
            var iconImageSpace = BuilderInspectorUtilities.LoadIcon("repeatBG_SPACE", "Background/");

            BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                "BackgroundRepeat", Repeat.NoRepeat.ToString()), out var styleValueTooltipNoRepeat);
            BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                "BackgroundRepeat", Repeat.Repeat.ToString()), out var styleValueTooltipRepeat);
            BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                "BackgroundRepeat", Repeat.Round.ToString()), out var styleValueTooltipRound);
            BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                "BackgroundRepeat", Repeat.Space.ToString()), out var styleValueTooltipSpace);

            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageNoRepeat, tooltip = styleValueTooltipNoRepeat });
            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageSpace, tooltip = styleValueTooltipSpace });
            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageRound, tooltip = styleValueTooltipRound });
            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageRepeat, tooltip = styleValueTooltipRepeat });

            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageNoRepeat, tooltip = styleValueTooltipNoRepeat });
            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageSpace, tooltip = styleValueTooltipSpace });
            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageRound, tooltip = styleValueTooltipRound });
            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageRepeat, tooltip = styleValueTooltipRepeat });

            value = new BackgroundRepeat();
        }

        public override void SetValueWithoutNotify(BackgroundRepeat newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            var toggleButtonGroupState = new ToggleButtonGroupState(1u << (int)value.x, 4);
            m_BackgroundRepeatXField.SetValueWithoutNotify(toggleButtonGroupState);

            toggleButtonGroupState = new ToggleButtonGroupState(1u << (int)value.y, 4);
            m_BackgroundRepeatYField.SetValueWithoutNotify(toggleButtonGroupState);
        }

        void UpdateBackgroundRepeatField()
        {
            var valueX = m_BackgroundRepeatXField.value;
            var selectedX = valueX.GetActiveOptions(stackalloc int[valueX.length]);

            var valueY = m_BackgroundRepeatYField.value;
            var selectedY = valueY.GetActiveOptions(stackalloc int[valueY.length]);

            int intX = selectedX[0];
            int intY = selectedY[0];

            value = new BackgroundRepeat((Repeat)k_ButtonRepeatMapping[intX], (Repeat)k_ButtonRepeatMapping[intY]);
        }
    }
}
