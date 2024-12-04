// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    enum BackgroundPositionMode
    {
        Invalid,
        Horizontal,
        Vertical
    };

    [UsedImplicitly]
    class BackgroundPositionStyleField : BaseField<BackgroundPosition>
    {
        public class BackgroundPositionConverter : UxmlAttributeConverter<BackgroundPosition>
        {
            public override BackgroundPosition FromString(string value) => throw new NotImplementedException();
            public override string ToString(BackgroundPosition value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<BackgroundPosition>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<BackgroundPosition>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(mode), "mode")
                });
            }

            public override object CreateInstance() => new BackgroundPositionStyleField();

#pragma warning disable 649
            [SerializeField] BackgroundPositionMode mode;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags mode_UxmlAttributeFlags;
#pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);
                var e = (BackgroundPositionStyleField)obj;
                if (ShouldWriteAttributeValue(mode_UxmlAttributeFlags))
                {
                    e.mode = mode;
                }
            }
        }

        static readonly string s_FieldClassName = "unity-background-position-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/BackgroundPositionStyleField.uxml";
        public static readonly string s_BackgroundPositionFieldName = "position";
        public static readonly string s_BackgroundPositionAlignFieldName = "align";

        DimensionStyleField m_BackgroundPositionField;
        ToggleButtonGroup m_BackgroundPositionAlign;

        BackgroundPositionMode m_Mode;

        public BackgroundPositionMode mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode == value)
                    return;
                m_Mode = value;

                List<Texture2D> icons = new List<Texture2D>();
                List<string> tooltips = new List<string>();

                if (m_Mode == BackgroundPositionMode.Horizontal)
                {
                    icons.Add(BuilderInspectorUtilities.LoadIcon("background-position-x-left", "Background/"));
                    icons.Add(BuilderInspectorUtilities.LoadIcon("background-position-x-center", "Background/"));
                    icons.Add(BuilderInspectorUtilities.LoadIcon("background-position-x-right", "Background/"));

                    BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                        BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                        "BackgroundPositionX", BackgroundPositionKeyword.Left.ToString()), out var styleValueTooltipLeft);

                    BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                        BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                        "BackgroundPositionX", BackgroundPositionKeyword.Center.ToString()), out var styleValueTooltipCenter);

                    BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                        BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                        "BackgroundPositionX", BackgroundPositionKeyword.Right.ToString()), out var styleValueTooltipRight);

                    tooltips.Add(styleValueTooltipLeft);
                    tooltips.Add(styleValueTooltipCenter);
                    tooltips.Add(styleValueTooltipRight);
                }
                else
                {
                    icons.Add(BuilderInspectorUtilities.LoadIcon("background-position-y-top", "Background/"));
                    icons.Add(BuilderInspectorUtilities.LoadIcon("background-position-y-center", "Background/"));
                    icons.Add(BuilderInspectorUtilities.LoadIcon("background-position-y-bottom", "Background/"));

                    BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                        BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                        "BackgroundPositionY", BackgroundPositionKeyword.Top.ToString()), out var styleValueTooltipLeft);

                    BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                        BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                        "BackgroundPositionY", BackgroundPositionKeyword.Center.ToString()), out var styleValueTooltipCenter);

                    BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(
                        BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                        "BackgroundPositionY", BackgroundPositionKeyword.Bottom.ToString()), out var styleValueTooltipRight);

                    tooltips.Add(styleValueTooltipLeft);
                    tooltips.Add(styleValueTooltipCenter);
                    tooltips.Add(styleValueTooltipRight);
                }

                for (var i = 0; i < icons.Count; ++i)
                {
                    var icon = icons[i];
                    var tooltip = tooltips[i];
                    var button = new Button() { iconImage = icon, tooltip = tooltip };
                    m_BackgroundPositionAlign.Add(button);
                }

                var toggleButtonGroupState = new ToggleButtonGroupState(2u, 3);
                m_BackgroundPositionAlign.SetValueWithoutNotify(toggleButtonGroupState);
            }
        }

        public BackgroundPositionStyleField() : base(null)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_BackgroundPositionField = this.Q<DimensionStyleField>(s_BackgroundPositionFieldName);
            m_BackgroundPositionAlign = this.Q<ToggleButtonGroup>(s_BackgroundPositionAlignFieldName);

            m_BackgroundPositionField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundPositionField();
                e.StopPropagation();
            });

            m_BackgroundPositionAlign.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundPositionField();
                e.StopPropagation();
            });

            value = new BackgroundPosition(BackgroundPositionKeyword.Center);
        }

        public override void SetValueWithoutNotify(BackgroundPosition newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            var toggleButtonGroupState = new ToggleButtonGroupState(0u, 3);

            int index = 0;

            switch (value.keyword)
            {
                case BackgroundPositionKeyword.Left:
                case BackgroundPositionKeyword.Top:
                    index = 0;
                    break;
                case BackgroundPositionKeyword.Center:
                    index = 1;
                    break;
                case BackgroundPositionKeyword.Right:
                case BackgroundPositionKeyword.Bottom:
                    index = 2;
                    break;
            }

            toggleButtonGroupState[index] = true;
            m_BackgroundPositionAlign.SetValueWithoutNotify(toggleButtonGroupState);

            m_BackgroundPositionField.SetEnabled(index != 1);
            m_BackgroundPositionField.SetValueWithoutNotify(value.offset.ToString());
        }

        void UpdateBackgroundPositionField()
        {
            // Rebuild value from sub fields
            var newPosition = new BackgroundPosition();

            var valueAlign = m_BackgroundPositionAlign.value;
            var selectedAlign = valueAlign.GetActiveOptions(stackalloc int[valueAlign.length]);
            int indexAlign = selectedAlign[0];


            if (indexAlign == 0) newPosition.keyword = (m_Mode == BackgroundPositionMode.Horizontal) ? BackgroundPositionKeyword.Left : BackgroundPositionKeyword.Top;
            else if (indexAlign == 1) newPosition.keyword = BackgroundPositionKeyword.Center;
            else newPosition.keyword = (m_Mode == BackgroundPositionMode.Horizontal) ? BackgroundPositionKeyword.Right : BackgroundPositionKeyword.Bottom;

            newPosition.offset = new Length(m_BackgroundPositionField.length, ConvertUnits(m_BackgroundPositionField.unit));

            value = newPosition;
        }

        static LengthUnit ConvertUnits(Dimension.Unit u)
        {
            if (u == Dimension.Unit.Pixel)
            {
                return LengthUnit.Pixel;
            }
            else if (u == Dimension.Unit.Percent)
            {
                return LengthUnit.Percent;
            }

            return LengthUnit.Pixel;
        }

        public bool OnFieldValueChange(StyleProperty styleProperty, StyleSheet styleSheet)
        {
            var stylePropertyValueCount = styleProperty.values.Length;
            var isNewValue = stylePropertyValueCount == 0;

            bool valid = stylePropertyValueCount == 2 &&
                (styleProperty.values[0].valueType == StyleValueType.Enum && styleProperty.values[1].valueType == StyleValueType.Dimension);

            if (!isNewValue && !valid)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (!isNewValue)
            {
                styleSheet.SetValue(styleProperty.values[0], value.keyword);
                Dimension position = new Dimension(value.offset.value, StyleSheetUtilities.ConvertToDimensionUnit(value.offset.unit));
                styleSheet.SetValue(styleProperty.values[1], position);
            }
            else
            {
                styleSheet.AddValue(styleProperty, value.keyword);
                Dimension position = new Dimension(value.offset.value, StyleSheetUtilities.ConvertToDimensionUnit(value.offset.unit));
                styleSheet.AddValue(styleProperty, position);
            }

            return isNewValue;
        }
    }
}
