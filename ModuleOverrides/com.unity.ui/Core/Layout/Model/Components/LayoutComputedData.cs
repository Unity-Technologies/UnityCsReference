// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
unsafe struct LayoutComputedData
{
    public static LayoutComputedData Default
    {
        get
        {
            var r = new LayoutComputedData
            {
                Direction = LayoutDirection.Inherit,
                ComputedFlexBasisGeneration = 0,
                ComputedFlexBasis = float.NaN,
                HadOverflow = false,
                GenerationCount = 0,
                LastParentDirection = (LayoutDirection) (-1),
                LastPointScaleFactor = 1.0f
            };
            r.Dimensions[0] = LayoutDefaults.DimensionValues[0];
            r.Dimensions[1] = LayoutDefaults.DimensionValues[1];
            r.MeasuredDimensions[0] = LayoutDefaults.DimensionValues[0];
            r.MeasuredDimensions[1] = LayoutDefaults.DimensionValues[1];
            return r;
        }
    }

    public fixed float Position[4];
    public fixed float Dimensions[2];
    public fixed float Margin[6];
    public fixed float Border[6];
    public fixed float Padding[6];
    public LayoutDirection Direction;
    public uint ComputedFlexBasisGeneration;
    public float ComputedFlexBasis;
    public bool HadOverflow;
    public uint GenerationCount;
    public LayoutDirection LastParentDirection;
    public float LastPointScaleFactor;
    public fixed float MeasuredDimensions[2];

    public float* MarginBuffer
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            fixed (float* m = Margin)
                return m;
        }
    }

    public float* BorderBuffer
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            fixed (float* m = Border)
                return m;
        }
    }

    public float* PaddingBuffer
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        get
        {
            fixed (float* m = Padding)
                return m;
        }
    }
}
