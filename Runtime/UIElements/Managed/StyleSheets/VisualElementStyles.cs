// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal struct CustomProperty
    {
        public int specificity;
        public StyleValueHandle handle;
        public StyleSheet data;
    }

    public interface ICustomStyles
    {
        void ApplyCustomProperty(string propertyName, ref Style<float> target);
        void ApplyCustomProperty(string propertyName, ref Style<int> target);
        void ApplyCustomProperty(string propertyName, ref Style<bool> target);
        void ApplyCustomProperty(string propertyName, ref Style<Color> target);
        void ApplyCustomProperty<T>(string propertyName, ref Style<T> target) where T : Object;
        void ApplyCustomProperty(string propertyName, ref Style<string> target);
    }

    internal class VisualElementStyles : ICustomStyles
    {
        public static VisualElementStyles none = new VisualElementStyles(isShared: true);

        internal readonly bool isShared;

        Dictionary<string, CustomProperty> m_CustomProperties;

        [StyleProperty("width", StylePropertyID.Width)]
        public Style<float> width;

        [StyleProperty("height", StylePropertyID.Height)]
        public Style<float> height;

        [StyleProperty("max-width", StylePropertyID.MaxWidth)]
        public Style<float> maxWidth;

        [StyleProperty("max-height", StylePropertyID.MaxHeight)]
        public Style<float> maxHeight;

        [StyleProperty("min-width", StylePropertyID.MinWidth)]
        public Style<float> minWidth;

        [StyleProperty("min-height", StylePropertyID.MinHeight)]
        public Style<float> minHeight;

        [StyleProperty("flex", StylePropertyID.Flex)]
        public Style<float> flex;

        [StyleProperty("overflow", StylePropertyID.Overflow)]
        public Style<int> overflow;

        [StyleProperty("position-left", StylePropertyID.PositionLeft)]
        public Style<float> positionLeft;

        [StyleProperty("position-top", StylePropertyID.PositionTop)]
        public Style<float> positionTop;

        [StyleProperty("position-right", StylePropertyID.PositionRight)]
        public Style<float> positionRight;

        [StyleProperty("position-bottom", StylePropertyID.PositionBottom)]
        public Style<float> positionBottom;

        [StyleProperty("margin-left", StylePropertyID.MarginLeft)]
        public Style<float> marginLeft;

        [StyleProperty("margin-top", StylePropertyID.MarginTop)]
        public Style<float> marginTop;

        [StyleProperty("margin-right", StylePropertyID.MarginRight)]
        public Style<float> marginRight;

        [StyleProperty("margin-bottom", StylePropertyID.MarginBottom)]
        public Style<float> marginBottom;

        [StyleProperty("border-left", StylePropertyID.BorderLeft)]
        public Style<float> borderLeft;

        [StyleProperty("border-top", StylePropertyID.BorderTop)]
        public Style<float> borderTop;

        [StyleProperty("border-right", StylePropertyID.BorderRight)]
        public Style<float> borderRight;

        [StyleProperty("border-bottom", StylePropertyID.BorderBottom)]
        public Style<float> borderBottom;

        [StyleProperty("padding-left", StylePropertyID.PaddingLeft)]
        public Style<float> paddingLeft;

        [StyleProperty("padding-top", StylePropertyID.PaddingTop)]
        public Style<float> paddingTop;

        [StyleProperty("padding-right", StylePropertyID.PaddingRight)]
        public Style<float> paddingRight;

        [StyleProperty("padding-bottom", StylePropertyID.PaddingBottom)]
        public Style<float> paddingBottom;

        [StyleProperty("position-type", StylePropertyID.PositionType)]
        public Style<int> positionType;

        [StyleProperty("align-self", StylePropertyID.AlignSelf)]
        public Style<int> alignSelf;

        [StyleProperty("text-alignment", StylePropertyID.TextAlignment)]
        public Style<int> textAlignment;

        [StyleProperty("font-style", StylePropertyID.FontStyle)]
        public Style<int> fontStyle;

        [StyleProperty("text-clipping", StylePropertyID.TextClipping)]
        public Style<int> textClipping;

        [StyleProperty("font", StylePropertyID.Font)]
        public Style<Font> font;

        [StyleProperty("font-size", StylePropertyID.FontSize)]
        public Style<int> fontSize;

        [StyleProperty("word-wrap", StylePropertyID.WordWrap)]
        public Style<bool> wordWrap;

        [StyleProperty("text-color", StylePropertyID.TextColor)]
        public Style<Color> textColor;

        [StyleProperty("flex-direction", StylePropertyID.FlexDirection)]
        public Style<int> flexDirection;

        [StyleProperty("background-color", StylePropertyID.BackgroundColor)]
        public Style<Color> backgroundColor;

        [StyleProperty("border-color", StylePropertyID.BorderColor)]
        public Style<Color> borderColor;

        [StyleProperty("background-image", StylePropertyID.BackgroundImage)]
        public Style<Texture2D> backgroundImage;

        [StyleProperty("background-size", StylePropertyID.BackgroundSize)]
        public Style<int> backgroundSize;

        [StyleProperty("align-items", StylePropertyID.AlignItems)]
        public Style<int> alignItems;

        [StyleProperty("align-content", StylePropertyID.AlignContent)]
        public Style<int> alignContent;

        [StyleProperty("justify-content", StylePropertyID.JustifyContent)]
        public Style<int> justifyContent;

        [StyleProperty("flex-wrap", StylePropertyID.FlexWrap)]
        public Style<int> flexWrap;

        [StyleProperty("border-width", StylePropertyID.BorderWidth)]
        public Style<float> borderWidth;

        [StyleProperty("border-radius", StylePropertyID.BorderRadius)]
        public Style<float> borderRadius;

        [StyleProperty("slice-left", StylePropertyID.SliceLeft)]
        public Style<int> sliceLeft;

        [StyleProperty("slice-top", StylePropertyID.SliceTop)]
        public Style<int> sliceTop;

        [StyleProperty("slice-right", StylePropertyID.SliceRight)]
        public Style<int> sliceRight;

        [StyleProperty("slice-bottom", StylePropertyID.SliceBottom)]
        public Style<int> sliceBottom;

        [StyleProperty("opacity", StylePropertyID.Opacity)]
        public Style<float> opacity;

        internal VisualElementStyles(bool isShared)
        {
            this.isShared = isShared;
        }

        public VisualElementStyles(VisualElementStyles other, bool isShared) : this(isShared)
        {
            Apply(other, StylePropertyApplyMode.Copy);
        }

        internal void Apply(VisualElementStyles other, StylePropertyApplyMode mode)
        {
            // Always just copy the reference to custom properties, since they can't be overriden per instance
            m_CustomProperties = other.m_CustomProperties;

            width.Apply(other.width, mode);
            height.Apply(other.height, mode);
            maxWidth.Apply(other.maxWidth, mode);
            maxHeight.Apply(other.maxHeight, mode);
            minWidth.Apply(other.minWidth, mode);
            minHeight.Apply(other.minHeight, mode);
            flex.Apply(other.flex, mode);
            overflow.Apply(other.overflow, mode);
            positionLeft.Apply(other.positionLeft, mode);
            positionTop.Apply(other.positionTop, mode);
            positionRight.Apply(other.positionRight, mode);
            positionBottom.Apply(other.positionBottom, mode);
            marginLeft.Apply(other.marginLeft, mode);
            marginTop.Apply(other.marginTop, mode);
            marginRight.Apply(other.marginRight, mode);
            marginBottom.Apply(other.marginBottom, mode);
            borderLeft.Apply(other.borderLeft, mode);
            borderTop.Apply(other.borderTop, mode);
            borderRight.Apply(other.borderRight, mode);
            borderBottom.Apply(other.borderBottom, mode);
            paddingLeft.Apply(other.paddingLeft, mode);
            paddingTop.Apply(other.paddingTop, mode);
            paddingRight.Apply(other.paddingRight, mode);
            paddingBottom.Apply(other.paddingBottom, mode);
            positionType.Apply(other.positionType, mode);
            alignSelf.Apply(other.alignSelf, mode);
            textAlignment.Apply(other.textAlignment, mode);
            fontStyle.Apply(other.fontStyle, mode);
            textClipping.Apply(other.textClipping, mode);
            fontSize.Apply(other.fontSize, mode);
            font.Apply(other.font, mode);
            wordWrap.Apply(other.wordWrap, mode);
            textColor.Apply(other.textColor, mode);
            flexDirection.Apply(other.flexDirection, mode);
            backgroundColor.Apply(other.backgroundColor, mode);
            borderColor.Apply(other.borderColor, mode);
            backgroundImage.Apply(other.backgroundImage, mode);
            backgroundSize.Apply(other.backgroundSize, mode);
            alignItems.Apply(other.alignItems, mode);
            alignContent.Apply(other.alignContent, mode);
            justifyContent.Apply(other.justifyContent, mode);
            flexWrap.Apply(other.flexWrap, mode);
            borderWidth.Apply(other.borderWidth, mode);
            borderRadius.Apply(other.borderRadius, mode);
            sliceLeft.Apply(other.sliceLeft, mode);
            sliceTop.Apply(other.sliceTop, mode);
            sliceRight.Apply(other.sliceRight, mode);
            sliceBottom.Apply(other.sliceBottom, mode);
            opacity.Apply(other.opacity, mode);
        }

        public void WriteToGUIStyle(GUIStyle style)
        {
            style.alignment = (TextAnchor)(textAlignment.GetSpecifiedValueOrDefault((int)style.alignment));
            style.wordWrap = wordWrap.GetSpecifiedValueOrDefault(style.wordWrap);
            style.clipping = (TextClipping)(textClipping.GetSpecifiedValueOrDefault((int)style.clipping));
            if (font.value != null)
            {
                style.font = font.value;
            }
            style.fontSize = fontSize.GetSpecifiedValueOrDefault(style.fontSize);
            style.fontStyle = (FontStyle)(fontStyle.GetSpecifiedValueOrDefault((int)style.fontStyle));

            AssignRect(style.margin, ref marginLeft, ref marginTop, ref marginRight, ref marginBottom);
            AssignRect(style.padding, ref paddingLeft, ref paddingTop, ref paddingRight, ref paddingBottom);
            AssignRect(style.border, ref sliceLeft, ref sliceTop, ref sliceRight, ref sliceBottom);
            AssignState(style.normal);
            AssignState(style.focused);
            AssignState(style.hover);
            AssignState(style.active);
            AssignState(style.onNormal);
            AssignState(style.onFocused);
            AssignState(style.onHover);
            AssignState(style.onActive);
        }

        void AssignState(GUIStyleState state)
        {
            state.textColor = textColor.GetSpecifiedValueOrDefault(state.textColor);
            if (backgroundImage.value != null)
            {
                state.background = backgroundImage.value;
                if (state.scaledBackgrounds == null || state.scaledBackgrounds.Length < 1 || state.scaledBackgrounds[0] != backgroundImage.value)
                    state.scaledBackgrounds = new Texture2D[1] { backgroundImage.value };
            }
        }

        void AssignRect(RectOffset rect, ref Style<int> left, ref Style<int> top, ref Style<int> right, ref Style<int> bottom)
        {
            rect.left = left.GetSpecifiedValueOrDefault(rect.left);
            rect.top = top.GetSpecifiedValueOrDefault(rect.top);
            rect.right = right.GetSpecifiedValueOrDefault(rect.right);
            rect.bottom = bottom.GetSpecifiedValueOrDefault(rect.bottom);
        }

        void AssignRect(RectOffset rect, ref Style<float> left, ref Style<float> top, ref Style<float> right, ref Style<float> bottom)
        {
            rect.left = (int)left.GetSpecifiedValueOrDefault(rect.left);
            rect.top = (int)top.GetSpecifiedValueOrDefault(rect.top);
            rect.right = (int)right.GetSpecifiedValueOrDefault(rect.right);
            rect.bottom = (int)bottom.GetSpecifiedValueOrDefault(rect.bottom);
        }

        internal void ApplyRule(StyleSheet registry, int specificity, StyleRule rule, StylePropertyID[] propertyIDs, LoadResourceFunction loadResourceFunc)
        {
            for (int i = 0; i < rule.properties.Length; i++)
            {
                UnityEngine.StyleSheets.StyleProperty styleProperty = rule.properties[i];
                StylePropertyID propertyID = propertyIDs[i];
                // no support for multiple values
                StyleValueHandle handle = styleProperty.values[0];

                switch (propertyID)
                {
                    case StylePropertyID.AlignContent:
                        registry.Apply<Align>(handle, specificity, ref alignContent);
                        break;

                    case StylePropertyID.AlignItems:
                        registry.Apply<Align>(handle, specificity, ref alignItems);
                        break;

                    case StylePropertyID.AlignSelf:
                        registry.Apply<Align>(handle, specificity, ref alignSelf);
                        break;

                    case StylePropertyID.BackgroundImage:
                        registry.Apply(handle, specificity, loadResourceFunc, ref backgroundImage);
                        break;

                    case StylePropertyID.BorderLeft:
                        registry.Apply(handle, specificity, ref borderLeft);
                        break;

                    case StylePropertyID.BorderTop:
                        registry.Apply(handle, specificity, ref borderTop);
                        break;

                    case StylePropertyID.BorderRight:
                        registry.Apply(handle, specificity, ref borderRight);
                        break;

                    case StylePropertyID.BorderBottom:
                        registry.Apply(handle, specificity, ref borderBottom);
                        break;

                    case StylePropertyID.Flex:
                        registry.Apply(handle, specificity, ref flex);
                        break;

                    case StylePropertyID.Font:
                        registry.Apply(handle, specificity, loadResourceFunc, ref font);
                        break;

                    case StylePropertyID.FontSize:
                        registry.Apply(handle, specificity, ref fontSize);
                        break;

                    case StylePropertyID.FontStyle:
                        registry.Apply<FontStyle>(handle, specificity, ref fontStyle);
                        break;

                    case StylePropertyID.FlexDirection:
                        registry.Apply<FlexDirection>(handle, specificity, ref flexDirection);
                        break;

                    case StylePropertyID.FlexWrap:
                        registry.Apply<Wrap>(handle, specificity, ref flexWrap);
                        break;

                    case StylePropertyID.Height:
                        registry.Apply(handle, specificity, ref height);
                        break;

                    case StylePropertyID.JustifyContent:
                        registry.Apply<Justify>(handle, specificity, ref justifyContent);
                        break;

                    case StylePropertyID.MarginLeft:
                        registry.Apply(handle, specificity, ref marginLeft);
                        break;

                    case StylePropertyID.MarginTop:
                        registry.Apply(handle, specificity, ref marginTop);
                        break;

                    case StylePropertyID.MarginRight:
                        registry.Apply(handle, specificity, ref marginRight);
                        break;

                    case StylePropertyID.MarginBottom:
                        registry.Apply(handle, specificity, ref marginBottom);
                        break;

                    case StylePropertyID.MaxHeight:
                        registry.Apply(handle, specificity, ref maxHeight);
                        break;

                    case StylePropertyID.MaxWidth:
                        registry.Apply(handle, specificity, ref maxWidth);
                        break;

                    case StylePropertyID.MinHeight:
                        registry.Apply(handle, specificity, ref minHeight);
                        break;

                    case StylePropertyID.MinWidth:
                        registry.Apply(handle, specificity, ref minWidth);
                        break;

                    case StylePropertyID.Overflow:
                        registry.Apply<Overflow>(handle, specificity, ref overflow);
                        break;

                    case StylePropertyID.PaddingLeft:
                        registry.Apply(handle, specificity, ref paddingLeft);
                        break;

                    case StylePropertyID.PaddingTop:
                        registry.Apply(handle, specificity, ref paddingTop);
                        break;

                    case StylePropertyID.PaddingRight:
                        registry.Apply(handle, specificity, ref paddingRight);
                        break;

                    case StylePropertyID.PaddingBottom:
                        registry.Apply(handle, specificity, ref paddingBottom);
                        break;

                    case StylePropertyID.PositionType:
                        registry.Apply<PositionType>(handle, specificity, ref positionType);
                        break;

                    case StylePropertyID.PositionTop:
                        registry.Apply(handle, specificity, ref positionTop);
                        break;

                    case StylePropertyID.PositionBottom:
                        registry.Apply(handle, specificity, ref positionBottom);
                        break;

                    case StylePropertyID.PositionLeft:
                        registry.Apply(handle, specificity, ref positionLeft);
                        break;

                    case StylePropertyID.PositionRight:
                        registry.Apply(handle, specificity, ref positionRight);
                        break;

                    case StylePropertyID.TextAlignment:
                        registry.Apply<TextAnchor>(handle, specificity, ref textAlignment);
                        break;

                    case StylePropertyID.TextClipping:
                        registry.Apply<TextClipping>(handle, specificity, ref textClipping);
                        break;

                    case StylePropertyID.TextColor:
                        registry.Apply(handle, specificity, ref textColor);
                        break;

                    case StylePropertyID.Width:
                        registry.Apply(handle, specificity, ref width);
                        break;

                    case StylePropertyID.WordWrap:
                        registry.Apply(handle, specificity, ref wordWrap);
                        break;

                    case StylePropertyID.BackgroundColor:
                        registry.Apply(handle, specificity, ref backgroundColor);
                        break;

                    case StylePropertyID.BackgroundSize:
                        registry.Apply(handle, specificity, ref backgroundSize);
                        break;

                    case StylePropertyID.BorderColor:
                        registry.Apply(handle, specificity, ref borderColor);
                        break;

                    case StylePropertyID.BorderWidth:
                        registry.Apply(handle, specificity, ref borderWidth);
                        break;

                    case StylePropertyID.BorderRadius:
                        registry.Apply(handle, specificity, ref borderRadius);
                        break;

                    case StylePropertyID.SliceLeft:
                        registry.Apply(handle, specificity, ref sliceLeft);
                        break;

                    case StylePropertyID.SliceTop:
                        registry.Apply(handle, specificity, ref sliceTop);
                        break;

                    case StylePropertyID.SliceRight:
                        registry.Apply(handle, specificity, ref sliceRight);
                        break;

                    case StylePropertyID.SliceBottom:
                        registry.Apply(handle, specificity, ref sliceBottom);
                        break;

                    case StylePropertyID.Opacity:
                        registry.Apply(handle, specificity, ref opacity);
                        break;

                    case StylePropertyID.Custom:
                        if (m_CustomProperties == null)
                        {
                            m_CustomProperties = new Dictionary<string, CustomProperty>();
                        }
                        CustomProperty customProp = default(CustomProperty);
                        if (!m_CustomProperties.TryGetValue(styleProperty.name, out customProp) || specificity >= customProp.specificity)
                        {
                            customProp.handle = handle;
                            customProp.data = registry;
                            customProp.specificity = specificity;
                            m_CustomProperties[styleProperty.name] = customProp;
                        }
                        break;
                    default:
                        throw new ArgumentException(string.Format("Non exhaustive switch statement (value={0})", propertyID));
                }
            }
        }

        public void ApplyCustomProperty(string propertyName, ref Style<float> target)
        {
            CustomProperty property;
            Style<float> tmp = new Style<float>(0.0f);

            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                if (property.handle.valueType == StyleValueType.Float)
                {
                    property.data.Apply(property.handle, property.specificity, ref tmp);
                }
                else
                {
                    Debug.LogWarning(string.Format("Trying to read value as float while parsed type is {0}", property.handle.valueType));
                }
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }

        public void ApplyCustomProperty(string propertyName, ref Style<int> target)
        {
            CustomProperty property;
            Style<int> tmp = new Style<int>(0);

            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                if (property.handle.valueType == StyleValueType.Float)
                {
                    property.data.Apply(property.handle, property.specificity, ref tmp);
                }
                else
                {
                    Debug.LogWarning(string.Format("Trying to read value as int while parsed type is {0}", property.handle.valueType));
                }
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }

        public void ApplyCustomProperty(string propertyName, ref Style<bool> target)
        {
            CustomProperty property;
            Style<bool> tmp = new Style<bool>(false);
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                if (property.handle.valueType == StyleValueType.Keyword)
                {
                    property.data.Apply(property.handle, property.specificity, ref tmp);
                }
                else
                {
                    Debug.LogWarning(string.Format("Trying to read value as bool while parsed type is {0}", property.handle.valueType));
                }
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }

        public void ApplyCustomProperty(string propertyName, ref Style<Color> target)
        {
            CustomProperty property;
            Style<Color> tmp = new Style<Color>(Color.clear);
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                if (property.handle.valueType == StyleValueType.Color)
                {
                    property.data.Apply(property.handle, property.specificity, ref tmp);
                }
                else
                {
                    Debug.LogWarning(string.Format("Trying to read value as Color while parsed type is {0}", property.handle.valueType));
                }
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }

        public void ApplyCustomProperty<T>(string propertyName, ref Style<T> target) where T : Object
        {
            ApplyCustomProperty<T>(propertyName, Resources.Load, ref target);
        }

        public void ApplyCustomProperty<T>(string propertyName, LoadResourceFunction function, ref Style<T> target) where T : Object
        {
            CustomProperty property;
            Style<T> tmp = new Style<T>(null);
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                if (property.handle.valueType == StyleValueType.ResourcePath)
                {
                    property.data.Apply(property.handle, property.specificity, function, ref tmp);
                }
                else
                {
                    Debug.LogWarning(string.Format("Trying to read value as Object while parsed type is {0}", property.handle.valueType));
                }
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }

        public void ApplyCustomProperty(string propertyName, ref Style<string> target)
        {
            CustomProperty property;
            Style<string> tmp = new Style<string>(string.Empty);
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                tmp.value = property.data.ReadAsString(property.handle);
                tmp.specificity = property.specificity;
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }
    }

    static class StyleSheetExtensions
    {
        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref Style<float> property)
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(float), specificity, ref property);
            }
            else
            {
                Apply(sheet.ReadFloat(handle), specificity, ref property);
            }
        }

        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref Style<Color> property)
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(Color), specificity, ref property);
            }
            else
            {
                Apply(sheet.ReadColor(handle), specificity, ref property);
            }
        }

        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref Style<int> property)
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(int), specificity, ref property);
            }
            else
            {
                Apply((int)sheet.ReadFloat(handle), specificity, ref property);
            }
        }

        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref Style<bool> property)
        {
            bool val = sheet.ReadKeyword(handle) == StyleValueKeyword.True;

            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(bool), specificity, ref property);
            }
            else
            {
                Apply(val, specificity, ref property);
            }
        }

        public static void Apply<T>(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref Style<int> property) where T : struct
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(int), specificity, ref property);
            }
            else
            {
                Apply(StyleSheetCache.GetEnumValue<T>(sheet, handle), specificity, ref property);
            }
        }

        public static void Apply<T>(this StyleSheet sheet, StyleValueHandle handle, int specificity, LoadResourceFunction loadResourceFunc, ref Style<T> property) where T : Object
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.None)
            {
                Apply((T)null, specificity, ref property);
                return;
            }

            string path = sheet.ReadResourcePath(handle);
            if (!string.IsNullOrEmpty(path))
            {
                T resource = loadResourceFunc(path, typeof(T)) as T;

                if (resource != null)
                {
                    Apply(resource, specificity, ref property);
                }
                else
                {
                    Debug.LogWarning(string.Format("{0} resource not found for path: {1}", typeof(T).Name, path));
                }
            }
        }

        static void Apply<T>(T val, int specificity, ref Style<T> property)
        {
            property.Apply(new Style<T>(val, specificity), StylePropertyApplyMode.CopyIfMoreSpecific);
        }

        public static string ReadAsString(this StyleSheet sheet, StyleValueHandle handle)
        {
            string value = string.Empty;
            switch (handle.valueType)
            {
                case StyleValueType.Float:
                    value = sheet.ReadFloat(handle).ToString();
                    break;
                case StyleValueType.Color:
                    value = sheet.ReadColor(handle).ToString();
                    break;
                case StyleValueType.ResourcePath:
                    value = sheet.ReadResourcePath(handle);
                    break;
                case StyleValueType.String:
                    value = sheet.ReadString(handle);
                    break;
                case StyleValueType.Enum:
                    value = sheet.ReadEnum(handle);
                    break;
                case StyleValueType.Keyword:
                    value = sheet.ReadKeyword(handle).ToString();
                    break;
                default:
                    throw new ArgumentException("Unhandled type " + handle.valueType);
            }
            return value;
        }
    }
}
