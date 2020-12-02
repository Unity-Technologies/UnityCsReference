using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Debugger;
using UnityEngine.Assertions;
using Cursor = UnityEngine.UIElements.Cursor;
using Toolbar = UnityEditor.UIElements.Toolbar;

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

            foreach (var sv in m_SelectedElement.inlineStyleAccess.m_Values)
            {
                m_PropertySpecificityDictionary[sv.id] = StyleDebug.InlineSpecificity;
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
            var customProperties = m_SelectedElement.computedStyle.m_CustomProperties;
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
                object val = StyleDebug.GetComputedStyleActualValue(m_SelectedElement.computedStyle, id);
                Type type = propertyInfo.type;

                int specificity = 0;
                m_PropertySpecificityDictionary.TryGetValue(id, out specificity);

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
            m_SpecificityLabel.AddToClassList("unity-style-field__specifity-label");

            RefreshPropertyValue(value, specificity);
        }

        public void RefreshPropertyValue(object val, int specificity)
        {
            if (val is float floatValue)
            {
                FloatField field = GetOrCreateField<FloatField, float>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(floatValue);
            }
            else if (val is int intValue)
            {
                IntegerField field = GetOrCreateField<IntegerField, int>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(intValue);
            }
            else if (val is Length lengthValue)
            {
                StyleLengthField field = GetOrCreateField<StyleLengthField, StyleLength>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(new StyleLength(lengthValue));
            }
            else if (val is Color colorValue)
            {
                ColorField field = GetOrCreateField<ColorField, Color>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(colorValue);
            }
            // Note: val may be null in case of reference type like "Font"
            else if (m_PropertyInfo.type == typeof(StyleFont))
            {
                ObjectField field;
                field = GetOrCreateObjectField<Font>();
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
                        field = TextEditorDelegates.GetObjectFieldOfTypeFontAsset();
                        SetField(field);
                    }
                    else
                        field = (ObjectField)ElementAt(0);
                }
                else
                    field = GetOrCreateObjectField<Font>();

                if (!IsFocused(field))
                    field.SetValueWithoutNotify(fontDefinition.fontAsset != null ? fontDefinition.fontAsset : fontDefinition.font);
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
                        var currentCursor = (Cursor)StyleDebug.GetComputedStyleActualValue(m_SelectedElement.computedStyle, m_PropertyInfo.id);
                        currentCursor.texture = e.newValue as Texture2D;
                        SetPropertyValue(new StyleCursor(currentCursor));
                    });
                    Add(uiTextureField);

                    var uiHotspotField = new Vector2Field("hotspot") { value = cursorValue.hotspot };
                    uiHotspotField.RegisterValueChangedCallback(e =>
                    {
                        var currentCursor = (Cursor)StyleDebug.GetComputedStyleActualValue(m_SelectedElement.computedStyle, m_PropertyInfo.id);
                        currentCursor.hotspot = e.newValue;
                        SetPropertyValue(new StyleCursor(currentCursor));
                    });
                    Add(uiHotspotField);
                }
                else
                {
                    int mouseId = cursorValue.defaultCursorId;
                    var uiField = new EnumField(m_PropertyName, (MouseCursor)mouseId);
                    uiField.RegisterValueChangedCallback(e =>
                    {
                        int cursorId = Convert.ToInt32(e.newValue);
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
                ColorField colorFieldfield = GetOrCreateFields<ColorField, Color>(m_PropertyName, 0);
                if (!IsFocused(colorFieldfield))
                    colorFieldfield.SetValueWithoutNotify(textShadow.color);

                Vector2Field vector2Field = GetOrCreateFields<Vector2Field, Vector2>("offset", 1);
                if (!IsFocused(vector2Field))
                    vector2Field.SetValueWithoutNotify(textShadow.offset);

                FloatField floatField = GetOrCreateFields<FloatField, float>("blur", 2);
                if (!IsFocused(floatField))
                    floatField.SetValueWithoutNotify(textShadow.blurRadius);

                if (childCount == 3)
                    Add(m_SpecificityLabel);
            }
            else
            {
                // Enum
                Debug.Assert(val is Enum, "Expected Enum value");
                Enum enumValue = (Enum)val;
                EnumField field = GetOrCreateEnumField(enumValue);
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(enumValue);
            }

            SetSpecificity(specificity);
        }

        private static bool IsFocused(VisualElement ve)
        {
            return ve.focusController != null && ve.focusController.IsFocused(ve);
        }

        private ObjectField GetOrCreateObjectField<T>()
        {
            bool isCreation = childCount == 0;
            ObjectField field = GetOrCreateField<ObjectField, UnityEngine.Object>();
            if (isCreation)
                field.objectType = typeof(T);
            return field;
        }

        private EnumField GetOrCreateEnumField(Enum enumValue)
        {
            bool isCreation = childCount == 0;
            EnumField field = GetOrCreateField<EnumField, Enum>();
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
                    SetPropertyValue(e.newValue));
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
                field.RegisterValueChangedCallback(e => SetPropertyValue(e.newValue));
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
            string specificityString = "";
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

        private void SetPropertyValue(object newValue)
        {
            object val = StyleDebug.GetComputedStyleActualValue(m_SelectedElement.computedStyle, m_PropertyInfo.id);
            Type type = m_PropertyInfo.type;

            if (newValue == null)
            {
                if (type == typeof(StyleBackground))
                    val = new StyleBackground();

                if (type == typeof(StyleFont))
                    val = new StyleFont();
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
