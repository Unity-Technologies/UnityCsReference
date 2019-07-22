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
        public StylePropertyValue value;
    }

    internal class VisualElementStylesData : ICustomStyle
    {
        private static StyleValuePropertyReader s_StyleValuePropertyReader = new StyleValuePropertyReader();
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
        internal StyleInt unityOverflowClipBox;
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
        internal StyleInt unityFontStyleAndWeight;
        internal StyleFont unityFont;
        internal StyleLength fontSize;
        internal StyleInt whiteSpace;
        internal StyleColor color;
        internal StyleInt flexDirection;
        internal StyleColor backgroundColor;
        internal StyleBackground backgroundImage;
        internal StyleInt unityBackgroundScaleMode;
        internal StyleColor unityBackgroundImageTintColor;
        internal StyleInt alignItems;
        internal StyleInt alignContent;
        internal StyleInt justifyContent;
        internal StyleInt flexWrap;
        internal StyleColor borderLeftColor;
        internal StyleColor borderTopColor;
        internal StyleColor borderRightColor;
        internal StyleColor borderBottomColor;
        internal StyleFloat borderLeftWidth;
        internal StyleFloat borderTopWidth;
        internal StyleFloat borderRightWidth;
        internal StyleFloat borderBottomWidth;
        internal StyleLength borderTopLeftRadius;
        internal StyleLength borderTopRightRadius;
        internal StyleLength borderBottomRightRadius;
        internal StyleLength borderBottomLeftRadius;
        internal StyleInt unitySliceLeft;
        internal StyleInt unitySliceTop;
        internal StyleInt unitySliceRight;
        internal StyleInt unitySliceBottom;
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

            // Initialize non trivial properties
            left = StyleSheetCache.GetInitialValue(StylePropertyID.PositionLeft).ToStyleLength();
            top = StyleSheetCache.GetInitialValue(StylePropertyID.PositionTop).ToStyleLength();
            right = StyleSheetCache.GetInitialValue(StylePropertyID.PositionRight).ToStyleLength();
            bottom = StyleSheetCache.GetInitialValue(StylePropertyID.PositionBottom).ToStyleLength();
            width = StyleSheetCache.GetInitialValue(StylePropertyID.Width).ToStyleLength();
            height = StyleSheetCache.GetInitialValue(StylePropertyID.Height).ToStyleLength();
            minWidth = StyleSheetCache.GetInitialValue(StylePropertyID.MinWidth).ToStyleLength();
            minHeight = StyleSheetCache.GetInitialValue(StylePropertyID.MinHeight).ToStyleLength();
            maxWidth = StyleSheetCache.GetInitialValue(StylePropertyID.MaxWidth).ToStyleLength();
            maxHeight = StyleSheetCache.GetInitialValue(StylePropertyID.MaxHeight).ToStyleLength();
            alignSelf = (int)StyleSheetCache.GetInitialValue(StylePropertyID.AlignSelf).number;
            alignItems = (int)StyleSheetCache.GetInitialValue(StylePropertyID.AlignItems).number;
            alignContent = (int)StyleSheetCache.GetInitialValue(StylePropertyID.AlignContent).number;
            flexGrow = StyleSheetCache.GetInitialValue(StylePropertyID.FlexGrow).ToStyleFloat();
            flexShrink = StyleSheetCache.GetInitialValue(StylePropertyID.FlexShrink).ToStyleFloat();
            flexBasis = StyleSheetCache.GetInitialValue(StylePropertyID.FlexBasis).ToStyleLength();
            color = StyleSheetCache.GetInitialValue(StylePropertyID.Color).color;
            borderLeftColor = StyleSheetCache.GetInitialValue(StylePropertyID.BorderLeftColor).color;
            borderTopColor = StyleSheetCache.GetInitialValue(StylePropertyID.BorderTopColor).color;
            borderRightColor = StyleSheetCache.GetInitialValue(StylePropertyID.BorderRightColor).color;
            borderBottomColor = StyleSheetCache.GetInitialValue(StylePropertyID.BorderBottomColor).color;
            opacity = StyleSheetCache.GetInitialValue(StylePropertyID.Opacity).number;
            unityBackgroundImageTintColor = StyleSheetCache.GetInitialValue(StylePropertyID.BackgroundImageTintColor).color;
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
            unityOverflowClipBox.Apply(other.unityOverflowClipBox, mode);
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
            unityFontStyleAndWeight.Apply(other.unityFontStyleAndWeight, mode);
            fontSize.Apply(other.fontSize, mode);
            unityFont.Apply(other.unityFont, mode);
            whiteSpace.Apply(other.whiteSpace, mode);
            color.Apply(other.color, mode);
            flexDirection.Apply(other.flexDirection, mode);
            backgroundColor.Apply(other.backgroundColor, mode);
            backgroundImage.Apply(other.backgroundImage, mode);
            unityBackgroundScaleMode.Apply(other.unityBackgroundScaleMode, mode);
            unityBackgroundImageTintColor.Apply(other.unityBackgroundImageTintColor, mode);
            alignItems.Apply(other.alignItems, mode);
            alignContent.Apply(other.alignContent, mode);
            justifyContent.Apply(other.justifyContent, mode);
            flexWrap.Apply(other.flexWrap, mode);
            borderLeftColor.Apply(other.borderLeftColor, mode);
            borderTopColor.Apply(other.borderTopColor, mode);
            borderRightColor.Apply(other.borderRightColor, mode);
            borderBottomColor.Apply(other.borderBottomColor, mode);
            borderLeftWidth.Apply(other.borderLeftWidth, mode);
            borderTopWidth.Apply(other.borderTopWidth, mode);
            borderRightWidth.Apply(other.borderRightWidth, mode);
            borderBottomWidth.Apply(other.borderBottomWidth, mode);
            borderTopLeftRadius.Apply(other.borderTopLeftRadius, mode);
            borderTopRightRadius.Apply(other.borderTopRightRadius, mode);
            borderBottomRightRadius.Apply(other.borderBottomRightRadius, mode);
            borderBottomLeftRadius.Apply(other.borderBottomLeftRadius, mode);
            unitySliceLeft.Apply(other.unitySliceLeft, mode);
            unitySliceTop.Apply(other.unitySliceTop, mode);
            unitySliceRight.Apply(other.unitySliceRight, mode);
            unitySliceBottom.Apply(other.unitySliceBottom, mode);
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

        public void SyncWithLayout(YogaNode targetNode)
        {
            targetNode.Flex = float.NaN;

            targetNode.FlexGrow = flexGrow.value;
            targetNode.FlexShrink = flexShrink.value;
            targetNode.FlexBasis = flexBasis.ToYogaValue();
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
            targetNode.AlignContent = (YogaAlign)alignContent.value;
            targetNode.AlignItems = (YogaAlign)alignItems.value;
            targetNode.JustifyContent = (YogaJustify)justifyContent.value;
            targetNode.Wrap = (YogaWrap)flexWrap.value;
            targetNode.Display = (YogaDisplay)display.value;
        }

        internal void ApplyProperties(StylePropertyReader reader, InheritedStylesData inheritedStylesData)
        {
            while (reader.IsValid())
            {
                var styleProperty = reader.property;
                var propertyID = reader.propertyID;

                if (reader.IsKeyword(0, StyleValueKeyword.Initial))
                {
                    ApplyInitialStyleValue(reader);
                }
                else if (reader.IsKeyword(0, StyleValueKeyword.Unset) && inheritedStylesData != null)
                {
                    ApplyUnsetStyleValue(reader, inheritedStylesData);
                }
                else
                {
                    switch (propertyID)
                    {
                        case StylePropertyID.Unknown:
                            break;
                        case StylePropertyID.Custom:
                            ApplyCustomStyleProperty(reader);
                            break;
                        case StylePropertyID.BorderColor:
                        case StylePropertyID.BorderRadius:
                        case StylePropertyID.BorderWidth:
                        case StylePropertyID.Flex:
                        case StylePropertyID.Margin:
                        case StylePropertyID.Padding:
                            ApplyShorthandProperty(reader);
                            break;
                        default:
                            ApplyStyleProperty(reader);
                            break;
                    }
                }

                reader.MoveNextProperty();
            }
        }

        internal void ApplyStyleCursor(StyleCursor styleCursor, int specificity)
        {
            s_StyleValuePropertyReader.Set(styleCursor, specificity);
            cursor = s_StyleValuePropertyReader.ReadStyleCursor(0);
        }

        internal void ApplyStyleValue(StylePropertyID propertyID, StyleValue value, int specificity)
        {
            if (value.keyword == StyleKeyword.Initial)
            {
                ApplyInitialStyleValue(propertyID, specificity);
            }
            else
            {
                s_StyleValuePropertyReader.Set(propertyID, value, specificity);
                ApplyStyleProperty(s_StyleValuePropertyReader);
            }
        }

        private void ApplyInitialStyleValue(StylePropertyReader reader)
        {
            if (reader.propertyID == StylePropertyID.Custom)
            {
                RemoveCustomStyleProperty(reader.property.name);
            }
            else
            {
                ApplyInitialStyleValue(reader.propertyID, reader.specificity);
            }
        }

        private void ApplyInitialStyleValue(StylePropertyID propertyID, int specificity)
        {
            switch (propertyID)
            {
                case StylePropertyID.Unknown:
                case StylePropertyID.Custom:
                {
                    Debug.LogAssertion($"Unexpected style property ID {propertyID.ToString()}.");
                    break;
                }
                case StylePropertyID.BorderColor:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderLeftColor);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderTopColor);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderRightColor);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderBottomColor);
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
                case StylePropertyID.BorderRadius:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderTopLeftRadius);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderTopRightRadius);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderBottomLeftRadius);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderBottomRightRadius);
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
                case StylePropertyID.BorderWidth:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderLeftWidth);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderTopWidth);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderRightWidth);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.BorderBottomWidth);
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
                case StylePropertyID.Flex:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(StylePropertyID.FlexGrow);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.FlexShrink);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.FlexBasis);
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
                case StylePropertyID.Margin:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(StylePropertyID.MarginLeft);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.MarginTop);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.MarginRight);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.MarginBottom);
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
                case StylePropertyID.Padding:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(StylePropertyID.PaddingLeft);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.PaddingTop);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.PaddingRight);
                    ApplyStyleValue(sv.id, sv, specificity);

                    sv = StyleSheetCache.GetInitialValue(StylePropertyID.PaddingBottom);
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
                case StylePropertyID.Cursor:
                {
                    ApplyStyleCursor(new StyleCursor(), specificity);
                    break;
                }
                default:
                {
                    StyleValue sv = StyleSheetCache.GetInitialValue(propertyID);
                    Debug.Assert(sv.keyword != StyleKeyword.Initial, "Recursive apply initial value");
                    ApplyStyleValue(sv.id, sv, specificity);
                    break;
                }
            }
        }

        private void ApplyUnsetStyleValue(StylePropertyReader reader, InheritedStylesData inheritedStylesData)
        {
            Debug.Assert(inheritedStylesData != null, "Unexpected call to ApplyUnsetStyleValue");
            var specificity = reader.specificity;
            switch (reader.propertyID)
            {
                case StylePropertyID.Color:
                    color = inheritedStylesData.color;
                    color.specificity = specificity;
                    break;
                case StylePropertyID.Font:
                    unityFont = inheritedStylesData.font;
                    unityFont.specificity = specificity;
                    break;
                case StylePropertyID.FontSize:
                    fontSize = inheritedStylesData.fontSize;
                    fontSize.specificity = specificity;
                    break;
                case StylePropertyID.FontStyleAndWeight:
                    unityFontStyleAndWeight = inheritedStylesData.unityFontStyle;
                    unityFontStyleAndWeight.specificity = specificity;
                    break;
                case StylePropertyID.UnityTextAlign:
                    unityTextAlign = inheritedStylesData.unityTextAlign;
                    unityTextAlign.specificity = specificity;
                    break;
                case StylePropertyID.Visibility:
                    visibility = inheritedStylesData.visibility;
                    visibility.specificity = specificity;
                    break;
                case StylePropertyID.WhiteSpace:
                    whiteSpace = inheritedStylesData.whiteSpace;
                    whiteSpace.specificity = specificity;
                    break;
                case StylePropertyID.Custom:
                    RemoveCustomStyleProperty(reader.property.name);
                    break;
                default:
                    ApplyInitialStyleValue(reader.propertyID, specificity);
                    break;
            }
        }

        internal void ApplyStyleProperty(IStylePropertyReader reader)
        {
            switch (reader.propertyID)
            {
                case StylePropertyID.AlignContent:
                    StyleSheetApplicator.ApplyAlign(reader, ref alignContent);
                    break;

                case StylePropertyID.AlignItems:
                    StyleSheetApplicator.ApplyAlign(reader, ref alignItems);
                    break;

                case StylePropertyID.AlignSelf:
                    StyleSheetApplicator.ApplyAlign(reader, ref alignSelf);
                    break;

                case StylePropertyID.BackgroundImage:
                    backgroundImage = reader.ReadStyleBackground(0);
                    break;

                case StylePropertyID.FlexBasis:
                    flexBasis = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.FlexGrow:
                    flexGrow = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.FlexShrink:
                    flexShrink = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.Font:
                    unityFont = reader.ReadStyleFont(0);
                    break;

                case StylePropertyID.FontSize:
                    fontSize = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.FontStyleAndWeight:
                    unityFontStyleAndWeight = reader.ReadStyleEnum<FontStyle>(0);
                    break;

                case StylePropertyID.FlexDirection:
                    flexDirection = reader.ReadStyleEnum<FlexDirection>(0);
                    break;

                case StylePropertyID.FlexWrap:
                    flexWrap = reader.ReadStyleEnum<Wrap>(0);
                    break;

                case StylePropertyID.Height:
                    height = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.JustifyContent:
                    justifyContent = reader.ReadStyleEnum<Justify>(0);
                    break;

                case StylePropertyID.MarginLeft:
                    marginLeft = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MarginTop:
                    marginTop = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MarginRight:
                    marginRight = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MarginBottom:
                    marginBottom = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MaxHeight:
                    maxHeight = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MaxWidth:
                    maxWidth = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MinHeight:
                    minHeight = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.MinWidth:
                    minWidth = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.Overflow:
                    overflow = reader.ReadStyleEnum<OverflowInternal>(0);
                    break;

                case StylePropertyID.OverflowClipBox:
                    unityOverflowClipBox = reader.ReadStyleEnum<OverflowClipBox>(0);
                    break;

                case StylePropertyID.PaddingLeft:
                    paddingLeft = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.PaddingTop:
                    paddingTop = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.PaddingRight:
                    paddingRight = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.PaddingBottom:
                    paddingBottom = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.Position:
                    position = reader.ReadStyleEnum<Position>(0);
                    break;

                case StylePropertyID.PositionTop:
                    top = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.PositionBottom:
                    bottom = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.PositionLeft:
                    left = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.PositionRight:
                    right = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.UnityTextAlign:
                    unityTextAlign = reader.ReadStyleEnum<TextAnchor>(0);
                    break;

                case StylePropertyID.Color:
                    color = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.Width:
                    width = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.WhiteSpace:
                    whiteSpace = reader.ReadStyleEnum<WhiteSpace>(0);
                    break;

                case StylePropertyID.BackgroundColor:
                    backgroundColor = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.BackgroundScaleMode:
                    unityBackgroundScaleMode = reader.ReadStyleEnum<ScaleMode>(0);
                    break;

                case StylePropertyID.BackgroundImageTintColor:
                    unityBackgroundImageTintColor = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.BorderLeftColor:
                    borderLeftColor = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.BorderTopColor:
                    borderTopColor = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.BorderRightColor:
                    borderRightColor = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.BorderBottomColor:
                    borderBottomColor = reader.ReadStyleColor(0);
                    break;

                case StylePropertyID.BorderLeftWidth:
                    borderLeftWidth = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.BorderTopWidth:
                    borderTopWidth = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.BorderRightWidth:
                    borderRightWidth = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.BorderBottomWidth:
                    borderBottomWidth = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.BorderTopLeftRadius:
                    borderTopLeftRadius = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.BorderTopRightRadius:
                    borderTopRightRadius = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.BorderBottomRightRadius:
                    borderBottomRightRadius = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.BorderBottomLeftRadius:
                    borderBottomLeftRadius = reader.ReadStyleLength(0);
                    break;

                case StylePropertyID.Cursor:
                    cursor = reader.ReadStyleCursor(0);
                    break;

                case StylePropertyID.SliceLeft:
                    unitySliceLeft = reader.ReadStyleInt(0);
                    break;

                case StylePropertyID.SliceTop:
                    unitySliceTop = reader.ReadStyleInt(0);
                    break;

                case StylePropertyID.SliceRight:
                    unitySliceRight = reader.ReadStyleInt(0);
                    break;

                case StylePropertyID.SliceBottom:
                    unitySliceBottom = reader.ReadStyleInt(0);
                    break;

                case StylePropertyID.Opacity:
                    opacity = reader.ReadStyleFloat(0);
                    break;

                case StylePropertyID.Visibility:
                    visibility = reader.ReadStyleEnum<Visibility>(0);
                    break;

                case StylePropertyID.Display:
                    StyleSheetApplicator.ApplyDisplay(reader, ref display);
                    break;

                default:
                    throw new ArgumentException(string.Format("Non exhaustive switch statement (value={0})", reader.propertyID));
            }
        }

        internal void ApplyShorthandProperty(StylePropertyReader reader)
        {
            switch (reader.propertyID)
            {
                case StylePropertyID.BorderColor:
                    ShorthandApplicator.ApplyBorderColor(reader, this);
                    break;

                case StylePropertyID.BorderRadius:
                    ShorthandApplicator.ApplyBorderRadius(reader, this);
                    break;

                case StylePropertyID.BorderWidth:
                    ShorthandApplicator.ApplyBorderWidth(reader, this);
                    break;

                case StylePropertyID.Flex:
                    ShorthandApplicator.ApplyFlex(reader, this);
                    break;

                case StylePropertyID.Margin:
                    ShorthandApplicator.ApplyMargin(reader, this);
                    break;

                case StylePropertyID.Padding:
                    ShorthandApplicator.ApplyPadding(reader, this);
                    break;

                default:
                    throw new ArgumentException(string.Format("Non exhaustive switch statement (value={0})", reader.propertyID));
            }
        }

        private void RemoveCustomStyleProperty(string name)
        {
            if (m_CustomProperties == null || !m_CustomProperties.ContainsKey(name))
                return;

            m_CustomProperties.Remove(name);
        }

        private void ApplyCustomStyleProperty(StylePropertyReader reader)
        {
            if (m_CustomProperties == null)
            {
                m_CustomProperties = new Dictionary<string, CustomPropertyHandle>();
            }

            var styleProperty = reader.property;
            var specificity = reader.specificity;

            CustomPropertyHandle customProp = default(CustomPropertyHandle);
            if (!m_CustomProperties.TryGetValue(styleProperty.name, out customProp) || specificity >= customProp.specificity)
            {
                // Custom property only support one value
                customProp.value = reader.GetValue(0);
                customProp.specificity = specificity;
                m_CustomProperties[styleProperty.name] = customProp;
            }
        }

        public bool TryGetValue(CustomStyleProperty<float> property, out float value)
        {
            CustomPropertyHandle propertyHandle;
            if (TryGetValue(property.name, StyleValueType.Float, out propertyHandle))
            {
                var propValue = propertyHandle.value;
                if (propValue.sheet.TryReadFloat(propValue.handle, out value))
                    return true;
            }

            value = 0f;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<int> property, out int value)
        {
            CustomPropertyHandle propertyHandle;
            if (TryGetValue(property.name, StyleValueType.Float, out propertyHandle))
            {
                float tmp = 0f;
                var propValue = propertyHandle.value;
                if (propValue.sheet.TryReadFloat(propValue.handle, out tmp))
                {
                    value = (int)tmp;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<bool> property, out bool value)
        {
            CustomPropertyHandle propertyHandle;
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                var propValue = propertyHandle.value;
                value = propValue.sheet.ReadKeyword(propValue.handle) == StyleValueKeyword.True;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Color> property, out Color value)
        {
            CustomPropertyHandle propertyHandle;
            if (TryGetValue(property.name, StyleValueType.Color, out propertyHandle))
            {
                var propValue = propertyHandle.value;
                if (propValue.sheet.TryReadColor(propValue.handle, out value))
                    return true;
            }

            value = Color.clear;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<Texture2D> property, out Texture2D value)
        {
            CustomPropertyHandle propertyHandle;
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                var source = new ImageSource();
                var propValue = propertyHandle.value;
                if (StylePropertyReader.TryGetImageSourceFromValue(propValue, out source) && source.texture != null)
                {
                    value = source.texture;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<VectorImage> property, out VectorImage value)
        {
            CustomPropertyHandle propertyHandle;
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                var source = new ImageSource();
                var propValue = propertyHandle.value;
                if (StylePropertyReader.TryGetImageSourceFromValue(propValue, out source) && source.vectorImage != null)
                {
                    value = source.vectorImage;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public bool TryGetValue(CustomStyleProperty<string> property, out string value)
        {
            CustomPropertyHandle propertyHandle;
            if (m_CustomProperties != null && m_CustomProperties.TryGetValue(property.name, out propertyHandle))
            {
                var propValue = propertyHandle.value;
                value = propValue.sheet.ReadAsString(propValue.handle);
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
                var handle = customPropertyHandle.value.handle;
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
