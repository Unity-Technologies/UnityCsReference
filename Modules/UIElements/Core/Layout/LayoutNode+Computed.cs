// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.Layout;

unsafe partial struct LayoutNode
{
    public LayoutDirection LayoutDirection => Layout.Direction;

    public float LayoutX => Layout.Position[(int) LayoutLayoutEdge.Left];
    public float LayoutY => Layout.Position[(int) LayoutLayoutEdge.Top];
    public float LayoutRight => Layout.Position[(int) LayoutLayoutEdge.Right];
    public float LayoutBottom => Layout.Position[(int) LayoutLayoutEdge.Bottom];
    public float LayoutWidth => Layout.Dimensions[(int) LayoutDimension.Width];
    public float LayoutHeight => Layout.Dimensions[(int) LayoutDimension.Height];

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public Rect GetLayoutRect()
    {
        if (IsUndefined)
        {
            return new(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        var lyt = Layout;
        return new Rect(lyt.Position[(int)LayoutLayoutEdge.Left],
        lyt.Position[(int)LayoutLayoutEdge.Top],
        lyt.Dimensions[(int)LayoutDimension.Width],
        lyt.Dimensions[(int)LayoutDimension.Height]);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public Vector2 GetLayoutSize()
    {
        if (IsUndefined)
        {
            return new(float.NaN, float.NaN);
        }

        var lyt = Layout;
        return new Vector2(lyt.Dimensions[(int)LayoutDimension.Width],
            lyt.Dimensions[(int)LayoutDimension.Height]);
    }

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    public Vector2 GetLayoutPosition()
    {
        if (IsUndefined)
        {
            return new(float.NaN, float.NaN);
        }

        var lyt = Layout;
        return new Vector2(lyt.Position[(int)LayoutLayoutEdge.Left],
            lyt.Position[(int)LayoutLayoutEdge.Top]);
    }

    public float LayoutMarginLeft => GetLayoutValue(Layout.MarginBuffer, LayoutLayoutEdge.Left);
    public float LayoutMarginTop => GetLayoutValue(Layout.MarginBuffer, LayoutLayoutEdge.Top);
    public float LayoutMarginRight => GetLayoutValue(Layout.MarginBuffer, LayoutLayoutEdge.Right);
    public float LayoutMarginBottom => GetLayoutValue(Layout.MarginBuffer, LayoutLayoutEdge.Bottom);
    public float LayoutMarginStart => GetLayoutValue(Layout.MarginBuffer, LayoutLayoutEdge.Start);
    public float LayoutMarginEnd => GetLayoutValue(Layout.MarginBuffer, LayoutLayoutEdge.End);

    public float LayoutPaddingLeft => GetLayoutValue(Layout.PaddingBuffer, LayoutLayoutEdge.Left);
    public float LayoutPaddingTop => GetLayoutValue(Layout.PaddingBuffer, LayoutLayoutEdge.Top);
    public float LayoutPaddingRight => GetLayoutValue(Layout.PaddingBuffer, LayoutLayoutEdge.Right);
    public float LayoutPaddingBottom => GetLayoutValue(Layout.PaddingBuffer, LayoutLayoutEdge.Bottom);
    public float LayoutPaddingStart => GetLayoutValue(Layout.PaddingBuffer, LayoutLayoutEdge.Start);
    public float LayoutPaddingEnd => GetLayoutValue(Layout.PaddingBuffer, LayoutLayoutEdge.End);

    public float LayoutBorderLeft => GetLayoutValue(Layout.BorderBuffer, LayoutLayoutEdge.Left);
    public float LayoutBorderTop => GetLayoutValue(Layout.BorderBuffer, LayoutLayoutEdge.Top);
    public float LayoutBorderRight => GetLayoutValue(Layout.BorderBuffer, LayoutLayoutEdge.Right);
    public float LayoutBorderBottom => GetLayoutValue(Layout.BorderBuffer, LayoutLayoutEdge.Bottom);
    public float LayoutBorderStart => GetLayoutValue(Layout.BorderBuffer, LayoutLayoutEdge.Start);
    public float LayoutBorderEnd => GetLayoutValue(Layout.BorderBuffer, LayoutLayoutEdge.End);

    public float ComputedFlexBasis => Layout.ComputedFlexBasis;

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    float GetLayoutValue(float* buffer, LayoutLayoutEdge edge)
    {
        return edge switch
        {
            LayoutLayoutEdge.Left => Layout.Direction == LayoutDirection.RTL ? buffer[(int)LayoutLayoutEdge.End] : buffer[(int)LayoutLayoutEdge.Start],
            LayoutLayoutEdge.Right => Layout.Direction == LayoutDirection.RTL ? buffer[(int)LayoutLayoutEdge.Start] : buffer[(int)LayoutLayoutEdge.End],
            _ => buffer[(int)edge]
        };
    }
}
