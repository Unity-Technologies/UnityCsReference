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
        public StyleValueHandle[] handles;
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
        internal StyleValue<float> borderTopLeftRadius;
        internal StyleValue<float> borderTopRightRadius;
        internal StyleValue<float> borderBottomRightRadius;
        internal StyleValue<float> borderBottomLeftRadius;
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
            borderTopLeftRadius.Apply(other.borderTopLeftRadius, mode);
            borderTopRightRadius.Apply(other.borderTopRightRadius, mode);
            borderBottomRightRadius.Apply(other.borderBottomRightRadius, mode);
            borderBottomLeftRadius.Apply(other.borderBottomLeftRadius, mode);
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

        internal void ApplyRule(StyleSheet registry, int specificity, StyleRule rule, StylePropertyID[] propertyIDs)
        {
            for (int i = 0; i < rule.properties.Length; i++)
            {
                UnityEngine.StyleSheets.StyleProperty styleProperty = rule.properties[i];
                StylePropertyID propertyID = propertyIDs[i];

                var handles = styleProperty.values;
                switch (propertyID)
                {
                    case StylePropertyID.AlignContent:
                        registry.Apply(handles, specificity, ref alignContent, StyleSheetApplicator.ApplyEnum<Align>);
                        break;

                    case StylePropertyID.AlignItems:
                        registry.Apply(handles, specificity, ref alignItems, StyleSheetApplicator.ApplyEnum<Align>);
                        break;

                    case StylePropertyID.AlignSelf:
                        registry.Apply(handles, specificity, ref alignSelf, StyleSheetApplicator.ApplyEnum<Align>);
                        break;

                    case StylePropertyID.BackgroundImage:
                        registry.Apply(handles, specificity, ref backgroundImage, StyleSheetApplicator.ApplyResource);
                        break;

                    case StylePropertyID.BorderLeft:
                        registry.Apply(handles, specificity, ref borderLeft, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderTop:
                        registry.Apply(handles, specificity, ref borderTop, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderRight:
                        registry.Apply(handles, specificity, ref borderRight, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderBottom:
                        registry.Apply(handles, specificity, ref borderBottom, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.Flex:
                        registry.Apply(handles, specificity, ref flex, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.Font:
                        registry.Apply(handles, specificity, ref font, StyleSheetApplicator.ApplyResource);
                        break;

                    case StylePropertyID.FontSize:
                        registry.Apply(handles, specificity, ref fontSize, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.FontStyle:
                        registry.Apply(handles, specificity, ref fontStyle, StyleSheetApplicator.ApplyEnum<FontStyle>);
                        break;

                    case StylePropertyID.FlexDirection:
                        registry.Apply(handles, specificity, ref flexDirection, StyleSheetApplicator.ApplyEnum<FlexDirection>);
                        break;

                    case StylePropertyID.FlexWrap:
                        registry.Apply(handles, specificity, ref flexWrap, StyleSheetApplicator.ApplyEnum<Wrap>);
                        break;

                    case StylePropertyID.Height:
                        registry.Apply(handles, specificity, ref height, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.JustifyContent:
                        registry.Apply(handles, specificity, ref justifyContent, StyleSheetApplicator.ApplyEnum<Justify>);
                        break;

                    case StylePropertyID.MarginLeft:
                        registry.Apply(handles, specificity, ref marginLeft, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MarginTop:
                        registry.Apply(handles, specificity, ref marginTop, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MarginRight:
                        registry.Apply(handles, specificity, ref marginRight, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MarginBottom:
                        registry.Apply(handles, specificity, ref marginBottom, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MaxHeight:
                        registry.Apply(handles, specificity, ref maxHeight, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MaxWidth:
                        registry.Apply(handles, specificity, ref maxWidth, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MinHeight:
                        registry.Apply(handles, specificity, ref minHeight, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.MinWidth:
                        registry.Apply(handles, specificity, ref minWidth, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.Overflow:
                        registry.Apply(handles, specificity, ref overflow, StyleSheetApplicator.ApplyEnum<Overflow>);
                        break;

                    case StylePropertyID.PaddingLeft:
                        registry.Apply(handles, specificity, ref paddingLeft, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PaddingTop:
                        registry.Apply(handles, specificity, ref paddingTop, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PaddingRight:
                        registry.Apply(handles, specificity, ref paddingRight, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PaddingBottom:
                        registry.Apply(handles, specificity, ref paddingBottom, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PositionType:
                        registry.Apply(handles, specificity, ref positionType, StyleSheetApplicator.ApplyEnum<PositionType>);
                        break;

                    case StylePropertyID.PositionTop:
                        registry.Apply(handles, specificity, ref positionTop, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PositionBottom:
                        registry.Apply(handles, specificity, ref positionBottom, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PositionLeft:
                        registry.Apply(handles, specificity, ref positionLeft, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.PositionRight:
                        registry.Apply(handles, specificity, ref positionRight, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.TextAlignment:
                        registry.Apply(handles, specificity, ref textAlignment, StyleSheetApplicator.ApplyEnum<TextAnchor>);
                        break;

                    case StylePropertyID.TextClipping:
                        registry.Apply(handles, specificity, ref textClipping, StyleSheetApplicator.ApplyEnum<TextClipping>);
                        break;

                    case StylePropertyID.TextColor:
                        registry.Apply(handles, specificity, ref textColor, StyleSheetApplicator.ApplyColor);
                        break;

                    case StylePropertyID.Width:
                        registry.Apply(handles, specificity, ref width, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.WordWrap:
                        registry.Apply(handles, specificity, ref wordWrap, StyleSheetApplicator.ApplyBool);
                        break;

                    case StylePropertyID.BackgroundColor:
                        registry.Apply(handles, specificity, ref backgroundColor, StyleSheetApplicator.ApplyColor);
                        break;

                    case StylePropertyID.BackgroundSize:
                        registry.Apply(handles, specificity, ref backgroundSize, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.BorderColor:
                        registry.Apply(handles, specificity, ref borderColor, StyleSheetApplicator.ApplyColor);
                        break;

                    case StylePropertyID.BorderLeftWidth:
                        registry.Apply(handles, specificity, ref borderLeftWidth, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderTopWidth:
                        registry.Apply(handles, specificity, ref borderTopWidth, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderRightWidth:
                        registry.Apply(handles, specificity, ref borderRightWidth, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderBottomWidth:
                        registry.Apply(handles, specificity, ref borderBottomWidth, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderTopLeftRadius:
                        registry.Apply(handles, specificity, ref borderTopLeftRadius, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderTopRightRadius:
                        registry.Apply(handles, specificity, ref borderTopRightRadius, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderBottomRightRadius:
                        registry.Apply(handles, specificity, ref borderBottomRightRadius, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.BorderBottomLeftRadius:
                        registry.Apply(handles, specificity, ref borderBottomLeftRadius, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.SliceLeft:
                        registry.Apply(handles, specificity, ref sliceLeft, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.SliceTop:
                        registry.Apply(handles, specificity, ref sliceTop, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.SliceRight:
                        registry.Apply(handles, specificity, ref sliceRight, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.SliceBottom:
                        registry.Apply(handles, specificity, ref sliceBottom, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.Opacity:
                        registry.Apply(handles, specificity, ref opacity, StyleSheetApplicator.ApplyFloat);
                        break;

                        // Shorthand values
                    case StylePropertyID.BorderRadius:
                        registry.Apply(handles, specificity, ref borderTopLeftRadius, StyleSheetApplicator.ApplyFloat);
                        registry.Apply(handles, specificity, ref borderTopRightRadius, StyleSheetApplicator.ApplyFloat);
                        registry.Apply(handles, specificity, ref borderBottomLeftRadius, StyleSheetApplicator.ApplyFloat);
                        registry.Apply(handles, specificity, ref borderBottomRightRadius, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.Custom:
                        if (m_CustomProperties == null)
                        {
                            m_CustomProperties = new Dictionary<string, CustomProperty>();
                        }
                        CustomProperty customProp = default(CustomProperty);
                        if (!m_CustomProperties.TryGetValue(styleProperty.name, out customProp) || specificity >= customProp.specificity)
                        {
                            customProp.handles = handles;
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
            ApplyCustomProperty(propertyName, ref target, StyleValueType.Float, StyleSheetApplicator.ApplyFloat);
        }

        public void ApplyCustomProperty(string propertyName, ref StyleValue<int> target)
        {
            ApplyCustomProperty(propertyName, ref target, StyleValueType.Float, StyleSheetApplicator.ApplyInt);
        }

        public void ApplyCustomProperty(string propertyName, ref StyleValue<bool> target)
        {
            ApplyCustomProperty(propertyName, ref target, StyleValueType.Keyword, StyleSheetApplicator.ApplyBool);
        }

        public void ApplyCustomProperty(string propertyName, ref StyleValue<Color> target)
        {
            ApplyCustomProperty(propertyName, ref target, StyleValueType.Color, StyleSheetApplicator.ApplyColor);
        }

        public void ApplyCustomProperty<T>(string propertyName, ref StyleValue<T> target) where T : Object
        {
            ApplyCustomProperty(propertyName, ref target, StyleValueType.ResourcePath, StyleSheetApplicator.ApplyResource);
        }

        public void ApplyCustomProperty(string propertyName, ref StyleValue<string> target)
        {
            CustomProperty property;
            StyleValue<string> tmp = new StyleValue<string>(string.Empty);
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                tmp.value = property.data.ReadAsString(property.handles[0]);
                tmp.specificity = property.specificity;
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }

        internal void ApplyCustomProperty<T>(string propertyName, ref StyleValue<T> target, StyleValueType valueType, HandlesApplicatorFunction<T> applicatorFunc)
        {
            CustomProperty property;
            StyleValue<T> tmp = new StyleValue<T>();
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                // CustomProperty only support one value
                var handle = property.handles[0];
                if (handle.valueType == valueType)
                {
                    property.data.Apply(property.handles, property.specificity, ref tmp, applicatorFunc);
                }
                else
                {
                    Debug.LogWarning(string.Format("Trying to read value as {0} while parsed type is {1}", valueType, handle.valueType));
                }
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
        }
    }
}
