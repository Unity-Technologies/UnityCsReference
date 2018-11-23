// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements.StyleSheets
{
    internal struct CustomPropertyHandle
    {
        public int specificity;
        public StyleValueHandle[] handles;
        public StyleSheet data;
    }

    internal class VisualElementStylesData : ICustomStyle
    {
        private static StyleSheetApplicator s_StyleSheetApplicator = new StyleSheetApplicator();
        public static readonly VisualElementStylesData none = new VisualElementStylesData(true);

        internal readonly bool isShared;
        internal YogaNode yogaNode;

        internal Dictionary<string, CustomPropertyHandle> m_CustomProperties;

        internal StyleLength width;
        internal StyleLength height;
        internal StyleLength maxWidth;
        internal StyleLength maxHeight;
        internal StyleLength minWidth;
        internal StyleLength minHeight;
        internal StyleLength flexBasis;
        internal StyleFloat flexShrink;
        internal StyleFloat flexGrow;
        internal StyleInt overflow;
        internal StyleLength left;
        internal StyleLength top;
        internal StyleLength right;
        internal StyleLength bottom;
        internal StyleLength marginLeft;
        internal StyleLength marginTop;
        internal StyleLength marginRight;
        internal StyleLength marginBottom;
        internal StyleLength paddingLeft;
        internal StyleLength paddingTop;
        internal StyleLength paddingRight;
        internal StyleLength paddingBottom;
        internal StyleInt position;
        internal StyleInt alignSelf;
        internal StyleInt unityTextAlign;
        internal StyleInt fontStyleAndWeight;
        internal StyleFont font;
        internal StyleLength fontSize;
        internal StyleInt whiteSpace;
        internal StyleColor color;
        internal StyleInt flexDirection;
        internal StyleColor backgroundColor;
        internal StyleColor borderColor;
        internal StyleBackground backgroundImage;
        internal StyleInt backgroundScaleMode;
        internal StyleInt alignItems;
        internal StyleInt alignContent;
        internal StyleInt justifyContent;
        internal StyleInt flexWrap;
        internal StyleFloat borderLeftWidth;
        internal StyleFloat borderTopWidth;
        internal StyleFloat borderRightWidth;
        internal StyleFloat borderBottomWidth;
        internal StyleLength borderTopLeftRadius;
        internal StyleLength borderTopRightRadius;
        internal StyleLength borderBottomRightRadius;
        internal StyleLength borderBottomLeftRadius;
        internal StyleInt sliceLeft;
        internal StyleInt sliceTop;
        internal StyleInt sliceRight;
        internal StyleInt sliceBottom;
        internal StyleFloat opacity;
        internal StyleCursor cursor;
        internal StyleInt visibility;
        internal StyleInt display;

        public int customPropertiesCount
        {
            get { return m_CustomProperties != null ? m_CustomProperties.Count : 0; }
        }

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
            left.Apply(other.left, mode);
            top.Apply(other.top, mode);
            right.Apply(other.right, mode);
            bottom.Apply(other.bottom, mode);
            marginLeft.Apply(other.marginLeft, mode);
            marginTop.Apply(other.marginTop, mode);
            marginRight.Apply(other.marginRight, mode);
            marginBottom.Apply(other.marginBottom, mode);
            paddingLeft.Apply(other.paddingLeft, mode);
            paddingTop.Apply(other.paddingTop, mode);
            paddingRight.Apply(other.paddingRight, mode);
            paddingBottom.Apply(other.paddingBottom, mode);
            position.Apply(other.position, mode);
            alignSelf.Apply(other.alignSelf, mode);
            unityTextAlign.Apply(other.unityTextAlign, mode);
            fontStyleAndWeight.Apply(other.fontStyleAndWeight, mode);
            fontSize.Apply(other.fontSize, mode);
            font.Apply(other.font, mode);
            whiteSpace.Apply(other.whiteSpace, mode);
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
            display.Apply(other.display, mode);
        }

        public void ApplyLayoutValues()
        {
            if (yogaNode == null)
                yogaNode = new YogaNode();

            SyncWithLayout(yogaNode);
        }

        internal const float k_DefaultFlexGrow = 0f;
        internal const float k_DefaultFlexShrink = 1f;

        internal const Align k_DefaultAlignContent = Align.FlexStart;
        internal const Align k_DefaultAlignItems = Align.Stretch;

        public void SyncWithLayout(YogaNode targetNode)
        {
            targetNode.Flex = float.NaN;

            var fb = new StyleLength(StyleKeyword.Auto);
            if (flexBasis.specificity > StyleValueExtensions.UndefinedSpecificity)
                fb = flexBasis;

            if (fb == StyleKeyword.Auto)
            {
                targetNode.FlexBasis = YogaValue.Auto();
            }
            else
            {
                targetNode.FlexBasis = fb.GetSpecifiedValueOrDefault(float.NaN);
            }

            targetNode.FlexGrow = flexGrow.GetSpecifiedValueOrDefault(k_DefaultFlexGrow);
            targetNode.FlexShrink = flexShrink.GetSpecifiedValueOrDefault(k_DefaultFlexShrink);
            targetNode.Left = left.ToYogaValue();
            targetNode.Top = top.ToYogaValue();
            targetNode.Right = right.ToYogaValue();
            targetNode.Bottom = bottom.ToYogaValue();
            targetNode.MarginLeft = marginLeft.ToYogaValue();
            targetNode.MarginTop = marginTop.ToYogaValue();
            targetNode.MarginRight = marginRight.ToYogaValue();
            targetNode.MarginBottom = marginBottom.ToYogaValue();
            targetNode.PaddingLeft = paddingLeft.ToYogaValue();
            targetNode.PaddingTop = paddingTop.ToYogaValue();
            targetNode.PaddingRight = paddingRight.ToYogaValue();
            targetNode.PaddingBottom = paddingBottom.ToYogaValue();
            targetNode.BorderLeftWidth = borderLeftWidth.value;
            targetNode.BorderTopWidth = borderTopWidth.value;
            targetNode.BorderRightWidth = borderRightWidth.value;
            targetNode.BorderBottomWidth = borderBottomWidth.value;
            targetNode.Width = width.ToYogaValue();
            targetNode.Height = height.ToYogaValue();

            targetNode.PositionType = (YogaPositionType)position.value;
            targetNode.Overflow = (YogaOverflow)(overflow.value);
            targetNode.AlignSelf = (YogaAlign)(alignSelf.value);
            targetNode.MaxWidth = maxWidth.ToYogaValue();
            targetNode.MaxHeight = maxHeight.ToYogaValue();
            targetNode.MinWidth = minWidth.ToYogaValue();
            targetNode.MinHeight = minHeight.ToYogaValue();

            targetNode.FlexDirection = (YogaFlexDirection)flexDirection.value;
            targetNode.AlignContent = (YogaAlign)alignContent.GetSpecifiedValueOrDefault((int)k_DefaultAlignContent);
            targetNode.AlignItems = (YogaAlign)alignItems.GetSpecifiedValueOrDefault((int)k_DefaultAlignItems);
            targetNode.JustifyContent = (YogaJustify)justifyContent.value;
            targetNode.Wrap = (YogaWrap)flexWrap.value;
            targetNode.Display = (YogaDisplay)display.value;
        }

        internal void ApplyRule(StyleSheet sheet, int specificity, StyleRule rule, StylePropertyID[] propertyIDs)
        {
            for (int i = 0; i < rule.properties.Length; i++)
            {
                var styleProperty = rule.properties[i];
                var propertyID = propertyIDs[i];

                switch (propertyID)
                {
                    case StylePropertyID.Unknown:
                        break;
                    case StylePropertyID.Custom:
                        ApplyCustomStyleProperty(sheet, styleProperty, specificity);
                        break;
                    case StylePropertyID.BorderRadius:
                    case StylePropertyID.BorderWidth:
                    case StylePropertyID.Flex:
                    case StylePropertyID.Margin:
                    case StylePropertyID.Padding:
                        ApplyShorthandProperty(sheet, propertyID, styleProperty.values, specificity);
                        break;
                    default:
                        ApplyStyleProperty(s_StyleSheetApplicator, sheet, propertyID, styleProperty.values, specificity);
                        break;
                }
            }
        }

        internal void ApplyStyleProperty(IStyleSheetApplicator applicator, StyleSheet sheet, StylePropertyID propertyID, StyleValueHandle[] handles, int specificity)
        {
            switch (propertyID)
            {
                case StylePropertyID.AlignContent:
                    applicator.ApplyAlign(sheet, handles, specificity, ref alignContent);
                    break;

                case StylePropertyID.AlignItems:
                    applicator.ApplyAlign(sheet, handles, specificity, ref alignItems);
                    break;

                case StylePropertyID.AlignSelf:
                    applicator.ApplyAlign(sheet, handles, specificity, ref alignSelf);
                    break;

                case StylePropertyID.BackgroundImage:
                    applicator.ApplyImage(sheet, handles, specificity, ref backgroundImage);
                    break;

                case StylePropertyID.FlexBasis:
                    applicator.ApplyFlexBasis(sheet, handles, specificity, ref flexBasis);
                    break;

                case StylePropertyID.FlexGrow:
                    applicator.ApplyFloat(sheet, handles, specificity, ref flexGrow);
                    break;

                case StylePropertyID.FlexShrink:
                    applicator.ApplyFloat(sheet, handles, specificity, ref flexShrink);
                    break;

                case StylePropertyID.Font:
                    applicator.ApplyFont(sheet, handles, specificity, ref font);
                    break;

                case StylePropertyID.FontSize:
                    applicator.ApplyLength(sheet, handles, specificity, ref fontSize);
                    break;

                case StylePropertyID.FontStyleAndWeight:
                    applicator.ApplyEnum<FontStyle>(sheet, handles, specificity, ref fontStyleAndWeight);
                    break;

                case StylePropertyID.FlexDirection:
                    applicator.ApplyEnum<FlexDirection>(sheet, handles, specificity, ref flexDirection);
                    break;

                case StylePropertyID.FlexWrap:
                    applicator.ApplyEnum<Wrap>(sheet, handles, specificity, ref flexWrap);
                    break;

                case StylePropertyID.Height:
                    applicator.ApplyLength(sheet, handles, specificity, ref height);
                    break;

                case StylePropertyID.JustifyContent:
                    applicator.ApplyEnum<Justify>(sheet, handles, specificity, ref justifyContent);
                    break;

                case StylePropertyID.MarginLeft:
                    applicator.ApplyLength(sheet, handles, specificity, ref marginLeft);
                    break;

                case StylePropertyID.MarginTop:
                    applicator.ApplyLength(sheet, handles, specificity, ref marginTop);
                    break;

                case StylePropertyID.MarginRight:
                    applicator.ApplyLength(sheet, handles, specificity, ref marginRight);
                    break;

                case StylePropertyID.MarginBottom:
                    applicator.ApplyLength(sheet, handles, specificity, ref marginBottom);
                    break;

                case StylePropertyID.MaxHeight:
                    applicator.ApplyLength(sheet, handles, specificity, ref maxHeight);
                    break;

                case StylePropertyID.MaxWidth:
                    applicator.ApplyLength(sheet, handles, specificity, ref maxWidth);
                    break;

                case StylePropertyID.MinHeight:
                    applicator.ApplyLength(sheet, handles, specificity, ref minHeight);
                    break;

                case StylePropertyID.MinWidth:
                    applicator.ApplyLength(sheet, handles, specificity, ref minWidth);
                    break;

                case StylePropertyID.Overflow:
                    applicator.ApplyEnum<OverflowInternal>(sheet, handles, specificity, ref overflow);
                    break;

                case StylePropertyID.PaddingLeft:
                    applicator.ApplyLength(sheet, handles, specificity, ref paddingLeft);
                    break;

                case StylePropertyID.PaddingTop:
                    applicator.ApplyLength(sheet, handles, specificity, ref paddingTop);
                    break;

                case StylePropertyID.PaddingRight:
                    applicator.ApplyLength(sheet, handles, specificity, ref paddingRight);
                    break;

                case StylePropertyID.PaddingBottom:
                    applicator.ApplyLength(sheet, handles, specificity, ref paddingBottom);
                    break;

                case StylePropertyID.Position:
                    applicator.ApplyEnum<Position>(sheet, handles, specificity, ref position);
                    break;

                case StylePropertyID.PositionTop:
                    applicator.ApplyLength(sheet, handles, specificity, ref top);
                    break;

                case StylePropertyID.PositionBottom:
                    applicator.ApplyLength(sheet, handles, specificity, ref bottom);
                    break;

                case StylePropertyID.PositionLeft:
                    applicator.ApplyLength(sheet, handles, specificity, ref left);
                    break;

                case StylePropertyID.PositionRight:
                    applicator.ApplyLength(sheet, handles, specificity, ref right);
                    break;

                case StylePropertyID.UnityTextAlign:
                    applicator.ApplyEnum<TextAnchor>(sheet, handles, specificity, ref unityTextAlign);
                    break;

                case StylePropertyID.Color:
                    applicator.ApplyColor(sheet, handles, specificity, ref color);
                    break;

                case StylePropertyID.Width:
                    applicator.ApplyLength(sheet, handles, specificity, ref width);
                    break;

                case StylePropertyID.WhiteSpace:
                    applicator.ApplyEnum<WhiteSpace>(sheet, handles, specificity, ref whiteSpace);
                    break;

                case StylePropertyID.BackgroundColor:
                    applicator.ApplyColor(sheet, handles, specificity, ref backgroundColor);
                    break;

                case StylePropertyID.BackgroundScaleMode:
                    applicator.ApplyInt(sheet, handles, specificity, ref backgroundScaleMode);
                    break;

                case StylePropertyID.BorderColor:
                    applicator.ApplyColor(sheet, handles, specificity, ref borderColor);
                    break;

                case StylePropertyID.BorderLeftWidth:
                    applicator.ApplyFloat(sheet, handles, specificity, ref borderLeftWidth);
                    break;

                case StylePropertyID.BorderTopWidth:
                    applicator.ApplyFloat(sheet, handles, specificity, ref borderTopWidth);
                    break;

                case StylePropertyID.BorderRightWidth:
                    applicator.ApplyFloat(sheet, handles, specificity, ref borderRightWidth);
                    break;

                case StylePropertyID.BorderBottomWidth:
                    applicator.ApplyFloat(sheet, handles, specificity, ref borderBottomWidth);
                    break;

                case StylePropertyID.BorderTopLeftRadius:
                    applicator.ApplyLength(sheet, handles, specificity, ref borderTopLeftRadius);
                    break;

                case StylePropertyID.BorderTopRightRadius:
                    applicator.ApplyLength(sheet, handles, specificity, ref borderTopRightRadius);
                    break;

                case StylePropertyID.BorderBottomRightRadius:
                    applicator.ApplyLength(sheet, handles, specificity, ref borderBottomRightRadius);
                    break;

                case StylePropertyID.BorderBottomLeftRadius:
                    applicator.ApplyLength(sheet, handles, specificity, ref borderBottomLeftRadius);
                    break;

                case StylePropertyID.Cursor:
                    applicator.ApplyCursor(sheet, handles, specificity, ref cursor);
                    break;

                case StylePropertyID.SliceLeft:
                    applicator.ApplyInt(sheet, handles, specificity, ref sliceLeft);
                    break;

                case StylePropertyID.SliceTop:
                    applicator.ApplyInt(sheet, handles, specificity, ref sliceTop);
                    break;

                case StylePropertyID.SliceRight:
                    applicator.ApplyInt(sheet, handles, specificity, ref sliceRight);
                    break;

                case StylePropertyID.SliceBottom:
                    applicator.ApplyInt(sheet, handles, specificity, ref sliceBottom);
                    break;

                case StylePropertyID.Opacity:
                    applicator.ApplyFloat(sheet, handles, specificity, ref opacity);
                    break;

                case StylePropertyID.Visibility:
                    applicator.ApplyEnum<Visibility>(sheet, handles, specificity, ref visibility);
                    break;

                case StylePropertyID.Display:
                    applicator.ApplyDisplay(sheet, handles, specificity, ref display);
                    break;

                default:
                    throw new ArgumentException(string.Format("Non exhaustive switch statement (value={0})", propertyID));
            }
        }

        internal void ApplyShorthandProperty(StyleSheet sheet, StylePropertyID propertyID, StyleValueHandle[] handles, int specificity)
        {
            switch (propertyID)
            {
                case StylePropertyID.BorderRadius:
                    ShorthandApplicator.ApplyBorderRadius(sheet, handles, specificity, this);
                    break;

                case StylePropertyID.BorderWidth:
                    ShorthandApplicator.ApplyBorderWidth(sheet, handles, specificity, this);
                    break;

                case StylePropertyID.Flex:
                    ShorthandApplicator.ApplyFlex(sheet, handles, specificity, this);
                    break;

                case StylePropertyID.Margin:
                    ShorthandApplicator.ApplyMargin(sheet, handles, specificity, this);
                    break;

                case StylePropertyID.Padding:
                    ShorthandApplicator.ApplyPadding(sheet, handles, specificity, this);
                    break;

                default:
                    throw new ArgumentException(string.Format("Non exhaustive switch statement (value={0})", propertyID));
            }
        }

        private void ApplyCustomStyleProperty(StyleSheet sheet, StyleProperty styleProperty, int specificity)
        {
            if (m_CustomProperties == null)
            {
                m_CustomProperties = new Dictionary<string, CustomPropertyHandle>();
            }

            CustomPropertyHandle customProp = default(CustomPropertyHandle);
            if (!m_CustomProperties.TryGetValue(styleProperty.name, out customProp) || specificity >= customProp.specificity)
            {
                customProp.handles = styleProperty.values;
                customProp.data = sheet;
                customProp.specificity = specificity;
                m_CustomProperties[styleProperty.name] = customProp;
            }
        }

        public bool TryGetValue(CustomStyleProperty<float> property, out float value)
        {
            CustomPropertyHandle propertyHandle;
            var tmp = new StyleFloat();
            if (TryGetValue(property.name, StyleValueType.Float, out propertyHandle))
            {
                s_StyleSheetApplicator.ApplyFloat(propertyHandle.data, propertyHandle.handles, propertyHandle.specificity, ref tmp);
                value = tmp.value;
                return true;
            }

            value = 0f;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<int> property, out int value)
        {
            CustomPropertyHandle propertyHandle;
            var tmp = new StyleInt();
            if (TryGetValue(property.name, StyleValueType.Float, out propertyHandle))
            {
                s_StyleSheetApplicator.ApplyInt(propertyHandle.data, propertyHandle.handles, propertyHandle.specificity, ref tmp);
                value = tmp.value;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<bool> property, out bool value)
        {
            CustomPropertyHandle propertyHandle;
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                value = propertyHandle.data.ReadKeyword(propertyHandle.handles[0]) == StyleValueKeyword.True;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Color> property, out Color value)
        {
            CustomPropertyHandle propertyHandle;
            var tmp = new StyleColor();
            if (TryGetValue(property.name, StyleValueType.Color, out propertyHandle))
            {
                s_StyleSheetApplicator.ApplyColor(propertyHandle.data, propertyHandle.handles, propertyHandle.specificity, ref tmp);
                value = tmp.value;
                return true;
            }

            value = Color.clear;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Texture2D> property, out Texture2D value)
        {
            CustomPropertyHandle propertyHandle;
            var tmp = new StyleBackground();
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                s_StyleSheetApplicator.ApplyImage(propertyHandle.data, propertyHandle.handles, propertyHandle.specificity, ref tmp);
                value = tmp.value.texture;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<string> property, out string value)
        {
            CustomPropertyHandle propertyHandle;
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                value = propertyHandle.data.ReadAsString(propertyHandle.handles[0]);
                return true;
            }

            value = string.Empty;
            return false;
        }

        private bool TryGetValue(string propertyName, StyleValueType valueType, out CustomPropertyHandle customPropertyHandle)
        {
            customPropertyHandle = new CustomPropertyHandle();
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(propertyName, out customPropertyHandle))
            {
                // CustomProperty only support one value
                var handle = customPropertyHandle.handles[0];
                if (handle.valueType != valueType)
                {
                    Debug.LogWarning(string.Format("Trying to read value as {0} while parsed type is {1}", valueType, handle.valueType));
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
