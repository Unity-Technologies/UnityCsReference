using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    internal struct StylePropertyValue
    {
        public StyleSheet sheet;
        public StyleValueHandle handle;
    }

    internal struct ImageSource
    {
        public Texture2D texture;
        public Sprite sprite;
        public VectorImage vectorImage;
        public RenderTexture renderTexture;

        public  bool IsNull()
        {
            return texture == null && sprite == null && vectorImage == null && renderTexture == null;
        }
    }

    internal class StylePropertyReader
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);
        internal static GetCursorIdFunction getCursorIdFunc = null;

        private List<StylePropertyValue> m_Values = new List<StylePropertyValue>();
        private List<int> m_ValueCount = new List<int>();
        private StyleVariableResolver m_Resolver = new StyleVariableResolver();
        private StyleSheet m_Sheet;
        private StyleProperty[] m_Properties;
        private StylePropertyId[] m_PropertyIds;
        private int m_CurrentValueIndex;
        private int m_CurrentPropertyIndex;

        public StyleProperty property { get; private set; }
        public StylePropertyId propertyId { get; private set; }
        public int valueCount { get; private set; }

        public float dpiScaling { get; private set; }

        public void SetContext(StyleSheet sheet, StyleComplexSelector selector, StyleVariableContext varContext, float dpiScaling = 1.0f)
        {
            m_Sheet = sheet;
            m_Properties = selector.rule.properties;
            m_PropertyIds = StyleSheetCache.GetPropertyIds(sheet, selector.ruleIndex);
            m_Resolver.variableContext = varContext;

            this.dpiScaling = dpiScaling;
            LoadProperties();
        }

        // This is for UXML inline sheet
        public void SetInlineContext(StyleSheet sheet, StyleProperty[] properties, StylePropertyId[] propertyIds, float dpiScaling = 1.0f)
        {
            m_Sheet = sheet;
            m_Properties = properties;
            m_PropertyIds = propertyIds;

            this.dpiScaling = dpiScaling;
            LoadProperties();
        }

        public StylePropertyId MoveNextProperty()
        {
            ++m_CurrentPropertyIndex;
            m_CurrentValueIndex += valueCount;
            SetCurrentProperty();
            return propertyId;
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

        public Length ReadLength(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];

            if (value.handle.valueType == StyleValueType.Keyword)
            {
                var keyword = (StyleValueKeyword)value.handle.valueIndex;
                switch (keyword)
                {
                    case StyleValueKeyword.Auto:
                        return Length.Auto();
                    case StyleValueKeyword.None:
                        return Length.None();
                    default:
                        return new Length();
                }
            }

            var dimension = value.sheet.ReadDimension(value.handle);
            return dimension.ToLength();
        }

        public float ReadFloat(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadFloat(value.handle);
        }

        public int ReadInt(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return (int)value.sheet.ReadFloat(value.handle);
        }

        public Color ReadColor(int index)
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
            return c;
        }

        public int ReadEnum(StyleEnumType enumType, int index)
        {
            string enumString = null;
            var value = m_Values[m_CurrentValueIndex + index];
            var handle = value.handle;

            if (handle.valueType == StyleValueType.Keyword)
            {
                var keyword = value.sheet.ReadKeyword(handle);
                enumString = keyword.ToUssString();
            }
            else
            {
                enumString = value.sheet.ReadEnum(handle);
            }

            return StylePropertyUtil.GetEnumIntValue(enumType, enumString);
        }

        public FontDefinition ReadFontDefinition(int index)
        {
            Object fontAsset = null;
            Font font = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = value.sheet.ReadResourcePath(value.handle);
                    if (!string.IsNullOrEmpty(path))
                    {
                        font = Panel.LoadResource(path, typeof(Font), dpiScaling) as Font;
                        if (font == null)
                            fontAsset = Panel.LoadResource(path, typeof(Object), dpiScaling);
                    }

                    if (fontAsset == null && font == null)
                        Debug.LogWarning(string.Format("Font not found for path: {0}", path));

                    break;
                }

                case StyleValueType.AssetReference:
                {
                    font = value.sheet.ReadAssetReference(value.handle) as Font;
                    if (font == null)
                        fontAsset = value.sheet.ReadAssetReference(value.handle);

                    if (fontAsset == null && font == null)
                        Debug.LogWarning("Invalid font reference");

                    break;
                }

                default:
                    Debug.LogWarning("Invalid value for font " + value.handle.valueType);
                    break;
            }

            FontDefinition sfd;
            if (font != null)
                sfd = FontDefinition.FromFont(font);
            else if (fontAsset != null)
                sfd = FontDefinition.FromSDFFont(fontAsset);
            else
                sfd = new FontDefinition();
            return sfd;
        }

        public Font ReadFont(int index)
        {
            Font font = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = value.sheet.ReadResourcePath(value.handle);
                    if (!string.IsNullOrEmpty(path))
                        font = Panel.LoadResource(path, typeof(Font), dpiScaling) as Font;

                    if (font == null)
                        Debug.LogWarning(string.Format("Font not found for path: {0}", path));
                    break;
                }

                case StyleValueType.AssetReference:
                {
                    font = value.sheet.ReadAssetReference(value.handle) as Font;

                    if (font == null)
                        Debug.LogWarning("Invalid font reference");
                    break;
                }

                default:
                    Debug.LogWarning("Invalid value for font " + value.handle.valueType);
                    break;
            }

            return font;
        }

        public Background ReadBackground(int index)
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
            else if (TryGetImageSourceFromValue(value, dpiScaling, out source) == false)
            {
                // Load a stand-in picture to make it easier to identify which image element is missing its picture
                source.texture = Panel.LoadResource("d_console.warnicon", typeof(Texture2D), dpiScaling) as Texture2D;
            }

            Background background;
            if (source.texture != null)
                background = Background.FromTexture2D(source.texture);
            else if (source.sprite != null)
                background = Background.FromSprite(source.sprite);
            else if (source.vectorImage != null)
                background = Background.FromVectorImage(source.vectorImage);
            else if (source.renderTexture != null)
                background = Background.FromRenderTexture(source.renderTexture);
            else
                background = new Background();
            return background;
        }

        public Cursor ReadCursor(int index)
        {
            float hotspotX = 0f;
            float hotspotY = 0f;
            int cursorId = 0;
            Texture2D texture = null;

            var valueType = GetValueType(index);
            bool isCustom = valueType == StyleValueType.ResourcePath || valueType == StyleValueType.AssetReference || valueType == StyleValueType.ScalableImage;

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
                    if (TryGetImageSourceFromValue(value, dpiScaling, out source))
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

            return new Cursor() { texture = texture, hotspot = new Vector2(hotspotX, hotspotY), defaultCursorId = cursorId };
        }

        public TextShadow ReadTextShadow(int index)
        {
            float offsetX = 0.0f;
            float offsetY = 0.0f;
            float blurRadius = 0.0f;
            var color = Color.clear;

            if (valueCount >= 2)
            {
                int i = index;
                var valueType = GetValueType(i);
                bool isColorRead = false;
                if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                {
                    color = ReadColor(i++);
                    isColorRead = true;
                }

                if ((i + 1) < valueCount)
                {
                    valueType = GetValueType(i);
                    var valueType2 = GetValueType(i + 1);
                    if (valueType == StyleValueType.Dimension && valueType2 == StyleValueType.Dimension)
                    {
                        var valueX = GetValue(i++);
                        var valueY = GetValue(i++);

                        offsetX = valueX.sheet.ReadDimension(valueX.handle).value;
                        offsetY = valueY.sheet.ReadDimension(valueY.handle).value;
                    }
                }

                if (i < valueCount)
                {
                    valueType = GetValueType(i);
                    if (valueType == StyleValueType.Dimension)
                    {
                        var valueBlur = GetValue(i++);
                        blurRadius = valueBlur.sheet.ReadDimension(valueBlur.handle).value;
                    }
                    else if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        if (!isColorRead)
                            color = ReadColor(i);
                    }
                }

                if (i < valueCount)
                {
                    valueType = GetValueType(i);
                    if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        if (!isColorRead)
                            color = ReadColor(i);
                    }
                }
            }

            var textShadow = new TextShadow() { offset = new Vector2(offsetX, offsetY), blurRadius = blurRadius, color = color };
            return textShadow;
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
                        m_Values.Add(new StylePropertyValue() { sheet = m_Sheet, handle = sp.values[i] });
                    }
                }

                m_ValueCount.Add(count);
            }

            SetCurrentProperty();
        }

        private void SetCurrentProperty()
        {
            if (m_CurrentPropertyIndex < m_PropertyIds.Length)
            {
                property = m_Properties[m_CurrentPropertyIndex];
                propertyId = m_PropertyIds[m_CurrentPropertyIndex];
                valueCount = m_ValueCount[m_CurrentPropertyIndex];
            }
            else
            {
                property = null;
                propertyId = StylePropertyId.Unknown;
                valueCount = 0;
            }
        }

        internal static bool TryGetImageSourceFromValue(StylePropertyValue propertyValue, float dpiScaling, out ImageSource source)
        {
            source = new ImageSource();

            switch (propertyValue.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = propertyValue.sheet.ReadResourcePath(propertyValue.handle);
                    if (!string.IsNullOrEmpty(path))
                    {
                        //TODO: This will use GUIUtility.pixelsPerPoint as targetDpi, this may not be the best value for the current panel
                        source.sprite = Panel.LoadResource(path, typeof(Sprite), dpiScaling) as Sprite;
                        if (source.IsNull())
                            source.texture = Panel.LoadResource(path, typeof(Texture2D), dpiScaling) as Texture2D;
                        if (source.IsNull())
                            source.vectorImage = Panel.LoadResource(path, typeof(VectorImage), dpiScaling) as VectorImage;
                        if (source.IsNull())
                            source.renderTexture = Panel.LoadResource(path, typeof(RenderTexture), dpiScaling) as RenderTexture;
                    }

                    if (source.IsNull())
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
                    source.sprite = o as Sprite;
                    source.vectorImage = o as VectorImage;
                    source.renderTexture = o as RenderTexture;
                    if (source.IsNull())
                    {
                        Debug.LogWarning("Invalid image specified");
                        return false;
                    }
                }
                break;

                case StyleValueType.MissingAssetReference:
                    return false;

                case StyleValueType.ScalableImage:
                {
                    var img = propertyValue.sheet.ReadScalableImage(propertyValue.handle);

                    if (img.normalImage == null && img.highResolutionImage == null)
                    {
                        Debug.LogWarning("Invalid scalable image specified");
                        return false;
                    }

                    if (dpiScaling > 1.0f)
                    {
                        source.texture = img.highResolutionImage;
                        source.texture.pixelsPerPoint = 2.0f;
                    }
                    else
                    {
                        source.texture = img.normalImage;
                    }

                    if (!Mathf.Approximately(dpiScaling % 1.0f, 0))
                    {
                        source.texture.filterMode = FilterMode.Bilinear;
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
}
