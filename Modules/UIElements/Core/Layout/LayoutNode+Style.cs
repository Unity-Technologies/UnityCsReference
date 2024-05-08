// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.Layout;

partial struct LayoutNode
{
    public LayoutDirection StyleDirection
    {
        get => Style.Direction;
        set
        {
            if (Style.Direction == value)
                return;

            Style.Direction = value;
            MarkDirty();
        }
    }

    public LayoutFlexDirection FlexDirection
    {
        get => Style.FlexDirection;
        set
        {
            if (Style.FlexDirection == value)
                return;

            Style.FlexDirection = value;
            MarkDirty();
        }
    }

    public LayoutJustify JustifyContent
    {
        get => Style.JustifyContent;
        set
        {
            if (Style.JustifyContent == value)
                return;

            Style.JustifyContent = value;
            MarkDirty();
        }
    }

    public LayoutDisplay Display
    {
        get => Style.Display;
        set
        {
            if (Style.Display == value)
                return;

            Style.Display = value;
            MarkDirty();
        }
    }

    public LayoutAlign AlignItems
    {
        get => Style.AlignItems;
        set
        {
            if (Style.AlignItems == value)
                return;

            Style.AlignItems = value;
            MarkDirty();
        }
    }

    public LayoutAlign AlignSelf
    {
        get => Style.AlignSelf;
        set
        {
            if (Style.AlignSelf == value)
                return;

            Style.AlignSelf = value;
            MarkDirty();
        }
    }

    public LayoutAlign AlignContent
    {
        get => Style.AlignContent;
        set
        {
            if (Style.AlignContent == value)
                return;

            Style.AlignContent = value;
            MarkDirty();
        }
    }

    public LayoutPositionType PositionType
    {
        get => Style.PositionType;
        set
        {
            if (Style.PositionType == value)
                return;

            Style.PositionType = value;
            MarkDirty();
        }
    }

    public LayoutWrap Wrap
    {
        get => Style.FlexWrap;
        set
        {
            if (Style.FlexWrap == value)
                return;

            Style.FlexWrap = value;
            MarkDirty();
        }
    }

    public float FlexGrow
    {
        get => Style.FlexGrow;
        set => SetValue(ref Style.FlexGrow, value);
    }

    public float FlexShrink
    {
        get => Style.FlexShrink;
        set => SetValue(ref Style.FlexShrink, value);
    }

    public LayoutValue FlexBasis
    {
        get => Style.FlexBasis;
        set => SetStyleValueUnit(ref Style.FlexBasis, value);
    }

    public LayoutValue Width
    {
        get => Style.dimensions[(int)LayoutDimension.Width];
        set => SetStyleValueUnit(ref Style.dimensions[(int)LayoutDimension.Width], value);
    }

    public LayoutValue Height
    {
        get => Style.dimensions[(int)LayoutDimension.Height];
        set => SetStyleValueUnit(ref Style.dimensions[(int)LayoutDimension.Height], value);
    }

    public LayoutValue MaxWidth
    {
        get => Style.maxDimensions[(int)LayoutDimension.Width];
        set => SetStyleValue(ref Style.maxDimensions[(int)LayoutDimension.Width], value);
    }

    public LayoutValue MaxHeight
    {
        get => Style.maxDimensions[(int)LayoutDimension.Height];
        set => SetStyleValue(ref Style.maxDimensions[(int)LayoutDimension.Height], value);
    }

    public LayoutValue MinWidth
    {
        get => Style.minDimensions[(int)LayoutDimension.Width];
        set => SetStyleValue(ref Style.minDimensions[(int)LayoutDimension.Width], value);
    }

    public LayoutValue MinHeight
    {
        get => Style.minDimensions[(int)LayoutDimension.Height];
        set => SetStyleValue(ref Style.minDimensions[(int)LayoutDimension.Height], value);
    }

    public float AspectRatio
    {
        get => Style.AspectRatio;
        set => SetValue(ref Style.AspectRatio, value);
    }

    public LayoutOverflow Overflow
    {
        get => Style.Overflow;
        set
        {
            if (Style.Overflow == value)
                return;

            Style.Overflow = value;
            MarkDirty();
        }
    }

    public LayoutValue Left
    {
        get => Style.position[(int)LayoutEdge.Left];
        set => SetStyleEdgePosition(LayoutEdge.Left, value);
    }

    public LayoutValue Top
    {
        get => Style.position[(int)LayoutEdge.Top];
        set => SetStyleEdgePosition(LayoutEdge.Top, value);
    }

    public LayoutValue Right
    {
        get => Style.position[(int)LayoutEdge.Right];
        set => SetStyleEdgePosition(LayoutEdge.Right, value);
    }

