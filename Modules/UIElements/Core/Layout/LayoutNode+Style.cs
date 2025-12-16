// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


namespace UnityEngine.UIElements.Layout;

partial struct LayoutNode
{
    public LayoutDirection StyleDirection
    {
        get => ReadOnlyStyle.Direction;
        set => Style.Direction = value;
    }

    public LayoutFlexDirection FlexDirection
    {
        get => ReadOnlyStyle.FlexDirection;
        set => Style.FlexDirection = value;
    }

    public LayoutJustify JustifyContent
    {
        get => ReadOnlyStyle.JustifyContent;
        set => Style.JustifyContent = value;
    }

    public LayoutDisplay Display
    {
        get => ReadOnlyStyle.Display;
        set => Style.Display = value;
    }

    public LayoutAlign AlignItems
    {
        get => ReadOnlyStyle.AlignItems;
        set => Style.AlignItems = value;
    }

    public LayoutAlign AlignSelf
    {
        get => ReadOnlyStyle.AlignSelf;
        set => Style.AlignSelf = value;
    }

    public LayoutAlign AlignContent
    {
        get => ReadOnlyStyle.AlignContent;
        set => Style.AlignContent = value;
    }

    public LayoutPositionType PositionType
    {
        get => ReadOnlyStyle.PositionType;
        set => Style.PositionType = value;
    }

    public LayoutWrap Wrap
    {
        get => ReadOnlyStyle.FlexWrap;
        set => Style.FlexWrap = value;
    }

    public float FlexGrow
    {
        get => ReadOnlyStyle.FlexGrow;
        set => Style.FlexGrow = value;
    }

    public float FlexShrink
    {
        get => ReadOnlyStyle.FlexShrink;
        set => Style.FlexShrink = value;
    }

    public Length FlexBasis
    {
        get => ReadOnlyStyle.FlexBasis;
        set => Style.FlexBasis = value;
    }

    public Length Width
    {
        get => ReadOnlyStyle.Dimensions[(int)LayoutDimension.Width];
        set => Style.Dimensions[(int)LayoutDimension.Width] = value;
    }

    public Length Height
    {
        get => ReadOnlyStyle.Dimensions[(int)LayoutDimension.Height];
        set => Style.Dimensions[(int)LayoutDimension.Height] = value;
    }

    public Length MaxWidth
    {
        get => ReadOnlyStyle.MaxDimensions[(int)LayoutDimension.Width];
        set => Style.MaxDimensions[(int)LayoutDimension.Width] = value;
    }

    public Length MaxHeight
    {
        get => ReadOnlyStyle.MaxDimensions[(int)LayoutDimension.Height];
        set => Style.MaxDimensions[(int)LayoutDimension.Height] = value;
    }

    public Length MinWidth
    {
        get => ReadOnlyStyle.MinDimensions[(int)LayoutDimension.Width];
        set => Style.MinDimensions[(int)LayoutDimension.Width] = value;
    }

    public Length MinHeight
    {
        get => ReadOnlyStyle.MinDimensions[(int)LayoutDimension.Height];
        set => Style.MinDimensions[(int)LayoutDimension.Height] = value;
    }

    public float AspectRatio
    {
        get => ReadOnlyStyle.AspectRatio;
        set => Style.AspectRatio = value;
    }

    public LayoutOverflow Overflow
    {
        get => ReadOnlyStyle.Overflow;
        set => Style.Overflow = value;
    }

    public Length Left
    {
        get => ReadOnlyStyle.Position[(int)LayoutStyleEdge.Left];
        set => Style.Position[(int)LayoutStyleEdge.Left] = value;
    }

    public Length Top
    {
        get => ReadOnlyStyle.Position[(int)LayoutStyleEdge.Top];
        set => Style.Position[(int)LayoutStyleEdge.Top] = value;
    }

    public Length Right
    {
        get => ReadOnlyStyle.Position[(int)LayoutStyleEdge.Right];
        set => Style.Position[(int)LayoutStyleEdge.Right] = value;
    }

    public Length Bottom
    {
        get => ReadOnlyStyle.Position[(int)LayoutStyleEdge.Bottom];
        set => Style.Position[(int)LayoutStyleEdge.Bottom] = value;
    }

    public Length MarginLeft
    {
        get => ReadOnlyStyle.Margin[(int)LayoutStyleEdge.Left];
        set => Style.Margin[(int)LayoutStyleEdge.Left] = value;
    }

    public Length MarginTop
    {
        get => ReadOnlyStyle.Margin[(int)LayoutStyleEdge.Top];
        set => Style.Margin[(int)LayoutStyleEdge.Top] = value;
    }

    public Length MarginRight
    {
        get => ReadOnlyStyle.Margin[(int)LayoutStyleEdge.Right];
        set => Style.Margin[(int)LayoutStyleEdge.Right] = value;
    }

    public Length MarginBottom
    {
        get => ReadOnlyStyle.Margin[(int)LayoutStyleEdge.Bottom];
        set => Style.Margin[(int)LayoutStyleEdge.Bottom] = value;
    }

    public Length MarginHorizontal
    {
        set => MarginLeft = MarginRight = value;
    }

    public Length MarginVertical
    {
        set => MarginTop = MarginBottom = value;
    }

    public Length Margin
    {
        set => MarginHorizontal = MarginVertical = value;
    }

    public Length PaddingLeft
    {
        get => ReadOnlyStyle.Padding[(int)LayoutStyleEdge.Left];
        set => Style.Padding[(int)LayoutStyleEdge.Left] = value;
    }

    public Length PaddingTop
    {
        get => ReadOnlyStyle.Padding[(int)LayoutStyleEdge.Top];
        set => Style.Padding[(int)LayoutStyleEdge.Top] = value;
    }

    public Length PaddingRight
    {
        get => ReadOnlyStyle.Padding[(int)LayoutStyleEdge.Right];
        set => Style.Padding[(int)LayoutStyleEdge.Right] = value;
    }

    public Length PaddingBottom
    {
        get => ReadOnlyStyle.Padding[(int)LayoutStyleEdge.Bottom];
        set => Style.Padding[(int)LayoutStyleEdge.Bottom] = value;
    }

    public Length PaddingHorizontal
    {
        set => PaddingLeft = PaddingRight = value;
    }

    public Length PaddingVertical
    {
        set => PaddingTop = PaddingBottom = value;
    }

    public Length Padding
    {
        set => PaddingHorizontal = PaddingVertical = value;
    }

    public float BorderLeftWidth
    {
        get => ReadOnlyStyle.Border[(int)LayoutStyleEdge.Left];
        set => Style.Border[(int)LayoutStyleEdge.Left] = value;
    }

    public float BorderTopWidth
    {
        get => ReadOnlyStyle.Border[(int)LayoutStyleEdge.Top];
        set => Style.Border[(int)LayoutStyleEdge.Top] = value;
    }

    public float BorderRightWidth
    {
        get => ReadOnlyStyle.Border[(int)LayoutStyleEdge.Right];
        set => Style.Border[(int)LayoutStyleEdge.Right] = value;
    }

    public float BorderBottomWidth
    {
        get => ReadOnlyStyle.Border[(int)LayoutStyleEdge.Bottom];
        set => Style.Border[(int)LayoutStyleEdge.Bottom] = value;
    }

    public float BorderWidth
    {
        set => BorderLeftWidth = BorderRightWidth = BorderTopWidth = BorderBottomWidth = value;
    }
}
