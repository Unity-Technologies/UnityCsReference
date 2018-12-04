// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal class InlineStyleAccess : IStyle
    {
        private List<StyleValue> m_InlineStyleValues = new List<StyleValue>();

        private VisualElement ve { get; set; }

        private bool m_HasInlineCursor;
        private StyleCursor m_InlineCursor;

        public InlineStyleAccess(VisualElement ve)
        {
            this.ve = ve;

            if (ve.specifiedStyle.isShared)
            {
                var inline = new VisualElementStylesData(false);
                inline.Apply(ve.m_SharedStyle, StylePropertyApplyMode.Copy);
                ve.m_Style = inline;
            }
        }

        ~InlineStyleAccess()
        {
            StyleValue inlineValue = new StyleValue();
            if (TryGetInlineStyleValue(StylePropertyID.BackgroundImage, ref inlineValue))
            {
                if (inlineValue.resource.IsAllocated)
                    inlineValue.resource.Free();
            }
            if (TryGetInlineStyleValue(StylePropertyID.Font, ref inlineValue))
            {
                if (inlineValue.resource.IsAllocated)
                    inlineValue.resource.Free();
            }
        }

        StyleLength IStyle.width
        {
            get { return GetInlineStyleLength(StylePropertyID.Width); }
            set
            {
                if (SetInlineStyle(StylePropertyID.Width, value, ve.sharedStyle.width))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Width = ve.computedStyle.width.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.height
        {
            get { return GetInlineStyleLength(StylePropertyID.Height); }
            set
            {
                if (SetInlineStyle(StylePropertyID.Height, value, ve.sharedStyle.height))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Height = ve.computedStyle.height.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.maxWidth
        {
            get { return GetInlineStyleLength(StylePropertyID.MaxWidth); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MaxWidth, value, ve.sharedStyle.maxWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MaxWidth = ve.computedStyle.maxWidth.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.maxHeight
        {
            get { return GetInlineStyleLength(StylePropertyID.MaxHeight); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MaxHeight, value, ve.sharedStyle.maxHeight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MaxHeight = ve.computedStyle.maxHeight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.minWidth
        {
            get { return GetInlineStyleLength(StylePropertyID.MinWidth); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MinWidth, value, ve.sharedStyle.minWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MinWidth = ve.computedStyle.minWidth.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.minHeight
        {
            get { return GetInlineStyleLength(StylePropertyID.MinHeight); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MinHeight, value, ve.sharedStyle.minHeight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MinHeight = ve.computedStyle.minHeight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.flexBasis
        {
            get { return GetInlineStyleLength(StylePropertyID.FlexBasis); }
            set
            {
                if (SetInlineStyle(StylePropertyID.FlexBasis, value, ve.sharedStyle.flexBasis))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexBasis = ve.computedStyle.flexBasis.ToYogaValue();
                }
            }
        }

        StyleFloat IStyle.flexGrow
        {
            get { return GetInlineStyleFloat(StylePropertyID.FlexGrow); }
            set
            {
                if (SetInlineStyle(StylePropertyID.FlexGrow, value, ve.sharedStyle.flexGrow))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexGrow = ve.computedStyle.flexGrow.value;
                }
            }
        }

        StyleFloat IStyle.flexShrink
        {
            get { return GetInlineStyleFloat(StylePropertyID.FlexShrink); }
            set
            {
                if (SetInlineStyle(StylePropertyID.FlexShrink, value, ve.sharedStyle.flexShrink))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexShrink = ve.computedStyle.flexShrink.value;
                }
            }
        }

        StyleEnum<Overflow> IStyle.overflow
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.Overflow);
                return new StyleEnum<Overflow>((Overflow)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.Overflow, value, ve.sharedStyle.overflow))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Overflow = (YogaOverflow)ve.computedStyle.overflow.value;
                }
            }
        }

        StyleLength IStyle.left
        {
            get { return GetInlineStyleLength(StylePropertyID.PositionLeft); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PositionLeft, value, ve.sharedStyle.left))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Left = ve.computedStyle.left.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.top
        {
            get { return GetInlineStyleLength(StylePropertyID.PositionTop); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PositionTop, value, ve.sharedStyle.top))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Top = ve.computedStyle.top.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.right
        {
            get { return GetInlineStyleLength(StylePropertyID.PositionRight); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PositionRight, value, ve.sharedStyle.right))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Right = ve.computedStyle.right.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.bottom
        {
            get { return GetInlineStyleLength(StylePropertyID.PositionBottom); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PositionBottom, value, ve.sharedStyle.bottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Bottom = ve.computedStyle.bottom.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginLeft
        {
            get { return GetInlineStyleLength(StylePropertyID.MarginLeft); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MarginLeft, value, ve.sharedStyle.marginLeft))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginLeft = ve.computedStyle.marginLeft.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginTop
        {
            get { return GetInlineStyleLength(StylePropertyID.MarginTop); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MarginTop, value, ve.sharedStyle.marginTop))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginTop = ve.computedStyle.marginTop.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginRight
        {
            get { return GetInlineStyleLength(StylePropertyID.MarginRight); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MarginRight, value, ve.sharedStyle.marginRight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginRight = ve.computedStyle.marginRight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginBottom
        {
            get { return GetInlineStyleLength(StylePropertyID.MarginBottom); }
            set
            {
                if (SetInlineStyle(StylePropertyID.MarginBottom, value, ve.sharedStyle.marginBottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginBottom = ve.computedStyle.marginBottom.ToYogaValue();
                }
            }
        }

        StyleFloat IStyle.borderLeftWidth
        {
            get { return GetInlineStyleFloat(StylePropertyID.BorderLeftWidth); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderLeftWidth, value, ve.sharedStyle.borderLeftWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderLeftWidth = ve.computedStyle.borderLeftWidth.value;
                }
            }
        }

        StyleFloat IStyle.borderTopWidth
        {
            get { return GetInlineStyleFloat(StylePropertyID.BorderTopWidth); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderTopWidth, value, ve.sharedStyle.borderTopWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderTopWidth = ve.computedStyle.borderTopWidth.value;
                }
            }
        }

        StyleFloat IStyle.borderRightWidth
        {
            get { return GetInlineStyleFloat(StylePropertyID.BorderRightWidth); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderRightWidth, value, ve.sharedStyle.borderRightWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderRightWidth = ve.computedStyle.borderRightWidth.value;
                }
            }
        }

        StyleFloat IStyle.borderBottomWidth
        {
            get { return GetInlineStyleFloat(StylePropertyID.BorderBottomWidth); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderBottomWidth, value, ve.sharedStyle.borderBottomWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderBottomWidth = ve.computedStyle.borderBottomWidth.value;
                }
            }
        }

        StyleLength IStyle.borderTopLeftRadius
        {
            get { return GetInlineStyleLength(StylePropertyID.BorderTopLeftRadius); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderTopLeftRadius, value, ve.sharedStyle.borderTopLeftRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderTopRightRadius
        {
            get { return GetInlineStyleLength(StylePropertyID.BorderTopRightRadius); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderTopRightRadius, value, ve.sharedStyle.borderTopRightRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderBottomRightRadius
        {
            get { return GetInlineStyleLength(StylePropertyID.BorderBottomRightRadius); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderBottomRightRadius, value, ve.sharedStyle.borderBottomRightRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderBottomLeftRadius
        {
            get { return GetInlineStyleLength(StylePropertyID.BorderBottomLeftRadius); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderBottomLeftRadius, value, ve.sharedStyle.borderBottomLeftRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.paddingLeft
        {
            get { return GetInlineStyleLength(StylePropertyID.PaddingLeft); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PaddingLeft, value, ve.sharedStyle.paddingLeft))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingLeft = ve.computedStyle.paddingLeft.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingTop
        {
            get { return GetInlineStyleLength(StylePropertyID.PaddingTop); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PaddingTop, value, ve.sharedStyle.paddingTop))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingTop = ve.computedStyle.paddingTop.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingRight
        {
            get { return GetInlineStyleLength(StylePropertyID.PaddingRight); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PaddingRight, value, ve.sharedStyle.paddingRight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingRight = ve.computedStyle.paddingRight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingBottom
        {
            get { return GetInlineStyleLength(StylePropertyID.PaddingBottom); }
            set
            {
                if (SetInlineStyle(StylePropertyID.PaddingBottom, value, ve.sharedStyle.paddingBottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingBottom = ve.computedStyle.paddingBottom.ToYogaValue();
                }
            }
        }

        StyleEnum<Position> IStyle.position
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.Position);
                return new StyleEnum<Position>((Position)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.Position, value, ve.sharedStyle.position))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PositionType = (YogaPositionType)ve.computedStyle.position.value;
                }
            }
        }

        StyleEnum<Align> IStyle.alignSelf
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.AlignSelf);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.AlignSelf, value, ve.sharedStyle.alignSelf))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.AlignSelf = (YogaAlign)ve.computedStyle.alignSelf.value;
                }
            }
        }

        StyleEnum<TextAnchor> IStyle.unityTextAlign
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.UnityTextAlign);
                return new StyleEnum<TextAnchor>((TextAnchor)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.UnityTextAlign, value, ve.sharedStyle.unityTextAlign))
                {
                    ve.IncrementVersion(VersionChangeType.Styles |  VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<FontStyle> IStyle.unityFontStyleAndWeight
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.FontStyleAndWeight);
                return new StyleEnum<FontStyle>((FontStyle)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.FontStyleAndWeight, value, ve.sharedStyle.unityFontStyleAndWeight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles |  VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleFont IStyle.unityFont
        {
            get { return GetInlineStyleFont(StylePropertyID.Font); }
            set
            {
                if (SetInlineStyle(StylePropertyID.Font, value, ve.sharedStyle.unityFont))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleLength IStyle.fontSize
        {
            get { return GetInlineStyleLength(StylePropertyID.FontSize); }
            set
            {
                if (SetInlineStyle(StylePropertyID.FontSize, value, ve.sharedStyle.fontSize))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleEnum<WhiteSpace> IStyle.whiteSpace
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.WhiteSpace);
                return new StyleEnum<WhiteSpace>((WhiteSpace)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.WhiteSpace, value, ve.sharedStyle.whiteSpace))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleColor IStyle.color
        {
            get { return GetInlineStyleColor(StylePropertyID.Color); }
            set
            {
                if (SetInlineStyle(StylePropertyID.Color, value, ve.sharedStyle.color))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<FlexDirection> IStyle.flexDirection
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.FlexDirection);
                return new StyleEnum<FlexDirection>((FlexDirection)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.FlexDirection, value, ve.sharedStyle.flexDirection))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                    ve.yogaNode.FlexDirection = (YogaFlexDirection)ve.computedStyle.flexDirection.value;
                }
            }
        }

        StyleColor IStyle.backgroundColor
        {
            get { return GetInlineStyleColor(StylePropertyID.BackgroundColor); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BackgroundColor, value, ve.sharedStyle.backgroundColor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleColor IStyle.borderColor
        {
            get { return GetInlineStyleColor(StylePropertyID.BorderColor); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BorderColor, value, ve.sharedStyle.borderColor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleBackground IStyle.backgroundImage
        {
            get { return GetInlineStyleBackground(StylePropertyID.BackgroundImage); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BackgroundImage, value, ve.sharedStyle.backgroundImage))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<ScaleMode> IStyle.unityBackgroundScaleMode
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.BackgroundScaleMode);
                return new StyleEnum<ScaleMode>((ScaleMode)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.BackgroundScaleMode, value, ve.sharedStyle.unityBackgroundScaleMode))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleColor IStyle.unityBackgroundImageTintColor
        {
            get { return GetInlineStyleColor(StylePropertyID.BackgroundImageTintColor); }
            set
            {
                if (SetInlineStyle(StylePropertyID.BackgroundImageTintColor, value, ve.sharedStyle.unityBackgroundImageTintColor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }


        StyleEnum<Align> IStyle.alignItems
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.AlignItems);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.AlignItems, value, ve.sharedStyle.alignItems))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.AlignItems = (YogaAlign)ve.computedStyle.alignItems.value;
                }
            }
        }

        StyleEnum<Align> IStyle.alignContent
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.AlignContent);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.AlignContent, value, ve.sharedStyle.alignContent))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.AlignContent = (YogaAlign)ve.computedStyle.alignContent.value;
                }
            }
        }

        StyleEnum<Justify> IStyle.justifyContent
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.JustifyContent);
                return new StyleEnum<Justify>((Justify)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.JustifyContent, value, ve.sharedStyle.justifyContent))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.JustifyContent = (YogaJustify)ve.computedStyle.justifyContent.value;
                }
            }
        }

        StyleEnum<Wrap> IStyle.flexWrap
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.FlexWrap);
                return new StyleEnum<Wrap>((Wrap)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.FlexWrap, value, ve.sharedStyle.flexWrap))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Wrap = (YogaWrap)ve.computedStyle.flexWrap.value;
                }
            }
        }

        StyleInt IStyle.unitySliceLeft
        {
            get { return GetInlineStyleInt(StylePropertyID.SliceLeft); }
            set
            {
                if (SetInlineStyle(StylePropertyID.SliceLeft, value, ve.sharedStyle.unitySliceLeft))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceTop
        {
            get { return GetInlineStyleInt(StylePropertyID.SliceTop); }
            set
            {
                if (SetInlineStyle(StylePropertyID.SliceTop, value, ve.sharedStyle.unitySliceTop))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceRight
        {
            get { return GetInlineStyleInt(StylePropertyID.SliceRight); }
            set
            {
                if (SetInlineStyle(StylePropertyID.SliceRight, value, ve.sharedStyle.unitySliceRight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceBottom
        {
            get { return GetInlineStyleInt(StylePropertyID.SliceBottom); }
            set
            {
                if (SetInlineStyle(StylePropertyID.SliceBottom, value, ve.sharedStyle.unitySliceBottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleFloat IStyle.opacity
        {
            get { return GetInlineStyleFloat(StylePropertyID.Opacity); }
            set
            {
                if (SetInlineStyle(StylePropertyID.Opacity, value, ve.sharedStyle.opacity))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<Visibility> IStyle.visibility
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.Visibility);
                return new StyleEnum<Visibility>((Visibility)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.Visibility, value, ve.sharedStyle.visibility))
                {
                    ve.IncrementVersion(VersionChangeType.Styles |  VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleCursor IStyle.cursor
        {
            get
            {
                var inlineCursor = new StyleCursor();
                if (TryGetInlineCursor(ref inlineCursor))
                    return inlineCursor;
                return StyleKeyword.Null;
            }
            set
            {
                if (SetInlineCursor(value, ve.sharedStyle.cursor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles);
                }
            }
        }

        StyleEnum<DisplayStyle> IStyle.display
        {
            get
            {
                var tmp = GetInlineStyleInt(StylePropertyID.Display);
                return new StyleEnum<DisplayStyle>((DisplayStyle)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetInlineStyle(StylePropertyID.Display, value, ve.sharedStyle.display))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Display = (YogaDisplay)ve.computedStyle.display.value;
                }
            }
        }

        private StyleLength GetInlineStyleLength(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetInlineStyleValue(id, ref inline))
                return new StyleLength(inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        private StyleFloat GetInlineStyleFloat(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetInlineStyleValue(id, ref inline))
                return new StyleFloat(inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        private StyleInt GetInlineStyleInt(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetInlineStyleValue(id, ref inline))
                return new StyleInt((int)inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        private StyleColor GetInlineStyleColor(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetInlineStyleValue(id, ref inline))
                return new StyleColor(inline.color, inline.keyword);
            return StyleKeyword.Null;
        }

        private StyleBackground GetInlineStyleBackground(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetInlineStyleValue(id, ref inline))
            {
                var texture = inline.resource.IsAllocated ? inline.resource.Target as Texture2D : null;
                return new StyleBackground(texture, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        private StyleFont GetInlineStyleFont(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetInlineStyleValue(id, ref inline))
            {
                var font = inline.resource.IsAllocated ? inline.resource.Target as Font : null;
                return new StyleFont(font, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        private bool SetInlineStyle(StylePropertyID id, StyleLength inlineValue, StyleLength sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetInlineStyleValue(id, ref sv))
            {
                if (sv.length == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.length = inlineValue.value;

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.length = sharedValue.value;
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineStyle(StylePropertyID id, StyleFloat inlineValue, StyleFloat sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetInlineStyleValue(id, ref sv))
            {
                if (sv.number == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = inlineValue.value;

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.number = sharedValue.value;
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineStyle(StylePropertyID id, StyleInt inlineValue, StyleInt sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetInlineStyleValue(id, ref sv))
            {
                if (sv.number == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = inlineValue.value;

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.number = sharedValue.value;
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineStyle(StylePropertyID id, StyleColor inlineValue, StyleColor sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetInlineStyleValue(id, ref sv))
            {
                if (sv.color == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.color = inlineValue.value;

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.color = sharedValue.value;
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineStyle<T>(StylePropertyID id, StyleEnum<T> inlineValue, StyleInt sharedValue) where T : struct, IConvertible
        {
            var sv = new StyleValue();
            int intValue = inlineValue.value.ToInt32(CultureInfo.InvariantCulture);
            if (TryGetInlineStyleValue(id, ref sv))
            {
                if (sv.number == intValue && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = intValue;

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.number = sharedValue.value;
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineStyle(StylePropertyID id, StyleBackground inlineValue, StyleBackground sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetInlineStyleValue(id, ref sv))
            {
                var texture = sv.resource.IsAllocated ? sv.resource.Target as Texture2D : null;
                if (texture == inlineValue.value.texture && sv.keyword == inlineValue.keyword)
                    return false;

                if (sv.resource.IsAllocated)
                    sv.resource.Free();
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.resource = inlineValue.value.texture != null ? GCHandle.Alloc(inlineValue.value.texture) : new GCHandle();

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.resource = sharedValue.value.texture != null ? GCHandle.Alloc(sharedValue.value.texture) : new GCHandle();
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineStyle(StylePropertyID id, StyleFont inlineValue, StyleFont sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetInlineStyleValue(id, ref sv))
            {
                if (sv.resource.IsAllocated)
                {
                    var font = sv.resource.IsAllocated ? sv.resource.Target as Font : null;
                    if (font == inlineValue.value && sv.keyword == inlineValue.keyword)
                        return false;

                    if (sv.resource.IsAllocated)
                        sv.resource.Free();
                }
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.resource = inlineValue.value != null ? GCHandle.Alloc(inlineValue.value) : new GCHandle();

            SetInlineStyle(sv);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (inlineValue.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                sv.keyword = sharedValue.keyword;
                sv.resource = sharedValue.value != null ? GCHandle.Alloc(sharedValue.value) : new GCHandle();
            }

            ApplyStyleValue(sv, specificity);
            return true;
        }

        private bool SetInlineCursor(StyleCursor inlineValue, StyleCursor sharedValue)
        {
            var styleCursor = new StyleCursor();
            if (TryGetInlineCursor(ref styleCursor))
            {
                if (styleCursor.value == inlineValue.value && styleCursor.keyword == inlineValue.keyword)
                    return false;
            }

            styleCursor.value = inlineValue.value;
            styleCursor.keyword = inlineValue.keyword;

            SetInlineCursor(styleCursor);

            int specificity = StyleValueExtensions.InlineSpecificity;
            if (styleCursor.keyword == StyleKeyword.Null)
            {
                specificity = sharedValue.specificity;
                styleCursor.keyword = sharedValue.keyword;
                styleCursor.value = sharedValue.value;
            }

            ve.specifiedStyle.ApplyStyleCursor(styleCursor, specificity);
            return true;
        }

        private void ApplyStyleValue(StyleValue value, int specificity)
        {
            ve.specifiedStyle.ApplyStyleValue(value.id, value, specificity);
        }

        public bool TryGetInlineStyleValue(StylePropertyID id, ref StyleValue value)
        {
            value.id = StylePropertyID.Unknown;
            foreach (var inlineStyle in m_InlineStyleValues)
            {
                if (inlineStyle.id == id)
                {
                    value = inlineStyle;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetInlineCursor(ref StyleCursor value)
        {
            if (m_HasInlineCursor)
            {
                value = m_InlineCursor;
                return true;
            }
            return false;
        }

        public void SetInlineStyle(StyleValue value)
        {
            for (int i = 0; i < m_InlineStyleValues.Count; i++)
            {
                if (m_InlineStyleValues[i].id == value.id)
                {
                    m_InlineStyleValues[i] = value;
                    return;
                }
            }

            m_InlineStyleValues.Add(value);
        }

        public void SetInlineCursor(StyleCursor value)
        {
            m_InlineCursor = value;
            m_HasInlineCursor = true;
        }
    }
}
