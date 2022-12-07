// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.Layout;

struct LayoutDefaults
{
    public static readonly FixedBuffer9<LayoutValue> EdgeValuesUnit = new FixedBuffer9<LayoutValue>
    {
        [0] = LayoutValue.Undefined(),
        [1] = LayoutValue.Undefined(),
        [2] = LayoutValue.Undefined(),
        [3] = LayoutValue.Undefined(),
        [4] = LayoutValue.Undefined(),
        [5] = LayoutValue.Undefined(),
        [6] = LayoutValue.Undefined(),
        [7] = LayoutValue.Undefined(),
        [8] = LayoutValue.Undefined(),
    };

    public static readonly float[] DimensionValues = {float.NaN, float.NaN};

    public static readonly FixedBuffer2<LayoutValue> DimensionValuesUnit = new FixedBuffer2<LayoutValue>
    {
        [0] = LayoutValue.Undefined(),
        [1] = LayoutValue.Undefined()
    };

    public static readonly FixedBuffer2<LayoutValue> DimensionValuesAutoUnit = new FixedBuffer2<LayoutValue>
    {
        [0] = LayoutValue.Auto(),
        [1] = LayoutValue.Auto()
    };
}
