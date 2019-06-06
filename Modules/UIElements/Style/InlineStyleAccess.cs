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
    internal class StyleValueCollection
    {
        internal List<StyleValue> m_Values = new List<StyleValue>();

        public StyleLength GetStyleLength(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleLength(inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleFloat GetStyleFloat(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleFloat(inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleInt GetStyleInt(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleInt((int)inline.number, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleColor GetStyleColor(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
                return new StyleColor(inline.color, inline.keyword);
            return StyleKeyword.Null;
        }

        public StyleBackground GetStyleBackground(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
            {
                var texture = inline.resource.IsAllocated ? inline.resource.Target as Texture2D : null;
                return new StyleBackground(texture, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        public StyleFont GetStyleFont(StylePropertyID id)
        {
            var inline = new StyleValue();
            if (TryGetStyleValue(id, ref inline))
            {
                var font = inline.resource.IsAllocated ? inline.resource.Target as Font : null;
                return new StyleFont(font, inline.keyword);
            }
            return StyleKeyword.Null;
        }

        public bool TryGetStyleValue(StylePropertyID id, ref StyleValue value)
        {
            value.id = StylePropertyID.Unknown;
            foreach (var inlineStyle in m_Values)
            {
                if (inlineStyle.id == id)
                {
                    value = inlineStyle;
                    return true;
                }
            }
            return false;
        }

        public void SetStyleValue(StyleValue value)
        {
            for (int i = 0; i < m_Values.Count; i++)
            {
                if (m_Values[i].id == value.id)
                {
                    m_Values[i] = value;
                    return;
                }
            }

            m_Values.Add(value);
        }
    }

    internal class InlineStyleAccess : StyleValueCollection, IStyle
    {
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
            if (TryGetStyleValue(StylePropertyID.BackgroundImage, ref inlineValue))
            {
                if (inlineValue.resource.IsAllocated)
                    inlineValue.resource.Free();
            }
            if (TryGetStyleValue(StylePropertyID.Font, ref inlineValue))
            {
                if (inlineValue.resource.IsAllocated)
                    inlineValue.resource.Free();
            }
        }

        StyleLength IStyle.width
        {
            get { return GetStyleLength(StylePropertyID.Width); }
            set
            {
                if (SetStyleValue(StylePropertyID.Width, value, ve.sharedStyle.width))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Width = ve.computedStyle.width.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.height
        {
            get { return GetStyleLength(StylePropertyID.Height); }
            set
            {
                if (SetStyleValue(StylePropertyID.Height, value, ve.sharedStyle.height))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Height = ve.computedStyle.height.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.maxWidth
        {
            get { return GetStyleLength(StylePropertyID.MaxWidth); }
            set
            {
                if (SetStyleValue(StylePropertyID.MaxWidth, value, ve.sharedStyle.maxWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MaxWidth = ve.computedStyle.maxWidth.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.maxHeight
        {
            get { return GetStyleLength(StylePropertyID.MaxHeight); }
            set
            {
                if (SetStyleValue(StylePropertyID.MaxHeight, value, ve.sharedStyle.maxHeight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MaxHeight = ve.computedStyle.maxHeight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.minWidth
        {
            get { return GetStyleLength(StylePropertyID.MinWidth); }
            set
            {
                if (SetStyleValue(StylePropertyID.MinWidth, value, ve.sharedStyle.minWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MinWidth = ve.computedStyle.minWidth.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.minHeight
        {
            get { return GetStyleLength(StylePropertyID.MinHeight); }
            set
            {
                if (SetStyleValue(StylePropertyID.MinHeight, value, ve.sharedStyle.minHeight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MinHeight = ve.computedStyle.minHeight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.flexBasis
        {
            get { return GetStyleLength(StylePropertyID.FlexBasis); }
            set
            {
                if (SetStyleValue(StylePropertyID.FlexBasis, value, ve.sharedStyle.flexBasis))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexBasis = ve.computedStyle.flexBasis.ToYogaValue();
                }
            }
        }

        StyleFloat IStyle.flexGrow
        {
            get { return GetStyleFloat(StylePropertyID.FlexGrow); }
            set
            {
                if (SetStyleValue(StylePropertyID.FlexGrow, value, ve.sharedStyle.flexGrow))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.FlexGrow = ve.computedStyle.flexGrow.value;
                }
            }
        }

        StyleFloat IStyle.flexShrink
        {
            get { return GetStyleFloat(StylePropertyID.FlexShrink); }
            set
            {
                if (SetStyleValue(StylePropertyID.FlexShrink, value, ve.sharedStyle.flexShrink))
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
                var tmp = GetStyleInt(StylePropertyID.Overflow);
                return new StyleEnum<Overflow>((Overflow)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.Overflow, value, ve.sharedStyle.overflow))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Overflow = (YogaOverflow)ve.computedStyle.overflow.value;
                }
            }
        }

        StyleEnum<OverflowClipBox> IStyle.unityOverflowClipBox
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.OverflowClipBox);
                return new StyleEnum<OverflowClipBox>((OverflowClipBox)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.OverflowClipBox, value, ve.sharedStyle.unityOverflowClipBox))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.left
        {
            get { return GetStyleLength(StylePropertyID.PositionLeft); }
            set
            {
                if (SetStyleValue(StylePropertyID.PositionLeft, value, ve.sharedStyle.left))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Left = ve.computedStyle.left.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.top
        {
            get { return GetStyleLength(StylePropertyID.PositionTop); }
            set
            {
                if (SetStyleValue(StylePropertyID.PositionTop, value, ve.sharedStyle.top))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Top = ve.computedStyle.top.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.right
        {
            get { return GetStyleLength(StylePropertyID.PositionRight); }
            set
            {
                if (SetStyleValue(StylePropertyID.PositionRight, value, ve.sharedStyle.right))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Right = ve.computedStyle.right.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.bottom
        {
            get { return GetStyleLength(StylePropertyID.PositionBottom); }
            set
            {
                if (SetStyleValue(StylePropertyID.PositionBottom, value, ve.sharedStyle.bottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Bottom = ve.computedStyle.bottom.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginLeft
        {
            get { return GetStyleLength(StylePropertyID.MarginLeft); }
            set
            {
                if (SetStyleValue(StylePropertyID.MarginLeft, value, ve.sharedStyle.marginLeft))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginLeft = ve.computedStyle.marginLeft.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginTop
        {
            get { return GetStyleLength(StylePropertyID.MarginTop); }
            set
            {
                if (SetStyleValue(StylePropertyID.MarginTop, value, ve.sharedStyle.marginTop))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginTop = ve.computedStyle.marginTop.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginRight
        {
            get { return GetStyleLength(StylePropertyID.MarginRight); }
            set
            {
                if (SetStyleValue(StylePropertyID.MarginRight, value, ve.sharedStyle.marginRight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginRight = ve.computedStyle.marginRight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.marginBottom
        {
            get { return GetStyleLength(StylePropertyID.MarginBottom); }
            set
            {
                if (SetStyleValue(StylePropertyID.MarginBottom, value, ve.sharedStyle.marginBottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.MarginBottom = ve.computedStyle.marginBottom.ToYogaValue();
                }
            }
        }

        StyleFloat IStyle.borderLeftWidth
        {
            get { return GetStyleFloat(StylePropertyID.BorderLeftWidth); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderLeftWidth, value, ve.sharedStyle.borderLeftWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderLeftWidth = ve.computedStyle.borderLeftWidth.value;
                }
            }
        }

        StyleFloat IStyle.borderTopWidth
        {
            get { return GetStyleFloat(StylePropertyID.BorderTopWidth); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderTopWidth, value, ve.sharedStyle.borderTopWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderTopWidth = ve.computedStyle.borderTopWidth.value;
                }
            }
        }

        StyleFloat IStyle.borderRightWidth
        {
            get { return GetStyleFloat(StylePropertyID.BorderRightWidth); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderRightWidth, value, ve.sharedStyle.borderRightWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderRightWidth = ve.computedStyle.borderRightWidth.value;
                }
            }
        }

        StyleFloat IStyle.borderBottomWidth
        {
            get { return GetStyleFloat(StylePropertyID.BorderBottomWidth); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderBottomWidth, value, ve.sharedStyle.borderBottomWidth))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.BorderBottomWidth = ve.computedStyle.borderBottomWidth.value;
                }
            }
        }

        StyleLength IStyle.borderTopLeftRadius
        {
            get { return GetStyleLength(StylePropertyID.BorderTopLeftRadius); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderTopLeftRadius, value, ve.sharedStyle.borderTopLeftRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderTopRightRadius
        {
            get { return GetStyleLength(StylePropertyID.BorderTopRightRadius); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderTopRightRadius, value, ve.sharedStyle.borderTopRightRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderBottomRightRadius
        {
            get { return GetStyleLength(StylePropertyID.BorderBottomRightRadius); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderBottomRightRadius, value, ve.sharedStyle.borderBottomRightRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.borderBottomLeftRadius
        {
            get { return GetStyleLength(StylePropertyID.BorderBottomLeftRadius); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderBottomLeftRadius, value, ve.sharedStyle.borderBottomLeftRadius))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleLength IStyle.paddingLeft
        {
            get { return GetStyleLength(StylePropertyID.PaddingLeft); }
            set
            {
                if (SetStyleValue(StylePropertyID.PaddingLeft, value, ve.sharedStyle.paddingLeft))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingLeft = ve.computedStyle.paddingLeft.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingTop
        {
            get { return GetStyleLength(StylePropertyID.PaddingTop); }
            set
            {
                if (SetStyleValue(StylePropertyID.PaddingTop, value, ve.sharedStyle.paddingTop))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingTop = ve.computedStyle.paddingTop.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingRight
        {
            get { return GetStyleLength(StylePropertyID.PaddingRight); }
            set
            {
                if (SetStyleValue(StylePropertyID.PaddingRight, value, ve.sharedStyle.paddingRight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.PaddingRight = ve.computedStyle.paddingRight.ToYogaValue();
                }
            }
        }

        StyleLength IStyle.paddingBottom
        {
            get { return GetStyleLength(StylePropertyID.PaddingBottom); }
            set
            {
                if (SetStyleValue(StylePropertyID.PaddingBottom, value, ve.sharedStyle.paddingBottom))
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
                var tmp = GetStyleInt(StylePropertyID.Position);
                return new StyleEnum<Position>((Position)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.Position, value, ve.sharedStyle.position))
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
                var tmp = GetStyleInt(StylePropertyID.AlignSelf);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.AlignSelf, value, ve.sharedStyle.alignSelf))
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
                var tmp = GetStyleInt(StylePropertyID.UnityTextAlign);
                return new StyleEnum<TextAnchor>((TextAnchor)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.UnityTextAlign, value, ve.sharedStyle.unityTextAlign))
                {
                    ve.IncrementVersion(VersionChangeType.Styles |  VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<FontStyle> IStyle.unityFontStyleAndWeight
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.FontStyleAndWeight);
                return new StyleEnum<FontStyle>((FontStyle)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.FontStyleAndWeight, value, ve.sharedStyle.unityFontStyleAndWeight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles |  VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleFont IStyle.unityFont
        {
            get { return GetStyleFont(StylePropertyID.Font); }
            set
            {
                if (SetStyleValue(StylePropertyID.Font, value, ve.sharedStyle.unityFont))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleLength IStyle.fontSize
        {
            get { return GetStyleLength(StylePropertyID.FontSize); }
            set
            {
                if (SetStyleValue(StylePropertyID.FontSize, value, ve.sharedStyle.fontSize))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleEnum<WhiteSpace> IStyle.whiteSpace
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.WhiteSpace);
                return new StyleEnum<WhiteSpace>((WhiteSpace)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.WhiteSpace, value, ve.sharedStyle.whiteSpace))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout);
                }
            }
        }

        StyleColor IStyle.color
        {
            get { return GetStyleColor(StylePropertyID.Color); }
            set
            {
                if (SetStyleValue(StylePropertyID.Color, value, ve.sharedStyle.color))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<FlexDirection> IStyle.flexDirection
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.FlexDirection);
                return new StyleEnum<FlexDirection>((FlexDirection)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.FlexDirection, value, ve.sharedStyle.flexDirection))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                    ve.yogaNode.FlexDirection = (YogaFlexDirection)ve.computedStyle.flexDirection.value;
                }
            }
        }

        StyleColor IStyle.backgroundColor
        {
            get { return GetStyleColor(StylePropertyID.BackgroundColor); }
            set
            {
                if (SetStyleValue(StylePropertyID.BackgroundColor, value, ve.sharedStyle.backgroundColor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleColor IStyle.borderColor
        {
            get { return GetStyleColor(StylePropertyID.BorderColor); }
            set
            {
                if (SetStyleValue(StylePropertyID.BorderColor, value, ve.sharedStyle.borderColor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleBackground IStyle.backgroundImage
        {
            get { return GetStyleBackground(StylePropertyID.BackgroundImage); }
            set
            {
                if (SetStyleValue(StylePropertyID.BackgroundImage, value, ve.sharedStyle.backgroundImage))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<ScaleMode> IStyle.unityBackgroundScaleMode
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.BackgroundScaleMode);
                return new StyleEnum<ScaleMode>((ScaleMode)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.BackgroundScaleMode, value, ve.sharedStyle.unityBackgroundScaleMode))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleColor IStyle.unityBackgroundImageTintColor
        {
            get { return GetStyleColor(StylePropertyID.BackgroundImageTintColor); }
            set
            {
                if (SetStyleValue(StylePropertyID.BackgroundImageTintColor, value, ve.sharedStyle.unityBackgroundImageTintColor))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }


        StyleEnum<Align> IStyle.alignItems
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.AlignItems);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.AlignItems, value, ve.sharedStyle.alignItems))
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
                var tmp = GetStyleInt(StylePropertyID.AlignContent);
                return new StyleEnum<Align>((Align)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.AlignContent, value, ve.sharedStyle.alignContent))
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
                var tmp = GetStyleInt(StylePropertyID.JustifyContent);
                return new StyleEnum<Justify>((Justify)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.JustifyContent, value, ve.sharedStyle.justifyContent))
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
                var tmp = GetStyleInt(StylePropertyID.FlexWrap);
                return new StyleEnum<Wrap>((Wrap)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.FlexWrap, value, ve.sharedStyle.flexWrap))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Wrap = (YogaWrap)ve.computedStyle.flexWrap.value;
                }
            }
        }

        StyleInt IStyle.unitySliceLeft
        {
            get { return GetStyleInt(StylePropertyID.SliceLeft); }
            set
            {
                if (SetStyleValue(StylePropertyID.SliceLeft, value, ve.sharedStyle.unitySliceLeft))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceTop
        {
            get { return GetStyleInt(StylePropertyID.SliceTop); }
            set
            {
                if (SetStyleValue(StylePropertyID.SliceTop, value, ve.sharedStyle.unitySliceTop))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceRight
        {
            get { return GetStyleInt(StylePropertyID.SliceRight); }
            set
            {
                if (SetStyleValue(StylePropertyID.SliceRight, value, ve.sharedStyle.unitySliceRight))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleInt IStyle.unitySliceBottom
        {
            get { return GetStyleInt(StylePropertyID.SliceBottom); }
            set
            {
                if (SetStyleValue(StylePropertyID.SliceBottom, value, ve.sharedStyle.unitySliceBottom))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleFloat IStyle.opacity
        {
            get { return GetStyleFloat(StylePropertyID.Opacity); }
            set
            {
                if (SetStyleValue(StylePropertyID.Opacity, value, ve.sharedStyle.opacity))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Repaint);
                }
            }
        }

        StyleEnum<Visibility> IStyle.visibility
        {
            get
            {
                var tmp = GetStyleInt(StylePropertyID.Visibility);
                return new StyleEnum<Visibility>((Visibility)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.Visibility, value, ve.sharedStyle.visibility))
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
                var tmp = GetStyleInt(StylePropertyID.Display);
                return new StyleEnum<DisplayStyle>((DisplayStyle)tmp.value, tmp.keyword);
            }
            set
            {
                if (SetStyleValue(StylePropertyID.Display, value, ve.sharedStyle.display))
                {
                    ve.IncrementVersion(VersionChangeType.Styles | VersionChangeType.Layout);
                    ve.yogaNode.Display = (YogaDisplay)ve.computedStyle.display.value;
                }
            }
        }


        private bool SetStyleValue(StylePropertyID id, StyleLength inlineValue, StyleLength sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.length == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.length = inlineValue.value;

            SetStyleValue(sv);

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

        private bool SetStyleValue(StylePropertyID id, StyleFloat inlineValue, StyleFloat sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.number == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = inlineValue.value;

            SetStyleValue(sv);

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

        private bool SetStyleValue(StylePropertyID id, StyleInt inlineValue, StyleInt sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.number == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = inlineValue.value;

            SetStyleValue(sv);

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

        private bool SetStyleValue(StylePropertyID id, StyleColor inlineValue, StyleColor sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.color == inlineValue.value && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.color = inlineValue.value;

            SetStyleValue(sv);

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

        private bool SetStyleValue<T>(StylePropertyID id, StyleEnum<T> inlineValue, StyleInt sharedValue) where T : struct, IConvertible
        {
            var sv = new StyleValue();
            int intValue = inlineValue.value.ToInt32(CultureInfo.InvariantCulture);
            if (TryGetStyleValue(id, ref sv))
            {
                if (sv.number == intValue && sv.keyword == inlineValue.keyword)
                    return false;
            }

            sv.id = id;
            sv.keyword = inlineValue.keyword;
            sv.number = intValue;

            SetStyleValue(sv);

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

        private bool SetStyleValue(StylePropertyID id, StyleBackground inlineValue, StyleBackground sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
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

            SetStyleValue(sv);

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

        private bool SetStyleValue(StylePropertyID id, StyleFont inlineValue, StyleFont sharedValue)
        {
            var sv = new StyleValue();
            if (TryGetStyleValue(id, ref sv))
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

            SetStyleValue(sv);

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

        public bool TryGetInlineCursor(ref StyleCursor value)
        {
            if (m_HasInlineCursor)
            {
                value = m_InlineCursor;
                return true;
            }
            return false;
        }

        public void SetInlineCursor(StyleCursor value)
        {
            m_InlineCursor = value;
            m_HasInlineCursor = true;
        }
    }
}
