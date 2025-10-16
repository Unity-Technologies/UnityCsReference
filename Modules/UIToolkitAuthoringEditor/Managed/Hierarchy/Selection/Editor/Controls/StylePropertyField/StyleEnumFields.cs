// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    internal class DisplayStyleEnumField : StyleEnumField<DisplayStyle>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<DisplayStyle>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<DisplayStyle>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new DisplayStyleEnumField();
        }

        DisplayStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(DisplayStyle.Flex, "Turns the element into a flexible container for aligning and distributing items.");
            valueField.SetTooltipForEnumValue(DisplayStyle.None, "Hides the element in the container. This might have an impact on the layout.");
        }
    }

    internal class VisibilityStyleEnumField : StyleEnumField<Visibility>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Visibility>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Visibility>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new VisibilityStyleEnumField();
        }

        public VisibilityStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Visibility.Visible, "Makes the UI element visible in its container. ");
            valueField.SetTooltipForEnumValue(Visibility.Hidden, "Makes the UI element hidden in its container. ");
        }
    }

    internal class OverflowStyleEnumField : StyleEnumField<Overflow>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Overflow>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Overflow>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new OverflowStyleEnumField();
        }

        public OverflowStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Overflow.Visible, "Overflowing content is not clipped and may be visible outside the element's container.");
            valueField.SetTooltipForEnumValue(Overflow.Hidden, "Overflowing content is clipped and clipped content is hidden from view.");
        }
    }

    internal class PositionStyleEnumField : StyleEnumField<Position>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Position>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Position>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new PositionStyleEnumField();
        }

        public PositionStyleEnumField()
        {
            valueField.SetTooltipForEnumValue(Position.Absolute, "The item is removed from the normal document flow, and no space is created for it in the layout.");
            valueField.SetTooltipForEnumValue(Position.Relative, "The item is positioned according to the normal flow of the page/screen, and can be offset relative to itself.");
        }
    }

    internal class FlexDirectionStyleEnumField : StyleEnumField<FlexDirection>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<FlexDirection>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<FlexDirection>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new FlexDirectionStyleEnumField();
        }

        public FlexDirectionStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(FlexDirection.Column, "Changes the main axis direction of a flex container, arranging its items from top to bottom.");
            valueField.SetTooltipForEnumValue(FlexDirection.ColumnReverse, "Changes the main axis direction of a flex container, arranging its items from bottom to top.");
            valueField.SetTooltipForEnumValue(FlexDirection.Row, "Changes the main axis direction of a flex container, arranging its items from left to right.");
            valueField.SetTooltipForEnumValue(FlexDirection.RowReverse, "Changes the main axis direction of a flex container, arranging its items from right to left.");
        }
    }

    internal class WrapStyleEnumField : StyleEnumField<Wrap>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Wrap>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Wrap>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new WrapStyleEnumField();
        }

        public WrapStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(Wrap.NoWrap, "Forces items to remain in a single line, which might cause overflow if available space is insufficient to fit all items.");
            valueField.SetTooltipForEnumValue(Wrap.Wrap, "Items will wrap onto multiple lines if there is not enough space to fit them on a single line. ");
            valueField.SetTooltipForEnumValue(Wrap.WrapReverse, "Items will wrap onto multiple lines, with the last item appearing first and subsequent items following in reverse order.");
        }
    }

    internal class AlignStyleEnumField : StyleEnumField<Align>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Align>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] string autoTooltip;
            [SerializeField] string flexStartTooltip;
            [SerializeField] string centerTooltip;
            [SerializeField] string flexEndTooltip;
            [SerializeField] string stretchTooltip;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags autoTooltip_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags flexStartTooltip_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags centerTooltip_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags flexEndTooltip_UxmlAttributeFlags;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags stretchTooltip_UxmlAttributeFlags;
            #pragma warning restore 649

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (AlignStyleEnumField)obj;
                if (ShouldWriteAttributeValue(autoTooltip_UxmlAttributeFlags))
                    e.autoTooltip = autoTooltip;
                if (ShouldWriteAttributeValue(flexStartTooltip_UxmlAttributeFlags))
                    e.flexStartTooltip = flexStartTooltip;
                if (ShouldWriteAttributeValue(centerTooltip_UxmlAttributeFlags))
                    e.centerTooltip = centerTooltip;
                if (ShouldWriteAttributeValue(flexEndTooltip_UxmlAttributeFlags))
                    e.flexEndTooltip = flexEndTooltip;
                if (ShouldWriteAttributeValue(stretchTooltip_UxmlAttributeFlags))
                    e.stretchTooltip = stretchTooltip;
            }

            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Align>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(autoTooltip), "auto-tooltip"),
                    new (nameof(flexStartTooltip), "flex-start-tooltip"),
                    new (nameof(centerTooltip), "center-tooltip"),
                    new (nameof(flexEndTooltip), "flex-end-tooltip"),
                    new (nameof(stretchTooltip), "stretch-tooltip")
                }, true);
            }

            public override object CreateInstance() => new AlignStyleEnumField();
        }

        string m_AutoTooltip, m_FlexStartTooltip, m_CenterTooltip, m_FlexEndTooltip, m_StretchTooltip;

        [CreateProperty]
        public string autoTooltip
        {
            get => m_AutoTooltip;
            set
            {
                m_AutoTooltip = value;
                valueField.SetTooltipForEnumValue(Align.Auto, value);
            }
        }

        [CreateProperty]
        public string flexStartTooltip
        {
            get => m_FlexStartTooltip;
            set
            {
                m_FlexStartTooltip = value;
                valueField.SetTooltipForEnumValue(Align.FlexStart, value);
            }
        }

        [CreateProperty]
        public string centerTooltip
        {
            get => m_CenterTooltip;
            set
            {
                m_CenterTooltip = value;
                valueField.SetTooltipForEnumValue(Align.Center, value);
            }
        }

        [CreateProperty]
        public string flexEndTooltip
        {
            get => m_FlexEndTooltip;
            set
            {
                m_FlexEndTooltip = value;
                valueField.SetTooltipForEnumValue(Align.FlexEnd, value);
            }
        }

        [CreateProperty]
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

    internal class JustifyStyleEnumField : StyleEnumField<Justify>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<Justify>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<Justify>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new JustifyStyleEnumField();
        }

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

    internal class UnityTextAlignStyleEnumField : StyleEnumField<TextAnchor>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextAnchor>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextAnchor>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new UnityTextAlignStyleEnumField();
        }

        public UnityTextAlignStyleEnumField()
            : base(true)
        {
        }
    }

    internal class WhiteSpaceStyleEnumField : StyleEnumField<WhiteSpace>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<WhiteSpace>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<WhiteSpace>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new WhiteSpaceStyleEnumField();
        }

        public WhiteSpaceStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(WhiteSpace.Normal, "Consecutive white spaces are collapsed into one and text wraps to fit the container.");
            valueField.SetTooltipForEnumValue(WhiteSpace.NoWrap, "Consecutive white spaces are collapsed into one, and text doesn't wrap and continues on the same line in the container.");
            valueField.SetTooltipForEnumValue(WhiteSpace.Pre, "Whitespace is preserved. Text will only wrap on line breaks.");
            valueField.SetTooltipForEnumValue(WhiteSpace.PreWrap, "Whitespace is preserved. Text will wrap when necessary.");
        }
    }

    internal class TextOverflowStyleEnumField : StyleEnumField<TextOverflow>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextOverflow>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextOverflow>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextOverflowStyleEnumField();
        }

        public TextOverflowStyleEnumField()
            : base(true)
        {
            valueField.SetTooltipForEnumValue(TextOverflow.Clip, "Text that extends beyond the boundaries of its container will be cut off and will not be visible.");
            valueField.SetTooltipForEnumValue(TextOverflow.Ellipsis, "Text that extends beyond the boundaries of its container will be truncated with an ellipsis.");
        }
    }

    internal class TextOverflowPositionStyleEnumField : StyleEnumField<TextOverflowPosition>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextOverflowPosition>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextOverflowPosition>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextOverflowPositionStyleEnumField();
        }
    }

    internal class EditorTextRenderingModeStyleEnumField : StyleEnumField<EditorTextRenderingMode>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<EditorTextRenderingMode>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<EditorTextRenderingMode>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new EditorTextRenderingModeStyleEnumField();
        }
    }

    internal class TextGeneratorTypeStyleEnumField : StyleEnumField<TextGeneratorType>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<TextGeneratorType>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<TextGeneratorType>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextGeneratorTypeStyleEnumField();
        }
    }

    internal class FontStyleStyleEnumField : StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleEnum<FontStyle>, FontStyleToggleField, FontStyle>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new FontStyleStyleEnumField();
        }

        public FontStyleStyleEnumField()
            : this(null) { }

        public FontStyleStyleEnumField(string label)
            : base(label, new FontStyleToggleField()) { }
    }

    internal class TextAlignStyleEnumField : StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StylePropertyField<StyleEnum<TextAnchor>, TextAlignToggleField, TextAnchor>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextAlignStyleEnumField();
        }

        public TextAlignStyleEnumField()
            : this(null) { }

        public TextAlignStyleEnumField(string label)
            : base(label, new TextAlignToggleField()) { }
    }

    internal class SliceTypeStyleEnumField : StyleEnumField<SliceType>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : StyleEnumField<SliceType>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                StyleEnumField<SliceType>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new SliceTypeStyleEnumField();
        }

        public SliceTypeStyleEnumField()
        {
            valueField.SetTooltipForEnumValue(SliceType.Sliced, "Fill the slices by stretching the center and sides.");
            valueField.SetTooltipForEnumValue(SliceType.Tiled, "Fill the slices by tiling the center and sides. Image must be imported as a Sprite (2D and UI) and have Mesh Type set to Full Rect.");
        }
    }
}
