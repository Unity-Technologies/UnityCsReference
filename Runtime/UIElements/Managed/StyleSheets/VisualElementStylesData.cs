// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal struct CustomProperty
    {
        public int specificity;
        public StyleValueHandle handle;
        public StyleSheet data;
    }

    public interface ICustomStyle
    {
        void ApplyCustomProperty(string propertyName, ref StyleValue<float> target);
        void ApplyCustomProperty(string propertyName, ref StyleValue<int> target);
        void ApplyCustomProperty(string propertyName, ref StyleValue<bool> target);
        void ApplyCustomProperty(string propertyName, ref StyleValue<Color> target);
        void ApplyCustomProperty<T>(string propertyName, ref StyleValue<T> target) where T : Object;
        void ApplyCustomProperty(string propertyName, ref StyleValue<string> target);
    }

    internal class VisualElementStylesData : ICustomStyle
    {
        public static VisualElementStylesData none = new VisualElementStylesData(true);

        internal readonly bool isShared;

        Dictionary<string, CustomProperty> m_CustomProperties;

        internal StyleValue<float> width;
        internal StyleValue<float> height;
        internal StyleValue<float> maxWidth;
        internal StyleValue<float> maxHeight;
        internal StyleValue<float> minWidth;
        internal StyleValue<float> minHeight;
        internal StyleValue<float> flex;
        internal StyleValue<int> overflow;
        internal StyleValue<float> positionLeft;
        internal StyleValue<float> positionTop;
        internal StyleValue<float> positionRight;
        internal StyleValue<float> positionBottom;
        internal StyleValue<float> marginLeft;
        internal StyleValue<float> marginTop;
        internal StyleValue<float> marginRight;
        internal StyleValue<float> marginBottom;
        internal StyleValue<float> borderLeft;
        internal StyleValue<float> borderTop;
        internal StyleValue<float> borderRight;
        internal StyleValue<float> borderBottom;
        internal StyleValue<float> paddingLeft;
        internal StyleValue<float> paddingTop;
        internal StyleValue<float> paddingRight;
        internal StyleValue<float> paddingBottom;
        internal StyleValue<int> positionType;
        internal StyleValue<int> alignSelf;
        internal StyleValue<int> textAlignment;
        internal StyleValue<int> fontStyle;
        internal StyleValue<int> textClipping;
        internal StyleValue<Font> font;
        internal StyleValue<int> fontSize;
        internal StyleValue<bool> wordWrap;
        internal StyleValue<Color> textColor;
        internal StyleValue<int> flexDirection;
        internal StyleValue<Color> backgroundColor;
        internal StyleValue<Color> borderColor;
        internal StyleValue<Texture2D> backgroundImage;
        internal StyleValue<int> backgroundSize;
        internal StyleValue<int> alignItems;
        internal StyleValue<int> alignContent;
        internal StyleValue<int> justifyContent;
        internal StyleValue<int> flexWrap;
        internal StyleValue<float> borderLeftWidth;
        internal StyleValue<float> borderTopWidth;
        internal StyleValue<float> borderRightWidth;
        internal StyleValue<float> borderBottomWidth;
        internal StyleValue<float> borderRadius;
        internal StyleValue<int> sliceLeft;
        internal StyleValue<int> sliceTop;
        internal StyleValue<int> sliceRight;
        internal StyleValue<int> sliceBottom;
        internal StyleValue<float> opacity;

        public VisualElementStylesData(bool isShared)
        {
            this.isShared = isShared;
        }

        public void Apply(VisualElementStylesData other, StylePropertyApplyMode mode)
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
            borderLeftWidth.Apply(other.borderLeftWidth, mode);
            borderTopWidth.Apply(other.borderTopWidth, mode);
            borderRightWidth.Apply(other.borderRightWidth, mode);
            borderBottomWidth.Apply(other.borderBottomWidth, mode);
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

        void AssignRect(RectOffset rect, ref StyleValue<int> left, ref StyleValue<int> top, ref StyleValue<int> right, ref StyleValue<int> bottom)
        {
            rect.left = left.GetSpecifiedValueOrDefault(rect.left);
            rect.top = top.GetSpecifiedValueOrDefault(rect.top);
            rect.right = right.GetSpecifiedValueOrDefault(rect.right);
            rect.bottom = bottom.GetSpecifiedValueOrDefault(rect.bottom);
        }

        void AssignRect(RectOffset rect, ref StyleValue<float> left, ref StyleValue<float> top, ref StyleValue<float> right, ref StyleValue<float> bottom)
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

                    case StylePropertyID.BorderLeftWidth:
                        registry.Apply(handle, specificity, ref borderLeftWidth);
                        break;

                    case StylePropertyID.BorderTopWidth:
                        registry.Apply(handle, specificity, ref borderTopWidth);
                        break;

                    case StylePropertyID.BorderRightWidth:
                        registry.Apply(handle, specificity, ref borderRightWidth);
                        break;

                    case StylePropertyID.BorderBottomWidth:
                        registry.Apply(handle, specificity, ref borderBottomWidth);
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

        public void ApplyCustomProperty(string propertyName, ref StyleValue<float> target)
        {
            CustomProperty property;
            StyleValue<float> tmp = new StyleValue<float>(0.0f);

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

        public void ApplyCustomProperty(string propertyName, ref StyleValue<int> target)
        {
            CustomProperty property;
            StyleValue<int> tmp = new StyleValue<int>(0);

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

        public void ApplyCustomProperty(string propertyName, ref StyleValue<bool> target)
        {
            CustomProperty property;
            StyleValue<bool> tmp = new StyleValue<bool>(false);
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

        public void ApplyCustomProperty(string propertyName, ref StyleValue<Color> target)
        {
            CustomProperty property;
            StyleValue<Color> tmp = new StyleValue<Color>(Color.clear);
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

        public void ApplyCustomProperty<T>(string propertyName, ref StyleValue<T> target) where T : Object
        {
            ApplyCustomProperty<T>(propertyName, Resources.Load, ref target);
        }

        internal void ApplyCustomProperty<T>(string propertyName, LoadResourceFunction function, ref StyleValue<T> target) where T : Object
        {
            CustomProperty property;
            StyleValue<T> tmp = new StyleValue<T>(null);
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

        public void ApplyCustomProperty(string propertyName, ref StyleValue<string> target)
        {
            CustomProperty property;
            StyleValue<string> tmp = new StyleValue<string>(string.Empty);
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                tmp.value = property.data.ReadAsString(property.handle);
                tmp.specificity = property.specificity;
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }
    }
}
