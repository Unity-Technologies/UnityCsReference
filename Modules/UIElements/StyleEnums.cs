// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Yoga;

namespace UnityEngine.Experimental.UIElements.StyleEnums
{
    // enum Position represents the accepted values for
    // the `position` property in USS files.
    // Both `position` and `-unity-position` are stored in VisualElementStylesData.positionType
    enum Position
    {
        Relative = YogaPositionType.Relative,
        Absolute = YogaPositionType.Absolute,
    }

    // enum PositionType represents the accepted values for
    // the `-unity-position` property in USS files.
    // Both `position` and `-unity-position` are stored in VisualElementStylesData.positionType
    public enum PositionType
    {
        Relative = YogaPositionType.Relative,
        Absolute = YogaPositionType.Absolute,
        Manual
    }

    public enum Overflow
    {
        Visible = YogaOverflow.Visible,
        Hidden = YogaOverflow.Hidden
    }

    public enum FlexDirection
    {
        Column = YogaFlexDirection.Column,
        ColumnReverse = YogaFlexDirection.ColumnReverse,
        Row = YogaFlexDirection.Row,
        RowReverse = YogaFlexDirection.RowReverse
    }

    public enum Wrap
    {
        NoWrap = YogaWrap.NoWrap,
        Wrap = YogaWrap.Wrap,
        WrapReverse = YogaWrap.WrapReverse
    }

    public enum Align
    {
        Auto = YogaAlign.Auto,
        FlexStart = YogaAlign.FlexStart,
        Center = YogaAlign.Center,
        FlexEnd = YogaAlign.FlexEnd,
        Stretch = YogaAlign.Stretch
    }

    public enum Justify
    {
        FlexStart = YogaJustify.FlexStart,
        Center = YogaJustify.Center,
        FlexEnd = YogaJustify.FlexEnd,
        SpaceBetween = YogaJustify.SpaceBetween,
        SpaceAround = YogaJustify.SpaceAround
    }

    public enum ImageScaleMode
    {
        StretchToFill = 0,
        ScaleAndCrop = 1,
        ScaleToFit = 2
    }

    public enum Visibility
    {
        Visible = 0,
        Hidden = 1
    }
}
