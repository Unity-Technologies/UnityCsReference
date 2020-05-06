using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defaines how the position values are interpreted by the layout engine.
    /// </summary>
    public enum Position
    {
        /// <summary>
        /// The element is positioned in relation to its default box as calculated by layout.
        /// </summary>
        Relative = YogaPositionType.Relative,
        /// <summary>
        /// The element is positioned in relation to its parent box and does not contribute to the layout anymore.
        /// </summary>
        Absolute = YogaPositionType.Absolute,
    }

    /// <summary>
    /// Defines what should happend if content overflows an element bounds.
    /// </summary>
    public enum Overflow
    {
        /// <summary>
        /// The overflow is not clipped. It renders outside the element's box. Default Value.
        /// </summary>
        Visible = YogaOverflow.Visible,
        /// <summary>
        /// The overflow is clipped, and the rest of the content will be invisible.
        /// </summary>
        Hidden = YogaOverflow.Hidden
    }

    internal enum OverflowInternal
    {
        Visible = YogaOverflow.Visible,
        Hidden = YogaOverflow.Hidden,
        Scroll = YogaOverflow.Scroll,
    }

    /// <summary>
    /// Boxes against which the VisualElement content is clipped.
    /// </summary>
    public enum OverflowClipBox
    {
        /// <summary>
        /// Clip the content against the box outside the padding areas but inside the borders.
        /// </summary>
        PaddingBox,
        /// <summary>
        /// Clip the content against the box inside the padding areas.
        /// </summary>
        ContentBox
    }

    /// <summary>
    /// Defines the main-axis of the flex layout.
    /// </summary>
    public enum FlexDirection
    {
        /// <summary>
        /// Top to Bottom.
        /// </summary>
        Column = YogaFlexDirection.Column,
        /// <summary>
        /// Bottom to Top.
        /// </summary>
        ColumnReverse = YogaFlexDirection.ColumnReverse,
        /// <summary>
        /// Left to Right.
        /// </summary>
        Row = YogaFlexDirection.Row,
        /// <summary>
        /// Right to Left.
        /// </summary>
        RowReverse = YogaFlexDirection.RowReverse
    }

    /// <summary>
    /// By default, items will all try to fit onto one line. You can change that and allow the items to wrap as needed with this property.
    /// </summary>
    public enum Wrap
    {
        /// <summary>
        /// All items will be on one line. Default Value.
        /// </summary>
        NoWrap = YogaWrap.NoWrap,
        /// <summary>
        /// All items will be on one line. Default Value.
        /// </summary>
        Wrap = YogaWrap.Wrap,
        /// <summary>
        /// Items will wrap onto multiple lines from bottom to top.
        /// </summary>
        WrapReverse = YogaWrap.WrapReverse
    }

    /// <summary>
    /// Defines the alignement behavior along an axis.
    /// </summary>
    public enum Align
    {
        /// <summary>
        /// Let Flex decide.
        /// </summary>
        Auto = YogaAlign.Auto,
        /// <summary>
        /// Start margin of the item is placed at the start of the axis.
        /// </summary>
        FlexStart = YogaAlign.FlexStart,
        /// <summary>
        /// Items are centered on the axis.
        /// </summary>
        Center = YogaAlign.Center,
        /// <summary>
        /// End margin of the item is placed at the end of the axis.
        /// </summary>
        FlexEnd = YogaAlign.FlexEnd,
        /// <summary>
        /// Default. stretch to fill the axis while respecting min/max values.
        /// </summary>
        Stretch = YogaAlign.Stretch
    }

    /// <summary>
    /// Defines the alignment along the main axis, how is extra space distributed.
    /// </summary>
    public enum Justify
    {
        /// <summary>
        /// Items are packed toward the start line. Default Value.
        /// </summary>
        FlexStart = YogaJustify.FlexStart,
        /// <summary>
        /// Items are centered along the line.
        /// </summary>
        Center = YogaJustify.Center,
        /// <summary>
        /// Items are packed toward the end line.
        /// </summary>
        FlexEnd = YogaJustify.FlexEnd,
        /// <summary>
        /// Items are evenly distributed in the line; first item is on the start line, last item on the end line.
        /// </summary>
        SpaceBetween = YogaJustify.SpaceBetween,
        /// <summary>
        /// Items are evenly distributed in the line with equal space around them.
        /// </summary>
        SpaceAround = YogaJustify.SpaceAround
    }

    /// <summary>
    /// Specifies which part of the text the Element replaces with an ellipsis when textOverflow is set to TextOverflow.Ellipsis.
    /// </summary>
    public enum TextOverflowPosition
    {
        /// <summary>
        /// The ellipsis replaces content at the end of the text. This is the default value.
        /// </summary>
        End = 0,
        /// <summary>
        /// The ellipsis replaces content at the beginning of the text.
        /// </summary>
        Start = 1,
        /// <summary>
        /// The ellipsis replaces content in the middle of the text.
        /// </summary>
        Middle = 2
    }

    /// <summary>
    /// Specifies how the text Element treats hidden overflow content.
    /// </summary>
    public enum TextOverflow
    {
        /// <summary>
        /// The Element clips overflow content and hides it. This is the default value.
        /// </summary>
        Clip = 0,
        /// <summary>
        /// The Element clips overflow content and hides it, but displays an ellipsis ("...") to indicate that clipped content exists.
        /// </summary>
        Ellipsis = 1
    }

    /// <summary>
    /// Specifies whether or not a VisualElement is visible.
    /// </summary>
    public enum Visibility
    {
        /// <summary>
        /// The VisualElement is visible. Default Value.
        /// </summary>
        Visible = 0,
        /// <summary>
        /// The VisualElement is hidden. Hidden VisualElements will take up space in their parent layout if their positionType is set to PositionType.Relative. Use the display property to both hide and remove a VisualElement from the parent VisualElement layout.
        /// </summary>
        Hidden = 1
    }

    /// <summary>
    /// Word wrapping over multiple lines if not enough space is available to draw the text of an element.
    /// </summary>
    public enum WhiteSpace
    {
        /// <summary>
        /// Text will wrap when necessary.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Text will never wrap to the next line.
        /// </summary>
        NoWrap = 1
    }

    // Display already exists in UnityEngine and would force fully qualified usage every time
    /// <summary>
    /// Defines how an element is displayed in the layout.
    /// </summary>
    public enum DisplayStyle
    {
        /// <summary>
        /// The element displays normally.
        /// </summary>
        Flex = YogaDisplay.Flex,
        /// <summary>
        /// The element is not visible and absent from the layout.
        /// </summary>
        None = YogaDisplay.None
    }
}
