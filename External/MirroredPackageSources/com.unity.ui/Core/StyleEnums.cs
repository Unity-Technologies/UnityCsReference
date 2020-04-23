using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    public enum Position
    {
        Relative = YogaPositionType.Relative,
        Absolute = YogaPositionType.Absolute,
    }

    public enum Overflow
    {
        Visible = YogaOverflow.Visible,
        Hidden = YogaOverflow.Hidden
    }

    internal enum OverflowInternal
    {
        Visible = YogaOverflow.Visible,
        Hidden = YogaOverflow.Hidden,
        Scroll = YogaOverflow.Scroll,
    }

    public enum OverflowClipBox
    {
        PaddingBox,
        ContentBox
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

    public enum TextOverflowPosition
    {
        End = 0,
        Start = 1,
        Middle = 2
    }

    public enum TextOverflow
    {
        Clip = 0,
        Ellipsis = 1
    }

    public enum Visibility
    {
        Visible = 0,
        Hidden = 1
    }

    public enum WhiteSpace
    {
        Normal = 0,
        NoWrap = 1
    }

    // Display already exists in UnityEngine and would force fully qualified usage every time
    public enum DisplayStyle
    {
        Flex = YogaDisplay.Flex,
        None = YogaDisplay.None
    }
}
