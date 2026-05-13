// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements.Layout;

namespace UnityEngine.UIElements;

[NativeHeader("Modules/UIElements/Core/Layout/Native/LayoutModel.h")]
[StructLayout(LayoutKind.Sequential)]
struct LayoutData : IStyleDataGroup<LayoutData>
{
    public static LayoutData Default = new()
    {
        Direction = LayoutDirection.Inherit,
        FlexDirection = LayoutFlexDirection.Column,
        JustifyContent = LayoutJustify.FlexStart,
        AlignContent = LayoutAlign.Auto,
        AlignItems = LayoutAlign.Stretch,
        AlignSelf = LayoutAlign.Auto,
        PositionType = LayoutPositionType.Relative,
        AspectRatio = float.NaN,

        FlexWrap = LayoutWrap.NoWrap,
        Overflow = LayoutOverflow.Visible,
        Display = LayoutDisplay.Flex,
        FlexGrow = float.NaN,
        FlexShrink = float.NaN,
        FlexBasis = Length.Auto(),

        Border = LayoutDefaults.BorderValues,
        Position = LayoutDefaults.EdgeValuesUnit,
        Margin = LayoutDefaults.EdgeValuesUnit,
        Padding = LayoutDefaults.EdgeValuesUnit,

        Dimensions = LayoutDefaults.DimensionValuesAutoUnit,
        MinDimensions = LayoutDefaults.DimensionValuesUnit,
        MaxDimensions = LayoutDefaults.DimensionValuesUnit,
    };

    public LayoutDirection Direction;
    public LayoutFlexDirection FlexDirection;
    public LayoutJustify JustifyContent;
    public LayoutAlign AlignContent;
    public LayoutAlign AlignItems;
    public LayoutAlign AlignSelf;
    public LayoutPositionType PositionType;
    public float AspectRatio;

    public LayoutWrap FlexWrap;
    public LayoutOverflow Overflow;
    public LayoutDisplay Display;
    public float FlexGrow;
    public float FlexShrink;
    public Length FlexBasis;

    public FixedBuffer4<float> Border;
    public FixedBuffer4<Length> Position;
    public FixedBuffer4<Length> Margin;
    public FixedBuffer4<Length> Padding;

    public FixedBuffer2<Length> MaxDimensions;
    public FixedBuffer2<Length> MinDimensions;
    public FixedBuffer2<Length> Dimensions;

    public Align alignContent { get => (Align)AlignContent; set => AlignContent = (LayoutAlign)value; }
    public Align alignItems { get => (Align)AlignItems; set => AlignItems = (LayoutAlign)value; }
    public Align alignSelf { get => (Align)AlignSelf; set => AlignSelf = (LayoutAlign)value; }
    public Ratio aspectRatio { get => (Ratio)AspectRatio; set => AspectRatio = (float)value; }
    public float borderBottomWidth { get => Border[(int) LayoutStyleEdge.Bottom]; set => Border[(int) LayoutStyleEdge.Bottom] = value; }
    public float borderLeftWidth { get => Border[(int) LayoutStyleEdge.Left]; set => Border[(int) LayoutStyleEdge.Left] = value; }
    public float borderRightWidth { get => Border[(int) LayoutStyleEdge.Right]; set => Border[(int) LayoutStyleEdge.Right] = value; }
    public float borderTopWidth { get => Border[(int) LayoutStyleEdge.Top]; set => Border[(int) LayoutStyleEdge.Top] = value; }
    public Length bottom { get => Position[(int) LayoutStyleEdge.Bottom]; set => Position[(int) LayoutStyleEdge.Bottom] = value; }
    public DisplayStyle display { get => (DisplayStyle)Display; set => Display = (LayoutDisplay)value; }
    public Length flexBasis { get => FlexBasis; set => FlexBasis = value; }
    public FlexDirection flexDirection { get => (FlexDirection)FlexDirection; set => FlexDirection = (LayoutFlexDirection)value; }
    public float flexGrow { get => FlexGrow; set => FlexGrow = value; }
    public float flexShrink { get => FlexShrink; set => FlexShrink = value; }
    public Wrap flexWrap { get => (Wrap)FlexWrap; set => FlexWrap = (LayoutWrap)value; }
    public Length height { get => Dimensions[(int)LayoutDimension.Height]; set => Dimensions[(int)LayoutDimension.Height] = value; }
    public Justify justifyContent { get => (Justify)JustifyContent; set => JustifyContent = (LayoutJustify)value; }
    public Length left { get => Position[(int) LayoutStyleEdge.Left]; set => Position[(int) LayoutStyleEdge.Left] = value; }
    public Length marginBottom { get => Margin[(int) LayoutStyleEdge.Bottom]; set => Margin[(int) LayoutStyleEdge.Bottom] = value; }
    public Length marginLeft { get => Margin[(int) LayoutStyleEdge.Left]; set => Margin[(int) LayoutStyleEdge.Left] = value; }
    public Length marginRight { get => Margin[(int) LayoutStyleEdge.Right]; set => Margin[(int) LayoutStyleEdge.Right] = value; }
    public Length marginTop { get => Margin[(int) LayoutStyleEdge.Top]; set => Margin[(int) LayoutStyleEdge.Top] = value; }
    public Length maxHeight { get => MaxDimensions[(int)LayoutDimension.Height]; set => MaxDimensions[(int)LayoutDimension.Height] = value; }
    public Length maxWidth { get => MaxDimensions[(int)LayoutDimension.Width]; set => MaxDimensions[(int)LayoutDimension.Width] = value; }
    public Length minHeight { get => MinDimensions[(int)LayoutDimension.Height]; set => MinDimensions[(int)LayoutDimension.Height] = value; }
    public Length minWidth { get => MinDimensions[(int)LayoutDimension.Width]; set => MinDimensions[(int)LayoutDimension.Width] = value; }
    public OverflowInternal overflow { get => (OverflowInternal)Overflow; set => Overflow = (LayoutOverflow)value; }
    public Length paddingBottom { get => Padding[(int) LayoutStyleEdge.Bottom]; set => Padding[(int) LayoutStyleEdge.Bottom] = value; }
    public Length paddingLeft { get => Padding[(int) LayoutStyleEdge.Left]; set => Padding[(int) LayoutStyleEdge.Left] = value; }
    public Length paddingRight { get => Padding[(int) LayoutStyleEdge.Right]; set => Padding[(int) LayoutStyleEdge.Right] = value; }
    public Length paddingTop { get => Padding[(int) LayoutStyleEdge.Top]; set => Padding[(int) LayoutStyleEdge.Top] = value; }
    public Position position { get => (Position)PositionType; set => PositionType = (LayoutPositionType)value; }
    public Length right { get => Position[(int) LayoutStyleEdge.Right]; set => Position[(int) LayoutStyleEdge.Right] = value; }
    public Length top { get => Position[(int) LayoutStyleEdge.Top]; set => Position[(int) LayoutStyleEdge.Top] = value; }
    public Length width { get => Dimensions[(int)LayoutDimension.Width]; set => Dimensions[(int)LayoutDimension.Width] = value; }

    public LayoutData GetDefault()
    {
        return Default;
    }

    public LayoutData Copy()
    {
        return this;
    }

    public void CopyFrom(ref LayoutData other)
    {
        this = other;
    }

    public void Dispose()
    {
    }
}
