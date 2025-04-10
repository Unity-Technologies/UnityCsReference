// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines how the position values are interpreted by the layout engine.
    /// </summary>
    public enum Position
    {
        /// <summary>
        /// The element is positioned in relation to its default box as calculated by layout.
        /// </summary>
        Relative = LayoutPositionType.Relative,
        /// <summary>
        /// The element is positioned in relation to its parent box and doesn't contribute to the layout anymore.
        /// </summary>
        Absolute = LayoutPositionType.Absolute,
    }

    /// <summary>
    /// Defines what should happened if content overflows an element bounds.
    /// </summary>
    public enum Overflow
    {
        /// <summary>
        /// The overflow isn't clipped. It renders outside the element's box. Default Value.
        /// </summary>
        Visible = LayoutOverflow.Visible,
        /// <summary>
        /// The overflow is clipped, and the rest of the content will be invisible.
        /// </summary>
        /// <remarks>
        /// Refer to [[wiki:UIE-masking|Apply masking effects in UI Toolkit]] for implementation details.
        /// </remarks>
        Hidden = LayoutOverflow.Hidden
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum OverflowInternal
    {
        Visible = LayoutOverflow.Visible,
        Hidden = LayoutOverflow.Hidden,
        Scroll = LayoutOverflow.Scroll,
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
        Column = LayoutFlexDirection.Column,
        /// <summary>
        /// Bottom to Top.
        /// </summary>
        ColumnReverse = LayoutFlexDirection.ColumnReverse,
        /// <summary>
        /// Left to Right.
        /// </summary>
        Row = LayoutFlexDirection.Row,
        /// <summary>
        /// Right to Left.
        /// </summary>
        RowReverse = LayoutFlexDirection.RowReverse
    }

    /// <summary>
    /// By default, items will all try to fit onto one line. You can change that and allow the items to wrap as needed with this property.
    /// </summary>
    /// <remarks>
    /// SA: [[UIElements.WhiteSpace]]
    /// </remarks>
    public enum Wrap
    {
        /// <summary>
        /// All items will be on one line. Default Value.
        /// </summary>
        NoWrap = LayoutWrap.NoWrap,
        /// <summary>
        /// Items will wrap onto multiple lines from top to bottom.
        /// </summary>
        Wrap = LayoutWrap.Wrap,
        /// <summary>
        /// Items will wrap onto multiple lines from bottom to top.
        /// </summary>
        WrapReverse = LayoutWrap.WrapReverse
    }

    /// <summary>
    /// Defines the alignment behavior along an axis.
    /// </summary>
    public enum Align
    {
        /// <summary>
        /// Let Flex decide.
        /// </summary>
        Auto = LayoutAlign.Auto,
        /// <summary>
        /// Start margin of the item is placed at the start of the axis.
        /// </summary>
        FlexStart = LayoutAlign.FlexStart,
        /// <summary>
        /// Items are centered on the axis.
        /// </summary>
        Center = LayoutAlign.Center,
        /// <summary>
        /// End margin of the item is placed at the end of the axis.
        /// </summary>
        FlexEnd = LayoutAlign.FlexEnd,
        /// <summary>
        /// Default. stretch to fill the axis while respecting min/max values.
        /// </summary>
        Stretch = LayoutAlign.Stretch
    }

    /// <summary>
    /// Defines the alignment along the main axis, how is extra space distributed.
    /// </summary>
    public enum Justify
    {
        /// <summary>
        /// Items are packed toward the start line. Default Value.
        /// </summary>
        FlexStart = LayoutJustify.FlexStart,
        /// <summary>
        /// Items are centered along the line.
        /// </summary>
        Center = LayoutJustify.Center,
        /// <summary>
        /// Items are packed toward the end line.
        /// </summary>
        FlexEnd = LayoutJustify.FlexEnd,
        /// <summary>
        /// Items are evenly distributed in the line; first item is on the start line, last item on the end line.
        /// </summary>
        SpaceBetween = LayoutJustify.SpaceBetween,
        /// <summary>
        /// Items are evenly distributed in the line with equal space around them. Space on the edge is half of the space between objects.
        /// </summary>
        SpaceAround = LayoutJustify.SpaceAround,
        /// <summary>
        /// Items are evenly distributed in the line with equal space around them.
        /// </summary>
        SpaceEvenly = LayoutJustify.SpaceEvenly,

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
    /// Specifies the alignment keywords for <see cref="TransformOrigin"/>.
    /// </summary>
    public enum TransformOriginOffset
    {
        /// <summary>
        /// The origin of the transform operation is set to the left of the element.
        /// </summary>
        Left = 1,

        /// <summary>
        /// The origin of the transform operation is set to the right of the element.
        /// </summary>
        Right = 2,

        /// <summary>
        /// The origin of the transform operation is set to the top of the element.
        /// </summary>
        Top = 3,

        /// <summary>
        /// The origin of the transform operation set to the bottom of the element.
        /// </summary>
        Bottom = 4,

        /// <summary>
        /// The origin of the transform operation is set to the center of the element.
        /// </summary>
        Center = 5,
    }


    /// <summary>
    /// Style value that specifies whether or not a VisualElement is visible.
    /// </summary>
    public enum Visibility
    {
        /// <summary>
        /// The VisualElement is visible. Default Value.
        /// </summary>
        Visible = 0,
        /// <summary>
        /// The VisualElement is hidden. Hidden VisualElements will take up space in their parent layout if their positionType is set to PositionType.Relative. Use the <see cref="DisplayStyle"/> style property to both hide and remove a VisualElement from the parent VisualElement layout. Note, this is the enum value used when setting styles via VisualElement.style.visibility. In C#, you can just use VisualElement.visible = false.
        /// </summary>
        Hidden = 1
    }

    /// <summary>
    /// Controls how whitespace and line breaks within an element's text are handled, similar to the CSS white-space property.
    /// Unlike the CSS property, only trailing whitespaces are collapsed, and whitespaces within the text are preserved.
    /// See <a href="https://developer.mozilla.org/en-US/docs/Web/CSS/white-space">white-space</a> for more details.
    /// </summary>
    /// <remarks>
    /// Whitespaces within TextFields are not collapsed.
    /// </remarks>
    public enum WhiteSpace
    {
        /// <summary>
        /// Trailing sequences of whitespace are collapsed into a single whitespace. Text will wrap when necessary. This is the default behavior.
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Trailing sequences of whitespace are collapsed into a single whitespace. Text will never wrap to the next line.
        /// </summary>
        NoWrap = 1,
        /// <summary>
        ///  Whitespace is preserved. Text will only wrap on line breaks.
        /// </summary>
        Pre = 2,
        /// <summary>
        /// Whitespace is preserved. Text will wrap when necessary.
        /// </summary>
        PreWrap = 3,
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
        Flex = LayoutDisplay.Flex,
        /// <summary>
        /// The element isn't visible and absent from the layout.
        /// </summary>
        None = LayoutDisplay.None
    }

    /// <summary>
    /// Defines the position of the background.
    /// </summary>
    public enum BackgroundPositionKeyword
    {
        /// <summary>
        /// Vertical alignment is centered and/or Horizontal alignment is centered.
        /// This is the default value to make sure it's backward compatible
        /// with unity-background-scale-mode default value.
        /// </summary>
        Center = 0,

        /// <summary>
        /// Vertical alignment is at the top.
        /// </summary>
        Top = 1,

        /// <summary>
        /// Vertical alignment is at the bottom.
        /// </summary>
        Bottom = 2,

        /// <summary>
        /// Horizontal alignment is to the left.
        /// </summary>
        Left = 3,

        /// <summary>
        /// Horizontal alignment is to the right.
        /// </summary>
        Right = 4,
    }

    /// <summary>
    /// Defines the position of an element.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum PositionProperty
    {
        /// <summary>
        /// The top edge of an element.
        /// </summary>
        Top = 0,

        /// <summary>
        /// The bottom edge of an element.
        /// </summary>
        Bottom = 1,

        /// <summary>
        /// The left edge of an element.
        /// </summary>
        Left = 2,

        /// <summary>
        /// The right edge of an element.
        /// </summary>
        Right = 3,
    }

    /// <summary>
    /// Defines how the background is repeated.
    /// </summary>
    public enum Repeat
    {
        /// <summary>
        /// The background-image isn't repeated. The image is only shown once.
        /// This is the default to keep background compatibility with
        /// unity-background-scale-mode.
        /// </summary>
        NoRepeat = 0,

        /// <summary>
        /// The background-image is repeated as much as possible without clipping.
        /// The first and last image is pinned to either side of the element,
        /// and whitespace is distributed evenly between the images.
        /// </summary>
        Space = 1,

        /// <summary>
        /// The background-image is repeated and squished or stretched to fill the space (no gaps).
        /// </summary>
        Round = 2,

        /// <summary>
        /// The background image is repeated both vertically and horizontally.
        /// The last image will be clipped if it doesn't fit.
        /// </summary>
        Repeat = 3,
    }

    /// <summary>
    /// Defines how the background is repeated (one-value only).
    /// </summary>
    internal enum RepeatXY
    {
        /// <summary>
        /// The background image is repeated horizontally.
        /// </summary>
        RepeatX = 0,

        /// <summary>
        /// The background image is repeated vertically.
        /// </summary>
        RepeatY = 1,
    }

    /// <summary>
    /// Defines the size of the background.
    /// </summary>
    public enum BackgroundSizeType
    {
        /// <summary>
        /// Determines if the size of the background image comes from the
        /// <see cref="BackgroundSize.x"/> and <see cref="BackgroundSize.y"/> length values.
        /// </summary>
        Length = 0,

        /// <summary>
        /// Resize the background image to cover the entire container,
        /// even if it has to stretch the image or cut a little bit off one of the edges.
        /// </summary>
        Cover = 1,

        /// <summary>
        /// Resize the background image to make sure the image is fully visible.
        /// </summary>
        Contain = 2,
    }

    internal enum Axis
    {
        X,Y,Z
    }

    /// <summary>
    /// Defines how the editor rendering the text by default
    /// </summary>
    public enum EditorTextRenderingMode
    {
        /// <summary>
        /// Sdf is using a single FontAsset is needed to render multiple resulting fontsize
        /// </summary>
        SDF = 0,
        /// <summary>
        /// Every font size needed is creating a new FontAsset to be as sharp as possible.
        /// </summary>
        Bitmap = 1
    }

    /// <summary>
    /// The slice type determines whether the center of the image is scaled or tiled.
    /// </summary>
    public enum SliceType
    {
        /// <summary>
        /// Fill the slices by stretching the center and sides.
        /// </summary>
        Sliced = 0,
        /// <summary>
        /// Fill the slices by tiling the center and sides.
        /// Image must be imported as a Sprite (2D and UI) and have Mesh Type set to Full Rect. 
        /// </summary>
        Tiled = 1
    }
}