    public LayoutValue Bottom
    {
        get => Style.position[(int)LayoutEdge.Bottom];
        set => SetStyleEdgePosition(LayoutEdge.Bottom, value);
    }

    public LayoutValue Start
    {
        get => Style.position[(int)LayoutEdge.Start];
        set => SetStyleEdgePosition(LayoutEdge.Start, value);
    }

    public LayoutValue End
    {
        get => Style.position[(int)LayoutEdge.End];
        set => SetStyleEdgePosition(LayoutEdge.End, value);
    }

    public LayoutValue MarginLeft
    {
        get => Style.margin[(int)LayoutEdge.Left];
        set => SetStyleEdgeMargin(LayoutEdge.Left, value);
    }

    public LayoutValue MarginTop
    {
        get => Style.margin[(int)LayoutEdge.Top];
        set => SetStyleEdgeMargin(LayoutEdge.Top, value);
    }

    public LayoutValue MarginRight
    {
        get => Style.margin[(int)LayoutEdge.Right];
        set => SetStyleEdgeMargin(LayoutEdge.Right, value);
    }

    public LayoutValue MarginBottom
    {
        get => Style.margin[(int)LayoutEdge.Bottom];
        set => SetStyleEdgeMargin(LayoutEdge.Bottom, value);
    }

    public LayoutValue MarginStart
    {
        get => Style.margin[(int)LayoutEdge.Start];
        set => SetStyleEdgeMargin(LayoutEdge.Start, value);
    }

    public LayoutValue MarginEnd
    {
        get => Style.margin[(int)LayoutEdge.End];
        set => SetStyleEdgeMargin(LayoutEdge.End, value);
    }

    public LayoutValue MarginHorizontal
    {
        get => Style.margin[(int)LayoutEdge.Horizontal];
        set => SetStyleEdgeMargin(LayoutEdge.Horizontal, value);
    }

    public LayoutValue MarginVertical
    {
        get => Style.margin[(int)LayoutEdge.Vertical];
        set => SetStyleEdgeMargin(LayoutEdge.Vertical, value);
    }

    public LayoutValue Margin
    {
        get => Style.margin[(int)LayoutEdge.All];
        set => SetStyleEdgeMargin(LayoutEdge.All, value);
    }

    public LayoutValue PaddingLeft
    {
        get => Style.padding[(int)LayoutEdge.Left];
        set => SetStyleEdgePadding(LayoutEdge.Left, value);
    }

    public LayoutValue PaddingTop
    {
        get => Style.padding[(int)LayoutEdge.Top];
        set => SetStyleEdgePadding(LayoutEdge.Top, value);
    }

    public LayoutValue PaddingRight
    {
        get => Style.padding[(int)LayoutEdge.Right];
        set => SetStyleEdgePadding(LayoutEdge.Right, value);
    }

    public LayoutValue PaddingBottom
    {
        get => Style.padding[(int)LayoutEdge.Bottom];
        set => SetStyleEdgePadding(LayoutEdge.Bottom, value);
    }

    public LayoutValue PaddingStart
    {
        get => Style.padding[(int)LayoutEdge.Start];
        set => SetStyleEdgePadding(LayoutEdge.Start, value);
    }

    public LayoutValue PaddingEnd
    {
        get => Style.padding[(int)LayoutEdge.End];
        set => SetStyleEdgePadding(LayoutEdge.End, value);
    }

    public LayoutValue PaddingHorizontal
    {
        get => Style.padding[(int)LayoutEdge.Horizontal];
        set => SetStyleEdgePadding(LayoutEdge.Horizontal, value);
    }

    public LayoutValue PaddingVertical
    {
        get => Style.padding[(int)LayoutEdge.Vertical];
        set => SetStyleEdgePadding(LayoutEdge.Vertical, value);
    }

    public LayoutValue Padding
    {
        get => Style.padding[(int)LayoutEdge.All];
        set => SetStyleEdgePadding(LayoutEdge.All, value);
    }

