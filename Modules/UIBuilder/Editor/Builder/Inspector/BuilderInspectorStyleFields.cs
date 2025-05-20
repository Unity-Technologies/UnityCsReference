// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Debugger;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Unity.Properties;
using Button = UnityEngine.UIElements.Button;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.UI.Builder
{
    internal partial class BuilderInspectorStyleFields
    {
        public enum NotifyType
        {
            Default,
            RefreshOnly,
        };

        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;

        internal Dictionary<string, List<VisualElement>> m_StyleFields;

        List<string> s_StyleChangeList = new List<string>();

        // Used in tests.
        // ReSharper disable MemberCanBePrivate.Global
        internal const string refreshStyleFieldMarkerName = "BuilderInspectorStyleFields.RefreshStyleField";
        internal const string refreshStyleFieldValueMarkerName = "BuilderInspectorStyleFields.RefreshStyleFieldValue";
        // ReSharper restore MemberCanBePrivate.Global

        static readonly ProfilerMarker k_RefreshStyleFieldMarker = new (refreshStyleFieldMarkerName);
        static readonly ProfilerMarker k_RefreshStyleFieldValueMarker = new (refreshStyleFieldValueMarkerName);

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;
        StyleSheet styleSheet => m_Inspector.styleSheet;
        StyleRule currentRule => m_Inspector.currentRule;

        public Action<Enum> updatePositionAnchorsFoldoutState { get; set; }

        public Action updateStyleCategoryFoldoutOverrides { get; set; }

        public BuilderInspectorStyleFields(BuilderInspector inspector)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;

            m_StyleFields = new Dictionary<string, List<VisualElement>>();
        }

        public List<VisualElement> GetFieldListForStyleName(string styleName)
        {
            List<VisualElement> fieldList;
            m_StyleFields.TryGetValue(styleName, out fieldList);
            return fieldList;
        }

        public List<VisualElement> GetOrCreateFieldListForStyleName(string styleName)
        {
            if (!m_StyleFields.TryGetValue(styleName, out var fieldList))
            {
                m_StyleFields[styleName] = fieldList = new List<VisualElement>();
            }
            return fieldList;
        }

        public static StyleProperty GetLastStyleProperty(StyleRule rule, string styleName)
        {
            if (rule == null)
                return null;

            for (var i = rule.properties.Length - 1; i >= 0; --i)
            {
                var property = rule.properties[i];
                if (property.name == styleName)
                    return property;
            }

            return null;
        }

        void RegisterFocusCallbackOnHighlightingField(VisualElement field)
        {
            field.RegisterCallback<FocusInEvent>(OnFieldFocusIn);
            field.RegisterCallback<FocusOutEvent>(OnFieldFocusOut);

            var valueField = field.Q<IntegerStyleField>();

            if (valueField != null)
            {
                valueField.RegisterCallback<FocusInEvent>(OnFieldFocusIn);
                valueField.RegisterCallback<FocusOutEvent>(OnFieldFocusOut);
            }
        }

        void OnFieldFocusIn(FocusInEvent evt)
        {
            m_Inspector.highlightOverlayPainter.ClearOverlay();
            m_Inspector.highlightOverlayPainter.AddOverlay(m_Inspector.selection.selection.First());
        }

        void OnFieldFocusOut(FocusOutEvent evt)
        {
            m_Inspector.highlightOverlayPainter.ClearOverlay();
        }

        // For mapping icons to the builder's ToggleButtonGroup's buttons
        // Note: The key represents the style name while the value is the folder name within the
        //       UIBuilderPackageResources' icons folder.
        readonly Dictionary<string, string> iconsFolderName = new()
        {
            { "display", "Display" },
            { "visibility", "Display Visibility" },
            { "overflow", "Display Overflow" },
            { "flex-direction", "Flex Direction" },
            { "flex-wrap", "Flex Wrap" },
            {"align-content", "Align Content"},
            { "align-items", "Align Items" },
            { "justify-content", "Justify Content" },
            { "align-self", "Align Self" },
            { "white-space", "Text White Space" },
            { "text-overflow", "Text Overflow" },
            { "-unity-background-scale-mode", "Background" },
            { "-unity-slice-type", "Background" },
            { FlexDirection.Column.ToString(), "Flex Column" },
            { FlexDirection.ColumnReverse.ToString(), "Flex Column" },
            { FlexDirection.Row.ToString(), "Flex Row" },
            { FlexDirection.RowReverse.ToString(), "Flex Row" },
        };

        List<ToggleButtonGroup> m_FlexAlignmentToggleButtonGroups = new();

        // A set of dictionaries to help map the correct icons based on the flex direction
        // Note: As there is a dependency on the flex direction we need to map a set of ToggleButtonGroups to react
        //       based on a specific flex direction. For the next set of dictionaries, the key represents the style's
        //       enum value while the value represents the icon's real name in their respective
        //       UIBuilderPackageResources icons folder.
        readonly Dictionary<string, string> m_AlignItemsColumnIcons = new()
        {
            { "FlexStart", "Left" },
            { "Center", "Center" },
            { "FlexEnd", "Right" },
            { "Stretch", "Stretch" }
        };
        readonly Dictionary<string, string> m_AlignItemsColumnReverseIcons = new()
        {
            { "FlexStart", "Left Reverse" },
            { "Center", "Center Reverse" },
            { "FlexEnd", "Right Reverse" },
            { "Stretch", "Stretch Reverse" },
        };
        readonly Dictionary<string, string> m_AlignItemsRowIcons = new()
        {
            { "FlexStart", "Upper" },
            { "Center", "Center" },
            { "FlexEnd", "Lower" },
            { "Stretch", "Stretch" },
        };
        readonly Dictionary<string, string> m_AlignItemsRowReverseIcons = new()
        {
            { "FlexStart", "Upper Reverse" },
            { "Center", "Center Reverse" },
            { "FlexEnd", "Lower Reverse" },
            { "Stretch", "Stretch Reverse" },
        };
        readonly Dictionary<string, string> m_JustifyContentColumnIcons = new()
        {
            { "FlexStart", "Upper" },
            { "Center", "Middle" },
            { "FlexEnd", "Lower" },
            { "SpaceBetween", "Space Between" },
            { "SpaceAround", "Space Around" },
            { "SpaceEvenly", "Space Evenly" },
        };
        readonly Dictionary<string, string> m_JustifyContentColumnReverseIcons = new()
        {
            { "FlexStart", "Upper Reverse" },
            { "Center", "Middle Reverse" },
            { "FlexEnd", "Lower Reverse" },
            { "SpaceBetween", "Space Between Reverse" },
            { "SpaceAround", "Space Around Reverse" },
            { "SpaceEvenly", "Space Evenly" },
        };
        readonly Dictionary<string, string> m_JustifyContentRowIcons = new()
        {
            { "FlexStart", "Left" },
            { "Center", "Center" },
            { "FlexEnd", "Right" },
            { "SpaceBetween", "Space Between" },
            { "SpaceAround", "Space Around" },
            { "SpaceEvenly", "Space Evenly" },
        };
        readonly Dictionary<string, string> m_JustifyContentRowReverseIcons = new()
        {
            { "FlexStart", "Left Reverse" },
            { "Center", "Center Reverse" },
            { "FlexEnd", "Right Reverse" },
            { "SpaceBetween", "Space Between Reverse" },
            { "SpaceAround", "Space Around Reverse" },
            { "SpaceEvenly", "Space Evenly" },
        };

        readonly string[] m_FlexDirectionDependentStyleNames = {"align-items", "justify-content", "align-self", "align-content" };

        static void SetFieldToolTip(VisualElement target, string styleName, VisualElement fieldElement)
        {
            if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, fieldElement.typeName, styleName), out var styleValueTooltip) ||
                BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, fieldElement.typeName, ""), out styleValueTooltip))
                target.tooltip = styleValueTooltip;
        }

        public void BindStyleField(BuilderStyleRow styleRow, string styleName, VisualElement fieldElement)
        {
            // Link the row.
            fieldElement.SetContainingRow(styleRow);

            var styleType = StyleDebug.GetComputedStyleType(styleName);
            if (styleType == null)
                return;

            m_Inspector.RegisterFieldToInlineEditingEvents(fieldElement);

            // We don't care which element we get the value for here as we're only interested
            // in the type of Enum it might be (and for validation), but not it's actual value.
            var val = StyleDebug.GetComputedStyleValue(fieldElement.computedStyle, styleName);

            if (IsComputedStyleFloat(val) && fieldElement is FloatField)
            {
                var uiField = fieldElement as FloatField;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;

                if (BuilderConstants.SpecialSnowflakeLengthStyles.Contains(styleName))
                    uiField.RegisterValueChangedCallback(e => OnFieldDimensionChange(e, styleName));
                else
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChangeIntToFloat(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is PercentSlider)
            {
                var uiField = fieldElement as PercentSlider;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
                else if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName, ""), out var genericPercentTooltip))
                    uiField.visualInput.tooltip = genericPercentTooltip;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is NumericStyleField)
            {
                var uiField = fieldElement as NumericStyleField;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName, ""), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
                uiField.RegisterValueChangedCallback(e => OnNumericStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                SetFieldToolTip(uiField.visualInput, styleName, uiField);
                uiField.RegisterValueChangedCallback(e => OnDimensionStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName, ""), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerStyleField)
            {
                var uiField = fieldElement as IntegerStyleField;
                SetFieldToolTip(uiField.visualInput, styleName, uiField);
                uiField.RegisterValueChangedCallback(e => OnIntegerStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleLength(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName, ""), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
                uiField.RegisterValueChangedCallback(e => OnFieldDimensionChange(e, styleName));
            }
            else if (IsComputedStyleLength(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                SetFieldToolTip(uiField.visualInput, styleName, uiField);
                uiField.RegisterValueChangedCallback(e => OnDimensionStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleColor(val) && fieldElement is ColorField)
            {
                var uiField = fieldElement as ColorField;
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, uiField.typeName, ""), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFont(val, styleName) && fieldElement is ObjectField fontField)
            {
                fontField.objectType = typeof(Font);
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(fontField.typeName + BuilderConstants.FieldTooltipDictionarySeparator, out var styleValueTooltip))
                    fontField.visualInput.tooltip = styleValueTooltip;
                fontField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFontAsset(val, styleName) && fieldElement is FontDefinitionStyleField fontAssetField)
            {
                fontAssetField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
                var objectField = fontAssetField.Q<ObjectField>();
                if (objectField != null)
                {
                    if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(objectField.typeName + BuilderConstants.FieldTooltipDictionarySeparator, out var styleValueTooltip))
                        objectField.visualInput.tooltip = styleValueTooltip;
                }
                var popupField = fontAssetField.Q<PopupField<string>>();
                if (popupField != null)
                {
                    fontAssetField.RegisterCallback<TooltipEvent>(e =>
                    {
                        if (!BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                                popupField.value + BuilderConstants.FieldTooltipDictionarySeparator, out var styleValueTooltip))
                            return;
                        popupField.tooltip = e.tooltip = styleValueTooltip;
                        e.rect = popupField.visualInput.GetTooltipRect();
                    });
                }
            }
            else if (IsComputedStyleTextShadow(val) && fieldElement is TextShadowStyleField textShadowStyleField)
            {
                textShadowStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeTextShadow(e, styleName));
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, "offset-x"), out var offsetXTooltip)
                    && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, "offset-y"), out var offsetYTooltip)
                    && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, "blur-radius"), out var blurRadiusTooltip)
                    && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, "color"), out var colorTooltip))
                    textShadowStyleField.UpdateSubFieldVisualInputTooltips(offsetXTooltip, offsetYTooltip, blurRadiusTooltip, colorTooltip);
            }
            else if (IsComputedStyleTransformOrigin(val) && fieldElement is TransformOriginStyleField transformOriginStyleField)
            {
                transformOriginStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeTransformOrigin(e, styleName));
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                    SetXAndYSubFieldsTooltips(transformOriginStyleField,
                    TransformOriginStyleField.s_TransformOriginXFieldName, styleValueTooltip,
                    TransformOriginStyleField.s_TransformOriginYFieldName, styleValueTooltip);
            }
            else if (IsComputedStyleTranslate(val) && fieldElement is TranslateStyleField translateStyleField)
            {
                translateStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeTranslate(e, styleName));
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                    SetXAndYSubFieldsTooltips(translateStyleField,
                    TranslateStyleField.s_TranslateXFieldName, styleValueTooltip,
                    TranslateStyleField.s_TranslateYFieldName, styleValueTooltip);
            }
            else if (IsComputedStyleRotate(val) && fieldElement is RotateStyleField rotateStyleField)
            {
                rotateStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeRotate(e, styleName));
                var popupField = rotateStyleField.Q<PopupField<string>>();
                if (popupField != null && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                {
                    rotateStyleField.RegisterCallback<TooltipEvent>(e =>
                    {

                        rotateStyleField.tooltip = e.tooltip = styleValueTooltip;
                        e.rect = rotateStyleField.visualInput.GetTooltipRect();
                    });
                }
            }
            else if (IsComputedStyleBackgroundRepeat(val) && fieldElement is BackgroundRepeatStyleField backgroundRepeatStyleField)
            {
                backgroundRepeatStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeBackgroundRepeat(e, styleName));
            }
            else if (IsComputedStyleBackgroundSize(val) && fieldElement is BackgroundSizeStyleField backgroundSizeStyleField)
            {
                backgroundSizeStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeBackgroundSize(e, styleName));
            }
            else if (IsComputedStyleBackgroundPosition(val) && fieldElement is BackgroundPositionStyleField backgroundPositionStyleField)
            {
                backgroundPositionStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeBackgroundPosition(e, styleName));
            }
            else if (IsComputedStyleScale(val) && fieldElement is ScaleStyleField scaleStyleField)
            {
                scaleStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeScale(e, styleName));
                if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                    SetXAndYSubFieldsTooltips(scaleStyleField,
                    ScaleStyleField.s_ScaleXFieldName, styleValueTooltip,
                    ScaleStyleField.s_ScaleYFieldName, styleValueTooltip);
            }
            else if (IsComputedStyleBackground(val) && fieldElement is ImageStyleField imageStyleField)
            {
                imageStyleField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));

                var objectField = imageStyleField.Q<ObjectField>();
                if (objectField != null && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                    objectField.tooltip = styleValueTooltip;

                var popupField = imageStyleField.Q<PopupField<string>>();
                if (popupField != null && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var popupValueTooltip))
                {
                    imageStyleField.RegisterCallback<TooltipEvent>(e =>
                    {
                        popupField.tooltip = e.tooltip = popupValueTooltip;
                        e.rect = popupField.visualInput.GetTooltipRect();
                    });
                }
            }
            else if (IsComputedStyleCursor(val) && fieldElement is CursorStyleField)
            {
                var uiField = fieldElement as CursorStyleField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));

                if (uiField != null && BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                        string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, ""), out var styleValueTooltip))
                    uiField.visualInput.tooltip = styleValueTooltip;
            }
            else if (IsComputedStyleEnum(val, styleType))
            {
                var enumValue = GetComputedStyleEnumValue(val, styleType);

                if (fieldElement is EnumField)
                {
                    var uiField = fieldElement as EnumField;

                    uiField.Init(enumValue);
                    if (BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                            string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, StyleSheetUtility.ConvertCamelToDash(enumValue.ToString())), out var styleValueTooltip))
                    {
                        uiField.RegisterCallback<TooltipEvent>(e =>
                        {
                            if (!BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                                    string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat, styleName, StyleSheetUtility.ConvertCamelToDash(uiField.value.ToString())), out styleValueTooltip))
                                return;
                            uiField.tooltip = e.tooltip = string.Format(BuilderConstants.InputFieldStyleValueTooltipWithDescription, uiField.valueAsString, styleValueTooltip);
                            e.rect = uiField.visualInput.GetTooltipRect();
                        });
                    }

                    uiField.RegisterValueChangedCallback(e =>
                    {
                        OnFieldValueChange(e, styleName);
                    });
                }
                else if (fieldElement is FontStyleStrip fontStyleStripField)
                {
                    fontStyleStripField.userData = enumValue.GetType();
                    fontStyleStripField.RegisterValueChangedCallback(e => OnFieldToggleButtonGroupChange(e, styleName));
                }
                else if (fieldElement is TextAlignStrip textAlignStripField)
                {
                    textAlignStripField.userData = enumValue.GetType();
                    textAlignStripField.RegisterValueChangedCallback(e => OnFieldToggleButtonGroupChange(e, styleName));
                }
                else if (fieldElement is ToggleButtonGroup)
                {
                    var uiField = fieldElement as ToggleButtonGroup;
                    var enumType = enumValue.GetType();

                    foreach (Enum item in Enum.GetValues(enumType))
                    {
                        var typeName = item.ToString();
                        // For the "Overflow" style, the Enum reflected from the computedStyle's
                        // is of type OverflowInternal, which, for some reason, includes the
                        // OverflowInternal.Scroll option, which...we don't support. Until we
                        // do support it, we'll have to live with a bit of a hack here.
                        if (typeName == "Scroll")
                            continue;

                        var enumAsDash = StyleSheetUtility.ConvertCamelToDash(typeName);
                        var tooltip = BuilderConstants.InspectorStylePropertiesValuesTooltipsDictionary.TryGetValue(
                            string.Format(BuilderConstants.InputFieldStyleValueTooltipDictionaryKeyFormat,
                                styleName, enumAsDash), out var styleValueTooltip)
                            ? string.Format(BuilderConstants.InputFieldStyleValueTooltipWithDescription,
                                enumAsDash, styleValueTooltip)
                            : enumAsDash;

                        if (typeName == "Auto")
                        {
                            uiField.Add(new Button() { name = "auto", text = "AUTO", tooltip = tooltip });
                        }
                        else
                        {
                            uiField.Add(new Button()
                            {
                                name = enumAsDash,
                                iconImage = BuilderInspectorUtilities.LoadIcon(BuilderNameUtilities.ConvertCamelToHuman(typeName), $"{iconsFolderName[styleName]}/"),
                                tooltip = tooltip
                            });
                        }
                    }

                    uiField.userData = enumType;
                    uiField.RegisterValueChangedCallback(e => OnFieldToggleButtonGroupChange(e, styleName));

                    // We store the ToggleButtonGroup that needs to be updated when the Flex Direction value changes
                    if (m_FlexDirectionDependentStyleNames.Contains(styleName))
                        m_FlexAlignmentToggleButtonGroups.Add(uiField);
                }
                else
                {
                    // Unsupported style value type.
                    return;
                }
            }
            else if (fieldElement is BoxModel)
            {
                // Nothing to do here.
            }
            else
            {
                // Unsupported style value type.
                return;
            }

            fieldElement.SetInspectorStylePropertyName(styleName);

            SetUpContextualMenuOnStyleField(fieldElement);

            // Add to styleName to field map.
            if (!m_StyleFields.ContainsKey(styleName))
            {
                var newList = new List<VisualElement>();
                newList.Add(fieldElement);
                m_StyleFields.Add(styleName, newList);
            }
            else
            {
                m_StyleFields[styleName].Add(fieldElement);
            }

            if (BuilderConstants.ViewportOverlayEnablingStyleProperties.Contains(styleName))
            {
                RegisterFocusCallbackOnHighlightingField(fieldElement);

                var foldoutNumberFieldParent = fieldElement.GetFirstAncestorOfType<FoldoutNumberField>();

                if (foldoutNumberFieldParent != null)
                {
                    RegisterFocusCallbackOnHighlightingField(foldoutNumberFieldParent);
                }
            }

            // Do not rely on reflection to set tooltip on field's label
            var labelElement = fieldElement.Q<Label>();
            if (labelElement != null && string.IsNullOrEmpty(labelElement.tooltip))
            {
                if (BuilderConstants.InspectorStylePropertiesTooltipsDictionary.TryGetValue(styleName,
                        out var ussTooltip))
                {
                    labelElement.tooltip = string.Format(BuilderConstants.FieldTooltipWithDescription,
                        BuilderConstants.FieldValueInfoTypeEnumUSSPropertyDisplayString, styleName, ussTooltip);
                }
                else
                {
                    labelElement.tooltip = string.Format(BuilderConstants.FieldTooltipNameOnlyFormatString,
                        BuilderConstants.FieldValueInfoTypeEnumUSSPropertyDisplayString, styleName);
                }
            }
        }

        public void BindDoubleFieldRow(BuilderStyleRow styleRow)
        {
            var styleFields = styleRow.Query<BindableElement>().Where(element => !string.IsNullOrEmpty(element.bindingPath)).Build();
            using (var enumerator = styleFields.GetEnumerator())
            {
                // If there are no fields, we don't need to do anything.
                if (enumerator.MoveNext())
                {
                    var headerLabel = styleRow.Q<BindableElement>(classes: "unity-builder-composite-field-label");
                    headerLabel.AddManipulator(new ContextualMenuManipulator(action =>
                    {
                        action.elementTarget.userData = styleFields.ToList();
                        BuildStyleFieldContextualMenu(action);
                    }));
                }
            }
        }

        public void BindStyleField(BuilderStyleRow styleRow, FoldoutNumberField foldoutElement)
        {
            var intFields = foldoutElement.Query<StyleFieldBase>().Build();

            foreach (var field in intFields)
            {
                field.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
                field.RegisterValueChangedCallback((evt) =>
                {
                    foldoutElement.UpdateFromChildFields();
                    foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                });
            }
            foldoutElement.header.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
            foldoutElement.headerInputField.RegisterValueChangedCallback(FoldoutNumberFieldOnValueChange);

            foldoutElement.headerInputField.AddManipulator(
                new ContextualMenuManipulator((e) =>
                {
                    e.elementTarget = foldoutElement.header;
                    BuildStyleFieldContextualMenu(e);
                }));

            foldoutElement.header.AddManipulator(
                new ContextualMenuManipulator(BuildStyleFieldContextualMenu));
        }

        public void BindStyleField(BuilderStyleRow styleRow, FoldoutColorField foldoutElement)
        {
            var colorFields = foldoutElement.Query<ColorField>().Build();

            foreach (var field in colorFields)
            {
                field.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
                field.RegisterValueChangedCallback((evt) =>
                {
                    var row = field.GetContainingRow();
                    if (row != null && !string.IsNullOrEmpty(row.bindingPath))
                        foldoutElement.UpdateFromChildField(row.bindingPath, field.value);

                    foldoutElement.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                });
            }
            foldoutElement.header.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
            foldoutElement.headerInputField.RegisterValueChangedCallback(FoldoutColorFieldOnValueChange);

            foldoutElement.headerInputField.AddManipulator(
                new ContextualMenuManipulator((e) =>
                {
                    e.elementTarget = foldoutElement.header;
                    BuildStyleFieldContextualMenu(e);
                }));

            foldoutElement.header.AddManipulator(
                new ContextualMenuManipulator(BuildStyleFieldContextualMenu));
        }

        void DispatchChangeEvent<TValueType>(BaseField<TValueType> field)
        {
            var e = ChangeEvent<TValueType>.GetPooled(field.value, field.value);
            e.elementTarget = field;
            field.SendEvent(e);
        }

        void FoldoutNumberFieldOnValueChange(ChangeEvent<string> evt)
        {
            var newValue = evt.newValue;
            var target = evt.elementTarget as TextField;
            var foldoutElement = target.GetFirstAncestorOfType<FoldoutNumberField>();

            var splitBy = new char[] { ' ' };
            string[] inputArray = newValue.Split(splitBy);
            var styleFields = foldoutElement.Query<StyleFieldBase>().ToList();

            if (inputArray.Length == 1 && styleFields.Count > 0)
            {
                var newCommonValue = inputArray[0];
                var newCommonValueWithUnit = newValue;
                bool needToForceSet = false;

                for (int i = 0; i < styleFields.Count; ++i)
                {
                    var styleField = styleFields[i];

                    if (i == 0 && styleField is DimensionStyleField)
                    {
                        if (newCommonValueWithUnit != styleFields[i].value)
                            needToForceSet = true;

                        styleField.value = newCommonValueWithUnit;
                        if (!newCommonValueWithUnit.StartsWith(BuilderConstants.UssVariablePrefix))
                            newCommonValueWithUnit = styleFields[i].value;

                        continue;
                    }

                    if (styleField is DimensionStyleField)
                        styleField.value = newCommonValueWithUnit;
                    else
                        styleField.value = newCommonValue;

                    // We need to force-set the rest of the fields because it's possible only
                    // the first one was set. The first one will get a value without a unit
                    // and so even if the value is the same as what it already has, it will
                    // not match on the unit and will cause a change event. But then we set
                    // newCommonValueWithUnit to be the new value + the unit (so we can make
                    // sure all the fields have the same unit in the end). This means that if
                    // some of the fields already had the same value and unit, they will not
                    // be set (bolded). This forces the set.
                    if (needToForceSet)
                    {
                        var styleName = styleField.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                        DispatchChangeEvent(styleName, styleField);
                    }
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Min(inputArray.Length, styleFields.Count); ++i)
                {
                    styleFields[i].value = inputArray[i];
                }
            }

            foldoutElement.UpdateFromChildFields();

            evt.StopPropagation();
        }

        void FoldoutColorFieldOnValueChange(ChangeEvent<Color> evt)
        {
            var newValue = evt.newValue;
            var target = evt.elementTarget as ColorField;
            var foldoutColorField = target.GetFirstAncestorOfType<FoldoutColorField>();

            foldoutColorField.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
            foldoutColorField.headerInputField.value = newValue;

            for (int i = 0; i < foldoutColorField.bindingPathArray.Length; i++)
            {
                var styleName = foldoutColorField.bindingPathArray[i];
                var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);

                // If the property was bound to a variable then clear it
                if (styleProperty.values.Length != 0 && styleProperty.IsVariable())
                    styleProperty.values = new StyleValueHandle[0];

                if (styleProperty.values.Length == 0)
                    styleSheet.AddValue(styleProperty, newValue);
                else // TODO: Assume only one value.
                    styleSheet.SetValue(styleProperty.values[0], newValue);
            }

            NotifyStyleChanges();
            m_Inspector.StylingChanged(foldoutColorField.bindingPathArray.ToList());
        }

        public void RefreshStyleField(string styleName, VisualElement fieldElement)
        {
            using var marker = k_RefreshStyleFieldMarker.Auto();

            var fieldRefreshed = RefreshStyleFieldValue(styleName, fieldElement);

            if (fieldRefreshed)
            {
                var cSharpStyleName = BuilderNameUtilities.ConvertUssNameToStyleName(styleName);
                var styleProperty = GetLastStyleProperty(currentRule, cSharpStyleName);
                m_Inspector.UpdateFieldStatus(fieldElement, styleProperty);
            }
        }

        public bool RefreshStyleFieldValue(string styleName, VisualElement fieldElement, bool forceInlineValue = false)
        {
            using var marker = k_RefreshStyleFieldValueMarker.Auto();

            var styleType = StyleDebug.GetComputedStyleType(styleName);
            if (styleType == null)
            {
                // Transitions are made from 4 different properties and does not have a dedicated C# property on ComputedStyle.
                if (!string.IsNullOrEmpty(styleName) &&
                    StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(styleName, out var id) &&
                    id.IsTransitionId() &&
                    fieldElement is TransitionsListView listView)
                    RefreshStyleField(listView);
                return false;
            }

            var val = StyleDebug.GetComputedStyleValue(currentVisualElement.computedStyle, styleName);
            var cSharpStyleName = BuilderNameUtilities.ConvertUssNameToStyleName(styleName);
            var styleProperty = GetLastStyleProperty(currentRule, cSharpStyleName);
            var hasBinding = BuilderInspectorUtilities.HasBinding(m_Inspector, fieldElement);
            var useStyleProperty = styleProperty != null && !styleProperty.IsVariable() && (!hasBinding || forceInlineValue);

            if (IsComputedStyleFloat(val) && fieldElement is FloatField)
            {
                var uiField = fieldElement as FloatField;
                var value = GetComputedStyleFloatValue(val);
                if (useStyleProperty && styleProperty.TryGetFloat(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleFloat(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                var value = (int)GetComputedStyleFloatValue(val);
                if (useStyleProperty && styleProperty.TryGetInt(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleFloat(val) && fieldElement is PercentSlider)
            {
                var uiField = fieldElement as PercentSlider;
                var value = GetComputedStyleFloatValue(val);
                if (useStyleProperty && styleProperty.TryGetFloat(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleTextShadow(val) && fieldElement is TextShadowStyleField)
            {
                var uiField = fieldElement as TextShadowStyleField;
                var value = GetComputedStyleTextShadowValue(val);
                if (useStyleProperty && styleProperty.TryGetTextShadow(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleTransformOrigin(val) && fieldElement is TransformOriginStyleField)
            {
                var uiField = fieldElement as TransformOriginStyleField;
                var value = GetComputedStyleTransformOriginValue(val);
                if (useStyleProperty && styleProperty.TryGetTransformOrigin(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleTranslate(val) && fieldElement is TranslateStyleField)
            {
                var uiField = fieldElement as TranslateStyleField;
                var value = GetComputedStyleTranslateValue(val);
                if (useStyleProperty && styleProperty.TryGetTranslate(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleRotate(val) && fieldElement is RotateStyleField)
            {
                var uiField = fieldElement as RotateStyleField;
                var value = GetComputedStyleRotateValue(val);
                if (useStyleProperty && styleProperty.TryGetRotate(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleBackgroundRepeat(val) && fieldElement is BackgroundRepeatStyleField)
            {
                var uiField = fieldElement as BackgroundRepeatStyleField;
                var value = GetComputedStyleBackgroundRepeatValue(val);
                if (useStyleProperty && styleProperty.TryGetBackgroundRepeat(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleBackgroundSize(val) && fieldElement is BackgroundSizeStyleField)
            {
                var uiField = fieldElement as BackgroundSizeStyleField;
                var value = GetComputedStyleBackgroundSizeValue(val);
                if (useStyleProperty && styleProperty.TryGetBackgroundSize(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleBackgroundPosition(val) && fieldElement is BackgroundPositionStyleField)
            {
                var uiField = fieldElement as BackgroundPositionStyleField;
                var value = GetComputedStyleBackgroundPositionValue(val);
                if (useStyleProperty && styleProperty.TryGetBackgroundPosition(styleSheet, out var propertyValue, uiField.mode))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleScale(val) && fieldElement is ScaleStyleField)
            {
                var uiField = fieldElement as ScaleStyleField;
                var value = GetComputedStyleScaleValue(val);
                if (useStyleProperty && styleProperty.TryGetScale(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleFloat(val) && fieldElement is NumericStyleField)
            {
                var uiField = fieldElement as NumericStyleField;
                var value = (int)GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword && styleProperty.TryGetKeyword(styleSheet, out StyleValueKeyword keyword))
                    {
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Float && styleProperty.TryGetFloat(styleSheet, out var floatValue))
                    {
                        uiField.SetValueWithoutNotify(floatValue.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    var keyword = GetComputedStyleKeyword(val);
                    if (keyword != StyleKeyword.Undefined)
                        uiField.keyword = keyword.ToStyleValueKeyword();
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }

                return true;
            }

            if (IsComputedStyleInt(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                var value = GetComputedStyleIntValue(val);
                if (useStyleProperty && styleProperty.TryGetInt(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleInt(val) && fieldElement is IntegerStyleField)
            {
                var uiField = fieldElement as IntegerStyleField;
                var value = GetComputedStyleIntValue(val);
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    switch (styleValue.valueType)
                    {
                        case StyleValueType.Keyword when styleProperty.TryGetKeyword(styleSheet, out StyleValueKeyword propertyValue):
                            uiField.keyword = propertyValue;
                            break;
                        case StyleValueType.Float when styleProperty.TryGetFloat(styleSheet, out var propertyValue):
                            uiField.SetValueWithoutNotify(propertyValue.ToString(CultureInfo.InvariantCulture));
                            break;
                        default:
                            throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    var keyword = GetComputedStyleKeyword(val);
                    if (keyword != StyleKeyword.Undefined)
                        uiField.keyword = keyword.ToStyleValueKeyword();
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }

                return true;
            }

            if (IsComputedStyleLength(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;

                var value = (int)GetComputedStyleLengthValue(val).value;
                if (useStyleProperty && styleProperty.TryGetLength(styleSheet, out var propertyValue))
                    value = (int)propertyValue.value;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleFloat(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                var value = (int)GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    switch (styleValue.valueType)
                    {
                        case StyleValueType.Keyword when styleProperty.TryGetKeyword(styleSheet, out StyleValueKeyword propertyValue):
                        {
                            uiField.keyword = propertyValue;
                            break;
                        }
                        case StyleValueType.Dimension when styleProperty.TryGetDimension(styleSheet, out var propertyValue):
                        {
                            uiField.unit = propertyValue.unit;
                            uiField.length = propertyValue.value;
                            break;
                        }
                        case StyleValueType.Float when styleProperty.TryGetFloat(styleSheet, out var propertyValue):
                        {
                            uiField.SetValueWithoutNotify(propertyValue + DimensionStyleField.defaultUnit);
                            break;
                        }
                        default:
                            throw new InvalidOperationException("StyleValueType " + styleValue.valueType + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    var keyword = GetComputedStyleKeyword(val);
                    if (keyword != StyleKeyword.Undefined)
                        uiField.keyword = keyword.ToStyleValueKeyword();
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }

                return true;
            }

            if (IsComputedStyleLength(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                var value = GetComputedStyleLengthValue(val).value;
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    switch (styleValue.valueType)
                    {
                        case StyleValueType.Keyword when styleProperty.TryGetKeyword(styleSheet, out StyleValueKeyword propertyValue):
                        {
                            uiField.keyword = propertyValue;
                            break;
                        }
                        case StyleValueType.Dimension when styleProperty.TryGetDimension(styleSheet, out var propertyValue):
                        {
                            uiField.unit = propertyValue.unit;
                            uiField.length = propertyValue.value;
                            break;
                        }
                        case StyleValueType.Float when styleProperty.TryGetFloat(styleSheet, out var propertyValue):
                        {
                            uiField.SetValueWithoutNotify(propertyValue + DimensionStyleField.defaultUnit);
                            break;
                        }
                        default:
                            throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleLength.");
                    }
                }
                else
                {
                    var keyword = GetComputedStyleKeyword(val);
                    if (keyword != StyleKeyword.Undefined)
                        uiField.keyword = keyword.ToStyleValueKeyword();
                    else
                    {
                        var lengthUnit = GetComputedStyleLengthUnit(val);
                        uiField.unit = lengthUnit.ToDimensionUnit();
                        uiField.length = value;
                    }
                }

                return true;
            }

            if (fieldElement is BoxModel boxModel)
            {
                boxModel.UpdateUnitFromFields();
                // return false so that UpdateFieldStatus isn't called on BoxModel since it's not applicable
                return false;
            }

            if (IsComputedStyleColor(val) && fieldElement is ColorField)
            {
                var uiField = fieldElement as ColorField;
                var value = GetComputedStyleColorValue(val);
                if (useStyleProperty && styleProperty.TryGetColor(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleFont(val, styleName) && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                var value = GetComputedStyleFontValue(val);
                if (useStyleProperty && styleProperty.TryGetAssetReference<Font>(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleFontAsset(val, styleName) && fieldElement is FontDefinitionStyleField)
            {
                var uiField = fieldElement as FontDefinitionStyleField;
                var value = GetComputedStyleFontAssetValue(val);
                if (useStyleProperty && styleProperty.TryGetAssetReference<FontAsset>(styleSheet, out var propertyValue))
                    value = propertyValue;

                uiField.SetValueWithoutNotify(value);
                return true;
            }

            if (IsComputedStyleBackground(val) && fieldElement is ImageStyleField imageStyleField)
            {
                var value = GetComputedStyleBackgroundValue(val);
                if (useStyleProperty && styleProperty.TryGetAssetReference(styleSheet, out var propertyValue))
                {
                    imageStyleField.SetValueWithoutNotify(propertyValue);
                    if (propertyValue != null)
                        imageStyleField.SetTypePopupValueWithoutNotify(propertyValue.GetType());
                }
                else
                {
                    if (value.texture != null)
                    {
                        imageStyleField.SetValueWithoutNotify(value.texture);
                        imageStyleField.SetTypePopupValueWithoutNotify(typeof(Texture2D));
                    }
                    else if (value.renderTexture != null)
                    {
                        imageStyleField.SetValueWithoutNotify(value.renderTexture);
                        imageStyleField.SetTypePopupValueWithoutNotify(typeof(RenderTexture));
                    }
                    else if (value.sprite != null)
                    {
                        imageStyleField.SetValueWithoutNotify(value.sprite);
                        imageStyleField.SetTypePopupValueWithoutNotify(typeof(Sprite));
                    }
                    else if (value.vectorImage != null)
                    {
                        imageStyleField.SetValueWithoutNotify(value.vectorImage);
                        imageStyleField.SetTypePopupValueWithoutNotify(typeof(VectorImage));
                    }
                    else
                    {
                        imageStyleField.SetValueWithoutNotify(null);
                        imageStyleField.SetTypePopupValueWithoutNotify(typeof(Texture2D));
                    }
                }

                return true;
            }

            if (IsComputedStyleCursor(val) && fieldElement is CursorStyleField)
            {
                var uiField = fieldElement as CursorStyleField;
                var value = GetComputedStyleCursorValue(val);

                if (useStyleProperty)
                {
                    switch (styleProperty.values[0].valueType)
                    {
                        case StyleValueType.AssetReference:
                        case StyleValueType.ResourcePath:
                        case StyleValueType.ScalableImage:
                            var texture = styleSheet.GetAsset(styleProperty.values[0]) as Texture2D;
                            uiField.SetValueWithoutNotify(new Cursor
                            {
                                texture = texture,
                                hotspot = value.hotspot,
                                defaultCursorId = value.defaultCursorId
                            });
                            break;
                        case StyleValueType.MissingAssetReference:
                            uiField.SetValueWithoutNotify(new Cursor
                            {
                                texture = null,
                                hotspot = value.hotspot,
                                defaultCursorId = value.defaultCursorId
                            });
                            break;
                        case StyleValueType.Enum:
                            // TODO: Add support for using the predefined list from the MouseCursor enum.
                            // This is only available in the editor.
                            uiField.SetValueWithoutNotify(new Cursor
                            {
                                texture = null,
                                hotspot = value.hotspot,
                                defaultCursorId = value.defaultCursorId
                            });
                            break;
                        default:
                            uiField.SetValueWithoutNotify(value);
                            break;
                    }
                }
                else
                {
                    uiField.SetValueWithoutNotify(value);
                }
                return true;
            }

            if (IsComputedStyleEnum(val, styleType))
            {
                var enumValue = GetComputedStyleEnumValue(val, styleType);

                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    var enumStr = string.Empty;

                    switch (styleValue.valueType)
                    {
                        // Some keywords may conflict with the enum values. We have
                        // to check for both here.
                        // Ex: display: none;
                        case StyleValueType.Keyword when styleProperty.TryGetKeyword(styleSheet, out StyleValueKeyword propertyValue):
                        {
                            enumStr = propertyValue.ToString();
                            break;
                        }
                        case StyleValueType.Enum when styleProperty.TryGetEnumString(styleSheet, out var propertyValue):
                            enumStr = propertyValue;
                            break;
                        default:
                            Debug.LogError("UIBuilder: Value type is enum but style property value type is not enum: " + styleProperty.name);
                            break;
                    }

                    if (!string.IsNullOrEmpty(enumStr))
                    {
                        var enumStrHungarian = StyleSheetUtility.ConvertDashToHungarian(enumStr);
                        if (!Enum.TryParse(enumValue.GetType(), enumStrHungarian, out var enumObj))
                        {
                            var values = string.Join(',', Enum.GetNames(enumValue.GetType()));
                            var error = $"UIBuilder: Unexpected Enum Value {enumStrHungarian}: {styleProperty.name}. Expected values are: {values}";
                            Debug.LogError(error);
                        }
                        if (enumObj is Enum enumEnum)
                            enumValue = enumEnum;
                    }
                }

                // The state of Flex Direction can affect many other Flex-related fields.
                if (styleName == "flex-direction")
                    UpdateFlexStyleFieldIcons(enumValue);

                if (styleName == "position")
                    updatePositionAnchorsFoldoutState?.Invoke(enumValue);

                switch (fieldElement)
                {
                    case EnumField field:
                    {
                        var uiField = field;
                        uiField.SetValueWithoutNotify(enumValue);
                        break;
                    }
                    case FontStyleStrip fontStyleStripField:
                    {
                        var enumStr = StyleSheetUtility.ConvertCamelToDash(enumValue.ToString());
                        fontStyleStripField.SetValueWithoutNotify(enumStr);
                        break;
                    }
                    case TextAlignStrip textAlignStripField:
                    {
                        var enumStr = StyleSheetUtility.ConvertCamelToDash(enumValue.ToString());
                        textAlignStripField.SetValueWithoutNotify(enumStr);
                        break;
                    }
                    case ToggleButtonGroup group:
                    {
                        var uiField = group;
                        var options = new ToggleButtonGroupState(0, 64);
                        options[Convert.ToInt32(enumValue)] = true;
                        uiField.SetValueWithoutNotify(options);
                        break;
                    }
                    default:
                        // Unsupported style value type.
                        return false;
                }

                return true;
            }

            return false;
        }

        internal void UpdateOverrideStyles(VisualElement fieldElement, StyleProperty styleProperty)
        {
            // Add override style to field if it is overwritten.
            var styleRow = fieldElement.GetContainingRow();
            Assert.IsNotNull(styleRow);
            if (styleRow == null)
                return;

            var handler = StyleVariableUtilities.GetOrCreateVarHandler(fieldElement as BindableElement);
            if (handler != null)
            {
                handler.editingEnabled = BuilderSharedStyles.IsSelectorElement(currentVisualElement);
                handler.RefreshField();
            }

            var styleFields = styleRow.Query<BindableElement>().Build();

            bool isRowOverride = false;

            foreach (var styleField in styleFields)
            {
                var lastStyleProperty = GetLastStyleProperty(currentRule, styleField.bindingPath);
                var fieldHasBinding = BuilderInspectorUtilities.HasBinding(m_Inspector, styleField);

                if (lastStyleProperty != null || fieldHasBinding)
                {
                    isRowOverride = true;
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                }
                else if (!string.IsNullOrEmpty(styleField.bindingPath))
                {
                    styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                }
            }

            var isFieldOverridden = styleProperty != null || isRowOverride;
            styleRow.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, isFieldOverridden);

            if (!isFieldOverridden)
            {
                foreach (var styleField in styleFields)
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                }
            }

            if (fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) is FoldoutField foldout)
            {
                foldout.UpdateFromChildFields();

                // disable initially so we can check if we have any overridden fields, otherwise it'll think of the header as an overriden field (UUM-53358)
                foldout.header.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, false);

                var hasOverriddenField = BuilderInspectorUtilities.HasOverriddenField(foldout);
                foldout.header.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, hasOverriddenField);
            }
        }

        public void DispatchChangeEvent(string styleName, VisualElement fieldElement)
        {
            var styleType = StyleDebug.GetComputedStyleType(styleName);
            if (styleType == null)
                return;

            var val = StyleDebug.GetComputedStyleValue(fieldElement.computedStyle, styleName);

            if (IsComputedStyleFloat(val) && fieldElement is FloatField floatField)
            {
                DispatchChangeEvent(floatField);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is IntegerField integerField)
            {
                DispatchChangeEvent(integerField);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is PercentSlider percentSlider)
            {
                DispatchChangeEvent(percentSlider);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is NumericStyleField numericStyleField)
            {
                DispatchChangeEvent(numericStyleField);
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerField intField)
            {
                DispatchChangeEvent(intField);
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerStyleField integerStyleField)
            {
                DispatchChangeEvent(integerStyleField);
            }
            else if (IsComputedStyleLength(val) && fieldElement is IntegerField intLengthField)
            {
                DispatchChangeEvent(intLengthField);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is DimensionStyleField dimensionFloatField)
            {
                DispatchChangeEvent(dimensionFloatField);
            }
            else if (IsComputedStyleLength(val) && fieldElement is DimensionStyleField dimensionLengthField)
            {
                DispatchChangeEvent(dimensionLengthField);
            }
            else if (IsComputedStyleColor(val) && fieldElement is ColorField colorField)
            {
                DispatchChangeEvent(colorField);
            }
            else if (IsComputedStyleFont(val, styleName) && fieldElement is ObjectField objectFontField)
            {
                DispatchChangeEvent(objectFontField);
            }
            else if (IsComputedStyleFontAsset(val, styleName) && fieldElement is FontDefinitionStyleField objectFontAssetField)
            {
                DispatchChangeEvent(objectFontAssetField);
            }
            else if (IsComputedStyleTextShadow(val) &&
                     fieldElement is TextShadowStyleField textShadowStyleField)
            {
                DispatchChangeEvent(textShadowStyleField);
            }
            else if (IsComputedStyleBackground(val) && fieldElement is ImageStyleField imageStyleField)
            {
                DispatchChangeEvent(imageStyleField);
            }
            else if (IsComputedStyleCursor(val) && fieldElement is CursorStyleField cursorStyleField)
            {
                DispatchChangeEvent(cursorStyleField);
            }
            else if (IsComputedStyleList<StylePropertyName>(val) && fieldElement is CategoryDropdownField transitionPropertyField)
            {
                DispatchChangeEvent(transitionPropertyField);
            }
            else if (IsComputedStyleList<TimeValue>(val) && fieldElement is DimensionStyleField transitionDurationOrDelayField)
            {
                DispatchChangeEvent(transitionDurationOrDelayField);
            }
            else if (IsComputedStyleList<EasingFunction>(val) && fieldElement is EnumField transitionTimingFunctionField)
            {
                DispatchChangeEvent(transitionTimingFunctionField);
            }
            else if (IsComputedStyleEnum(val, styleType))
            {
                switch (fieldElement)
                {
                    case EnumField enumField:
                        DispatchChangeEvent(enumField);
                        break;
                    case ToggleButtonGroup toggleButtonGroup:
                        DispatchChangeEvent(toggleButtonGroup);
                        break;
                }
            }
        }

        public void RefreshStyleField(FoldoutField foldoutElement)
        {
            if (foldoutElement is FoldoutNumberField foldoutNumberField)
                RefreshStyleFoldoutNumberField(foldoutNumberField);
            else if (foldoutElement is FoldoutColorField foldoutColorField)
                RefreshStyleFoldoutColorField(foldoutColorField);
        }

        void RefreshStyleFoldoutNumberField(FoldoutNumberField foldoutElement)
        {
            var isDirty = false;

            foreach (var path in foldoutElement.bindingPathArray)
            {
                var cSharpStyleName = BuilderNameUtilities.ConvertUssNameToStyleName(path);
                var styleProperty = GetLastStyleProperty(currentRule, cSharpStyleName);

                var styleType = StyleDebug.GetComputedStyleType(path);
                if (styleType == null)
                    continue;

                if (styleProperty != null)
                {
                    isDirty = true;
                    break;
                }
            }

            foldoutElement.header.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, isDirty);
        }

        void RefreshStyleFoldoutColorField(FoldoutColorField foldoutElement)
        {
            var isDirty = false;
            foreach (var path in foldoutElement.bindingPathArray)
            {
                var cSharpStyleName = BuilderNameUtilities.ConvertUssNameToStyleName(path);
                var styleProperty = GetLastStyleProperty(currentRule, cSharpStyleName);

                var styleType = StyleDebug.GetComputedStyleType(path);
                if (styleType == null)
                    continue;

                var val = StyleDebug.GetComputedStyleValue(currentVisualElement.computedStyle, path);
                if (val is StyleColor)
                {
                    var style = (StyleColor)val;
                    var value = style.value;

                    if (styleProperty != null && !styleProperty.IsVariable())
                    {
                        isDirty = true;
                        value = styleSheet.ReadColor(styleProperty.values[0]);
                    }

                    foldoutElement.UpdateFromChildField(path, value);
                }
            }

            foldoutElement.header.EnableInClassList(BuilderConstants.InspectorLocalStyleOverrideClassName, isDirty);
        }

        void SetVariableEditor(BindableElement element, int displayIndex)
        {
            var handler = StyleVariableUtilities.GetOrCreateVarHandler(element);
            if (handler != null)
            {
                handler.index = displayIndex;
                handler.editingEnabled = BuilderSharedStyles.IsSelectorElement(currentVisualElement);
                handler.RefreshField();
            }
        }

        internal StylePropertyManipulator GetStylePropertyManipulator(string stylePropertyName)
        {
            return styleSheet.GetStylePropertyManipulator(
                currentVisualElement,
                currentRule,
                stylePropertyName,
                m_Inspector.document.fileSettings.editorExtensionMode);
        }

        void BuildStyleFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            BuildStyleFieldContextualMenu(evt.menu, evt.elementTarget);
        }

        void BuildStyleFieldContextualMenu(DropdownMenu menu, VisualElement fieldElement)
        {
            // if the context menu is already populated by the field (text field) then ignore
            if (menu.MenuItems() != null & menu.MenuItems().Count > 0)
                return;

            var isSelector = BuilderSharedStyles.IsSelectorElement(currentVisualElement);

            if (fieldElement.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
            {
                var fieldValueInfo =
                    (FieldValueInfo) fieldElement.GetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName);
                var isBoundToVariable = fieldValueInfo.valueBinding.type == FieldValueBindingInfoType.USSVariable;

                if (!isSelector)
                {
                    var ve = currentVisualElement;
                    var csPropertyName = "style." + BuilderNameUtilities.ConvertUssNameToStyleCSharpName(fieldValueInfo.name);
                    var isBindableElement = UxmlSerializedDataRegistry.GetDescription(ve.GetType().FullName) != null;
                    var isBindableProperty = PropertyContainer.IsPathValid(ve, csPropertyName);

                    // Do show binding related actions if the underlying property is not bindable or if the element is
                    // not using the new serialization system to define attributes.
                    if (isBindableElement && isBindableProperty)
                    {
                        var hasDataBinding = false;
                        var vea = m_Inspector.currentVisualElement.GetVisualElementAsset();

                        if (vea != null)
                        {
                            hasDataBinding = BuilderBindingUtility.TryGetBinding(csPropertyName, out _, out _);
                        }

                        if (hasDataBinding)
                        {
                            menu.AppendAction(BuilderConstants.ContextMenuEditBindingMessage,
                                (a) => BuilderBindingUtility.OpenBindingWindowToEdit(csPropertyName, m_Inspector),
                                (a) => DropdownMenuAction.Status.Normal,
                                fieldElement);
                            menu.AppendAction(BuilderConstants.ContextMenuRemoveBindingMessage,
                                (a) =>
                                {
                                    BuilderBindingUtility.DeleteBinding(fieldElement, csPropertyName);
                                },
                                (a) => DropdownMenuAction.Status.Normal,
                                fieldElement);

                            DataBindingUtility.TryGetLastUIBindingResult(new BindingId(csPropertyName), m_Inspector.currentVisualElement,
                                out var bindingResult);

                            if (bindingResult.status == BindingStatus.Success)
                            {
                                menu.AppendAction(BuilderConstants.ContextMenuEditInlineValueMessage,
                                    (a) => { m_Inspector.EnableInlineValueEditing(fieldElement); },
                                    (a) => DropdownMenuAction.Status.Normal,
                                    fieldElement);
                            }

                            menu.AppendAction(
                                BuilderConstants.ContextMenuUnsetInlineValueMessage,
                                UnsetStylePropertyInlineValue,
                                UnsetInlineValueActionStatus,
                                fieldElement);
                        }
                        else
                        {
                            menu.AppendAction(BuilderConstants.ContextMenuAddBindingMessage,
                                (a) => BuilderBindingUtility.OpenBindingWindowToCreate(csPropertyName, m_Inspector),
                                (a) => DropdownMenuAction.Status.Normal,
                                fieldElement);
                        }

                        menu.AppendSeparator();
                    }

                    var matchingSelectors = BuilderSharedStyles.GetMatchingSelectorsOnElementFromLocalStyleSheet(m_Inspector.currentVisualElement);
                    var styleProperty = GetLastStyleProperty(currentRule, fieldValueInfo.name);
                    var hasInlineValue = styleProperty != null;
                    var hasAnyInlineValue = currentRule.properties.Length > 0;

                    var actionName = BuilderConstants.ContextMenuExtractInlineValueMessage;
                    if (hasInlineValue)
                        actionName += "/" + BuilderConstants.ContextMenuNewClassMessage;
                    menu.AppendAction(actionName, _ => OpenNewSelectorWindow(styleProperty),
                        _ => hasInlineValue ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                        fieldElement);

                    actionName = BuilderConstants.ContextMenuExtractAllInlineValuesMessage;
                    if (hasAnyInlineValue)
                        actionName += "/" + BuilderConstants.ContextMenuNewClassMessage;
                    menu.AppendAction(actionName, _ => OpenNewSelectorWindow(),
                        _ => hasAnyInlineValue ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled,
                        fieldElement);

                    if (matchingSelectors.Count > 0)
                    {
                        if (hasInlineValue) menu.AppendSeparator(BuilderConstants.ContextMenuExtractInlineValueMessage + "/");
                        if (hasAnyInlineValue) menu.AppendSeparator(BuilderConstants.ContextMenuExtractAllInlineValuesMessage + "/");
                    }

                    foreach (var matchingSelector in matchingSelectors)
                    {
                        var selectorStr = StyleSheetToUss.ToUssSelector(matchingSelector.complexSelector);
                        // replace space with Unicode character before # to avoid the shortcut handling in the menu system
                        selectorStr = selectorStr.Replace(" #", "\u00A0#");

                        if (hasInlineValue)
                            menu.AppendAction(BuilderConstants.ContextMenuExtractInlineValueMessage + "/" + selectorStr,
                                _ => PushLocalStylesToSelector(matchingSelector, styleProperty),
                                _ => DropdownMenuAction.Status.Normal,
                                fieldElement);

                        if (hasAnyInlineValue)
                            menu.AppendAction(BuilderConstants.ContextMenuExtractAllInlineValuesMessage + "/" + selectorStr,
                                _ => PushLocalStylesToSelector(matchingSelector),
                                _ => DropdownMenuAction.Status.Normal,
                                fieldElement);

                    }
                }

                if (isBoundToVariable)
                {
                    menu.AppendAction(
                        isSelector
                            ? BuilderConstants.ContextMenuEditVariableMessage
                            : BuilderConstants.ContextMenuViewVariableMessage,
                        ViewVariableViaContextMenu,
                        VariableActionStatus,
                        fieldElement);

                    if (isSelector)
                        menu.AppendAction(BuilderConstants.ContextMenuRemoveVariableMessage, RemoveVariableViaContextMenu,
                            (a) => DropdownMenuAction.Status.Normal, fieldElement);
                }
                else if (isSelector && fieldElement is not BoxModelStyleField)
                {
                    menu.AppendAction(BuilderConstants.ContextMenuSetVariableMessage, ViewVariableViaContextMenu,
                        VariableActionStatus,
                        fieldElement);
                }

                menu.AppendSeparator();

                if (fieldValueInfo.valueSource.type.IsFromUSSSelector())
                {
                    var matchedRule = fieldValueInfo.valueSource.matchedRule;

                    if (!isSelector)
                    {
                        var builder = m_Inspector.paneWindow as Builder;

                        if (builder != null && matchedRule.matchRecord.sheet != null)
                        {
                            // Look for the element matching the source selector
                            var selectorElement = builder.viewport.styleSelectorElementContainer.FindElement(
                                (e) => e.GetStyleComplexSelector() == matchedRule.matchRecord.complexSelector);

                            if (selectorElement != null)
                            {
                                menu.AppendAction(BuilderConstants.ContextMenuGoToSelectorMessage, GoToSelector,
                                    (a) => DropdownMenuAction.Status.Normal,
                                    fieldElement);
                            }
                        }
                    }

                    menu.AppendAction(BuilderConstants.ContextMenuOpenSelectorInIDEMessage, OpenSelectorInIDE,
                        (a) => DropdownMenuAction.Status.Normal,
                        matchedRule);
                    menu.AppendSeparator();
                }
            }

            menu.AppendAction(
                isSelector ? BuilderConstants.ContextMenuSetAsValueMessage : BuilderConstants.ContextMenuSetAsInlineValueMessage,
                SetStyleProperty,
                SetActionStatus,
                fieldElement);

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetMessage,
                UnsetStyleProperty,
                UnsetActionStatus,
                fieldElement);

            menu.AppendAction(
                BuilderConstants.ContextMenuUnsetAllMessage,
                (action) => UnsetAllStyleProperties(),
                UnsetAllActionStatus,
                fieldElement);
        }

        void SetUpContextualMenuOnStyleField(VisualElement fieldElement)
        {
            var menuTarget = fieldElement;

            // show the context menu when right-clicking anywhere in the containing row if the row only contains this field
            if (fieldElement.parent is BuilderStyleRow row && !row.ClassListContains(BuilderConstants.InspectorMultiFieldsRowClassName))
                menuTarget = row;
            // if the field is in a row containing multiple fields then show the context menu when right clicking of the field and its related status indicator.
            else
                fieldElement.GetFieldStatusIndicator().AddManipulator(new ContextualMenuManipulator(evt => BuildStyleFieldContextualMenu(evt.menu, fieldElement)));

            menuTarget.AddManipulator(new ContextualMenuManipulator(evt => BuildStyleFieldContextualMenu(evt.menu, fieldElement)));
            fieldElement.GetFieldStatusIndicator().populateMenuItems = (menu) => BuildStyleFieldContextualMenu(menu, fieldElement);
        }

        bool OnFieldVariableChangeImplInBatch(string newValue, string styleName, int index, out StyleProperty property)
        {
            StyleSheet styleSheet = m_Inspector.styleSheet;
            StyleRule currentRule = m_Inspector.currentRule;

            using (var manipulator = styleSheet.GetStylePropertyManipulator(currentVisualElement, currentRule, styleName, m_Inspector.document.fileSettings.editorExtensionMode))
            {
                var isNewValue = null == manipulator.styleProperty;

                property = manipulator.styleProperty;

                if (index >= manipulator.GetValuesCount()
                    && StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(styleName, out var id)
                    && id.IsTransitionId())
                {
                    var transitionData = m_Inspector.currentVisualElement.computedStyle.transitionData.Read();
                    switch (id)
                    {
                        case StylePropertyId.TransitionProperty:
                            UnpackTransitionProperty(manipulator, transitionData.transitionProperty, transitionData.MaxCount());
                            break;
                        case StylePropertyId.TransitionDuration:
                            UnpackTransitionDurationOrDelay(manipulator, transitionData.transitionDuration, transitionData.MaxCount());
                            break;
                        case StylePropertyId.TransitionTimingFunction:
                            UnpackTransitionTimingFunction(manipulator, transitionData.transitionTimingFunction, transitionData.MaxCount());
                            break;
                        case StylePropertyId.TransitionDelay:
                            UnpackTransitionDurationOrDelay(manipulator, transitionData.transitionDelay, transitionData.MaxCount());
                            break;
                    }
                    manipulator.SetVariableAtIndex(index, newValue);
                }
                else
                {
                    if (isNewValue)
                        manipulator.AddVariable(newValue);
                    else
                        manipulator.SetVariableAtIndex(index, newValue);
                }

                if (styleName != "transition-property" && null != GetLastStyleProperty(currentRule, "transition-property"))
                {
                    var transitionData = currentVisualElement.computedStyle.transitionData.Read();
                    var maxCount = Mathf.Max(transitionData.MaxCount(), manipulator.GetValuesCount());
                    using var propertyManipulator = GetStylePropertyManipulator("transition-property");
                    if (null == manipulator.styleProperty)
                        TransferTransitionComputedData(manipulator, transitionData.transitionProperty, StyleValueType.Enum);
                }

                return isNewValue;
            }
        }

        public void OnFieldVariableChange(string newValue, VisualElement target, string styleName, int index, NotifyType notifyType = NotifyType.Default)
        {
            if (newValue.Length <= BuilderConstants.UssVariablePrefix.Length)
                return;

            StyleProperty styleProperty = null;

            bool isNewValue = OnFieldVariableChangeImplInBatch(newValue, styleName, index, out styleProperty);
            PostStyleFieldSteps(target, styleProperty, styleName, isNewValue, true, notifyType);
        }

        public DropdownMenuAction.Status UnsetAllActionStatus(DropdownMenuAction action)
        {
            foreach (var pair in m_StyleFields)
            {
                var styleFields = pair.Value;

                foreach (var fieldElement in styleFields)
                {
                    if (!fieldElement.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
                    {
                        continue;
                    }

                    var fieldValueInfo = (FieldValueInfo) fieldElement.GetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName);

                    if (fieldValueInfo.valueBinding.type == FieldValueBindingInfoType.Binding)
                    {
                        return DropdownMenuAction.Status.Normal;
                    }
                }
            }

            if (currentRule == null)
                return DropdownMenuAction.Status.Disabled;

            if (currentRule.properties.Length == 0)
                return DropdownMenuAction.Status.Disabled;

            return currentRule.properties.Any(property => !property.name.StartsWith(BuilderConstants.UssVariablePrefix))
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
        }

        DropdownMenuAction.Status VariableActionStatus(DropdownMenuAction action)
        {
            var bindableElement = action.userData as BindableElement;
            if (bindableElement == null)
                return DropdownMenuAction.Status.Disabled;

            var varEditingHandler = StyleVariableUtilities.GetVarHandler(bindableElement);
            if (varEditingHandler == null)
                return DropdownMenuAction.Status.Disabled;

            return DropdownMenuAction.Status.Normal;
        }

        void ViewVariableViaContextMenu(DropdownMenuAction action)
        {
            var bindableElement = action.userData as BindableElement;
            var varEditingHandler = StyleVariableUtilities.GetVarHandler(bindableElement);
            varEditingHandler.ShowVariableField();
        }

        void RemoveVariableViaContextMenu(DropdownMenuAction action)
        {
            var bindableElement = action.userData as BindableElement;

            UnsetStylePropertyForElement(bindableElement, true);
        }

        DropdownMenuAction.Status UnsetInlineValueActionStatus(DropdownMenuAction action)
        {
            var hasBinding = false;
            if (action.userData is VisualElement fieldElement)
            {
                if (fieldElement.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
                {
                    var fieldValueInfo = (FieldValueInfo) fieldElement.GetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName);

                    hasBinding = fieldValueInfo.valueBinding.type == FieldValueBindingInfoType.Binding;
                }
            }

            return StyleActionStatus(action, property => property != null && hasBinding);
        }

        DropdownMenuAction.Status UnsetActionStatus(DropdownMenuAction action)
        {
            var hasBinding = false;
            if (action.userData is VisualElement fieldElement)
            {
                if (fieldElement.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
                {
                    var fieldValueInfo = (FieldValueInfo) fieldElement.GetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName);

                    hasBinding = fieldValueInfo.valueBinding.type == FieldValueBindingInfoType.Binding;
                }
            }

            return StyleActionStatus(action, property => property != null || hasBinding);
        }

        DropdownMenuAction.Status StyleActionStatus(DropdownMenuAction action, Func<StyleProperty, bool> normalStatusCondition)
        {
            var fieldElement = action.userData as VisualElement;
            var listToUnset = fieldElement?.userData;
            if (listToUnset != null && listToUnset is List<BindableElement> bindableElements)
            {
                return CanUnsetStyleProperties(bindableElements, normalStatusCondition);
            }

            return CanUnsetStyleProperties(new List<VisualElement> { fieldElement }, normalStatusCondition);
        }

        DropdownMenuAction.Status SetActionStatus(DropdownMenuAction action)
        {
            var isBoundToVariable = false;

            if (action.userData is VisualElement fieldElement)
            {
                var styleName =
                    fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                if (styleName == "-unity-font" || styleName == "-unity-font-definition")
                    return DropdownMenuAction.Status.Disabled;

                if (fieldElement.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
                {
                    var fieldValueInfo = (FieldValueInfo) fieldElement.GetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName);

                    isBoundToVariable = fieldValueInfo.valueBinding.type == FieldValueBindingInfoType.USSVariable;
                }
            }

            return StyleActionStatus(action, property => property == null || isBoundToVariable);
        }

        DropdownMenuAction.Status CanUnsetStyleProperties(IEnumerable<VisualElement> fields, Func<StyleProperty, bool> normalStatusCondition)
        {
            foreach (var fieldElement in fields)
            {
                StyleProperty styleProperty;
                if (fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) is FoldoutField foldout)
                {
                    // Check if the unset element was the header field.
                    if (fieldElement.ClassListContains(BuilderConstants.FoldoutFieldHeaderClassName))
                    {
                        foreach (var path in foldout.bindingPathArray)
                        {
                            styleProperty = styleSheet.FindLastProperty(currentRule, path);
                            if (normalStatusCondition(styleProperty))
                            {
                                return DropdownMenuAction.Status.Normal;
                            }
                        }
                    }
                }

                var styleName = fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                if (!string.IsNullOrEmpty(styleName))
                {
                    styleProperty = styleSheet.FindLastProperty(currentRule, styleName);
                    if (normalStatusCondition(styleProperty))
                        return DropdownMenuAction.Status.Normal;
                }
            }

            return DropdownMenuAction.Status.Disabled;
        }

        public void UnsetAllStyleProperties()
        {
            var fields = new List<VisualElement>();
            foreach (var pair in m_StyleFields)
            {
                var styleFields = pair.Value;
                fields.AddRange(styleFields);
            }

            UnsetStyleProperties(fields, true);
        }

        void SetStyleProperty(DropdownMenuAction action)
        {
            var listToUnset = (action.userData as VisualElement)?.userData;
            if (listToUnset != null && listToUnset is List<BindableElement> bindableElements)
            {
                SetStyleProperties(bindableElements);
                NotifyStyleChanges();
                return;
            }

            var fieldElement = action.userData as VisualElement;
            SetStyleProperties(new List<VisualElement> { fieldElement });
            NotifyStyleChanges();
        }

        void SetStyleProperties(IEnumerable<VisualElement> fields)
        {
            foreach (var fieldElement in fields)
            {
                var styleName = fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
                RefreshStyleField(styleName, fieldElement);
                DispatchChangeEvent(styleName, fieldElement);

                if (fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) is FoldoutField foldout)
                {
                    // Check if the unset element was the header field.
                    if (fieldElement.ClassListContains(BuilderConstants.FoldoutFieldHeaderClassName))
                    {
                        foreach (var path in foldout.bindingPathArray)
                        {
                            foreach (var linkedField in m_StyleFields[path])
                            {
                                RefreshStyleField(path, linkedField);
                                DispatchChangeEvent(path, linkedField);
                            }
                        }
                    }
                }
            }
        }

        void UnsetStylePropertyInlineValue(DropdownMenuAction action)
        {
            UnsetStylePropertyForElement(action.userData as VisualElement, false);
        }

        void UnsetStyleProperty(DropdownMenuAction action)
        {
            UnsetStylePropertyForElement(action.userData as VisualElement, true);
        }

        public void UnsetStylePropertyForElement(VisualElement fieldElement, bool removeBinding)
        {
            var listToUnset = fieldElement?.userData;
            if (listToUnset != null && listToUnset is List<BindableElement> bindableElements)
            {
                UnsetStyleProperties(bindableElements, removeBinding);
                return;
            }

            UnsetStyleProperties(new List<VisualElement> { fieldElement }, removeBinding);
        }

        public void UnsetStyleProperties(IEnumerable<VisualElement> fields, bool removeBinding)
        {
            var undoGroup = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(
                m_Inspector.document.visualTreeAsset, BuilderConstants.AddStyleClassUndoMessage);

            foreach (var fieldElement in fields)
            {
                var styleName = fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

                // TODO: The computed style still has the old (set) value at this point.
                // We need to reset the field with the value after styling has been
                // recomputed.

                styleSheet.RemoveProperty(currentRule, styleName);

                if (!string.IsNullOrEmpty(styleName) && fieldElement is not TransitionsListView && !BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                {
                    var cSharpStyleName = BuilderNameUtilities.ConvertStyleUssNameToCSharpName(styleName);
                    var bindingProperty = $"style.{cSharpStyleName}";

                    var elementHasBinding = BuilderInspectorUtilities.HasBinding(m_Inspector, fieldElement);
                    if (elementHasBinding && removeBinding)
                    {
                        m_Inspector.attributeSection.RemoveBindingFromSerializedData(fieldElement, bindingProperty);
                    }
                }

                var foldout = fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) as FoldoutField;
                if (foldout != null)
                {
                    // Check if the unset element was the header field.
                    if (fieldElement.ClassListContains(BuilderConstants.FoldoutFieldHeaderClassName))
                    {
                        foreach (var path in foldout.bindingPathArray)
                        {
                            styleSheet.RemoveProperty(currentRule, path);
                        }
                    }
                }

                if (fieldElement is TransitionsListView)
                {
                    styleSheet.RemoveProperty(currentRule, TransitionConstants.Property);
                    styleSheet.RemoveProperty(currentRule, TransitionConstants.Duration);
                    styleSheet.RemoveProperty(currentRule, TransitionConstants.TimingFunction);
                    styleSheet.RemoveProperty(currentRule, TransitionConstants.Delay);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
            NotifyStyleChanges(null, true);
        }

        void GoToSelector(DropdownMenuAction action)
        {
            var field = action.userData as VisualElement;

            GoToSelector(field);
        }

        // Public for testing
        public void GoToSelector(VisualElement field)
        {
            var fieldValueInfo = (FieldValueInfo)field.GetProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName);
            var matchedRule = fieldValueInfo.valueSource.matchedRule;
            var builder = m_Inspector.paneWindow as Builder;

            if (builder != null && matchedRule.matchRecord.sheet != null)
            {
                // Look for the element matching the source selector
                var selectorElement = builder.viewport.styleSelectorElementContainer.FindElement(
                    (e) => e.GetStyleComplexSelector() == matchedRule.matchRecord.complexSelector);

                if (selectorElement != null)
                {
                    m_Selection.Select(null, selectorElement);
                }
            }
        }

        void OpenSelectorInIDE(DropdownMenuAction action)
        {
            var matchedRule = (MatchedRule) action.userData;
            bool opened = false;

            if (!string.IsNullOrEmpty(matchedRule.displayPath) && File.Exists(matchedRule.fullPath))
            {
                opened = InternalEditorUtility.OpenFileAtLineExternal(matchedRule.fullPath, matchedRule.lineNumber, -1);
            }

            if (!opened)
            {
                Builder.ShowWarning(BuilderConstants.CouldNotOpenSelectorMessage);
            }
        }

        void OpenNewSelectorWindow(StyleProperty styleProperty = null)
        {
            if (BuilderNewClassWindow.activeWindow != null)
                BuilderNewClassWindow.activeWindow.Close();

            var worldBound = GUIUtility.GUIToScreenRect(m_Inspector.document.primaryViewportWindow.viewport.worldBound);
            var window = BuilderNewClassWindow.Open(worldBound);
            window.OnClassCreated += (className) => ExtractLocalStylesToNewClass(styleProperty, className);
            window.ShowModal();
        }

        void ExtractLocalStylesToNewClass(StyleProperty styleProperty, StyleComplexSelector className)
        {
            var classNameStr = StyleSheetToUss.ToUssSelector(className);
            classNameStr = classNameStr.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);
            // Add the new selector to the current element
            currentVisualElement.AddToClassList(classNameStr);
            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(m_Inspector.document, currentVisualElement, classNameStr);

            // Get Selector Match Record from the new selector
            var selectorMatchRecord = new SelectorMatchRecord(m_Inspector.document.activeStyleSheet, 0)
            {
                complexSelector = className
            };
            PushLocalStylesToSelector(selectorMatchRecord, styleProperty);
        }

        internal void PushLocalStylesToSelector(SelectorMatchRecord matchingSelector, StyleProperty styleProperty = null)
        {
            // Get StyleSheet this selector belongs to.
            var toStyleSheet = matchingSelector.sheet;
            if (toStyleSheet == null)
                return;

            var selector = matchingSelector.complexSelector;

            // Transfer property from inline styles rule to selector.
            if (styleProperty != null)
                toStyleSheet.TransferPropertyToSelector(selector, styleSheet, currentRule, styleProperty);
            else
                // Transfer all properties from inline styles rule to selector.
                toStyleSheet.TransferRulePropertiesToSelector(selector, styleSheet, currentRule);

            // Overwrite Undo Message.
            Undo.RegisterCompleteObjectUndo(
                new Object[] { m_Inspector.document.visualTreeAsset, toStyleSheet },
                BuilderConstants.ContextMenuExtractInlineValueMessage);

            // We actually want to get the notification back and refresh ourselves.
            m_Inspector.selection.NotifyOfStylingChange();
            m_Inspector.selection.NotifyOfHierarchyChange(null, m_Inspector.currentVisualElement);
        }

        void NotifyStyleChanges(List<string> styles = null, bool selfNotify = false, NotifyType notifyType = NotifyType.Default, bool ignoreUnsavedChanges = false)
        {
            BuilderStylingChangeType styleChangeType;
            var hierarchyChangeType = BuilderHierarchyChangeType.InlineStyle;
            if (notifyType == NotifyType.RefreshOnly || ignoreUnsavedChanges)
            {
                styleChangeType = BuilderStylingChangeType.RefreshOnly;
            }
            else
            {
                styleChangeType = BuilderStylingChangeType.Default;
                hierarchyChangeType |= BuilderHierarchyChangeType.FullRefresh;
            }

            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
            {
                m_Selection.NotifyOfStylingChange(selfNotify ? null : m_Inspector, styles, styleChangeType);
                currentVisualElement.IncrementVersion(VersionChangeTypeUtility.StylingChanged());
            }
            else
            {
                // UUM-97052
                // The order of these notifications is important. NotifyOfStylingChange will clear the StyleCache.
                // Otherwise, we call IncrementVersion without the correct flags due to an inaccurate ComputedStyle comparison.
                if (!ignoreUnsavedChanges)
                    m_Selection.NotifyOfHierarchyChange(m_Inspector, currentVisualElement, hierarchyChangeType);
                else
                    m_Selection.ForceVisualAssetUpdateWithoutSave(currentVisualElement, hierarchyChangeType);

                m_Selection.NotifyOfStylingChange(selfNotify ? null : m_Inspector, styles, styleChangeType);
            }
        }

        // Style Updates

        StyleProperty GetOrCreateStylePropertyByStyleName(string styleName)
        {
            var styleProperty = styleSheet.FindLastProperty(currentRule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(currentRule, styleName);

            return styleProperty;
        }

        internal void PostStyleFieldSteps(VisualElement target, StyleProperty styleProperty, string styleName, bool isNewValue, bool isVariable = false, NotifyType notifyType = NotifyType.Default, bool ignoreUnsavedChanges = false)
        {
            // If inline editing is enabled on a property that has a UXML binding, we cache the binding
            // to preview the inline value in the canvas.
            if (m_Inspector.cachedBinding != null)
            {
                currentVisualElement.ClearBinding(m_Inspector.cachedBinding.property);
                ResetInlineStyle(styleName);
            }

            if (isVariable)
            {
                var foldout = target.GetProperty(BuilderConstants.FoldoutFieldPropertyName) as FoldoutField;

                if (foldout != null)
                {
                    foldout.header.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                }
            }
            else
            {
                target.RemoveFromClassList(BuilderConstants.InspectorLocalStyleVariableClassName);

                var styleRow = target.GetContainingRow();
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

                var styleFields = styleRow.Query<BindableElement>();

                var bindableElement = target as BindableElement;
                foreach (var styleField in styleFields.Build())
                {
                    styleField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    if (bindableElement.bindingPath == styleField.bindingPath)
                    {
                        styleField.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);
                    }
                    else if (!string.IsNullOrEmpty(styleField.bindingPath) &&
                             bindableElement.bindingPath != styleField.bindingPath &&
                             !styleField.ClassListContains(BuilderConstants.InspectorLocalStyleOverrideClassName))
                    {
                        styleField.AddToClassList(BuilderConstants.InspectorLocalStyleResetClassName);
                    }
                }
            }

            s_StyleChangeList.Clear();
            s_StyleChangeList.Add(styleName);
            NotifyStyleChanges(s_StyleChangeList, isVariable, notifyType, ignoreUnsavedChanges);

            if (isNewValue && updateStyleCategoryFoldoutOverrides != null)
                updateStyleCategoryFoldoutOverrides();
            m_Inspector.UpdateFieldStatus(target, styleProperty);

            var list = m_StyleFields[styleName];

            foreach (var item in list.Where(item => item != target))
            {
                RefreshStyleField(styleName, item);
            }
        }

        internal void ResetInlineStyle(string styleName)
        {
            var style = currentVisualElement.style;
            PropertyContainer.TrySetValue(ref style, styleName, StyleKeyword.Null);
            var vea = currentVisualElement.GetVisualElementAsset();
            var vta = m_Inspector.document.visualTreeAsset;
            if (vea == null || vea.ruleIndex < 0)
                return;

            var rule = vta.inlineSheet.GetRule(vea.ruleIndex);
            currentVisualElement.UpdateInlineRule(vta.inlineSheet, rule);
        }

        void OnFieldKeywordChange(StyleValueKeyword keyword, VisualElement target, string styleName, NotifyType notifyType = NotifyType.Default)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetKeyword(styleSheet, keyword);
            PostStyleFieldSteps(target, styleProperty, styleName, isNewValue, false, notifyType);
        }

        void OnFieldDimensionChangeImpl(float newValue, Dimension.Unit newUnit, VisualElement target, string styleName, NotifyType notifyType = NotifyType.Default)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetDimension(styleSheet, new Dimension { unit = newUnit, value = newValue });
            PostStyleFieldSteps(target, styleProperty, styleName, isNewValue, false, notifyType);
        }

        void OnFieldDimensionChange(ChangeEvent<int> e, string styleName)
        {
            OnFieldDimensionChangeImpl(e.newValue, Dimension.Unit.Pixel, e.elementTarget, styleName);
        }

        void OnDimensionStyleFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var dimensionStyleField = e.target as DimensionStyleField;
            var notifyType = dimensionStyleField.isUsingLabelDragger ? NotifyType.RefreshOnly : NotifyType.Default;

            if (!dimensionStyleField.isKeyword)
            {
                dimensionStyleField.length = Math.Clamp(dimensionStyleField.length, dimensionStyleField.min, dimensionStyleField.max);
            }

            if (!dimensionStyleField.isUsingLabelDragger && BuilderSharedStyles.IsSelectorElement(currentVisualElement))
            {
                var startsWithUssVariablePrefix = e.newValue.Trim().StartsWith(BuilderConstants.UssVariablePrefix);

                if (startsWithUssVariablePrefix)
                {
                    OnFieldVariableChange(
                        StyleSheetUtilities.GetCleanVariableName(e.newValue),
                        dimensionStyleField,
                        styleName,
                        0,
                        notifyType);

                    return;
                }
            }

            if (dimensionStyleField.isKeyword)
            {
                OnFieldKeywordChange(
                    dimensionStyleField.keyword,
                    dimensionStyleField,
                    styleName,
                    notifyType);
            }
            else
            {
                OnFieldDimensionChangeImpl(
                    dimensionStyleField.length,
                    dimensionStyleField.unit,
                    dimensionStyleField,
                    styleName,
                    notifyType);
            }
        }

        bool OnFieldValueChangeImplBatch(int newValue, VisualElement target, string styleName, out StyleProperty styleProperty)
        {
            styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetInt(styleSheet, newValue);
            return isNewValue;
        }

        void OnFieldValueChangeImpl(int newValue, VisualElement target, string styleName)
        {
            StyleProperty styleProperty = null;
            bool isNewValue = OnFieldValueChangeImplBatch(newValue, target, styleName, out styleProperty);

            PostStyleFieldSteps(target, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<int> e, string styleName)
        {
            OnFieldValueChangeImpl(e.newValue, e.elementTarget, styleName);
        }

        void OnFieldValueChangeIntToFloat(ChangeEvent<int> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetFloat(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeImpl(float newValue, VisualElement target, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetFloat(styleSheet, newValue);
            PostStyleFieldSteps(target, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<float> e, string styleName)
        {
            OnFieldValueChangeImpl(e.newValue, e.elementTarget, styleName);
        }

        void OnNumericStyleFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var numericStyleField = e.target as NumericStyleField;
            bool isVar = e.newValue.Trim().StartsWith(BuilderConstants.UssVariablePrefix);

            if (isVar && BuilderSharedStyles.IsSelectorElement(currentVisualElement))
            {
                OnFieldVariableChange(StyleSheetUtilities.GetCleanVariableName(e.newValue), numericStyleField, styleName, 0);
            }
            else if (numericStyleField.isKeyword)
            {
                OnFieldKeywordChange(
                    numericStyleField.keyword,
                    numericStyleField,
                    styleName);
            }
            else
            {
                OnFieldValueChangeImpl(
                    numericStyleField.number,
                    numericStyleField,
                    styleName);
            }
        }

        void OnIntegerStyleFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var styleField = e.target as IntegerStyleField;
            bool isVar = e.newValue.Trim().StartsWith(BuilderConstants.UssVariablePrefix);

            if (isVar && BuilderSharedStyles.IsSelectorElement(currentVisualElement))
            {
                OnFieldVariableChange(StyleSheetUtilities.GetCleanVariableName(e.newValue), styleField, styleName, 0);
            }
            else if (styleField.isKeyword)
            {
                OnFieldKeywordChange(
                    styleField.keyword,
                    styleField,
                    styleName);
            }
            else
            {
                OnFieldValueChangeImpl(
                    styleField.number,
                    styleField,
                    styleName);
            }
        }

        void OnFieldValueChange(ChangeEvent<Color> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetColor(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldToggleButtonGroupChange<T>(ChangeEvent<T> e, string styleName)
        {
            var field = e.elementTarget;

            // HACK: For some reason, when using "Pick Element" feature of Debugger and
            // hovering over the button strips, we get bogus value change events with
            // empty strings.
            if ((field is FontStyleStrip || field is TextAlignStrip) && string.IsNullOrEmpty(e.newValue as string))
                return;

            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            Enum newEnumValue;
            if (typeof(T) == typeof(string))
            {
                Type enumType;

                if (field is FontStyleStrip fontStyleStripField)
                    enumType = fontStyleStripField.userData as Type;
                else
                    enumType = (field as TextAlignStrip).userData as Type;

                var newValue = e.newValue as string;
                var newEnumValueStr = StyleSheetUtility.ConvertDashToHungarian(newValue);
                newEnumValue = Enum.Parse(enumType, newEnumValueStr) as Enum;
            }
            else
            {
                Span<int> selected = stackalloc int[0];
                if (e.newValue is ToggleButtonGroupState toggleButtonGroupState)
                    selected = toggleButtonGroupState.GetActiveOptions(stackalloc int[toggleButtonGroupState.length]);

                // These properties have only one active state
                var newValue = selected.IsEmpty == false ? selected[0] : 0;
                var enumType = (field as ToggleButtonGroup).userData as Type;
                newEnumValue = Enum.GetValues(enumType).GetValue(newValue) as Enum;
            }

            styleProperty.SetEnum(styleSheet, newEnumValue);

            // The state of Flex Direction can affect many other Flex-related fields.
            if (styleName == "flex-direction")
                UpdateFlexStyleFieldIcons(newEnumValue);

            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void UpdateFlexStyleFieldIcons(Enum value)
        {
            (string folderPath, bool isReverse) flexStyleIconConfig = (
                $"{iconsFolderName[value.ToString()]}/",
                (FlexDirection)value == FlexDirection.ColumnReverse ||
                (FlexDirection)value == FlexDirection.RowReverse
            );

            var index = 0;
            foreach (var toggleButtonGroup in m_FlexAlignmentToggleButtonGroups)
            {
                var buttons = toggleButtonGroup.Query<Button>(className: "unity-button-group__button").ToList();
                var count = buttons.Count;
                Dictionary<string, string> dictionary = new();
                for (var i = 0; i < count; i++)
                {
                    if (buttons[i].text == "AUTO") continue;

                    dictionary = m_FlexDirectionDependentStyleNames[index] switch
                    {
                        "align-items" when flexStyleIconConfig.folderPath.Contains("Column") => flexStyleIconConfig.isReverse
                            ? m_AlignItemsColumnReverseIcons
                            : m_AlignItemsColumnIcons,
                        "align-items" => flexStyleIconConfig.isReverse ? m_AlignItemsRowReverseIcons : m_AlignItemsRowIcons,
                        "justify-content" when flexStyleIconConfig.folderPath.Contains("Column") => flexStyleIconConfig.isReverse
                            ? m_JustifyContentColumnReverseIcons
                            : m_JustifyContentColumnIcons,
                        "justify-content" => flexStyleIconConfig.isReverse ? m_JustifyContentRowReverseIcons : m_JustifyContentRowIcons,
                        _ => dictionary
                    };

                    var path = $"{iconsFolderName[m_FlexDirectionDependentStyleNames[index]]}/{flexStyleIconConfig.folderPath}";
                    var enumValue = Enum.GetValues(toggleButtonGroup.userData as Type).GetValue(i).ToString();

                    if (dictionary.TryGetValue(enumValue, out var iconName))
                        buttons[i].iconImage = BuilderInspectorUtilities.LoadIcon(iconName, path);
                    else
                        buttons[i].iconImage = BuilderInspectorUtilities.LoadIcon(BuilderNameUtilities.ConvertCamelToHuman(enumValue), path);
                }
                index++;
            }
        }

        void OnFieldValueChange(ChangeEvent<Object> e, string styleName)
        {
            OnFieldValueChangeImpl(e.target, e.newValue, e.previousValue, styleName);
        }

        void OnFieldValueChangeImpl(IEventHandler target, Object newValue, Object previousValue, string styleName)
        {
            if (newValue != null)
            {
                OnFieldObjectValueChange(target, newValue, previousValue, styleName);
            }
            else
            {
                var keywords = StyleFieldConstants.GetStyleKeywords(styleName);
                if (keywords == StyleFieldConstants.KLNone)
                    OnFieldKeywordChange(StyleValueKeyword.None, target as VisualElement, styleName);
                else if (keywords == StyleFieldConstants.KLAuto)
                    OnFieldKeywordChange(StyleValueKeyword.Auto, target as VisualElement, styleName);
                else
                    OnFieldKeywordChange(StyleValueKeyword.Initial, target as VisualElement, styleName);
            }
        }

        void OnFieldObjectValueChange(IEventHandler target, Object newValue, Object previousValue, string styleName)
        {
            var assetPath = AssetDatabase.GetAssetPath(newValue);
            if (BuilderAssetUtilities.IsBuiltinPath(assetPath))
            {
                Builder.ShowWarning(BuilderConstants.BuiltInAssetPathsNotSupportedMessage);

                // Revert the change.
                ((BaseField<Object>)target).SetValueWithoutNotify(previousValue);
                return;
            }

            var resourcesPath = BuilderAssetUtilities.GetResourcesPathForAsset(assetPath);
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            if (string.IsNullOrEmpty(resourcesPath))
                styleProperty.SetAssetReference(styleSheet, newValue);
            else
                styleProperty.SetResourcePath(styleSheet, resourcesPath);

            PostStyleFieldSteps(target as VisualElement, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<Cursor> e, string styleName)
        {
            var newValue = e.newValue;
            var assetPath = AssetDatabase.GetAssetPath(newValue.texture);
            if (BuilderAssetUtilities.IsBuiltinPath(assetPath))
            {
                Builder.ShowWarning(BuilderConstants.BuiltInAssetPathsNotSupportedMessage);

                // Revert the change.
                ((BaseField<Cursor>)e.target).SetValueWithoutNotify(e.previousValue);
                return;
            }

            var resourcesPath = BuilderAssetUtilities.GetResourcesPathForAsset(assetPath);
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

            if (newValue.texture == null)
            {
                styleProperty.SetKeyword(styleSheet, StyleKeyword.Initial);
            }
            else if (newValue.hotspot == Vector2.zero)
            {
                if (string.IsNullOrEmpty(resourcesPath))
                    styleProperty.SetAssetReference(styleSheet, newValue.texture);
                else
                    styleProperty.SetResourcePath(styleSheet, resourcesPath);
            }
            else
            {
                if (styleProperty.handleCount != 3)
                    styleProperty.values = new StyleValueHandle[3];
                if (string.IsNullOrEmpty(resourcesPath))
                    styleSheet.WriteAssetReference(ref styleProperty.values[0], newValue.texture);
                else
                    styleSheet.WriteResourcePath(ref styleProperty.values[0], resourcesPath);
                styleSheet.WriteFloat(ref styleProperty.values[1], newValue.hotspot.x);
                styleSheet.WriteFloat(ref styleProperty.values[2], newValue.hotspot.y);
            }

            PostStyleFieldSteps(e.target as VisualElement, styleProperty, styleName, isNewValue);
        }


        void OnFieldValueChangeTextShadow(ChangeEvent<TextShadow> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetTextShadow(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeTransformOrigin(ChangeEvent<TransformOrigin> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetTransformOrigin(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeTranslate(ChangeEvent<Translate> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetTranslate(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeRotate(ChangeEvent<Rotate> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetRotate(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeBackgroundRepeat(ChangeEvent<BackgroundRepeat> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetBackgroundRepeat(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeBackgroundSize(ChangeEvent<BackgroundSize> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetBackgroundSize(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeBackgroundPosition(ChangeEvent<BackgroundPosition> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetBackgroundPosition(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeScale(ChangeEvent<Scale> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetScale(styleSheet, e.newValue);
            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<Enum> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = !styleProperty.HasValue();

            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            styleProperty.SetEnum(styleSheet, e.newValue);

            // The state of Flex Direction can affect many other Flex-related fields.
            if (styleName == "flex-direction")
                UpdateFlexStyleFieldIcons(e.newValue);

            if (styleName == "position")
                updatePositionAnchorsFoldoutState?.Invoke(e.newValue);

            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        private void SetXAndYSubFieldsTooltips(VisualElement parentField, string xFieldName, string xTooltip, string yFieldName, string yTooltip)
        {
            var xField =
                parentField.Q<DimensionStyleField>(xFieldName);
            if (xField != null)
            {
                xField.visualInput.tooltip = xTooltip;
            }
            var yField =
                parentField.Q<DimensionStyleField>(yFieldName);
            if (yField != null)
            {
                yField.visualInput.tooltip = yTooltip;
            }
        }

        // TODO: Transition Utilities (to be moved to new BuilderStyleUtilities.cs once the Intuitive Placement branch is merged)
        //  https://github.cds.internal.unity3d.com/unity/com.unity.ui.builder/pull/164

        // TODO: Use StyleDebug namespace to replace this reflection with built-in internal APIs
        //  https://jira.unity3d.com/browse/UIT-1093

        // Value Questions

        static public StyleKeyword GetComputedStyleKeyword(object val)
        {
            if (val is Length length)
            {
                if (length.IsNone())
                    return StyleKeyword.None;
                else if (length.IsAuto())
                    return StyleKeyword.Auto;

                return StyleKeyword.Undefined;
            }
            else if (val is StyleLength styleLength)
            {
                return styleLength.keyword;
            }

            return StyleKeyword.Undefined;
        }

        static public LengthUnit GetComputedStyleLengthUnit(object val)
        {
            if (val is Length length)
            {
                return length.unit;
            }
            else if (val is StyleLength styleLength)
            {
                return styleLength.value.unit;
            }

            return LengthUnit.Pixel;
        }

        // Type Questions

        static public bool IsComputedStyleFloat(object val)
        {
            return val is StyleFloat || val is float;
        }

        static public bool IsComputedStyleInt(object val)
        {
            return val is StyleInt || val is int;
        }

        static public bool IsComputedStyleLength(object val)
        {
            return val is StyleLength || val is Length;
        }

        static public bool IsComputedStyleColor(object val)
        {
            return val is StyleColor || val is Color;
        }

        static public bool IsComputedStyleFont(object val, string styleName)
        {
            return val is StyleFont || val is Font || styleName == "-unity-font";
        }

        public static bool IsComputedStyleList<T>(object val)
        {
            return val is StyleList<T> || val is List<T>;
        }

        static public bool IsComputedStyleFontAsset(object val, string styleName)
        {
            return IsComputedStyleFont(val, styleName) ||
                val is StyleFontDefinition ||
                val is FontDefinition ||
                val is FontAsset ||
                styleName == "-unity-font-definition";
        }

        static public bool IsComputedStyleTextShadow(object val)
        {
            return val is StyleTextShadow || val is TextShadow;
        }

        static public bool IsComputedStyleTransformOrigin(object val)
        {
            return val is StyleTransformOrigin || val is TransformOrigin;
        }

        static public bool IsComputedStyleTranslate(object val)
        {
            return val is StyleTranslate || val is Translate;
        }

        static public bool IsComputedStyleRotate(object val)
        {
            return val is StyleRotate || val is Rotate;
        }

        static public bool IsComputedStyleBackgroundRepeat(object val)
        {
            return val is StyleBackgroundRepeat || val is BackgroundRepeat;
        }

        static public bool IsComputedStyleBackgroundSize(object val)
        {
            return val is StyleBackgroundSize || val is BackgroundSize;
        }

        static public bool IsComputedStyleBackgroundPosition(object val)
        {
            return val is StyleBackgroundPosition || val is BackgroundPosition;
        }

        static public bool IsComputedStyleScale(object val)
        {
            return val is StyleScale || val is Scale;
        }

        static public bool IsComputedStyleBackground(object val)
        {
            return val is StyleBackground || val is Background;
        }

        static public bool IsComputedStyleCursor(object val)
        {
            return val is StyleCursor || val is UnityEngine.UIElements.Cursor;
        }

        static public bool IsComputedStyleEnum(object val, Type valType)
        {
            if (val is Enum)
                return true;

            return valType.IsGenericType && valType.GetGenericArguments()[0].IsEnum;
        }

        // Getters

        static public float GetComputedStyleFloatValue(object val)
        {
            if (val is float)
                return (float)val;

            var style = (StyleFloat)val;
            return style.value;
        }

        static public TextShadow GetComputedStyleTextShadowValue(object val)
        {
            if (val is TextShadow textShadow)
                return textShadow;

            var style = (StyleTextShadow)val;
            return style.value;
        }

        static public TransformOrigin GetComputedStyleTransformOriginValue(object val)
        {
            if (val is TransformOrigin transformOrigin)
                return transformOrigin;

            var style = (StyleTransformOrigin)val;
            return style.value;
        }

        static public Translate GetComputedStyleTranslateValue(object val)
        {
            if (val is Translate translate)
                return translate;

            var style = (StyleTranslate)val;
            return style.value;
        }

        static public Rotate GetComputedStyleRotateValue(object val)
        {
            if (val is Rotate rotate)
                return rotate;

            var style = (StyleRotate)val;
            return style.value;
        }

        static public BackgroundRepeat GetComputedStyleBackgroundRepeatValue(object val)
        {
            if (val is BackgroundRepeat)
                return (BackgroundRepeat)val;

            var style = (StyleBackgroundRepeat)val;
            return style.value;
        }

        static public BackgroundSize GetComputedStyleBackgroundSizeValue(object val)
        {
            if (val is BackgroundSize)
                return (BackgroundSize)val;

            var style = (StyleBackgroundSize)val;
            return style.value;
        }

        static public BackgroundPosition GetComputedStyleBackgroundPositionValue(object val)
        {
            if (val is BackgroundPosition)
                return (BackgroundPosition)val;

            var style = (StyleBackgroundPosition)val;
            return style.value;
        }

        static public Scale GetComputedStyleScaleValue(object val)
        {
            if (val is Scale scale)
                return scale;

            var style = (StyleScale)val;
            return style.value;
        }

        static public int GetComputedStyleIntValue(object val)
        {
            if (val is int)
                return (int)val;

            var style = (StyleInt)val;
            return style.value;
        }

        static public Length GetComputedStyleLengthValue(object val)
        {
            if (val is Length)
                return (Length)val;

            var style = (StyleLength)val;
            return style.value;
        }

        static public Color GetComputedStyleColorValue(object val)
        {
            if (val is Color)
                return (Color)val;

            var style = (StyleColor)val;
            return style.value;
        }

        static public Font GetComputedStyleFontValue(object val)
        {
            // Since StyleFont has two implicit constructors, it can't implicitly cast null into a StyleFont.
            if (val == null)
                return null;

            if (val is Font font)
                return font;

            // Since StyleFont has two implicit constructors, it can't implicitly cast null into a StyleFont.
            if (val == null)
                return null;

            var style = (StyleFont)val;
            return style.value;
        }

        private static Object GetComputedStyleFontAssetValue(object val)
        {
            switch (val)
            {
                case Font font: return font;
                case FontAsset fontAsset: return fontAsset;
                case FontDefinition fontDefinition: return (Object)fontDefinition.fontAsset ?? fontDefinition.font;
                case StyleFont styleFont: return styleFont.value;
                case StyleFontDefinition styleFontDefinition: return (Object)styleFontDefinition.value.fontAsset ?? styleFontDefinition.value.font;
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        static public Background GetComputedStyleBackgroundValue(object val)
        {
            if (val is Background)
                return (Background)val;

            var style = (StyleBackground)val;
            return style.value;
        }

        static public UnityEngine.UIElements.Cursor GetComputedStyleCursorValue(object val)
        {
            if (val is UnityEngine.UIElements.Cursor)
                return (UnityEngine.UIElements.Cursor)val;

            var style = (StyleCursor)val;
            return style.value;
        }

        static public Enum GetComputedStyleEnumValue(object val, Type valType)
        {
            if (val is Enum)
                return val as Enum;

            var propInfo = valType.GetProperty("value");
            var enumValue = propInfo.GetValue(val, null) as Enum;
            return enumValue;
        }
    }
}
