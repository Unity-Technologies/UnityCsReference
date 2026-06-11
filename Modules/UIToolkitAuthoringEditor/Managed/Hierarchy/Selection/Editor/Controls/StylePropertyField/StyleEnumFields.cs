// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    internal partial class DisplayStyleEnumField : StyleEnumField<DisplayStyle>
    {
        public DisplayStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(DisplayStyle.Flex, "Turns the element into a flexible container for aligning and distributing items.");
            valueField.SetTooltipForEnumValue(DisplayStyle.None, "Hides the element in the container. This might have an impact on the layout.");
        }
    }

    [UxmlElement]
    internal partial class VisibilityStyleEnumField : StyleEnumField<Visibility>
    {
        public VisibilityStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Visibility.Visible, "Makes the UI element visible in its container. ");
            valueField.SetTooltipForEnumValue(Visibility.Hidden, "Makes the UI element hidden in its container. ");
        }
    }

    [UxmlElement]
    internal partial class OverflowStyleEnumField : StyleEnumField<Overflow>
    {
        public OverflowStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Overflow.Visible, "Overflowing content is not clipped and may be visible outside the element's container.");
            valueField.SetTooltipForEnumValue(Overflow.Hidden, "Overflowing content is clipped and clipped content is hidden from view.");
        }
    }

    [UxmlElement]
    internal partial class PositionStyleEnumField : StyleEnumField<Position>
    {
        public PositionStyleEnumField()
        {
            valueField.SetTooltipForEnumValue(Position.Absolute, "The item is removed from the normal document flow, and no space is created for it in the layout.");
            valueField.SetTooltipForEnumValue(Position.Relative, "The item is positioned according to the normal flow of the page/screen, and can be offset relative to itself.");
        }
    }

    [UxmlElement]
    internal partial class FlexDirectionStyleEnumField : StyleEnumField<FlexDirection>
    {
        public FlexDirectionStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(FlexDirection.Column, "Changes the main axis direction of a flex container, arranging its items from top to bottom.");
            valueField.SetTooltipForEnumValue(FlexDirection.ColumnReverse, "Changes the main axis direction of a flex container, arranging its items from bottom to top.");
            valueField.SetTooltipForEnumValue(FlexDirection.Row, "Changes the main axis direction of a flex container, arranging its items from left to right.");
            valueField.SetTooltipForEnumValue(FlexDirection.RowReverse, "Changes the main axis direction of a flex container, arranging its items from right to left.");
        }
    }

    [UxmlElement]
    internal partial class WrapStyleEnumField : StyleEnumField<Wrap>
    {
        public WrapStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Wrap.NoWrap, "Forces items to remain in a single line, which might cause overflow if available space is insufficient to fit all items.");
            valueField.SetTooltipForEnumValue(Wrap.Wrap, "Items will wrap onto multiple lines if there is not enough space to fit them on a single line. ");
            valueField.SetTooltipForEnumValue(Wrap.WrapReverse, "Items will wrap onto multiple lines, with the last item appearing first and subsequent items following in reverse order.");
        }
    }

    [UxmlElement]
    internal partial class AlignStyleEnumField : StyleEnumField<Align>
    {
        string m_AutoTooltip, m_FlexStartTooltip, m_CenterTooltip, m_FlexEndTooltip, m_StretchTooltip;

        [UxmlAttribute, CreateProperty]
        public string autoTooltip
        {
            get => m_AutoTooltip;
            set
            {
                m_AutoTooltip = value;
                valueField.SetTooltipForEnumValue(Align.Auto, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string flexStartTooltip
        {
            get => m_FlexStartTooltip;
            set
            {
                m_FlexStartTooltip = value;
                valueField.SetTooltipForEnumValue(Align.FlexStart, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string centerTooltip
        {
            get => m_CenterTooltip;
            set
            {
                m_CenterTooltip = value;
                valueField.SetTooltipForEnumValue(Align.Center, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string flexEndTooltip
        {
            get => m_FlexEndTooltip;
            set
            {
                m_FlexEndTooltip = value;
                valueField.SetTooltipForEnumValue(Align.FlexEnd, value);
            }
        }

        [UxmlAttribute, CreateProperty]
        public string stretchTooltip
        {
            get => m_StretchTooltip;
            set
            {
                m_StretchTooltip = value;
                valueField.SetTooltipForEnumValue(Align.Stretch, value);
            }
        }

        public AlignStyleEnumField()
            : base(true)
        {
        }
    }

    [UxmlElement]
    internal partial class JustifyStyleEnumField : StyleEnumField<Justify>
    {
        public JustifyStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Justify.FlexStart, "Items are packed flush to each other to the left edge of the container if the main axis is horizontal or to the top edge of the container if the main axis is vertical.");
            valueField.SetTooltipForEnumValue(Justify.Center, "Items are packed to the center of the container along the main axis.");
            valueField.SetTooltipForEnumValue(Justify.FlexEnd, "Items are packed flush to each other to the right edge of the container if the main axis is horizontal or to the bottom edge of the container if the main axis is vertical.");
            valueField.SetTooltipForEnumValue(Justify.SpaceBetween, "Items are spaced out evenly along the main axis with equal spacing between them. The first item is aligned to the start of the container, and the last item is aligned to the end.");
            valueField.SetTooltipForEnumValue(Justify.SpaceAround, "Items are spaced out evenly along the main axis with equal spacing between each item, and half the space before the first item and after the last item.");
            valueField.SetTooltipForEnumValue(Justify.SpaceEvenly, "Items are spaced out evenly along the main axis with equal spacing between each item, before the first item and after the last item.");
        }
    }

    [UxmlElement]
    internal partial class UnityTextAlignStyleEnumField : StyleEnumField<TextAnchor>
    {
        public UnityTextAlignStyleEnumField()
            : base(true)
        {
        }
    }

    [UxmlElement]
    internal partial class WhiteSpaceStyleEnumField : StyleEnumField<WhiteSpace>
    {
        public WhiteSpaceStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(WhiteSpace.Normal, "Consecutive white spaces are collapsed into one and text wraps to fit the container.");
            valueField.SetTooltipForEnumValue(WhiteSpace.NoWrap, "Consecutive white spaces are collapsed into one, and text doesn't wrap and continues on the same line in the container.");
            valueField.SetTooltipForEnumValue(WhiteSpace.Pre, "Whitespace is preserved. Text will only wrap on line breaks.");
            valueField.SetTooltipForEnumValue(WhiteSpace.PreWrap, "Whitespace is preserved. Text will wrap when necessary.");
        }
    }

    [UxmlElement]
    internal partial class TextOverflowStyleEnumField : StyleEnumField<TextOverflow>
    {
        public TextOverflowStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(TextOverflow.Clip, "Text that extends beyond the boundaries of its container will be cut off and will not be visible.");
            valueField.SetTooltipForEnumValue(TextOverflow.Ellipsis, "Text that extends beyond the boundaries of its container will be truncated with an ellipsis.");
        }
    }

    [UxmlElement]
    internal partial class TextOverflowPositionStyleEnumField : StyleEnumField<TextOverflowPosition>
    {
    }

    [UxmlElement]
    internal partial class AnimationPlayStateStyleEnumField : StyleEnumField<AnimationPlayState>
    {
        public AnimationPlayStateStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(AnimationPlayState.Running, "The animation is currently playing.");
            valueField.SetTooltipForEnumValue(AnimationPlayState.Paused, "The animation is currently paused.");
        }
    }

    [UxmlElement]
    internal partial class EditorTextRenderingModeStyleEnumField : StyleEnumField<EditorTextRenderingMode>
    {
    }

    [UxmlElement]
    internal partial class TextGeneratorTypeStyleEnumField : StyleEnumField<TextGeneratorType>
    {
    }

    [UxmlElement]
    internal partial class FontStyleStyleEnumField : StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>
    {
        public FontStyleStyleEnumField()
            : this(null) { }

        public FontStyleStyleEnumField(string label)
            : base(label, new FontStyleToggleField()) { }

        protected override FontStyleToggleField CreateValueField()
        {
            return new FontStyleToggleField();
        }

        protected override StyleEnum<FontStyle> CreateStyleValue(FontStyle v)
        {
            return v;
        }
    }

    [UxmlElement]
    internal partial class TextAlignStyleEnumField : StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>
    {
        public TextAlignStyleEnumField()
            : this(null) { }

        public TextAlignStyleEnumField(string label)
            : base(label, new TextAlignToggleField()) { }

        protected override TextAlignToggleField CreateValueField()
        {
            return new TextAlignToggleField();
        }

        protected override StyleEnum<TextAnchor> CreateStyleValue(TextAnchor v)
        {
            return v;
        }
    }

    [UxmlElement]
    internal partial class SliceTypeStyleEnumField : StyleEnumField<SliceType>
    {
        public SliceTypeStyleEnumField() : base(true)
        {
            valueField.SetTooltipForEnumValue(SliceType.Sliced, "Fill the slices by stretching the center and sides.");
            valueField.SetTooltipForEnumValue(SliceType.Tiled, "Fill the slices by tiling the center and sides. Image must be imported as a Sprite (2D and UI) and have Mesh Type set to Full Rect.");
        }
    }
}
