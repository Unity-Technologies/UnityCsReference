// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutValue
{
    float value;
    LayoutUnit unit;

    public LayoutUnit Unit => unit;
    public float Value => value;

    public static LayoutValue Point(float value)
    {
        return new LayoutValue
        {
            value = value,
            unit = float.IsNaN(value) ? LayoutUnit.Undefined : LayoutUnit.Point
        };
    }

    public bool Equals(LayoutValue other)
    {
        return Unit == other.Unit && (Value.Equals(other.Value) || Unit == LayoutUnit.Undefined);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is LayoutValue yogaValue && Equals(yogaValue);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Value.GetHashCode() * 397) ^ (int) Unit;
        }
    }

    public static LayoutValue Undefined()
    {
        return new LayoutValue
        {
            value = float.NaN,
            unit = LayoutUnit.Undefined
        };
    }

    public static LayoutValue Auto()
    {
        return new LayoutValue
        {
            value = float.NaN,
            unit = LayoutUnit.Auto
        };
    }

    public static LayoutValue Percent(float value)
    {
        return new LayoutValue
        {
            value = value,
            unit = float.IsNaN(value) ? LayoutUnit.Undefined : LayoutUnit.Percent
        };
    }

    public static implicit operator LayoutValue(float value)
    {
        return Point(value);
    }
}

static class LayoutValueExtensions
{
    public static LayoutValue Percent(this float value)
    {
        return LayoutValue.Percent(value);
    }

    public static LayoutValue Pt(this float value)
    {
        return LayoutValue.Point(value);
    }

    public static LayoutValue Percent(this int value)
    {
        return LayoutValue.Percent(value);
    }

    public static LayoutValue Pt(this int value)
    {
        return LayoutValue.Point(value);
    }
}

