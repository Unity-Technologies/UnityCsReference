// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.UIElements.Debugger
{
    internal class StylePropertyDebugger : VisualElement
    {
        private static readonly PropertyInfo[] k_FieldInfos = typeof(ComputedStyle).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private static readonly PropertyInfo[] k_SortedFieldInfos = k_FieldInfos.OrderBy(f => f.Name).ToArray();

        private Dictionary<string, StyleField> m_NameToFieldDictionary = new Dictionary<string, StyleField>();

        private Toolbar m_Toolbar;
        private VisualElement m_CustomPropertyFieldsContainer;
        private VisualElement m_FieldsContainer;
        private VisualElement m_SelectedElement;
        private string m_SearchFilter;
        private bool m_ShowAll;
        private bool m_Sort;

        public VisualElement selectedElement
        {
            get
            {
                return m_SelectedElement;
            }
            set
            {
                if (m_SelectedElement == value)
                    return;

                m_SelectedElement = value;
                BuildFields();
            }
        }

        public StylePropertyDebugger(VisualElement debuggerSelection)
        {
            selectedElement = debuggerSelection;

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

            var sortToggle = new ToolbarToggle();
            sortToggle.AddToClassList("unity-style-debugger-toggle");
            sortToggle.text = "Sort";
            sortToggle.RegisterValueChangedCallback(e =>
            {
                m_Sort = e.newValue;
                BuildFields();
            });
            m_Toolbar.Add(sortToggle);

            m_CustomPropertyFieldsContainer = new VisualElement();
            Add(m_CustomPropertyFieldsContainer);

            m_FieldsContainer = new VisualElement();
            Add(m_FieldsContainer);

            if (selectedElement != null)
                BuildFields();

            AddToClassList("unity-style-debugger");
        }

        public void Refresh()
        {
            RefreshFields();
        }

        private void BuildFields()
        {
            m_FieldsContainer.Clear();
            m_NameToFieldDictionary.Clear();

            RefreshFields();
        }

        private void RefreshFields()
        {
            if (m_SelectedElement == null)
                return;

            m_CustomPropertyFieldsContainer.Clear();
            var customProperties = m_SelectedElement.specifiedStyle.m_CustomProperties;
            if (customProperties != null && customProperties.Any())
            {
                foreach (KeyValuePair<string, CustomPropertyHandle> customProperty in customProperties)
                {
                    var styleName = customProperty.Key;
                    foreach (StyleValueHandle handle in customProperty.Value.handles)
                    {
                        TextField textField = new TextField(styleName) { isReadOnly = true };
                        textField.AddToClassList("unity-style-field");
                        textField.value = customProperty.Value.data.ReadAsString(handle).ToLower();
                        m_CustomPropertyFieldsContainer.Add(textField);
                    }
                }
            }

            foreach (PropertyInfo propertyInfo in m_Sort ? k_SortedFieldInfos : k_FieldInfos)
            {
                var styleName = GetUSSPropertyNameFromComputedStyleName(propertyInfo.Name);
                if (!string.IsNullOrEmpty(m_SearchFilter) &&
                    styleName.IndexOf(m_SearchFilter, StringComparison.InvariantCultureIgnoreCase) == -1)
                    continue;

                object val = propertyInfo.GetValue(selectedElement.computedStyle, null);
                Type type = val.GetType();

                int specificity = (int)type.GetProperty("specificity", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(val, null);
                StyleField sf = null;
                m_NameToFieldDictionary.TryGetValue(styleName, out sf);
                if (m_ShowAll || specificity != StyleValueExtensions.UndefinedSpecificity)
                {
                    if (sf != null)
                    {
                        sf.RefreshPropertyValue(val, specificity);
                    }
                    else
                    {
                        var sfPropInfo = new StyleFieldPropertyInfo() { name = styleName, value = val, specificity = specificity, computedPropertyInfo = propertyInfo };
                        sf = new StyleField(m_SelectedElement, sfPropInfo);
                        m_FieldsContainer.Add(sf);
                        m_NameToFieldDictionary[styleName] = sf;
                    }
                }
                else if (sf != null)
                {
                    // Style property is not applied anymore, remove the field
                    m_NameToFieldDictionary.Remove(styleName);
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

    internal struct StyleFieldPropertyInfo
    {
        public string name;
        public object value;
        public int specificity;
        public PropertyInfo computedPropertyInfo;
    }

    internal class StyleField : VisualElement
    {
        private VisualElement m_SelectedElement;
        private Label m_SpecificityLabel;
        private PropertyInfo m_PropertyInfo;
        private string m_PropertyName;

        public StyleField(VisualElement selectedElement, StyleFieldPropertyInfo propInfo)
        {
            AddToClassList("unity-style-field");

            m_SelectedElement = selectedElement;
            m_PropertyInfo = propInfo.computedPropertyInfo;
            m_PropertyName = propInfo.name;

            m_SpecificityLabel = new Label();
            m_SpecificityLabel.AddToClassList("unity-style-field__specifity-label");

            RefreshPropertyValue(propInfo.value, propInfo.specificity);
        }

        public void RefreshPropertyValue(object val, int specificity)
        {
            if (val is StyleFloat)
            {
                var style = (StyleFloat)val;
                FloatField field = GetOrCreateField<FloatField, float>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(style.value);
            }
            else if (val is StyleInt)
            {
                var style = (StyleInt)val;
                IntegerField field = GetOrCreateField<IntegerField, int>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(style.value);
            }
            else if (val is StyleLength)
            {
                var style = (StyleLength)val;
                StyleLengthField field = GetOrCreateField<StyleLengthField, StyleLength>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(style);
            }
            else if (val is StyleColor)
            {
                var style = (StyleColor)val;
                ColorField field = GetOrCreateField<ColorField, Color>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(style.value);
            }
            else if (val is StyleFont)
            {
                var style = (StyleFont)val;
                ObjectField field = GetOrCreateObjectField<Font>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(style.value);
            }
            else if (val is StyleBackground)
            {
                var style = (StyleBackground)val;
                ObjectField field = GetOrCreateObjectField<Texture2D>();
                if (!IsFocused(field))
                    field.SetValueWithoutNotify(style.value.texture);
            }
            else if (val is StyleCursor)
            {
                // Recreate the cursor fields every time since StyleCursor
                // can be made of different fields (based on the value)
                Clear();

                StyleCursor style = (StyleCursor)val;
                if (style.value.texture != null)
                {
                    var uiTextureField = new ObjectField(m_PropertyName) { value = style.value.texture };
                    uiTextureField.RegisterValueChangedCallback(e =>
                    {
                        StyleCursor styleCursor = (StyleCursor)m_PropertyInfo.GetValue(m_SelectedElement.computedStyle, null);
                        var currentCursor = styleCursor.value;
                        currentCursor.texture = e.newValue as Texture2D;
                        SetPropertyValue(new StyleCursor(currentCursor));
                    });
                    Add(uiTextureField);

                    var uiHotspotField = new Vector2Field("hotspot") { value = style.value.hotspot };
                    uiHotspotField.RegisterValueChangedCallback(e =>
                    {
                        StyleCursor styleCursor = (StyleCursor)m_PropertyInfo.GetValue(m_SelectedElement.computedStyle, null);
                        var currentCursor = styleCursor.value;
                        currentCursor.hotspot = e.newValue;
                        SetPropertyValue(new StyleCursor(currentCursor));
                    });
                    Add(uiHotspotField);
                }
                else
                {
                    int mouseId = style.value.defaultCursorId;
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
            else
            {
                // StyleEnum<T>
                var type = val.GetType();
                var propInfo = type.GetProperty("value");
                Enum enumValue = propInfo.GetValue(val, null) as Enum;
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

        private void SetSpecificity(int specificity)
        {
            string specificityString = "";
            switch (specificity)
            {
                case StyleValueExtensions.UnitySpecificity:
                    specificityString = "unity stylesheet";
                    break;
                case StyleValueExtensions.InlineSpecificity:
                    specificityString = "inline";
                    break;
                case StyleValueExtensions.UndefinedSpecificity:
                    break;
                default:
                    specificityString = specificity.ToString();
                    break;
            }

            m_SpecificityLabel.text = specificityString;
        }

        private void SetPropertyValue(object newValue)
        {
            object val = m_PropertyInfo.GetValue(m_SelectedElement.computedStyle, null);
            Type type = val.GetType();

            if (type == newValue.GetType())
            {
                val = newValue;
            }
            else
            {
                if (type == typeof(StyleBackground))
                    newValue = new Background(newValue as Texture2D);

                var valueInfo = type.GetProperty("value");
                try
                {
                    valueInfo.SetValue(val, newValue, null);
                }
                catch (Exception)
                {
                    Debug.LogError($"Invalid value for property '{m_PropertyName}'");
                    return;
                }
            }

            var propertyName = m_PropertyInfo.Name;
            var inlineStyle = typeof(IStyle).GetProperty(propertyName);
            inlineStyle.SetValue(m_SelectedElement.style, val, null);

            SetSpecificity(StyleValueExtensions.InlineSpecificity);
        }
    }
}