    public float BorderLeftWidth
    {
        get => Style.border[(int) LayoutEdge.Left].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.Left], value);
    }

    public float BorderTopWidth
    {
        get => Style.border[(int) LayoutEdge.Top].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.Top], value);
    }

    public float BorderRightWidth
    {
        get => Style.border[(int) LayoutEdge.Right].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.Right], value);
    }

    public float BorderBottomWidth
    {
        get => Style.border[(int) LayoutEdge.Bottom].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.Bottom], value);
    }

    public float BorderStartWidth
    {
        get => Style.border[(int) LayoutEdge.Start].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.Start], value);
    }

    public float BorderEndWidth
    {
        get => Style.border[(int) LayoutEdge.End].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.End], value);
    }

    public float BorderWidth
    {
        get => Style.border[(int) LayoutEdge.All].Value;
        set => StyleEdgeSetPoint(ref Style.border[(int)LayoutEdge.All], value);
    }

    void SetValue(ref float currentValue, float newValue)
    {
        if (currentValue.Equals(newValue))
            return;

        currentValue = newValue;
        MarkDirty();
    }

    void SetStyleValue(ref LayoutValue currentValue, LayoutValue newValue)
    {
        if (newValue.Unit == LayoutUnit.Percent)
        {
            SetStyleValuePercent(ref currentValue, newValue);
        }
        else
        {
            SetStyleValuePoint(ref currentValue, newValue);
        }
    }

    void SetStyleValueUnit(ref LayoutValue currentValue, LayoutValue newValue)
    {
        if (newValue.Unit == LayoutUnit.Percent)
        {
            SetStyleValuePercent(ref currentValue, newValue);
        }
        else if (newValue.Unit == LayoutUnit.Auto)
        {
            SetStyleValueAuto(ref currentValue);
        }
        else
        {
            SetStyleValuePoint(ref currentValue, newValue);
        }
    }

    void SetStyleValuePoint(ref LayoutValue currentValue, LayoutValue newValue)
    {
        if (float.IsNaN(currentValue.Value) && float.IsNaN(newValue.Value) && newValue.Unit == currentValue.Unit)
        {
            return;
        }

        if (currentValue.Value != newValue.Value || currentValue.Unit != LayoutUnit.Point)
        {
            if (float.IsNaN(newValue.Value))
                currentValue = LayoutValue.Auto();
            else
                currentValue = LayoutValue.Point(newValue.Value);

            MarkDirty();
        }
    }

    void SetStyleValuePercent(ref LayoutValue currentValue, LayoutValue newValue)
    {
        if (currentValue.Value != newValue.Value || currentValue.Unit != LayoutUnit.Percent)
        {
            if (float.IsNaN(newValue.Value))
                currentValue = LayoutValue.Auto();
            else
                currentValue = newValue;

            MarkDirty();
        }
    }

    void SetStyleValueAuto(ref LayoutValue currentValue)
    {
        if (currentValue.Unit != LayoutUnit.Auto)
        {
            currentValue = LayoutValue.Auto();
            MarkDirty();
        }
    }

    void SetStyleEdgePosition(LayoutEdge edge, LayoutValue value)
    {
        if (value.Unit == LayoutUnit.Percent)
        {
            StyleEdgeSetPercent(ref Style.position[(int)edge], value.Value);
        }
        else
        {
            StyleEdgeSetPoint(ref Style.position[(int)edge], value.Value);
        }
    }

    void SetStyleEdgeMargin(LayoutEdge edge, LayoutValue value)
    {
        if (value.Unit == LayoutUnit.Percent)
        {
            StyleEdgeSetPercent(ref Style.margin[(int)edge], value.Value);
        }
        else if (value.Unit == LayoutUnit.Auto)
        {
            StyleEdgeSetAuto(ref Style.margin[(int)edge]);
        }
        else
        {
            StyleEdgeSetPoint(ref Style.margin[(int)edge], value.Value);
        }
    }

    void SetStyleEdgePadding(LayoutEdge edge, LayoutValue value)
    {
        if (value.Unit == LayoutUnit.Percent)
        {
            StyleEdgeSetPercent(ref Style.padding[(int)edge], value.Value);
        }
        else
        {
            StyleEdgeSetPoint(ref Style.padding[(int)edge], value.Value);
        }
    }

    void StyleEdgeSetPercent(ref LayoutValue value, float newValue)
    {
        if (value.Value != newValue || value.Unit != LayoutUnit.Percent)
        {
            value = float.IsNaN(newValue)
                ? LayoutValue.Undefined()
                : LayoutValue.Percent(newValue);

            MarkDirty();
        }
    }

    void StyleEdgeSetAuto(ref LayoutValue value)
    {
        if (value.Unit != LayoutUnit.Auto)
        {
            value = LayoutValue.Auto();
            MarkDirty();
        }
    }

    void StyleEdgeSetPoint(ref LayoutValue value, float newValue)
    {
        if (float.IsNaN(value.Value) && float.IsNaN(newValue))
        {
            return;
        }

        if (value.Value != newValue || value.Unit != LayoutUnit.Point)
        {
            value = float.IsNaN(newValue)
                ? LayoutValue.Undefined()
                : LayoutValue.Point(newValue);

            MarkDirty();
        }
    }
}
