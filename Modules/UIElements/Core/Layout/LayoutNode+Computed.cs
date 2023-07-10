// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.Layout;

unsafe partial struct LayoutNode
{
    public LayoutDirection LayoutDirection => Layout.Direction;

    public float LayoutX => Layout.Position[(int) LayoutEdge.Left];
    public float LayoutY => Layout.Position[(int) LayoutEdge.Top];
    public float LayoutRight => Layout.Position[(int) LayoutEdge.Right];
    public float LayoutBottom => Layout.Position[(int) LayoutEdge.Bottom];
    public float LayoutWidth => Layout.Dimensions[(int) LayoutDimension.Width];
    public float LayoutHeight => Layout.Dimensions[(int) LayoutDimension.Height];

    public float LayoutMarginLeft => GetLayoutValue(Layout.MarginBuffer, LayoutEdge.Left);
    public float LayoutMarginTop => GetLayoutValue(Layout.MarginBuffer, LayoutEdge.Top);
    public float LayoutMarginRight => GetLayoutValue(Layout.MarginBuffer, LayoutEdge.Right);
    public float LayoutMarginBottom => GetLayoutValue(Layout.MarginBuffer, LayoutEdge.Bottom);
    public float LayoutMarginStart => GetLayoutValue(Layout.MarginBuffer, LayoutEdge.Start);
    public float LayoutMarginEnd => GetLayoutValue(Layout.MarginBuffer, LayoutEdge.End);

    public float LayoutPaddingLeft => GetLayoutValue(Layout.PaddingBuffer, LayoutEdge.Left);
    public float LayoutPaddingTop => GetLayoutValue(Layout.PaddingBuffer, LayoutEdge.Top);
    public float LayoutPaddingRight => GetLayoutValue(Layout.PaddingBuffer, LayoutEdge.Right);
    public float LayoutPaddingBottom => GetLayoutValue(Layout.PaddingBuffer, LayoutEdge.Bottom);
    public float LayoutPaddingStart => GetLayoutValue(Layout.PaddingBuffer, LayoutEdge.Start);
    public float LayoutPaddingEnd => GetLayoutValue(Layout.PaddingBuffer, LayoutEdge.End);

    public float LayoutBorderLeft => GetLayoutValue(Layout.BorderBuffer, LayoutEdge.Left);
    public float LayoutBorderTop => GetLayoutValue(Layout.BorderBuffer, LayoutEdge.Top);
    public float LayoutBorderRight => GetLayoutValue(Layout.BorderBuffer, LayoutEdge.Right);
    public float LayoutBorderBottom => GetLayoutValue(Layout.BorderBuffer, LayoutEdge.Bottom);
    public float LayoutBorderStart => GetLayoutValue(Layout.BorderBuffer, LayoutEdge.Start);
    public float LayoutBorderEnd => GetLayoutValue(Layout.BorderBuffer, LayoutEdge.End);

    public float ComputedFlexBasis => Layout.ComputedFlexBasis;

    [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
    float GetLayoutValue(float* buffer, LayoutEdge edge)
    {
        return edge switch
        {
            LayoutEdge.Left => Layout.Direction == LayoutDirection.RTL ? buffer[(int)LayoutEdge.End] : buffer[(int)LayoutEdge.Start],
            LayoutEdge.Right => Layout.Direction == LayoutDirection.RTL ? buffer[(int)LayoutEdge.Start] : buffer[(int)LayoutEdge.End],
            _ => buffer[(int)edge]
        };
    }
}
