// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.CSSLayout;

namespace UnityEngine.Experimental.UIElements.StyleEnums
{
    public enum PositionType
    {
        Relative = CSSPositionType.Relative,
        Absolute = CSSPositionType.Absolute,
        Manual
    }

    public enum Overflow
    {
        Visible = CSSOverflow.Visible,
        Scroll = CSSOverflow.Scroll,
        Hidden = CSSOverflow.Hidden
    }

    public enum FlexDirection
    {
        Column = CSSFlexDirection.Column,
        ColumnReverse = CSSFlexDirection.ColumnReverse,
        Row = CSSFlexDirection.Row,
        RowReverse = CSSFlexDirection.RowReverse
    }

    public enum Wrap
    {
        NoWrap = CSSWrap.NoWrap,
        Wrap = CSSWrap.Wrap
    }

    public enum Align
    {
        Auto = CSSAlign.Auto,
        FlexStart = CSSAlign.FlexStart,
        Center = CSSAlign.Center,
        FlexEnd = CSSAlign.FlexEnd,
        Stretch = CSSAlign.Stretch
    }

    public enum Justify
    {
        FlexStart = CSSJustify.FlexStart,
        Center = CSSJustify.Center,
        FlexEnd = CSSJustify.FlexEnd,
        SpaceBetween = CSSJustify.SpaceBetween,
        SpaceAround = CSSJustify.SpaceAround
    }

    public enum ImageScaleMode
    {
        StretchToFill = 0,
        ScaleAndCrop = 1,
        ScaleToFit = 2
    }
}
