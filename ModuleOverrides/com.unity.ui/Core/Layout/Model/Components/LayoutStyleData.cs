// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutStyleData
{
    public static LayoutStyleData Default = new()
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
        Flex = float.NaN,
        FlexGrow = float.NaN,
        FlexShrink = float.NaN,
        FlexBasis = LayoutValue.Auto()
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
    public float Flex;
    public float FlexGrow;
    public float FlexShrink;
    public LayoutValue FlexBasis;
}

[StructLayout(LayoutKind.Sequential)]
struct LayoutStyleBorderData
{
    public static LayoutStyleBorderData Default = new()
    {
        border = LayoutDefaults.EdgeValuesUnit,
        position = LayoutDefaults.EdgeValuesUnit
    };

    public FixedBuffer9<LayoutValue> border;
    public FixedBuffer9<LayoutValue> position;
}

[StructLayout(LayoutKind.Sequential)]
struct LayoutStyleMarginData
{
    public static LayoutStyleMarginData Default = new()
    {
        margin = LayoutDefaults.EdgeValuesUnit,
        padding = LayoutDefaults.EdgeValuesUnit
    };

    public FixedBuffer9<LayoutValue> margin;
    public FixedBuffer9<LayoutValue> padding;
}

[StructLayout(LayoutKind.Sequential)]
struct LayoutStyleDimensionData
{
    public static LayoutStyleDimensionData Default = new()
    {
        dimensions = LayoutDefaults.DimensionValuesAutoUnit,
        minDimensions = LayoutDefaults.DimensionValuesUnit,
        maxDimensions = LayoutDefaults.DimensionValuesUnit
    };

    public FixedBuffer2<LayoutValue> maxDimensions;
    public FixedBuffer2<LayoutValue> minDimensions;
    public FixedBuffer2<LayoutValue> dimensions;
}
