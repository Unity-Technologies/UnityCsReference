// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Yoga;

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
        void ApplyCustomProperty(string propertyName, ref StyleValue<Texture2D> target);
        void ApplyCustomProperty(string propertyName, ref StyleValue<string> target);
    }

    internal class VisualElementStylesData : ICustomStyle
    {
        public static VisualElementStylesData none = new VisualElementStylesData(true);

        internal readonly bool isShared;
        internal YogaNode yogaNode;

        internal Dictionary<string, CustomProperty> m_CustomProperties;

        internal StyleValue<float> width;
        internal StyleValue<float> height;
        internal StyleValue<float> maxWidth;
        internal StyleValue<float> maxHeight;
        internal StyleValue<float> minWidth;
        internal StyleValue<float> minHeight;
        internal StyleValue<FloatOrKeyword> flexBasis;
        internal StyleValue<float> flexShrink;
        internal StyleValue<float> flexGrow;
        internal StyleValue<int> overflow;
        internal StyleValue<float> positionLeft;
        internal StyleValue<float> positionTop;
        internal StyleValue<float> positionRight;
        internal StyleValue<float> positionBottom;
        internal StyleValue<float> marginLeft;
        internal StyleValue<float> marginTop;
        internal StyleValue<float> marginRight;
        internal StyleValue<float> marginBottom;
        internal StyleValue<float> paddingLeft;
        internal StyleValue<float> paddingTop;
        internal StyleValue<float> paddingRight;
        internal StyleValue<float> paddingBottom;
        internal StyleValue<int> positionType;
        internal StyleValue<int> alignSelf;
        internal StyleValue<int> unityTextAlign;
        internal StyleValue<int> fontStyleAndWeight;
        internal StyleValue<int> textClipping;
        internal StyleValue<Font> font;
        internal StyleValue<int> fontSize;
        internal StyleValue<bool> wordWrap;
        internal StyleValue<Color> color;
        internal StyleValue<int> flexDirection;
        internal StyleValue<Color> backgroundColor;
        internal StyleValue<Color> borderColor;
        internal StyleValue<Texture2D> backgroundImage;
        internal StyleValue<int> backgroundScaleMode;
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
        internal StyleValue<CursorStyle> cursor;
        internal StyleValue<int> visibility;

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
            flexBasis.Apply(other.flexBasis, mode);
            flexGrow.Apply(other.flexGrow, mode);
            flexShrink.Apply(other.flexShrink, mode);
            overflow.Apply(other.overflow, mode);
            positionLeft.Apply(other.positionLeft, mode);
            positionTop.Apply(other.positionTop, mode);
            positionRight.Apply(other.positionRight, mode);
            positionBottom.Apply(other.positionBottom, mode);
            marginLeft.Apply(other.marginLeft, mode);
            marginTop.Apply(other.marginTop, mode);
            marginRight.Apply(other.marginRight, mode);
            marginBottom.Apply(other.marginBottom, mode);
            paddingLeft.Apply(other.paddingLeft, mode);
            paddingTop.Apply(other.paddingTop, mode);
            paddingRight.Apply(other.paddingRight, mode);
            paddingBottom.Apply(other.paddingBottom, mode);
            positionType.Apply(other.positionType, mode);
            alignSelf.Apply(other.alignSelf, mode);
            unityTextAlign.Apply(other.unityTextAlign, mode);
            fontStyleAndWeight.Apply(other.fontStyleAndWeight, mode);
            textClipping.Apply(other.textClipping, mode);
            fontSize.Apply(other.fontSize, mode);
            font.Apply(other.font, mode);
            wordWrap.Apply(other.wordWrap, mode);
            color.Apply(other.color, mode);
            flexDirection.Apply(other.flexDirection, mode);
            backgroundColor.Apply(other.backgroundColor, mode);
            borderColor.Apply(other.borderColor, mode);
            backgroundImage.Apply(other.backgroundImage, mode);
            backgroundScaleMode.Apply(other.backgroundScaleMode, mode);
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
            cursor.Apply(other.cursor, mode);
            visibility.Apply(other.visibility, mode);
        }

        public void WriteToGUIStyle(GUIStyle style)
        {
            style.alignment = (TextAnchor)(unityTextAlign.GetSpecifiedValueOrDefault((int)style.alignment));
            style.wordWrap = wordWrap.GetSpecifiedValueOrDefault(style.wordWrap);
            style.clipping = (TextClipping)(textClipping.GetSpecifiedValueOrDefault((int)style.clipping));
            if (font.value != null)
            {
                style.font = font.value;
            }
            style.fontSize = fontSize.GetSpecifiedValueOrDefault(style.fontSize);
            style.fontStyle = (FontStyle)(fontStyleAndWeight.GetSpecifiedValueOrDefault((int)style.fontStyle));

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
            state.textColor = color.GetSpecifiedValueOrDefault(state.textColor);
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

        public void ApplyLayoutValues()
        {
            if (yogaNode == null)
                yogaNode = new YogaNode();

            SyncWithLayout(yogaNode);
        }

        public StyleValue<float> FlexBasisToFloat()
        {
            if (flexBasis.value.isKeyword)
            {
                if (flexBasis.value.keyword == StyleValueKeyword.Auto)
                {
                    // Negative values are illegal. Return -1 to indicate auto.
                    return new StyleValue<float>(-1f, flexBasis.specificity);
                }
                else
                {
                    return new StyleValue<float>(0f, flexBasis.specificity);
                }
            }
            else
            {
                return new StyleValue<float>(flexBasis.value.floatValue, flexBasis.specificity);
            }
        }

        internal const Align DefaultAlignContent = Align.FlexStart;
        internal const Align DefaultAlignItems = Align.Stretch;

        public void SyncWithLayout(YogaNode targetNode)
        {
            targetNode.Flex = float.NaN;

            float fb = FlexBasisToFloat().GetSpecifiedValueOrDefault(float.NaN);
            if (fb == -1f)
            {
                targetNode.FlexBasis = YogaValue.Auto();
            }
            else
            {
                targetNode.FlexBasis = fb;
            }

            targetNode.FlexGrow = flexGrow.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.FlexShrink = flexShrink.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Left = positionLeft.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Top = positionTop.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Right = positionRight.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Bottom = positionBottom.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MarginLeft = marginLeft.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MarginTop = marginTop.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MarginRight = marginRight.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MarginBottom = marginBottom.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.PaddingLeft = paddingLeft.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.PaddingTop = paddingTop.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.PaddingRight = paddingRight.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.PaddingBottom = paddingBottom.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.BorderLeftWidth = borderLeftWidth.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.BorderTopWidth = borderTopWidth.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.BorderRightWidth = borderRightWidth.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.BorderBottomWidth = borderBottomWidth.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Width = width.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Height = height.GetSpecifiedValueOrDefault(float.NaN);

            PositionType posType = (PositionType)positionType.value;
            switch (posType)
            {
                case PositionType.Absolute:
                case PositionType.Manual:
                    targetNode.PositionType = YogaPositionType.Absolute;
                    break;
                case PositionType.Relative:
                    targetNode.PositionType = YogaPositionType.Relative;
                    break;
            }

            targetNode.Overflow = (YogaOverflow)(overflow.value);
            targetNode.AlignSelf = (YogaAlign)(alignSelf.value);
            targetNode.MaxWidth = maxWidth.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MaxHeight = maxHeight.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MinWidth = minWidth.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.MinHeight = minHeight.GetSpecifiedValueOrDefault(float.NaN);

            // Note: the following applies to VisualContainer only
            // but it won't cause any trouble and we avoid making this method virtual
            targetNode.FlexDirection = (YogaFlexDirection)flexDirection.value;
            targetNode.AlignContent = (YogaAlign)alignContent.GetSpecifiedValueOrDefault((int)DefaultAlignContent);
            targetNode.AlignItems = (YogaAlign)alignItems.GetSpecifiedValueOrDefault((int)DefaultAlignItems);
            targetNode.JustifyContent = (YogaJustify)justifyContent.value;
            targetNode.Wrap = (YogaWrap)flexWrap.value;
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
                        registry.Apply(handles, specificity, ref backgroundImage, StyleSheetApplicator.ApplyImage);
                        break;

                    case StylePropertyID.Flex:
                        registry.ApplyShorthand(handles, specificity, this, StyleSheetApplicator.ApplyFlexShorthand);
                        break;

                    case StylePropertyID.FlexBasis:
                        registry.Apply(handles, specificity, ref flexBasis, StyleSheetApplicator.ApplyFloatOrKeyword);
                        break;

                    case StylePropertyID.FlexGrow:
                        registry.Apply(handles, specificity, ref flexGrow, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.FlexShrink:
                        registry.Apply(handles, specificity, ref flexShrink, StyleSheetApplicator.ApplyFloat);
                        break;

                    case StylePropertyID.Font:
                        registry.Apply(handles, specificity, ref font, StyleSheetApplicator.ApplyFont);
                        break;

                    case StylePropertyID.FontSize:
                        registry.Apply(handles, specificity, ref fontSize, StyleSheetApplicator.ApplyInt);
                        break;

                    case StylePropertyID.FontStyleAndWeight:
                        registry.Apply(handles, specificity, ref fontStyleAndWeight, StyleSheetApplicator.ApplyEnum<FontStyle>);
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

                    case StylePropertyID.Position:
                        registry.Apply(handles, specificity, ref positionType, StyleSheetApplicator.ApplyEnum<Position>);
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

                    case StylePropertyID.UnityTextAlign:
                        registry.Apply(handles, specificity, ref unityTextAlign, StyleSheetApplicator.ApplyEnum<TextAnchor>);
                        break;

                    case StylePropertyID.TextClipping:
                        registry.Apply(handles, specificity, ref textClipping, StyleSheetApplicator.ApplyEnum<TextClipping>);
                        break;

                    case StylePropertyID.Color:
                        registry.Apply(handles, specificity, ref color, StyleSheetApplicator.ApplyColor);
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

                    case StylePropertyID.BackgroundScaleMode:
                        registry.Apply(handles, specificity, ref backgroundScaleMode, StyleSheetApplicator.ApplyInt);
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

                    case StylePropertyID.Cursor:
                        registry.Apply(handles, specificity, ref cursor, StyleSheetApplicator.ApplyCursor);
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

                    case StylePropertyID.Visibility:
                        registry.Apply(handles, specificity, ref visibility, StyleSheetApplicator.ApplyEnum<Visibility>);
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

        public void ApplyCustomProperty(string propertyName, ref StyleValue<Texture2D> target)
        {
            CustomProperty property;
            StyleValue<Texture2D> tmp = new StyleValue<Texture2D>();
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out property))
            {
                property.data.Apply(property.handles, property.specificity, ref tmp, StyleSheetApplicator.ApplyImage);
            }
            target.Apply(tmp, StylePropertyApplyMode.CopyIfNotInline);
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
