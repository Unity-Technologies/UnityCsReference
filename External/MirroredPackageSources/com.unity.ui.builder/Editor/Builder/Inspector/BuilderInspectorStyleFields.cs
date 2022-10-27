using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Debugger;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

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

        VisualElement currentVisualElement => m_Inspector.currentVisualElement;
        StyleSheet styleSheet => m_Inspector.styleSheet;
        StyleRule currentRule => m_Inspector.currentRule;

        public Action<Enum> updateFlexColumnGlobalState { get; set; }

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

        public static string ConvertUssStyleNameToCSharpStyleName(string ussStyleName)
        {
            if (ussStyleName == "-unity-font-style")
                return "-unity-font-style-and-weight";

            return ussStyleName;
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

        public void BindStyleField(BuilderStyleRow styleRow, string styleName, VisualElement fieldElement)
        {
            // Link the row.
            fieldElement.SetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName, styleRow);

            var styleType = StyleDebug.GetComputedStyleType(styleName);
            if (styleType == null)
                return;

            // We don't care which element we get the value for here as we're only interested
            // in the type of Enum it might be (and for validation), but not it's actual value.
            var val = StyleDebug.GetComputedStyleValue(fieldElement.computedStyle, styleName);

            if (IsComputedStyleFloat(val) && fieldElement is FloatField)
            {
                var uiField = fieldElement as FloatField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;

                if (BuilderConstants.SpecialSnowflakeLengthStyles.Contains(styleName))
                    uiField.RegisterValueChangedCallback(e => OnFieldDimensionChange(e, styleName));
                else
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChangeIntToFloat(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is PercentSlider)
            {
                var uiField = fieldElement as PercentSlider;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is NumericStyleField)
            {
                var uiField = fieldElement as NumericStyleField;
                uiField.RegisterValueChangedCallback(e => OnNumericStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFloat(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                uiField.RegisterValueChangedCallback(e => OnDimensionStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerStyleField)
            {
                var uiField = fieldElement as IntegerStyleField;
                uiField.RegisterValueChangedCallback(e => OnIntegerStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleLength(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                uiField.RegisterValueChangedCallback(e => OnFieldDimensionChange(e, styleName));
            }
            else if (IsComputedStyleLength(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                uiField.RegisterValueChangedCallback(e => OnDimensionStyleFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleColor(val) && fieldElement is ColorField)
            {
                var uiField = fieldElement as ColorField;
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleFont(val, styleName) && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                uiField.objectType = typeof(Font);
                uiField.RegisterValueChangedCallback(e => OnFieldValueChangeFont(e, styleName));
            }
            else if (IsComputedStyleFontAsset(val, styleName) && fieldElement is ObjectField objectField)
            {
                objectField.objectType = typeof(FontAsset);
                objectField.RegisterValueChangedCallback(e => OnFieldValueChangeFont(e, styleName));
            }
            else if (IsComputedStyleTextShadow(val) && fieldElement is TextShadowStyleField textShadowStyleField)
            {
                textShadowStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeTextShadow(e, styleName));
            }
            else if (IsComputedStyleTransformOrigin(val) && fieldElement is TransformOriginStyleField transformOriginStyleField)
            {
                transformOriginStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeTransformOrigin(e, styleName));
            }
            else if (IsComputedStyleTranslate(val) && fieldElement is TranslateStyleField translateStyleField)
            {
                translateStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeTranslate(e, styleName));
            }
            else if (IsComputedStyleRotate(val) && fieldElement is RotateStyleField rotateStyleField)
            {
                rotateStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeRotate(e, styleName));
            }
            else if (IsComputedStyleScale(val) && fieldElement is ScaleStyleField scaleStyleField)
            {
                scaleStyleField.RegisterValueChangedCallback(e => OnFieldValueChangeScale(e, styleName));
            }
            else if (IsComputedStyleBackground(val) && fieldElement is ImageStyleField imageStyleField)
            {
                imageStyleField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleCursor(val) && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                uiField.objectType = typeof(Texture2D);
                uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
            }
            else if (IsComputedStyleEnum(val, styleType))
            {
                var enumValue = GetComputedStyleEnumValue(val, styleType);

                if (fieldElement is EnumField)
                {
                    var uiField = fieldElement as EnumField;

                    uiField.Init(enumValue);
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
                }
                else if (fieldElement is IToggleButtonStrip)
                {
                    var uiField = fieldElement as IToggleButtonStrip;

                    var choices = new List<string>();
                    var labels = new List<string>();
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

                        var label = string.Empty;
                        if (typeName == "Auto")
                            label = "AUTO";
                        var choice = BuilderNameUtilities.ConvertCamelToDash(typeName);
                        choices.Add(choice);
                        labels.Add(label);
                    }

                    uiField.enumType = enumType;
                    uiField.choices = choices;
                    uiField.labels = labels;
                    uiField.RegisterValueChangedCallback(e => OnFieldValueChange(e, styleName));
                }
                else
                {
                    // Unsupported style value type.
                    return;
                }
            }
            else
            {
                // Unsupported style value type.
                return;
            }

            fieldElement.SetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName, styleName);
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
        }

        public void BindDoubleFieldRow(BuilderStyleRow styleRow)
        {
            var styleFields = styleRow.Query<BindableElement>().ToList()
                .Where(element => !string.IsNullOrEmpty(element.bindingPath)).ToList();
            if (styleFields.Count > 0)
            {
                var headerLabel = styleRow.Q<BindableElement>(classes: "unity-builder-composite-field-label");
                headerLabel.AddManipulator(new ContextualMenuManipulator(action =>
                {
                    action.elementTarget.userData = styleFields;
                    BuildStyleFieldContextualMenu(action);
                }));
            }
        }

        public void BindStyleField(BuilderStyleRow styleRow, FoldoutNumberField foldoutElement)
        {
            var intFields = foldoutElement.Query<StyleFieldBase>().ToList();

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
            var colorFields = foldoutElement.Query<ColorField>().ToList();

            foreach (var field in colorFields)
            {
                field.SetProperty(BuilderConstants.FoldoutFieldPropertyName, foldoutElement);
                field.RegisterValueChangedCallback((evt) =>
                {
                    var row = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
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
            var target = evt.target as TextField;
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
            var target = evt.target as ColorField;
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
            var styleType = StyleDebug.GetComputedStyleType(styleName);
            if (styleType == null)
            {
                // Transitions are made from 4 different properties and does not have a dedicated C# property on ComputedStyle.
                if (!string.IsNullOrEmpty(styleName) &&
                    StylePropertyUtil.s_NameToId.TryGetValue(styleName, out var id) &&
                    id.IsTransitionId() &&
                    fieldElement is TransitionsListView listView)
                    RefreshStyleField(listView);
                return;
            }

            var val = StyleDebug.GetComputedStyleValue(currentVisualElement.computedStyle, styleName);
            var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(styleName);
            var styleProperty = GetLastStyleProperty(currentRule, cSharpStyleName);
            bool useStyleProperty = styleProperty != null && !styleProperty.IsVariable();

            if (IsComputedStyleFloat(val) && fieldElement is FloatField)
            {
                var uiField = fieldElement as FloatField;
                var value = GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetFloat(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                var value = (int)GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                    value = (int)styleSheet.GetFloat(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is PercentSlider)
            {
                var uiField = fieldElement as PercentSlider;
                var value = GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetFloat(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleTextShadow(val) && fieldElement is TextShadowStyleField)
            {
                var uiField = fieldElement as TextShadowStyleField;
                var value = GetComputedStyleTextShadowValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetTextShadow(styleProperty);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleTransformOrigin(val) && fieldElement is TransformOriginStyleField)
            {
                var uiField = fieldElement as TransformOriginStyleField;
                var value = GetComputedStyleTransformOriginValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetTransformOrigin(styleProperty);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleTranslate(val) && fieldElement is TranslateStyleField)
            {
                var uiField = fieldElement as TranslateStyleField;
                var value = GetComputedStyleTranslateValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetTranslate(styleProperty);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleRotate(val) && fieldElement is RotateStyleField)
            {
                var uiField = fieldElement as RotateStyleField;
                var value = GetComputedStyleRotateValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetRotate(styleProperty);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleScale(val) && fieldElement is ScaleStyleField)
            {
                var uiField = fieldElement as ScaleStyleField;
                var value = GetComputedStyleScaleValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetScale(styleProperty);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is NumericStyleField)
            {
                var uiField = fieldElement as NumericStyleField;
                var value = (int)GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var number = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(number.ToString());
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
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;
                var value = GetComputedStyleIntValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetInt(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleInt(val) && fieldElement is IntegerStyleField)
            {
                var uiField = fieldElement as IntegerStyleField;
                var value = GetComputedStyleIntValue(val);
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var number = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(number.ToString());
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
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
            }
            else if (IsComputedStyleLength(val) && fieldElement is IntegerField)
            {
                var uiField = fieldElement as IntegerField;

                var value = (int)GetComputedStyleLengthValue(val).value;
                if (useStyleProperty)
                    value = (int)styleSheet.GetDimension(styleProperty.values[0]).value;

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleFloat(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                var value = (int)GetComputedStyleFloatValue(val);
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Dimension)
                    {
                        var dimension = styleSheet.GetDimension(styleValue);
                        uiField.unit = dimension.unit;
                        uiField.length = dimension.value;
                    }
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var length = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(length + DimensionStyleField.defaultUnit);
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType + " not compatible with StyleFloat.");
                    }
                }
                else
                {
                    var keyword = GetComputedStyleKeyword(val);
                    if (keyword != StyleKeyword.Undefined)
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(keyword);
                    else
                        uiField.SetValueWithoutNotify(value.ToString());
                }
            }
            else if (IsComputedStyleLength(val) && fieldElement is DimensionStyleField)
            {
                var uiField = fieldElement as DimensionStyleField;
                var value = GetComputedStyleLengthValue(val).value;
                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        uiField.keyword = keyword;
                    }
                    else if (styleValue.valueType == StyleValueType.Dimension)
                    {
                        var dimension = styleSheet.GetDimension(styleValue);
                        uiField.unit = dimension.unit;
                        uiField.length = dimension.value;
                    }
                    else if (styleValue.valueType == StyleValueType.Float)
                    {
                        var length = styleSheet.GetFloat(styleValue);
                        uiField.SetValueWithoutNotify(length + DimensionStyleField.defaultUnit);
                    }
                    else
                    {
                        throw new InvalidOperationException("StyleValueType " + styleValue.valueType.ToString() + " not compatible with StyleLength.");
                    }
                }
                else
                {
                    var keyword = GetComputedStyleKeyword(val);
                    if (keyword != StyleKeyword.Undefined)
                        uiField.keyword = StyleSheetUtilities.ConvertStyleKeyword(keyword);
                    else
                    {
                        var lengthUnit = GetComputedStyleLengthUnit(val);
                        uiField.unit = StyleSheetUtilities.ConvertToDimensionUnit(lengthUnit);
                        uiField.length = value;
                    }
                }
            }
            else if (IsComputedStyleColor(val) && fieldElement is ColorField)
            {
                var uiField = fieldElement as ColorField;
                var value = GetComputedStyleColorValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetColor(styleProperty.values[0]);

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleFont(val, styleName) && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                var value = GetComputedStyleFontValue(val);
                if (useStyleProperty)
                    value = styleSheet.GetAsset(styleProperty.values[0]) as Font;

                uiField.SetValueWithoutNotify(value);
            }
            else if (IsComputedStyleFontAsset(val, styleName) && fieldElement is ObjectField objectField)
            {
                var value = GetComputedStyleFontAssetValue(val);
                objectField.SetValueWithoutNotify(useStyleProperty
                    ? styleSheet.GetAsset(styleProperty.values[0])
                    : value);
            }
            else if (IsComputedStyleBackground(val) && fieldElement is ImageStyleField imageStyleField)
            {
                var value = GetComputedStyleBackgroundValue(val);
                if (useStyleProperty)
                {
                    var asset = styleSheet.GetAsset(styleProperty.values[0]);
                    imageStyleField.SetValueWithoutNotify(asset);
                    if (asset != null)
                        imageStyleField.SetTypePopupValueWithoutNotify(asset.GetType());
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
            }
            else if (IsComputedStyleCursor(val) && fieldElement is ObjectField)
            {
                var uiField = fieldElement as ObjectField;
                var value = GetComputedStyleCursorValue(val);

                if (useStyleProperty)
                {
                    switch (styleProperty.values[0].valueType)
                    {
                        case StyleValueType.AssetReference:
                        case StyleValueType.ResourcePath:
                        case StyleValueType.ScalableImage:
                            value.texture = styleSheet.GetAsset(styleProperty.values[0]) as Texture2D;
                            uiField.SetValueWithoutNotify(value.texture);
                            break;
                        case StyleValueType.MissingAssetReference:
                            uiField.SetValueWithoutNotify(null);
                            break;
                        case StyleValueType.Enum:
                            // TODO: Add support for using the predefined list from the MouseCursor enum.
                            // This is only available in the editor.
                            uiField.SetValueWithoutNotify(null);
                            break;
                        default:
                            uiField.SetValueWithoutNotify(null);
                            break;
                    }
                }
                else
                {
                    uiField.SetValueWithoutNotify(null);
                }
            }
            else if (IsComputedStyleEnum(val, styleType))
            {
                var enumValue = GetComputedStyleEnumValue(val, styleType);

                if (useStyleProperty)
                {
                    var styleValue = styleProperty.values[0];
                    var enumStr = string.Empty;

                    // Some keywords may conflict with the enum values. We have
                    // to check for both here.
                    // Ex: display: none;
                    if (styleValue.valueType == StyleValueType.Keyword)
                    {
                        var keyword = styleSheet.GetKeyword(styleValue);
                        enumStr = keyword.ToString();
                    }
                    else if (styleValue.valueType == StyleValueType.Enum)
                    {
                        enumStr = styleSheet.GetEnum(styleProperty.values[0]);
                    }
                    else
                    {
                        Debug.LogError("UIBuilder: Value type is enum but style property value type is not enum: " + styleProperty.name);
                    }

                    if (!string.IsNullOrEmpty(enumStr))
                    {
                        var enumStrHungarian = BuilderNameUtilities.ConvertDashToHungarian(enumStr);
                        var enumObj = Enum.Parse(enumValue.GetType(), enumStrHungarian);
                        if (enumObj is Enum enumEnum)
                            enumValue = enumEnum;
                    }
                }

                // The state of Flex Direction can affect many other Flex-related fields.
                if (styleName == "flex-direction")
                    updateFlexColumnGlobalState?.Invoke(enumValue);

                if (styleName == "position")
                    updatePositionAnchorsFoldoutState?.Invoke(enumValue);

                if (fieldElement is EnumField)
                {
                    var uiField = fieldElement as EnumField;
                    uiField.SetValueWithoutNotify(enumValue);
                }
                else if (fieldElement is IToggleButtonStrip)
                {
                    var enumStr = BuilderNameUtilities.ConvertCamelToDash(enumValue.ToString());
                    var uiField = fieldElement as IToggleButtonStrip;
                    uiField.SetValueWithoutNotify(enumStr);
                }
                else
                {
                    // Unsupported style value type.
                    return;
                }
            }
            else
            {
                // Unsupported style value type.
                return;
            }

            UpdateFieldStatus(fieldElement, styleProperty);
        }

        void UpdateFieldStatus(VisualElement fieldElement, StyleProperty styleProperty)
        {
            UpdateOverrideStyles(fieldElement, styleProperty);
            m_Inspector.UpdateFieldStatus(fieldElement, styleProperty);
        }

        void UpdateOverrideStyles(VisualElement fieldElement, StyleProperty styleProperty)
        {
            // Add override style to field if it is overwritten.
            var styleRow = fieldElement.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
            Assert.IsNotNull(styleRow);
            if (styleRow == null)
                return;

            var handler = StyleVariableUtilities.GetOrCreateVarHandler(fieldElement as BindableElement);
            if (handler != null)
            {
                handler.editingEnabled = BuilderSharedStyles.IsSelectorElement(currentVisualElement);
                handler.RefreshField();
            }

            var styleFields = styleRow.Query<BindableElement>().ToList();

            bool isRowOverride = false;
            foreach (var styleField in styleFields)
            {
                if (GetLastStyleProperty(currentRule, styleField.bindingPath) != null)
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

            var foldout = fieldElement.GetProperty(BuilderConstants.FoldoutFieldPropertyName) as FoldoutField;
            if (foldout != null)
            {
                foldout.UpdateFromChildFields();
            }

            updateStyleCategoryFoldoutOverrides?.Invoke();
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
            else if (IsComputedStyleFontAsset(val, styleName) && fieldElement is ObjectField objectFontAssetField)
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
            else if (IsComputedStyleCursor(val) && fieldElement is ObjectField objectCursorField)
            {
                DispatchChangeEvent(objectCursorField);
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
                    case IToggleButtonStrip toggleButtonStrip:
                        DispatchChangeEvent((BaseField<string>)toggleButtonStrip);
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
                var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(path);
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
                var cSharpStyleName = ConvertUssStyleNameToCSharpStyleName(path);
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
                        value = styleSheet.GetColor(styleProperty.values[0]);
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
                else if (isSelector)
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
                    && StylePropertyUtil.s_NameToId.TryGetValue(styleName, out var id)
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

            UnsetStylePropertyForElement(bindableElement);
        }

        DropdownMenuAction.Status UnsetActionStatus(DropdownMenuAction action)
        {
            return StyleActionStatus(action, property => property != null);
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

            UnsetStyleProperties(fields);
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

        void UnsetStyleProperty(DropdownMenuAction action)
        {
            UnsetStylePropertyForElement(action.userData as VisualElement);
        }

        public void UnsetStylePropertyForElement(VisualElement fieldElement)
        {
            var listToUnset = fieldElement?.userData;
            if (listToUnset != null && listToUnset is List<BindableElement> bindableElements)
            {
                UnsetStyleProperties(bindableElements);
                return;
            }

            UnsetStyleProperties(new List<VisualElement> { fieldElement });
        }

        public void UnsetStyleProperties(IEnumerable<VisualElement> fields)
        {
            foreach (var fieldElement in fields)
            {
                var styleName = fieldElement.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;

                // TODO: The computed style still has the old (set) value at this point.
                // We need to reset the field with the value after styling has been
                // recomputed.

                styleSheet.RemoveProperty(currentRule, styleName);
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

        void NotifyStyleChanges(List<string> styles = null, bool selfNotify = false, NotifyType notifyType = NotifyType.Default)
        {
            BuilderStylingChangeType styleChangeType;
            var hierarchyChangeType = BuilderHierarchyChangeType.InlineStyle;
            if (notifyType == NotifyType.RefreshOnly)
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
                currentVisualElement.IncrementVersion((VersionChangeType)(-1));
            }
            else
            {
                m_Selection.NotifyOfStylingChange(selfNotify ? null : m_Inspector, styles, styleChangeType);
                m_Selection.NotifyOfHierarchyChange(m_Inspector, currentVisualElement, hierarchyChangeType);
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

        void PostStyleFieldSteps(VisualElement target, StyleProperty styleProperty, string styleName, bool isNewValue, bool isVariable = false, NotifyType notifyType = NotifyType.Default)
        {
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

                var styleRow = target.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
                styleRow.AddToClassList(BuilderConstants.InspectorLocalStyleOverrideClassName);

                var styleFields = styleRow.Query<BindableElement>().ToList();

                var bindableElement = target as BindableElement;
                foreach (var styleField in styleFields)
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
            NotifyStyleChanges(s_StyleChangeList, isVariable, notifyType);

            if (isNewValue && updateStyleCategoryFoldoutOverrides != null)
                updateStyleCategoryFoldoutOverrides();
            m_Inspector.UpdateFieldStatus(target, styleProperty);
        }

        bool IsNewValue(StyleProperty styleProperty, params StyleValueType[] supportedValueTypes)
        {
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to re-save it here as the new type. We do this by just removing the current value.
            if (!isNewValue && !supportedValueTypes.Contains(styleProperty.values[0].valueType))
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }


            return isNewValue;
        }

        void OnFieldKeywordChange(StyleValueKeyword keyword, VisualElement target, string styleName, NotifyType notifyType = NotifyType.Default)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = IsNewValue(styleProperty, StyleValueType.Keyword);

            if (!isNewValue && styleProperty.IsVariable())
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, keyword, BuilderConstants.ChangeUIStyleValueUndoMessage);
            else // TODO: Assume only one value.
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                var styleValue = styleProperty.values[0];
                styleValue.valueIndex = (int)keyword;
                styleProperty.values[0] = styleValue;
            }

            PostStyleFieldSteps(target, styleProperty, styleName, isNewValue, false, notifyType);
        }

        bool OnFieldDimensionChangeImplBatch(float newValue, Dimension.Unit newUnit, VisualElement target, string styleName, out StyleProperty styleProperty)
        {
            styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var dimension = new Dimension();
            dimension.unit = newUnit;
            dimension.value = newValue;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, dimension);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], dimension);

            return isNewValue;
        }

        void OnFieldDimensionChangeImpl(float newValue, Dimension.Unit newUnit, VisualElement target, string styleName, NotifyType notifyType = NotifyType.Default)
        {
            StyleProperty styleProperty = null;
            bool isNewValue = OnFieldDimensionChangeImplBatch(newValue, newUnit, target, styleName, out styleProperty);

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
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Float)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

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
            var isNewValue = styleProperty.values.Length == 0;

            if (!isNewValue && styleProperty.IsVariable())
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var newValue = (float)e.newValue;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeImpl(float newValue, VisualElement target, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Float)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

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

            // The UI Builder does not support color keyword value. Color set by a builder will be in rgba format.
            var isNewValue = IsNewValue(styleProperty, StyleValueType.Color);

            if (!isNewValue && styleProperty.IsVariable())
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<string> e, string styleName)
        {
            var field = e.elementTarget;

            // HACK: For some reason, when using "Pick Element" feature of Debugger and
            // hovering over the button strips, we get bogus value change events with
            // empty strings.
            if (field is IToggleButtonStrip && string.IsNullOrEmpty(e.newValue))
                return;

            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a different type than the new style type,
            // we need to resave it here as the new type. We do this by just removing the current value.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Enum)
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);

                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (field is IToggleButtonStrip)
            {
                var newValue = e.newValue;
                var newEnumValueStr = BuilderNameUtilities.ConvertDashToHungarian(newValue);
                var enumType = (field as IToggleButtonStrip).enumType;
                var newEnumValue = Enum.Parse(enumType, newEnumValueStr) as Enum;

                if (isNewValue)
                    styleSheet.AddValue(styleProperty, newEnumValue);
                else // TODO: Assume only one value.
                    styleSheet.SetValue(styleProperty.values[0], newEnumValue);

                // The state of Flex Direction can affect many other Flex-related fields.
                if (styleName == "flex-direction")
                    updateFlexColumnGlobalState?.Invoke(newEnumValue);
            }
            else
            {
                if (isNewValue)
                    styleSheet.AddValue(styleProperty, e.newValue);
                else // TODO: Assume only one value.
                    styleSheet.SetValue(styleProperty.values[0], e.newValue);
            }

            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
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
            var isNewValue = IsNewValue(styleProperty, StyleValueType.ResourcePath, StyleValueType.AssetReference);

            if (!isNewValue)
            {
                if (styleProperty.values[0].valueType == StyleValueType.ResourcePath && string.IsNullOrEmpty(resourcesPath))
                {
                    styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);
                    isNewValue = true;
                }
                else if (styleProperty.values[0].valueType == StyleValueType.AssetReference && !string.IsNullOrEmpty(resourcesPath))
                {
                    styleSheet.RemoveValue(styleProperty, styleProperty.values[0]);
                    isNewValue = true;
                }
                else if (styleProperty.IsVariable())
                {
                    Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                    styleProperty.values = new StyleValueHandle[0];
                    isNewValue = true;
                }
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], newValue);

            PostStyleFieldSteps(target as VisualElement, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeFont(ChangeEvent<Object> e, string styleName)
        {
            var field = e.target as ObjectField;
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (!isNewValue && styleProperty.IsVariable())
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            PostStyleFieldSteps(field, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeTextShadow(ChangeEvent<BuilderTextShadow> e, string styleName)
        {
            var field = e.target as TextShadowStyleField;
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);

            var isNewValue = field.OnFieldValueChange(styleProperty, styleSheet);

            PostStyleFieldSteps(field, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeTransformOrigin(ChangeEvent<BuilderTransformOrigin> e, string styleName)
        {
            var field = e.target as TransformOriginStyleField;
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);

            var isNewValue = field.OnFieldValueChange(styleProperty, styleSheet);

            PostStyleFieldSteps(field, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeTranslate(ChangeEvent<BuilderTranslate> e, string styleName)
        {
            var field = e.target as TranslateStyleField;
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);

            var isNewValue = field.OnFieldValueChange(styleProperty, styleSheet);

            PostStyleFieldSteps(field, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeRotate(ChangeEvent<BuilderRotate> e, string styleName)
        {
            var field = e.target as RotateStyleField;
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);

            var isNewValue = field.OnFieldValueChange(styleProperty, styleSheet);

            PostStyleFieldSteps(field, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChangeScale(ChangeEvent<BuilderScale> e, string styleName)
        {
            var field = e.target as ScaleStyleField;
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);

            var isNewValue = field.OnFieldValueChange(styleProperty, styleSheet);

            PostStyleFieldSteps(field, styleProperty, styleName, isNewValue);
        }

        void OnFieldValueChange(ChangeEvent<Enum> e, string styleName)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (!isNewValue && styleProperty.IsVariable())
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, e.newValue);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], e.newValue);

            // The state of Flex Direction can affect many other Flex-related fields.
            if (styleName == "flex-direction")
                updateFlexColumnGlobalState?.Invoke(e.newValue);

            if (styleName == "position")
                updatePositionAnchorsFoldoutState?.Invoke(e.newValue);

            PostStyleFieldSteps(e.elementTarget, styleProperty, styleName, isNewValue);
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
                if (length.CallBoolMethodByReflection("IsNone"))
                    return StyleKeyword.None;
                else if (length.CallBoolMethodByReflection("IsAuto"))
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
            return val is StyleTextShadow || val is TextShadow || val is BuilderTextShadow;
        }

        static public bool IsComputedStyleTransformOrigin(object val)
        {
            return val is StyleTransformOrigin || val is TransformOrigin || val is BuilderTransformOrigin;
        }

        static public bool IsComputedStyleTranslate(object val)
        {
            return val is StyleTranslate || val is Translate || val is BuilderTranslate;
        }

        static public bool IsComputedStyleRotate(object val)
        {
            return val is StyleRotate || val is Rotate || val is BuilderRotate;
        }

        static public bool IsComputedStyleScale(object val)
        {
            return val is StyleScale || val is Scale || val is BuilderScale;
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

        static public BuilderTextShadow GetComputedStyleTextShadowValue(object val)
        {
            if (val is BuilderTextShadow)
                return (BuilderTextShadow)val;

            if (val is TextShadow)
                return new BuilderTextShadow((TextShadow)val);

            var style = (StyleTextShadow)val;
            return new BuilderTextShadow(style);
        }

        static public BuilderTransformOrigin GetComputedStyleTransformOriginValue(object val)
        {
            if (val is BuilderTransformOrigin)
                return (BuilderTransformOrigin)val;

            if (val is TransformOrigin)
                return new BuilderTransformOrigin((TransformOrigin)val);

            var style = (StyleTransformOrigin)val;
            return new BuilderTransformOrigin(style);
        }

        static public BuilderTranslate GetComputedStyleTranslateValue(object val)
        {
            if (val is BuilderTranslate)
                return (BuilderTranslate)val;

            if (val is Translate)
                return new BuilderTranslate((Translate)val);

            var style = (StyleTranslate)val;
            return new BuilderTranslate(style);
        }

        static public BuilderRotate GetComputedStyleRotateValue(object val)
        {
            if (val is BuilderRotate)
                return (BuilderRotate)val;

            if (val is Rotate)
                return new BuilderRotate((Rotate)val);

            var style = (StyleRotate)val;
            return new BuilderRotate(style);
        }

        static public BuilderScale GetComputedStyleScaleValue(object val)
        {
            if (val is BuilderScale)
                return (BuilderScale)val;

            if (val is Scale)
                return new BuilderScale((Scale)val);

            var style = (StyleScale)val;
            return new BuilderScale(style);
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

        static public FontAsset GetComputedStyleFontAssetValue(object val)
        {
            if (val is FontAsset fontAsset)
                return fontAsset;

            if (val is FontDefinition fontDefinition)
                return (FontAsset)fontDefinition.fontAsset;

            var style = (StyleFontDefinition)val;
            return (FontAsset)style.value.fontAsset;
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
