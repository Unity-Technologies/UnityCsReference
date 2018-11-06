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

    internal class VisualElementStylesData : ICustomStyle, IComputedStyle
    {
        private static StyleSheetApplicator s_StyleSheetApplicator = new StyleSheetApplicator();
        public static VisualElementStylesData none = new VisualElementStylesData(true);

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
        }

        public void WriteToGUIStyle(GUIStyle style)
        {
            style.alignment = (TextAnchor)(unityTextAlign.GetSpecifiedValueOrDefault((int)style.alignment));
            style.wordWrap = whiteSpace.specificity > StyleValueExtensions.UndefinedSpecificity
                ? (WhiteSpace)whiteSpace.value == WhiteSpace.Normal
                : style.wordWrap;
            style.clipping = (TextClipping)(overflow.GetSpecifiedValueOrDefault((int)style.clipping));
            if (font.value != null)
            {
                style.font = font.value;
            }

            style.fontSize = (int)fontSize.GetSpecifiedValueOrDefault(style.fontSize);
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
            if (backgroundImage.value.texture != null)
            {
                state.background = backgroundImage.value.texture;
                if (state.scaledBackgrounds == null || state.scaledBackgrounds.Length < 1 || state.scaledBackgrounds[0] != backgroundImage.value.texture)
                    state.scaledBackgrounds = new Texture2D[1] { backgroundImage.value.texture };
            }
        }

        void AssignRect(RectOffset rect, ref StyleLength left, ref StyleLength top, ref StyleLength right, ref StyleLength bottom)
        {
            rect.left = (int)left.GetSpecifiedValueOrDefault(rect.left);
            rect.top = (int)top.GetSpecifiedValueOrDefault(rect.top);
            rect.right = (int)right.GetSpecifiedValueOrDefault(rect.right);
            rect.bottom = (int)bottom.GetSpecifiedValueOrDefault(rect.bottom);
        }

        void AssignRect(RectOffset rect, ref StyleInt left, ref StyleInt top, ref StyleInt right, ref StyleInt bottom)
        {
            rect.left = left.GetSpecifiedValueOrDefault(rect.left);
            rect.top = top.GetSpecifiedValueOrDefault(rect.top);
            rect.right = right.GetSpecifiedValueOrDefault(rect.right);
            rect.bottom = bottom.GetSpecifiedValueOrDefault(rect.bottom);
        }

        public void ApplyLayoutValues()
        {
            if (yogaNode == null)
                yogaNode = new YogaNode();

            SyncWithLayout(yogaNode);
        }

        internal const Align DefaultAlignContent = Align.FlexStart;
        internal const Align DefaultAlignItems = Align.Stretch;

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

            targetNode.FlexGrow = flexGrow.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.FlexShrink = flexShrink.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Left = left.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Top = top.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Right = right.GetSpecifiedValueOrDefault(float.NaN);
            targetNode.Bottom = bottom.GetSpecifiedValueOrDefault(float.NaN);
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

            targetNode.PositionType = (YogaPositionType)position.value;
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

        internal void ApplyRule(StyleSheet sheet, int specificity, StyleRule rule, StylePropertyID[] propertyIDs)
        {
            for (int i = 0; i < rule.properties.Length; i++)
            {
                var styleProperty = rule.properties[i];
                var propertyID = propertyIDs[i];

                if (propertyID == StylePropertyID.Unknown)
                    continue;

                if (propertyID == StylePropertyID.Custom)
                {
                    ApplyCustomStyleProperty(sheet, styleProperty, specificity);
                }
                else
                {
                    ApplyStyleProperty(s_StyleSheetApplicator, sheet, propertyID, styleProperty.values, specificity);
                }
            }
        }

        internal void ApplyStyleProperty(IStyleSheetApplicator applicator, StyleSheet sheet, StylePropertyID propertyID, StyleValueHandle[] handles, int specificity)
        {
            switch (propertyID)
            {
                case StylePropertyID.AlignContent:
                    applicator.ApplyEnum<Align>(sheet, handles, specificity, ref alignContent);
                    break;

                case StylePropertyID.AlignItems:
                    applicator.ApplyEnum<Align>(sheet, handles, specificity, ref alignItems);
                    break;

                case StylePropertyID.AlignSelf:
                    applicator.ApplyEnum<Align>(sheet, handles, specificity, ref alignSelf);
                    break;

                case StylePropertyID.BackgroundImage:
                    applicator.ApplyImage(sheet, handles, specificity, ref backgroundImage);
                    break;

                case StylePropertyID.Flex:
                    applicator.ApplyFlexShorthand(sheet, handles, specificity, this);
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

                    // Shorthand values
                case StylePropertyID.BorderRadius:
                    applicator.ApplyLength(sheet, handles, specificity, ref borderTopLeftRadius);
                    applicator.ApplyLength(sheet, handles, specificity, ref borderTopRightRadius);
                    applicator.ApplyLength(sheet, handles, specificity, ref borderBottomLeftRadius);
                    applicator.ApplyLength(sheet, handles, specificity, ref borderBottomRightRadius);
                    break;

                case StylePropertyID.Visibility:
                    applicator.ApplyEnum<Visibility>(sheet, handles, specificity, ref visibility);
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

        StyleLength IComputedStyle.width => width;
        StyleLength IComputedStyle.height => height;
        StyleLength IComputedStyle.maxWidth => maxWidth.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.None);
        StyleLength IComputedStyle.maxHeight => maxHeight.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.None);
        StyleLength IComputedStyle.minWidth => minWidth.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.Auto);
        StyleLength IComputedStyle.minHeight => minHeight.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.Auto);
        StyleLength IComputedStyle.flexBasis => flexBasis.GetSpecifiedValueOrDefault<StyleLength, Length>(StyleKeyword.Auto);
        StyleFloat IComputedStyle.flexGrow => flexGrow;
        StyleFloat IComputedStyle.flexShrink => flexShrink;
        StyleEnum<FlexDirection> IComputedStyle.flexDirection => flexDirection.ToStyleEnum((FlexDirection)flexDirection.value);
        StyleEnum<Wrap> IComputedStyle.flexWrap => flexWrap.ToStyleEnum((Wrap)flexWrap.value);
        StyleEnum<Overflow> IComputedStyle.overflow => overflow.ToStyleEnum((Overflow)overflow.value);
        StyleLength IComputedStyle.left => left;
        StyleLength IComputedStyle.top => top;
        StyleLength IComputedStyle.right => right;
        StyleLength IComputedStyle.bottom => bottom;
        StyleLength IComputedStyle.marginLeft => marginLeft;
        StyleLength IComputedStyle.marginTop => marginTop;
        StyleLength IComputedStyle.marginRight => marginRight;
        StyleLength IComputedStyle.marginBottom => marginBottom;
        StyleLength IComputedStyle.paddingLeft => paddingLeft;
        StyleLength IComputedStyle.paddingTop => paddingTop;
        StyleLength IComputedStyle.paddingRight => paddingRight;
        StyleLength IComputedStyle.paddingBottom => paddingBottom;
        StyleEnum<Position> IComputedStyle.position => position.ToStyleEnum((Position)position.value);
        StyleEnum<Align> IComputedStyle.alignSelf => alignSelf.ToStyleEnum((Align)alignSelf.value);
        StyleEnum<TextAnchor> IComputedStyle.unityTextAlign => unityTextAlign.ToStyleEnum((TextAnchor)unityTextAlign.value);
        StyleEnum<FontStyle> IComputedStyle.unityFontStyleAndWeight => fontStyleAndWeight.ToStyleEnum((FontStyle)fontStyleAndWeight.value);
        StyleFont IComputedStyle.unityFont => font;
        StyleLength IComputedStyle.fontSize => fontSize;
        StyleEnum<WhiteSpace> IComputedStyle.whiteSpace => whiteSpace.ToStyleEnum((WhiteSpace)whiteSpace.value);
        StyleColor IComputedStyle.color => color;
        StyleColor IComputedStyle.backgroundColor => backgroundColor;
        StyleColor IComputedStyle.borderColor => borderColor;
        StyleBackground IComputedStyle.backgroundImage => backgroundImage;
        StyleEnum<ScaleMode> IComputedStyle.unityBackgroundScaleMode => backgroundScaleMode.ToStyleEnum((ScaleMode)backgroundScaleMode.value);
        StyleEnum<Align> IComputedStyle.alignItems => alignItems.ToStyleEnum((Align)alignItems.value);
        StyleEnum<Align> IComputedStyle.alignContent => alignContent.ToStyleEnum((Align)alignContent.value);
        StyleEnum<Justify> IComputedStyle.justifyContent => justifyContent.ToStyleEnum((Justify)justifyContent.value);
        StyleFloat IComputedStyle.borderLeftWidth => borderLeftWidth;
        StyleFloat IComputedStyle.borderTopWidth => borderTopWidth;
        StyleFloat IComputedStyle.borderRightWidth => borderRightWidth;
        StyleFloat IComputedStyle.borderBottomWidth => borderBottomWidth;
        StyleLength IComputedStyle.borderTopLeftRadius => borderTopLeftRadius;
        StyleLength IComputedStyle.borderTopRightRadius => borderTopRightRadius;
        StyleLength IComputedStyle.borderBottomRightRadius => borderBottomRightRadius;
        StyleLength IComputedStyle.borderBottomLeftRadius => borderBottomLeftRadius;
        StyleInt IComputedStyle.unitySliceLeft => sliceLeft;
        StyleInt IComputedStyle.unitySliceTop => sliceTop;
        StyleInt IComputedStyle.unitySliceRight => sliceRight;
        StyleInt IComputedStyle.unitySliceBottom => sliceBottom;
        StyleFloat IComputedStyle.opacity => opacity.GetSpecifiedValueOrDefault(1.0f);
        StyleCursor IComputedStyle.cursor => cursor;
        StyleEnum<Visibility> IComputedStyle.visibility => visibility.ToStyleEnum((Visibility)visibility.value);
    }
}
