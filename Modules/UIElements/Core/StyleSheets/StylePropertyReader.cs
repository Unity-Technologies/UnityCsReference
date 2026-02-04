// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.StyleSheets
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal struct StylePropertyValue
    {
        public StyleSheet sheet;
        public StyleValueHandle handle;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal partial class StylePropertyReader
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);

        internal static GetCursorIdFunction getCursorIdFunc = null;

        private List<StylePropertyValue> m_Values = new List<StylePropertyValue>();
        private List<int> m_ValueCount = new List<int>();
        private StyleVariableResolver m_Resolver = new StyleVariableResolver();
        private StyleSheet m_Sheet;
        private StyleProperty[] m_Properties;
        private int m_CurrentValueIndex { get; set; }
        private int m_CurrentPropertyIndex;

        public StyleProperty property { get; private set; }
        public StylePropertyId propertyId { get; private set; }
        public int valueCount { get; private set; }

        public float dpiScaling { get; private set; }

        public void SetContext(StyleSheet sheet, StyleComplexSelector selector, StyleVariableContext varContext, float dpiScaling = 1.0f)
        {
            m_Sheet = sheet;
            m_Properties = selector.rule.properties;
            m_Resolver.variableContext = varContext;

            this.dpiScaling = dpiScaling;
            LoadProperties();
        }

        // This is for UXML inline sheet
        public void SetInlineContext(StyleSheet sheet, StyleProperty[] properties, float dpiScaling = 1.0f)
        {
            m_Sheet = sheet;
            m_Properties = properties;

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

        public TimeValue ReadTimeValue(int index)
        {
            var value = m_Values[m_CurrentValueIndex + index];
            return value.sheet.ReadDimension(value.handle).ToTime();
        }

        public Translate ReadTranslate(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;

            return ReadTranslate(valueCount, val1, val2, val3);
        }

        public TransformOrigin ReadTransformOrigin(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;

            return ReadTransformOrigin(valueCount, val1, val2, val3);
        }

        public Rotate ReadRotate(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            var val4 = valueCount > 3 ? m_Values[m_CurrentValueIndex + index + 3] : default;

            return ReadRotate(valueCount, val1, val2, val3, val4);
        }

        public Scale ReadScale(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;

            return ReadScale(valueCount, val1, val2, val3);
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
            return value.sheet.ReadColor(value.handle);
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

            StylePropertyUtil.TryGetEnumIntValue(enumType, enumString, out var intValue);
            return intValue;
        }

        public Object ReadAsset(int index)
        {
            Object o = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    o = value.sheet.ReadResourcePath(value.handle).LoadResource<Object>(dpiScaling);
                    break;
                }
                case StyleValueType.AssetReference:
                {
                    o = value.sheet.ReadAssetReference(value.handle);
                    break;
                }
            }
            return o;
        }

        public EntityId ReadFontDefinition(int index)
        {
            FontAsset fontAsset = null;
            Font font = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    var resourcePath = value.sheet.ReadResourcePath(value.handle);
                    font = resourcePath.LoadResource<Font>(dpiScaling);
                    if (font == null)
                        fontAsset = resourcePath.LoadResource<FontAsset>(dpiScaling);

                    if (fontAsset == null && font == null)
                        Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, "Font not found for path: {0}", resourcePath.ToString()));

                    break;
                }

                case StyleValueType.AssetReference:
                {
                    font = value.sheet.ReadAssetReference(value.handle) as Font;
                    if (font == null)
                        fontAsset = value.sheet.ReadAssetReference(value.handle) as FontAsset;

                    break;
                }

                case StyleValueType.Keyword:
                {
                    if (value.handle.valueIndex != (int)StyleValueKeyword.None)
                        Debug.LogWarning("Invalid keyword for font " + (StyleValueKeyword)value.handle.valueIndex);

                    break;
                }

                case StyleValueType.MissingAssetReference:
                {
                    var missingAssetUrl = value.sheet.ReadMissingAssetReferenceUrl(value.handle);
                    Debug.LogWarning(string.Format(CultureInfo.InvariantCulture,
                        "Missing font asset reference '{0}' in stylesheet '{1}'. The font asset may have been deleted or moved.",
                        missingAssetUrl, value.sheet.name), value.sheet);
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

            FontDefinition.To(in sfd, out var entityId);
            return entityId;
        }

        public EntityId ReadFont(int index)
        {
            Font font = null;
            var value = m_Values[m_CurrentValueIndex + index];
            switch (value.handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    var resourcePath = value.sheet.ReadResourcePath(value.handle);
                    font = resourcePath.LoadResource<Font>(dpiScaling);
                    if (font == null)
                        Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, "Font not found for path: {0}", resourcePath.ToString()));
                    break;
                }

                case StyleValueType.AssetReference:
                {
                    font = value.sheet.ReadAssetReference(value.handle) as Font;

                    break;
                }

                case StyleValueType.Keyword:
                {
                    if (value.handle.valueIndex != (int)StyleValueKeyword.None)
                        Debug.LogWarning("Invalid keyword for font " + (StyleValueKeyword)value.handle.valueIndex);

                    break;
                }

                case StyleValueType.MissingAssetReference:
                {
                    var missingAssetUrl = value.sheet.ReadMissingAssetReferenceUrl(value.handle);
                    Debug.LogWarning(string.Format(CultureInfo.InvariantCulture,
                        "Missing font asset reference '{0}' in stylesheet '{1}'. The font asset may have been deleted or moved.",
                        missingAssetUrl, value.sheet.name), value.sheet);
                    break;
                }

                default:
                    Debug.LogWarning("Invalid value for font " + value.handle.valueType);
                    break;
            }

            return font != null ? font.GetEntityId() : EntityId.None;
        }

        public void ReadMaterialDefinition(ref UnmanagedMaterialDefinition data, int index)
        {
            if (!property.TryGetMaterialDefinition(m_Sheet, ref data))
            {
                data.CopyFrom(UnmanagedMaterialDefinition.Empty);
            }
        }

        public EntityId ReadBackground(int index)
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

            Background.To(background, out EntityId entityId);
            return entityId;
        }

        public Cursor ReadCursor(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            return ReadCursor(valueCount, val1, val2, val3, dpiScaling);
        }

        public TextShadow ReadTextShadow(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            var val4 = valueCount > 3 ? m_Values[m_CurrentValueIndex + index + 3] : default;
            return ReadTextShadow(valueCount, val1, val2, val3, val4);
        }

        public TextAutoSize ReadTextAutoSize(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            var val3 = valueCount > 2 ? m_Values[m_CurrentValueIndex + index + 2] : default;
            return ReadTextAutoSize(valueCount, val1, val2, val3);
        }

        public BackgroundPosition ReadBackgroundPositionX(int index)
        {
            return ReadBackgroundPosition(index, BackgroundPositionKeyword.Left);
        }

        public BackgroundPosition ReadBackgroundPositionY(int index)
        {
            return ReadBackgroundPosition(index, BackgroundPositionKeyword.Top);
        }

        private BackgroundPosition ReadBackgroundPosition(int index, BackgroundPositionKeyword keyword)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            return ReadBackgroundPosition(valueCount, val1, val2, keyword);
        }


        public BackgroundRepeat ReadBackgroundRepeat(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            return ReadBackgroundRepeat(valueCount, val1, val2);
        }

        public BackgroundSize ReadBackgroundSize(int index)
        {
            var val1 = m_Values[m_CurrentValueIndex + index];
            var val2 = valueCount > 1 ? m_Values[m_CurrentValueIndex + index + 1] : default;
            return ReadBackgroundSize(valueCount, val1, val2);
        }

        public void ReadListEasingFunction(ref UnmanagedRefCountedList<EasingFunction> result, int index)
        {
            using var _ = ListPool<EasingFunction>.Get(out var list);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];
                var handle = value.handle;
                if (handle.valueType == StyleValueType.Enum)
                {
                    var enumString = value.sheet.ReadEnum(handle);
                    StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.EasingMode, enumString, out var intValue);
                    list.Add(new EasingFunction((EasingMode)intValue));
                    ++index;
                }

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);
            result.CopyFrom(list);
        }

        public void ReadListTimeValue(ref UnmanagedRefCountedList<TimeValue> result, int index)
        {
            using var _ = ListPool<TimeValue>.Get(out var list);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];
                var time = value.sheet.ReadDimension(value.handle).ToTime();
                list.Add(time);
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list);
        }

        public void ReadListUnmanagedFilterFunction(ref UnmanagedRefCountedList<UnmanagedFilterFunction> result, int index)
        {
            using var _ = ListPool<FilterFunction>.Get(out var list);
            do
            {
                var filterType = (StyleValueFunction)GetValue(index++).handle.valueIndex;
                int argCount = ReadInt(index++);

                bool isCustom = false;
                FilterFunctionDefinition filterDef = null;
                if (filterType == StyleValueFunction.CustomFilter && argCount > 0)
                {
                    isCustom = true;
                    filterDef = ReadAsset(index++) as FilterFunctionDefinition;
                    --argCount;
                }

                var args = new FixedBuffer4<FilterParameter>();
                for (int i = 0; i < argCount; i++)
                {
                    var valueType = GetValueType(index);
                    if (valueType == StyleValueType.Color || valueType == StyleValueType.Enum)
                    {
                        var color = ReadColor(index++);
                        args[i] = new FilterParameter()
                        {
                            type = FilterParameterType.Color,
                            colorValue = color
                        };
                    }
                    else if (valueType == StyleValueType.Dimension || valueType == StyleValueType.Float)
                    {
                        var dimValue = GetValue(index++);
                        var dim = dimValue.sheet.ReadDimension(dimValue.handle);
                        args[i] = new FilterParameter()
                        {
                            type = FilterParameterType.Float,
                            floatValue = StyleProperty.ConvertDimensionToFilterFloat(dim)
                        };
                    }
                    else if (valueType == StyleValueType.CommaSeparator)
                    {
                        // Not technically a valid syntax, but we'll allow it
                        continue;
                    }
                    else
                    {
                        Debug.LogError($"Unexpected value type {valueType} in filter function argument");
                    }
                }

                if (isCustom)
                    list.Add(new FilterFunction(filterDef, args, argCount));
                else
                    list.Add(new FilterFunction(StyleProperty.ToFilterFunctionType(filterType), args, argCount));
            }
            while (index < valueCount);

            result.CopyFrom(list);
        }

        public void ReadListStylePropertyId(ref UnmanagedRefCountedList<StylePropertyId> result, int index)
        {
            using var _ = ListPool<StylePropertyId>.Get(out var list);
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];

                StylePropertyName propertyName;
                if (value.handle.valueType == StyleValueType.Keyword)
                {
                    var keyword = value.sheet.ReadKeyword(value.handle);
                    propertyName = new StylePropertyName(keyword.ToUssString());
                }
                else
                {
                    propertyName = value.sheet.ReadStylePropertyName(value.handle);
                }
                list.Add(propertyName.id);
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);

            result.CopyFrom(list);
        }

        public void ReadListString(List<string> list, int index)
        {
            list.Clear();
            do
            {
                var value = m_Values[m_CurrentValueIndex + index];
                var str = value.sheet.ReadAsString(value.handle);
                list.Add(str);
                ++index;

                if (index < valueCount)
                {
                    var nextValue = m_Values[m_CurrentValueIndex + index];
                    if (nextValue.handle.valueType == StyleValueType.CommaSeparator)
                        ++index;
                }
            }
            while (index < valueCount);
        }

        public StyleRatio ReadRatio(int index)
        {
            if(valueCount == 1 && GetValueType(0) == StyleValueType.Float )
                return new StyleRatio(ReadFloat(index));

            if (valueCount == 3)
            {
                var val2 = m_Values[m_CurrentValueIndex + index + 1];
                var str = val2.sheet.ReadAsString(val2.handle);

                if(str == "/" )
                    return ReadFloat(index) / ReadFloat(index + 2);

            }

            if(!IsKeyword(0, StyleValueKeyword.Auto))
                Debug.LogError($"Unexpected value {m_Values[0].ToString() } in ratio parsing");

            return StyleRatio.Auto();

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
                            valid = m_Resolver.ResolveVarFunction(ref i);
                        }
                        else
                        {
                            m_Resolver.AddValue(handle);
                        }
                    }

                    if (valid && m_Resolver.ValidateResolvedValues())
                    {
                        m_Values.AddRange(m_Resolver.resolvedValues);
                        count += m_Resolver.resolvedValues.Count;
                    }
                    else
                    {
                        // Resolve failed
                        // When this happens, the computed value of the property is either the property’s
                        // inherited value or its initial value depending on whether the property is inherited or not.
                        // This is the same behavior as the unset keyword so we simply resolve to that value.
                        var unsetHandle = new StyleValueHandle() { valueType = StyleValueType.Keyword, valueIndex = (int)StyleValueKeyword.Unset};
                        m_Values.Add(new StylePropertyValue() { sheet = m_Sheet, handle = unsetHandle });
                        ++count;
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
            if (m_CurrentPropertyIndex < m_Properties.Length)
            {
                property = m_Properties[m_CurrentPropertyIndex];
                propertyId = property.id;
                valueCount = m_ValueCount[m_CurrentPropertyIndex];
            }
            else
            {
                property = null;
                propertyId = StylePropertyId.Unknown;
                valueCount = 0;
            }
        }
    }
}
