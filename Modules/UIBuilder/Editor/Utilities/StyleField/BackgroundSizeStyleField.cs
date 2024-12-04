// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    [UsedImplicitly]
    class BackgroundSizeStyleField : BaseField<BackgroundSize>
    {
        public class BackgroundSizeConverter : UxmlAttributeConverter<BackgroundSize>
        {
            public override BackgroundSize FromString(string value) => throw new NotImplementedException();
            public override string ToString(BackgroundSize value) => throw new NotImplementedException();
        }

        [Serializable]
        public new class UxmlSerializedData : BaseField<BackgroundSize>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<BackgroundSize>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new BackgroundSizeStyleField();
        }

        static readonly string s_FieldClassName = "unity-background-Size-style-field";
        static readonly string s_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/BackgroundSizeStyleField.uxml";
        public static readonly string s_BackgroundSizeWidthFieldName = "width";
        public static readonly string s_BackgroundSizeHeightFieldName = "height";

        DimensionStyleField m_BackgroundSizeXField;
        DimensionStyleField m_BackgroundSizeYField;

        public BackgroundSizeStyleField() : this(null) { }

        public BackgroundSizeStyleField(string label) : base(label)
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(s_FieldClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlPath);
            template.CloneTree(this);

            m_BackgroundSizeXField = this.Q<DimensionStyleField>(s_BackgroundSizeWidthFieldName);
            m_BackgroundSizeYField = this.Q<DimensionStyleField>(s_BackgroundSizeHeightFieldName);


            m_BackgroundSizeXField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundSizeField();
                e.StopPropagation();
            });

            m_BackgroundSizeXField.units.Add(StyleFieldConstants.UnitPercent);

            m_BackgroundSizeXField.UpdateOptionsMenu();

            m_BackgroundSizeYField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundSizeField();
                e.StopPropagation();
            });

            m_BackgroundSizeYField.units.Add(StyleFieldConstants.UnitPercent);
            m_BackgroundSizeYField.populatesOptionsMenuFromParentRow = false;

            m_BackgroundSizeYField.UpdateOptionsMenu();

            value = new BackgroundSize();
        }

        public override void SetValueWithoutNotify(BackgroundSize newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            if (value.sizeType == BackgroundSizeType.Cover)
            {
                m_BackgroundSizeXField.SetValueWithoutNotify("cover");
                m_BackgroundSizeYField.SetEnabled(false);
            }
            else if (value.sizeType == BackgroundSizeType.Contain)
            {
                m_BackgroundSizeXField.SetValueWithoutNotify("contain");
                m_BackgroundSizeYField.SetEnabled(false);
            }
            else
            {
                m_BackgroundSizeYField.SetEnabled(true);
                m_BackgroundSizeXField.SetValueWithoutNotify(value.x.ToString());
                m_BackgroundSizeYField.SetValueWithoutNotify(value.y.ToString());
            }
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

        void UpdateBackgroundSizeField()
        {
            // Rebuild value from sub fields

            var newSize = new BackgroundSize();

            if (m_BackgroundSizeXField.isKeyword)
            {
                if (m_BackgroundSizeXField.keyword == StyleValueKeyword.Cover)
                {

                    newSize.sizeType = BackgroundSizeType.Cover;
                    value = newSize;
                    return;
                }
                else if (m_BackgroundSizeXField.keyword == StyleValueKeyword.Contain)
                {
                    newSize.sizeType = BackgroundSizeType.Contain;
                    value = newSize;
                    return;
                }
            }

            Length newX = new Length(m_BackgroundSizeXField.length, ConvertUnits(m_BackgroundSizeXField.unit));
            Length newY = new Length(m_BackgroundSizeYField.length, ConvertUnits(m_BackgroundSizeYField.unit));

            value = new BackgroundSize(newX, newY);
        }

        public bool OnFieldValueChange(StyleProperty styleProperty, StyleSheet styleSheet)
        {
            var stylePropertyValueCount = styleProperty.values.Length;
            var isNewValue = stylePropertyValueCount == 0;

            bool valid1 = stylePropertyValueCount == 1 &&
                (styleProperty.values[0].valueType == StyleValueType.Dimension || styleProperty.values[0].valueType == StyleValueType.Keyword);

            bool valid2 = stylePropertyValueCount == 2 &&
                (styleProperty.values[0].valueType == StyleValueType.Dimension || styleProperty.values[0].valueType == StyleValueType.Keyword) &&
                (styleProperty.values[1].valueType == StyleValueType.Dimension);

            if (!isNewValue && !valid1 && !valid2)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (!isNewValue)
            {
                if (value.sizeType == BackgroundSizeType.Length)
                {
                    if (stylePropertyValueCount > 1)
                    {
                        styleSheet.RemoveValue(styleProperty, styleProperty.values[1]);
                    }

                    styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);

                    Dimension x = new Dimension(value.x.value, StyleSheetUtilities.ConvertToDimensionUnit(value.x.unit));
                    Dimension y = new Dimension(value.y.value, StyleSheetUtilities.ConvertToDimensionUnit(value.y.unit));
                    styleSheet.AddValue(styleProperty, x);
                    styleSheet.AddValue(styleProperty, y);
                }
                else
                {
                    if (stylePropertyValueCount > 1)
                    {
                        styleSheet.RemoveValue(styleProperty, styleProperty.values[1]);
                    }

                    styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);


                    if (value.sizeType == BackgroundSizeType.Contain)
                    {
                        styleSheet.AddValue(styleProperty, StyleValueKeyword.Contain);
                    }
                    else
                    {
                        styleSheet.AddValue(styleProperty, StyleValueKeyword.Cover);
                    }
                }
            }
            else
            {
                if (value.sizeType == BackgroundSizeType.Length)
                {
                    Dimension x = new Dimension(value.x.value, StyleSheetUtilities.ConvertToDimensionUnit(value.x.unit));
                    Dimension y = new Dimension(value.y.value, StyleSheetUtilities.ConvertToDimensionUnit(value.y.unit));
                    styleSheet.AddValue(styleProperty, x);
                    styleSheet.AddValue(styleProperty, y);
                }
                else
                {
                    if (value.sizeType == BackgroundSizeType.Contain)
                    {
                        styleSheet.AddValue(styleProperty, StyleValueKeyword.Contain);
                    }
                    else
                    {
                        styleSheet.AddValue(styleProperty, StyleValueKeyword.Cover);
                    }
                }
            }

            return isNewValue;
        }
    }
}
