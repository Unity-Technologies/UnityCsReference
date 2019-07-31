// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    internal interface IStylePropertyReader
    {
        StylePropertyID propertyID { get; }
        int specificity { get; }
        int valueCount { get; }

        bool IsValueType(int index, StyleValueType type);
        bool IsKeyword(int index, StyleValueKeyword keyword);

        string ReadAsString(int index);
        StyleLength ReadStyleLength(int index);
        StyleFloat ReadStyleFloat(int index);
        StyleInt ReadStyleInt(int index);
        StyleColor ReadStyleColor(int index);
        StyleInt ReadStyleEnum<T>(int index);
        StyleFont ReadStyleFont(int index);
        StyleBackground ReadStyleBackground(int index);
        StyleCursor ReadStyleCursor(int index);
    }

    internal struct StylePropertyValue
    {
        public StyleSheet sheet;
        public StyleValueHandle handle;
    }

    internal struct ImageSource
    {
        public Texture2D texture;
        public VectorImage vectorImage;
    }

    internal class StylePropertyReader : IStylePropertyReader
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);
        internal static GetCursorIdFunction getCursorIdFunc = null;

        private List<StylePropertyValue> m_Values = new List<StylePropertyValue>();
        private List<int> m_ValueCount = new List<int>();
        private StyleVariableResolver m_Resolver = new StyleVariableResolver();
        private StyleSheet m_Sheet;
        private StyleProperty[] m_Properties;
        private StylePropertyID[] m_PropertyIDs;
        private int m_CurrentValueIndex;
        private int m_CurrentPropertyIndex;

        public StyleProperty property { get; private set; }
        public StylePropertyID propertyID { get; private set; }
        public int valueCount { get; private set; }
        public int specificity { get; private set; }

        public void SetContext(StyleSheet sheet, StyleComplexSelector selector, StyleVariableContext varContext)
        {
            m_Sheet = sheet;
            m_Properties = selector.rule.properties;
            m_PropertyIDs = StyleSheetCache.GetPropertyIDs(sheet, selector.ruleIndex);
            m_Resolver.variableContext = varContext;

            specificity = sheet.isUnityStyleSheet ? StyleValueExtensions.UnitySpecificity : selector.specificity;
            LoadProperties();
        }

        // This is for UXML inline sheet
        public void SetInlineContext(StyleSheet sheet, StyleRule rule, int ruleIndex)
        {
            m_Sheet = sheet;
            m_Properties = rule.properties;
            m_PropertyIDs = StyleSheetCache.GetPropertyIDs(sheet, ruleIndex);

            specificity = StyleValueExtensions.InlineSpecificity;
            LoadProperties();
        }

        public StylePropertyID MoveNextProperty()
        {
            ++m_CurrentPropertyIndex;
            m_CurrentValueIndex += valueCount;
            SetCurrentProperty();
            return propertyID;
        }

        public StylePropertyValue GetValue(int index)
        {
            return m_Values[m_CurrentValueIndex + index];
        }

        public StyleValueType GetValueType(int index)
        {
            return m_Values[m_CurrentValueIndex + index].handle.valueType;
        }

        public bool IsValueType(int index, StyleValueType type)
        {
            return m_Values[m_CurrentValueIndex + index].handle.valueType == type;
        }

        public bool IsKeyword(int index, StyleValueKeyword keyword)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.handle.valueType == StyleValueType.Keyword && (StyleValueKeyword)value.handle.valueIndex == keyword;
        }

        public string ReadAsString(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadAsString(value.handle);
        }

        public StyleLength ReadStyleLength(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];

            if (value.handle.valueType == StyleValueType.Keyword)
            {
                var keyword = (StyleValueKeyword)value.handle.valueIndex;
                return new StyleLength(keyword.ToStyleKeyword()) { specificity = specificity };
            }
            else
            {
                var dimension = value.sheet.ReadDimension(value.handle);
                return new StyleLength(dimension.ToLength()) { specificity = specificity };
            }
        }

        public StyleFloat ReadStyleFloat(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return new StyleFloat(value.sheet.ReadFloat(value.handle)) {specificity = specificity};
        }

        public StyleInt ReadStyleInt(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return new StyleInt((int)value.sheet.ReadFloat(value.handle)) {specificity = specificity};
        }

        public StyleColor ReadStyleColor(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            Color c = Color.clear;
            if (value.handle.valueType == StyleValueType.Enum)
            {
                var colorName = value.sheet.ReadAsString(value.handle);
                StyleSheetColor.TryGetColor(colorName.ToLower(), out c);
            }
            else
            {
                c = value.sheet.ReadColor(value.handle);
            }
            return new StyleColor(c) {specificity = specificity};
        }

        public StyleInt ReadStyleEnum<T>(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return new StyleInt(StyleSheetCache.GetEnumValue<T>(value.sheet, value.handle)) {specificity = specificity};
        }

        public StyleFont ReadStyleFont(int index)
        {
            Font font = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = value.sheet.ReadResourcePath(value.handle);
                    if (!string.IsNullOrEmpty(path))
                    {
                        font = Panel.LoadResource(path, typeof(Font)) as Font;
                    }

                    if (font == null)
                    {
                        Debug.LogWarning(string.Format("Font not found for path: {0}", path));
                    }
                    break;
                }

                case StyleValueType.AssetReference:
                {
                    font = value.sheet.ReadAssetReference(value.handle) as Font;

                    if (font == null)
                    {
                        Debug.LogWarning("Invalid font reference");
                    }
                    break;
                }

                default:
                    Debug.LogWarning("Invalid value for font " + value.handle.valueType);
                    break;
            }

            return new StyleFont(font) {specificity = specificity};
        }

        public StyleBackground ReadStyleBackground(int index)
        {
            var source = new ImageSource();
            var value = m_Values[m_CurrentValueIndex + index];
            if (value.handle.valueType == StyleValueType.Keyword)
            {
                if (value.handle.valueIndex != (int)StyleValueKeyword.None)
                {
                    Debug.LogWarning("Invalid keyword for image source " + (StyleValueKeyword)value.handle.valueIndex);
                }
                else
                {
                    // it's OK, we let none be assigned to the source
                }
            }
            else if (TryGetImageSourceFromValue(value, out source) == false)
            {
                // Load a stand-in picture to make it easier to identify which image element is missing its picture
                source.texture = Panel.LoadResource("d_console.warnicon", typeof(Texture2D)) as Texture2D;
            }

            StyleBackground sb;
            if (source.texture != null)
                sb = new StyleBackground(source.texture) { specificity = specificity };
            else if (source.vectorImage != null)
                sb = new StyleBackground(source.vectorImage) { specificity = specificity };
            else
                sb = new StyleBackground() {specificity = specificity};
            return sb;
        }

        public StyleCursor ReadStyleCursor(int index)
        {
            float hotspotX = 0f;
            float hotspotY = 0f;
            int cursorId = 0;
            Texture2D texture = null;

            var valueType = GetValueType(index);
            bool isCustom = valueType == StyleValueType.ResourcePath || valueType == StyleValueType.AssetReference;

            if (isCustom)
            {
                if (valueCount < 1)
                {
                    Debug.LogWarning($"USS 'cursor' has invalid value at {index}.");
                }
                else
                {
                    var source = new ImageSource();
                    var value = GetValue(index);
                    if (TryGetImageSourceFromValue(value, out source))
                    {
                        texture = source.texture;
                        if (valueCount >= 3)
                        {
                            var valueX = GetValue(index + 1);
                            var valueY = GetValue(index + 2);
                            if (valueX.handle.valueType != StyleValueType.Float || valueY.handle.valueType != StyleValueType.Float)
                            {
                                Debug.LogWarning("USS 'cursor' property requires two integers for the hot spot value.");
                            }
                            else
                            {
                                hotspotX = valueX.sheet.ReadFloat(valueX.handle);
                                hotspotY = valueY.sheet.ReadFloat(valueY.handle);
                            }
                        }
                    }
                }
            }
            else
            {
                // Default cursor
                if (getCursorIdFunc != null)
                {
                    var value = GetValue(index);
                    cursorId = getCursorIdFunc(value.sheet, value.handle);
                }
            }

            var cursor = new Cursor() { texture = texture, hotspot = new Vector2(hotspotX, hotspotY), defaultCursorId = cursorId };
            return new StyleCursor(cursor) {specificity = specificity};
        }

        private void LoadProperties()
        {
            m_CurrentPropertyIndex = 0;
            m_CurrentValueIndex = 0;
            m_Values.Clear();
            m_ValueCount.Clear();

            foreach (var sp in m_Properties)
            {
                int count = 0;
                bool valid = true;

                if (sp.requireVariableResolve)
                {
                    // Slow path - Values contain one or more var
                    m_Resolver.Init(sp, m_Sheet, sp.values);
                    for (int i = 0; i < sp.values.Length && valid; ++i)
                    {
                        var handle = sp.values[i];
                        if (handle.IsVarFunction())
                        {
                            var result = m_Resolver.ResolveVarFunction(ref i);
                            if (result != StyleVariableResolver.Result.Valid)
                            {
                                // Resolve failed
                                // When this happens, the computed value of the property is either the property’s
                                // inherited value or its initial value depending on whether the property is inherited or not.
                                // This is the same behavior as the unset keyword so we simply resolve to that value.
                                var unsetHandle = new StyleValueHandle() { valueType = StyleValueType.Keyword, valueIndex = (int)StyleValueKeyword.Unset};
                                m_Values.Add(new StylePropertyValue() { sheet = m_Sheet, handle = unsetHandle });
                                ++count;

                                valid = false;
                            }
                        }
                        else
                        {
                            m_Resolver.AddValue(handle);
                        }
                    }

                    if (valid)
                    {
                        m_Values.AddRange(m_Resolver.resolvedValues);
                        count += m_Resolver.resolvedValues.Count;
                    }
                }
                else
                {
                    // Fast path - no var
                    count = sp.values.Length;
                    for (int i = 0; i < count; ++i)
                    {
                        var handle = sp.values[i];
                        m_Values.Add(new StylePropertyValue() { sheet = m_Sheet, handle = sp.values[i] });
                    }
                }

                m_ValueCount.Add(count);
            }

            SetCurrentProperty();
        }

        private void SetCurrentProperty()
        {
            if (m_CurrentPropertyIndex < m_PropertyIDs.Length)
            {
                property = m_Properties[m_CurrentPropertyIndex];
                propertyID = m_PropertyIDs[m_CurrentPropertyIndex];
                valueCount = m_ValueCount[m_CurrentPropertyIndex];
            }
            else
            {
                property = null;
                propertyID = StylePropertyID.Unknown;
                valueCount = 0;
            }
        }

        internal static bool TryGetImageSourceFromValue(StylePropertyValue propertyValue, out ImageSource source)
        {
            source = new ImageSource();

            switch (propertyValue.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = propertyValue.sheet.ReadResourcePath(propertyValue.handle);
                    if (!string.IsNullOrEmpty(path))
                    {
                        source.texture = Panel.LoadResource(path, typeof(Texture2D)) as Texture2D;
                        if (source.texture == null)
                            source.vectorImage = Panel.LoadResource(path, typeof(VectorImage)) as VectorImage;
                    }

                    if (source.texture == null && source.vectorImage == null)
                    {
                        Debug.LogWarning(string.Format("Image not found for path: {0}", path));
                        return false;
                    }
                }
                break;

                case StyleValueType.AssetReference:
                {
                    var o = propertyValue.sheet.ReadAssetReference(propertyValue.handle);
                    source.texture = o as Texture2D;
                    source.vectorImage = o as VectorImage;
                    if (source.texture == null && source.vectorImage == null)
                    {
                        Debug.LogWarning("Invalid image specified");
                        return false;
                    }
                }
                break;

                default:
                    Debug.LogWarning("Invalid value for image texture " + propertyValue.handle.valueType);
                    return false;
            }

            return true;
        }
    }

    internal class StyleValuePropertyReader : IStylePropertyReader
    {
        public StylePropertyID propertyID { get; private set; }
        public int specificity { get; private set; }
        public int valueCount => 1;

        private StyleValue m_CurrentStyleValue;
        private StyleCursor m_CurrentCursor;

        public void Set(StylePropertyID id, StyleValue value, int spec)
        {
            propertyID = id;
            m_CurrentStyleValue = value;
            specificity = spec;
        }

        public void Set(StyleCursor cursor, int spec)
        {
            propertyID = StylePropertyID.Cursor;
            m_CurrentCursor = cursor;
            specificity = spec;
        }

        public bool IsValueType(int index, StyleValueType type)
        {
            if (type == StyleValueType.Keyword)
                return m_CurrentStyleValue.keyword != StyleKeyword.Undefined;

            return m_CurrentStyleValue.keyword == StyleKeyword.Undefined;
        }

        public bool IsKeyword(int index, StyleValueKeyword keyword)
        {
            if (m_CurrentStyleValue.keyword == StyleKeyword.Undefined)
                return false;

            return m_CurrentStyleValue.keyword == keyword.ToStyleKeyword();
        }

        public string ReadAsString(int index)
        {
            if (m_CurrentStyleValue.keyword != StyleKeyword.Undefined)
                return m_CurrentStyleValue.keyword.ToString();

            return m_CurrentStyleValue.number.ToString();
        }

        public StyleLength ReadStyleLength(int index)
        {
            return new StyleLength(m_CurrentStyleValue.length, m_CurrentStyleValue.keyword) { specificity = specificity };
        }

        public StyleFloat ReadStyleFloat(int index)
        {
            return new StyleFloat(m_CurrentStyleValue.number, m_CurrentStyleValue.keyword) { specificity = specificity };
        }

        public StyleInt ReadStyleInt(int index)
        {
            return new StyleInt((int)m_CurrentStyleValue.number, m_CurrentStyleValue.keyword) { specificity = specificity };
        }

        public StyleColor ReadStyleColor(int index)
        {
            return new StyleColor(m_CurrentStyleValue.color, m_CurrentStyleValue.keyword) { specificity = specificity };
        }

        public StyleInt ReadStyleEnum<T>(int index)
        {
            return new StyleInt((int)m_CurrentStyleValue.number, m_CurrentStyleValue.keyword) { specificity = specificity };
        }

        public StyleFont ReadStyleFont(int index)
        {
            Font font = null;
            if (m_CurrentStyleValue.resource.IsAllocated)
                font = m_CurrentStyleValue.resource.Target as Font;

            return new StyleFont(font, m_CurrentStyleValue.keyword) { specificity = specificity };
        }

        public StyleBackground ReadStyleBackground(int index)
        {
            var styleBackground = new StyleBackground(m_CurrentStyleValue.keyword);
            if (m_CurrentStyleValue.resource.IsAllocated)
            {
                var texture = m_CurrentStyleValue.resource.Target as Texture2D;
                if (texture != null)
                    styleBackground = new StyleBackground(texture, m_CurrentStyleValue.keyword);
                else
                {
                    var vectorImage = m_CurrentStyleValue.resource.Target as VectorImage;
                    if (vectorImage != null)
                        styleBackground = new StyleBackground(vectorImage, m_CurrentStyleValue.keyword);
                }
            }

            styleBackground.specificity = specificity;
            return styleBackground;
        }

        public StyleCursor ReadStyleCursor(int index)
        {
            return new StyleCursor(m_CurrentCursor.value, m_CurrentCursor.keyword) { specificity = specificity };
        }
    }
}
