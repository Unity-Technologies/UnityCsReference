// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.UIElements.StyleSheets
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("id = {id}, keyword = {keyword}, number = {number}, boolean = {boolean}, color = {color}, resource = {resource}")]
    internal struct StyleValue
    {
        [FieldOffset(0)]
        public StylePropertyID id;

        [FieldOffset(4)]
        public StyleKeyword keyword;

        [FieldOffset(8)]
        public float number;   // float, int, enum
        [FieldOffset(8)]
        public Length length;
        [FieldOffset(8)]
        public Color color;
        [FieldOffset(8)]
        public GCHandle resource;
    }

    internal enum StylePropertyID
    {
        Unknown = -1,
        // All float values
        MarginLeft,
        MarginTop,
        MarginRight,
        MarginBottom,
        PaddingLeft,
        PaddingTop,
        PaddingRight,
        PaddingBottom,
        Position,
        PositionLeft,
        PositionTop,
        PositionRight,
        PositionBottom,
        Width,
        Height,
        MinWidth,
        MinHeight,
        MaxWidth,
        MaxHeight,
        FlexBasis,
        FlexGrow,
        FlexShrink,
        BorderLeftWidth,
        BorderTopWidth,
        BorderRightWidth,
        BorderBottomWidth,
        BorderTopLeftRadius,
        BorderTopRightRadius,
        BorderBottomRightRadius,
        BorderBottomLeftRadius,
        // All enum values
        FlexDirection,
        FlexWrap,
        JustifyContent,
        AlignContent,
        AlignSelf,
        AlignItems,
        UnityTextAlign,
        WhiteSpace,
        Font,
        FontSize,
        FontStyleAndWeight,
        BackgroundScaleMode,
        Cursor,
        Visibility,
        Display,
        // All string values
        BackgroundImage,
        // All color values
        Color,
        BackgroundColor,
        BorderColor,
        Overflow,
        SliceLeft,
        SliceTop,
        SliceRight,
        SliceBottom,
        Opacity,
        // Shorthand value
        BorderRadius,
        BorderWidth,
        Flex,
        Margin,
        Padding,
        // Always leave as last value
        Custom
    }

    internal enum StylePropertyApplyMode
    {
        Copy,
        CopyIfEqualOrGreaterSpecificity,
        CopyIfNotInline
    }
}
