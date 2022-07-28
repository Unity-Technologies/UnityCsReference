// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.UIElements.Debugger
{
    internal struct StylePropertyInfo
    {
        public string name;
        public StylePropertyId id;
        public Type type;
        public string[] longhands; // For shorthands property only

        public bool isShorthand => longhands != null;
    }

    internal class StylePropertyDebugger : VisualElement
    {
        static readonly List<StylePropertyInfo> s_StylePropertyInfos = new List<StylePropertyInfo>();

        private Dictionary<StylePropertyId, StyleField> m_IdToFieldDictionary = new Dictionary<StylePropertyId, StyleField>();
        private Dictionary<StylePropertyId, int> m_PropertySpecificityDictionary = new Dictionary<StylePropertyId, int>();

        private Toolbar m_Toolbar;
        private VisualElement m_CustomPropertyFieldsContainer;
        private VisualElement m_FieldsContainer;
        private string m_SearchFilter;
        private bool m_ShowAll;

        private VisualElement m_SelectedElement;

        internal bool showAll
        {
            get => m_ShowAll;
            set => m_ShowAll = value;
        }

        static StylePropertyDebugger()
        {
            // Retrieve all style property infos
            var names = StyleDebug.GetStylePropertyNames();
            foreach (var name in names)
            {
                var id = StyleDebug.GetStylePropertyIdFromName(name);

                var info = new StylePropertyInfo();
                info.name = name;
                info.id = id;
                info.type = StyleDebug.GetInlineStyleType(name);
                info.longhands = StyleDebug.GetLonghandPropertyNames(name);

                s_StylePropertyInfos.Add(info);
            }
        }

        public StylePropertyDebugger(VisualElement debuggerSelection)
        {
            m_SelectedElement = debuggerSelection;

            m_Toolbar = new Toolbar();
            Add(m_Toolbar);

            var searchField = new ToolbarSearchField();
            searchField.AddToClassList("unity-style-debugger-search");
            searchField.RegisterValueChangedCallback(e =>
            {
                m_SearchFilter = e.newValue;
                BuildFields();
            });
            m_Toolbar.Add(searchField);

            var showAllToggle = new ToolbarToggle();
            showAllToggle.AddToClassList("unity-style-debugger-toggle");
            showAllToggle.text = "Show all";
            showAllToggle.RegisterValueChangedCallback(e =>
            {
                m_ShowAll = e.newValue;
                BuildFields();
            });
            m_Toolbar.Add(showAllToggle);

            m_CustomPropertyFieldsContainer = new VisualElement();
            Add(m_CustomPropertyFieldsContainer);

            m_FieldsContainer = new VisualElement();
            Add(m_FieldsContainer);

            if (m_SelectedElement != null)
                BuildFields();

            AddToClassList("unity-style-debugger");
        }

        public void Refresh()
        {
            RefreshFields();
        }

        public void SetMatchRecords(VisualElement selectedElement, IEnumerable<SelectorMatchRecord> matchRecords)
        {
            m_SelectedElement = selectedElement;
            m_PropertySpecificityDictionary.Clear();

            if (selectedElement != null)
                StyleDebug.FindSpecifiedStyles(selectedElement.computedStyle, matchRecords, m_PropertySpecificityDictionary);

            BuildFields();
        }

        private void FindInlineStyles()
        {
            if (m_SelectedElement.inlineStyleAccess == null)
                return;

            var inlineRulePropIds = m_SelectedElement.inlineStyleAccess.inlineRule.propertyIds;
            if (inlineRulePropIds != null)
            {
                foreach (var id in inlineRulePropIds)
                {
                    m_PropertySpecificityDictionary[id] = StyleDebug.InlineSpecificity;
                }
            }

            foreach (var id in StylePropertyUtil.AllPropertyIds())
            {
                if (m_SelectedElement.inlineStyleAccess.IsValueSet(id))
                    m_PropertySpecificityDictionary[id] = StyleDebug.InlineSpecificity;
            }
        }

        private void BuildFields()
        {
            m_FieldsContainer.Clear();
            m_IdToFieldDictionary.Clear();

            RefreshFields();
        }

        private void RefreshFields()
        {
            if (m_SelectedElement == null)
                return;

            FindInlineStyles();

            m_CustomPropertyFieldsContainer.Clear();
            var customProperties = m_SelectedElement.computedStyle.customProperties;
            if (customProperties != null && customProperties.Any())
            {
                foreach (KeyValuePair<string, StylePropertyValue> customProperty in customProperties)
                {
                    var styleName = customProperty.Key;
                    var propValue = customProperty.Value;
                    TextField textField = new TextField(styleName) { isReadOnly = true };
                    textField.AddToClassList("unity-style-field");
                    textField.value = propValue.sheet.ReadAsString(propValue.handle).ToLower();
                    m_CustomPropertyFieldsContainer.Add(textField);
                }
            }

            foreach (var propertyInfo in s_StylePropertyInfos)
            {
                if (propertyInfo.isShorthand)
                    continue;

                var styleName = propertyInfo.name;
                if (!string.IsNullOrEmpty(m_SearchFilter) &&
                    styleName.IndexOf(m_SearchFilter, StringComparison.InvariantCultureIgnoreCase) == -1)
                    continue;

                var id = propertyInfo.id;
                var val = StyleDebug.GetComputedStyleValue(m_SelectedElement.computedStyle, id);

                m_PropertySpecificityDictionary.TryGetValue(id, out var specificity);

                StyleField sf = null;
                m_IdToFieldDictionary.TryGetValue(id, out sf);
                if (m_ShowAll || specificity != StyleDebug.UndefinedSpecificity)
                {
                    if (sf != null)
                    {
                        sf.RefreshPropertyValue(val, specificity);
                    }
                    else
                    {
                        sf = new StyleField(m_SelectedElement, propertyInfo, val, specificity);
                        m_FieldsContainer.Add(sf);
                        m_IdToFieldDictionary[id] = sf;
                    }
                }
                else if (sf != null)
                {
                    // Style property is not applied anymore, remove the field
                    m_IdToFieldDictionary.Remove(id);
                    m_FieldsContainer.Remove(sf);
                }
            }
        }

        internal static string GetUSSPropertyNameFromComputedStyleName(string computedStyleName)
        {
            var sb = new StringBuilder();

            if (computedStyleName == "unityFontStyleAndWeight")
                sb.Append("unity-font-style");
            else
            {
                foreach (var c in computedStyleName)
                {
                    if (char.IsUpper(c))
                        sb.Append("-");

                    sb.Append(char.ToLower(c));
                }
            }

            return sb.ToString();
        }
    }

    internal class StyleField : VisualElement
    {
        private VisualElement m_SelectedElement;
        private Label m_SpecificityLabel;
        private StylePropertyInfo m_PropertyInfo;
        private string m_PropertyName;

        public StyleField(VisualElement selectedElement, StylePropertyInfo propInfo, object value, int specificity)
        {
            AddToClassList("unity-style-field");

            m_SelectedElement = selectedElement;
            m_PropertyInfo = propInfo;
            m_PropertyName = propInfo.name;

            m_SpecificityLabel = new Label();
            m_SpecificityLabel.AddToClassList("unity-style-field__specificity-label");

            RefreshPropertyValue(value, specificity);
        }

        public void RefreshPropertyValue(object val, int specificity)
        {
            if (val is float floatValue)
            {
                var field = GetOrCreateField<FloatField, float>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(floatValue);
            }
            else if (val is int intValue)
            {
                var field = GetOrCreateField<IntegerField, int>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(intValue);
            }
            else if (val is Length lengthValue)
            {
                var field = GetOrCreateField<StyleLengthField, StyleLength>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(new StyleLength(lengthValue));
            }
            else if (val is Color colorValue)
            {
                var field = GetOrCreateField<ColorField, Color>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(colorValue);
            }
            // Note: val may be null in case of reference type like "Font"
            else if (m_PropertyInfo.type == typeof(StyleFont))
            {
                var field = GetOrCreateObjectField<Font>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(val as Font);
            }
            else if (val is FontDefinition fontDefinition)
            {
                ObjectField field;
                if (fontDefinition.fontAsset != null)
                {
                    if (childCount == 0)
                    {
                        var o = new ObjectField();
                        o.objectType = typeof(FontAsset);
                        field = o;

                        SetField(field);
                    }
                    else
                        field = (ObjectField)ElementAt(0);
                }
                else
                    field = GetOrCreateObjectField<Font>();

                if (!IsFocused(field))
                    field.SetValueWithoutNotify(fontDefinition.fontAsset != null ? (UnityEngine.Object)fontDefinition.fontAsset : (UnityEngine.Object)fontDefinition.font);
            }
            else if (val is Background bgValue)
            {
                ObjectField field;
                if (bgValue.vectorImage != null)
                    field = GetOrCreateObjectField<VectorImage>();
                else if (bgValue.sprite != null)
                    field = GetOrCreateObjectField<Sprite>();
                else if (bgValue.renderTexture != null)
                    field = GetOrCreateObjectField<RenderTexture>();
                else
                    field = GetOrCreateObjectField<Texture2D>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(
                        bgValue.vectorImage != null ? (UnityEngine.Object)bgValue.vectorImage :
                        (bgValue.sprite != null ? (UnityEngine.Object)bgValue.sprite :
                            (bgValue.renderTexture != null ? (UnityEngine.Object)bgValue.renderTexture :
                                (UnityEngine.Object)bgValue.texture)));
            }
            else if (val is Cursor cursorValue)
            {
                // Recreate the cursor fields every time since StyleCursor
                // can be made of different fields (based on the value)
                Clear();

                if (cursorValue.texture != null)
                {
                    var uiTextureField = new ObjectField(m_PropertyName) { value = cursorValue.texture };
                    uiTextureField.RegisterValueChangedCallback(e =>
                    {
                        var currentCursor = (Cursor)StyleDebug.GetComputedStyleValue(m_SelectedElement.computedStyle, m_PropertyInfo.id);
                        currentCursor.texture = e.newValue as Texture2D;
                        SetPropertyValue(new StyleCursor(currentCursor));
                    });
                    Add(uiTextureField);

                    var uiHotspotField = new Vector2Field("hotspot") { value = cursorValue.hotspot };
                    uiHotspotField.RegisterValueChangedCallback(e =>
                    {
                        var currentCursor = (Cursor)StyleDebug.GetComputedStyleValue(m_SelectedElement.computedStyle, m_PropertyInfo.id);
                        currentCursor.hotspot = e.newValue;
                        SetPropertyValue(new StyleCursor(currentCursor));
                    });
                    Add(uiHotspotField);
                }
                else
                {
                    var mouseId = cursorValue.defaultCursorId;
                    var uiField = new EnumField(m_PropertyName, (MouseCursor)mouseId);
                    uiField.RegisterValueChangedCallback(e =>
                    {
                        var cursorId = Convert.ToInt32(e.newValue);
                        var cursor = new Cursor() { defaultCursorId = cursorId };
                        SetPropertyValue(new StyleCursor(cursor));
                    });
                    Add(uiField);
                }
                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
            }
            else if (val is TextShadow textShadow)
            {
                var colorFieldfield = GetOrCreateFields<ColorField, Color>(m_PropertyName, 0);
                if (!IsFocused(colorFieldfield))
                    colorFieldfield.SetValueWithoutNotify(textShadow.color);

                var vector2Field = GetOrCreateFields<Vector2Field, Vector2>("offset", 1);
                if (!IsFocused(vector2Field))
                    vector2Field.SetValueWithoutNotify(textShadow.offset);

                var floatField = GetOrCreateFields<FloatField, float>("blur", 2);
                if (!IsFocused(floatField))
                    floatField.SetValueWithoutNotify(textShadow.blurRadius);

                if (childCount == 3)
                    Add(m_SpecificityLabel);
            }
            else if (val is Rotate rotate)
            {
                var floatField = GetOrCreateFields<FloatField, float>(m_PropertyName, 0);
                if (!IsFocused(floatField))
                    floatField.SetValueWithoutNotify(rotate.angle.value);
                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
            }
            else if (val is Scale scale)
            {
                var vector3Field = GetOrCreateFields<Vector3Field, Vector3>(m_PropertyName, 0);
                if (!IsFocused(vector3Field))
                    vector3Field.SetValueWithoutNotify(scale.value);
                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
            }
            else if (val is Translate translate)
            {
                var fieldx = GetOrCreateFields<StyleLengthField, StyleLength>(m_PropertyName, 0);
                if (!IsFocused(fieldx))
                    fieldx.SetValueWithoutNotify(translate.x);
                var fieldy = GetOrCreateFields<StyleLengthField, StyleLength>("y", 1);
                if (!IsFocused(fieldy))
                    fieldy.SetValueWithoutNotify(translate.x);
                var floatField = GetOrCreateFields<FloatField, float>("z", 2);
                if (!IsFocused(floatField))
                    floatField.SetValueWithoutNotify(translate.z);
                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
                SetEnabled(false);
            }
            else if (val is TransformOrigin transformOrigin)
            {
                var fieldx = GetOrCreateFields<StyleLengthField, StyleLength>(m_PropertyName, 0);
                if (!IsFocused(fieldx))
                    fieldx.SetValueWithoutNotify(transformOrigin.x);
                var fieldy = GetOrCreateFields<StyleLengthField, StyleLength>("y", 1);
                if (!IsFocused(fieldy))
                    fieldy.SetValueWithoutNotify(transformOrigin.x);
                var floatField = GetOrCreateFields<FloatField, float>("z", 2);
                if (!IsFocused(floatField))
                    floatField.SetValueWithoutNotify(transformOrigin.z);
                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
                SetEnabled(false);
            }
            else if (val is Enum)
            {
                var enumValue = (Enum)val;
                var field = GetOrCreateEnumField(enumValue);
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(enumValue);
                Add(m_SpecificityLabel);
            }
            else if (val is BackgroundPosition backgroundPosition)
            {
                var keyword = backgroundPosition.keyword;
                var field = GetOrCreateEnumField(keyword);
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(keyword);

                var propertyName = m_PropertyName.EndsWith("x") ? "x" : "y";
                var fieldX = GetOrCreateFields<StyleLengthField, StyleLength>(propertyName, 1);
                if (!IsFocused(fieldX))
                    fieldX.SetValueWithoutNotify(backgroundPosition.offset);

                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
            }
            else if (val is BackgroundRepeat backgroundRepeat)
            {
                var enumValue1 = backgroundRepeat.x;
                var field1 = GetOrCreateEnumField(enumValue1);
                if (!IsFocused(field1))
                    field1.SetValueWithoutNotify(enumValue1);

                var enumValue2 = backgroundRepeat.y;
                var isCreation = childCount == 1;
                var field2 = GetOrCreateFields<EnumField, Enum>("y", 1);
                if (isCreation)
                    field2.Init(enumValue2);
                if (!IsFocused(field2))
                    field2.SetValueWithoutNotify(enumValue2);

                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
            }
            else if (val is BackgroundSize backgroundSize)
            {
                var type = backgroundSize.sizeType;
                var field = GetOrCreateEnumField(type);
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(type);

                var fieldX = GetOrCreateFields<StyleLengthField, StyleLength>("x", 1);
                if (!IsFocused(fieldX))
                    fieldX.SetValueWithoutNotify(backgroundSize.x);

                var fieldY = GetOrCreateFields<StyleLengthField, StyleLength>("y", 2);
                if (!IsFocused(fieldY))
                    fieldY.SetValueWithoutNotify(backgroundSize.y);

                Add(m_SpecificityLabel);
                SetSpecificity(specificity);
            }
            else
            {
                var type = val.GetType();
                Debug.Assert(type.IsArrayOrList(), "Expected List type");

                var listValue = val as System.Collections.IList;
                var valueString = listValue[0].ToString();
                for (int i = 1; i < listValue.Count; i++)
                {
                    valueString += $", {listValue[i]}";
                }
                TextField field = GetOrCreateField<TextField, string>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(valueString);
            }

            SetSpecificity(specificity);
        }

        private static bool IsFocused(VisualElement ve)
        {
            return ve.focusController != null && ve.focusController.IsFocused(ve);
        }

        private ObjectField GetOrCreateObjectField<T>()
        {
            var isCreation = childCount == 0;
            var field = GetOrCreateField<ObjectField, UnityEngine.Object>();
            if (isCreation)
                field.objectType = typeof(T);
            return field;
        }

        private EnumField GetOrCreateEnumField(Enum enumValue)
        {
            var isCreation = childCount == 0;
            var field = GetOrCreateFields<EnumField, Enum>(m_PropertyName, 0);
            if (isCreation)
                field.Init(enumValue);
            return field;
        }

        // This method has been temporarily added for TextShadow. It should be removed once we add a TextShadowField.
        // Make sure this is called in the right order (index = 0, index = 1, index = 2...)
        private T GetOrCreateFields<T, U>(string propertyName, int index = 0) where T : BaseField<U>, new()
        {
            Assert.IsFalse(index > (childCount + 1), "It seems GetOrCreateFields is called in the wrong order.");
            T field = null;
            if (childCount == index)
            {
                field = new T();
                field.label = propertyName;
                field.RegisterValueChangedCallback(e =>
                    SetPropertyValue(e.newValue, index));
                Add(field);
            }
            else
            {
                field = (T)ElementAt(index);
            }

            return field;
        }

        private T GetOrCreateField<T, U>() where T : BaseField<U>, new()
        {
            T field = null;
            if (childCount == 0)
            {
                field = new T();
                field.label = m_PropertyName;
                field.RegisterValueChangedCallback(e => SetPropertyValue(e.newValue, 0));
                Add(field);
                Add(m_SpecificityLabel);
            }
            else
            {
                field = (T)ElementAt(0);
            }

            return field;
        }

        private void SetField(ObjectField field)
        {
            field.label = m_PropertyName;
            field.RegisterValueChangedCallback(e => SetPropertyValue(e.newValue));
            Add(field);
            Add(m_SpecificityLabel);
        }

        private void SetSpecificity(int specificity)
        {
            var specificityString = "";
            switch (specificity)
            {
                case StyleDebug.UnitySpecificity:
                    specificityString = "unity stylesheet";
                    break;
                case StyleDebug.InheritedSpecificity:
                    specificityString = "inherited";
                    break;
                case StyleDebug.InlineSpecificity:
                    specificityString = "inline";
                    break;
                case StyleDebug.UndefinedSpecificity:
                    break;
                default:
                    specificityString = specificity.ToString();
                    break;
            }

            m_SpecificityLabel.text = specificityString;
        }

        private void SetPropertyValue(object newValue, int childIndex = -1)
        {
            var val = StyleDebug.GetComputedStyleValue(m_SelectedElement.computedStyle, m_PropertyInfo.id);
            var type = m_PropertyInfo.type;

            if (newValue == null)
            {
                if (type == typeof(StyleBackground))
                    val = new StyleBackground();

                if (type == typeof(StyleFont))
                    val = new StyleFont();

                if (type == typeof(StyleFontDefinition))
                    val = new StyleFontDefinition();
            }
            else if (type == newValue.GetType())
            {
                // For StyleLengthField
                val = newValue;
            }
            else
            {
                if (type == typeof(StyleBackground))
                {
                    val = new StyleBackground(newValue as Texture2D);
                }
                else if (type == typeof(StyleFontDefinition))
                {
                    val = new StyleFontDefinition(newValue);
                }
                else if (val is TextShadow textShadow)
                {
                    if (newValue is Color newColor)
                        textShadow.color = newColor;
                    if (newValue is Vector2 newOffset)
                        textShadow.offset = newOffset;
                    if (newValue is float newBlur)
                        textShadow.blurRadius = newBlur;

                    val = new StyleTextShadow(textShadow);
                }
                else if (type == typeof(StyleEnum<Overflow>) && newValue is OverflowInternal)
                {
                    OverflowInternal newV = (OverflowInternal)newValue;
                    Overflow v = newV == OverflowInternal.Hidden ? Overflow.Hidden : Overflow.Visible;
                    val = new StyleEnum<Overflow>(v);
                }
                else if (val is Scale scale && newValue is Vector3 newScale)
                {
                    val = new StyleScale(new Scale(newScale));
                }
                else if (val is Rotate rotate && newValue is float newAngle)
                {
                    val = new StyleRotate(new Rotate(newAngle));
                }
                else if (val is BackgroundPosition backgroundPosition)
                {
                    if (childIndex == 0)
                    {
                        backgroundPosition.keyword = (BackgroundPositionKeyword)newValue;
                    }
                    else if (childIndex == 1)
                    {
                        if (newValue is StyleLength l)
                        {
                            backgroundPosition.offset = l.value;
                        }
                    }

                    val = new StyleBackgroundPosition(backgroundPosition);
                }
                else if (val is BackgroundRepeat backgroundRepeat)
                {
                    if (childIndex == 0)
                    {
                        backgroundRepeat.x = (Repeat)newValue;
                    }
                    else if (childIndex == 1)
                    {
                        backgroundRepeat.y = (Repeat)newValue;
                    }

                    val = new StyleBackgroundRepeat(backgroundRepeat);
                }
                else if (val is BackgroundSize backgroundSize)
                {
                    if (childIndex == 0)
                    {
                        backgroundSize.sizeType = (BackgroundSizeType)newValue;
                    }
                    else if (childIndex == 1)
                    {
                        if (newValue is StyleLength l)
                        {
                            backgroundSize.x = l.value;
                        }
                    }
                    else if (childIndex == 2)
                    {
                        if (newValue is StyleLength l)
                        {
                            backgroundSize.y = l.value;
                        }
                    }

                    val = new StyleBackgroundSize(backgroundSize);
                }
                else
                {
                    var underlyingType = type.GetProperty("value").PropertyType;
                    var ctor = type.GetConstructor(new[] { underlyingType });
                    try
                    {
                        val = ctor.Invoke(new[] { newValue });
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"Invalid value for property '{m_PropertyName}'");
                        return;
                    }
                }
            }

            StyleDebug.SetInlineStyleValue(m_SelectedElement.style, m_PropertyInfo.id, val);
            SetSpecificity(StyleDebug.InlineSpecificity);
        }
    }
}
