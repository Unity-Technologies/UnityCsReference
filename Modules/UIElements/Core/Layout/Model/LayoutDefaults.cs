// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.Layout;

struct LayoutDefaults
{
    public static readonly FixedBuffer4<float> BorderValues = new()
    {
        [0] = float.NaN,
        [1] = float.NaN,
        [2] = float.NaN,
        [3] = float.NaN,
    };

    public static readonly FixedBuffer4<Length> EdgeValuesUnit = new()
    {
        [0] = Length.None(),
        [1] = Length.None(),
        [2] = Length.None(),
        [3] = Length.None(),
    };

    public static readonly float[] DimensionValues = {float.NaN, float.NaN};

    public static readonly FixedBuffer2<Length> DimensionValuesUnit = new()
    {
        [0] = Length.None(),
        [1] = Length.None()
    };

    public static readonly FixedBuffer2<Length> DimensionValuesAutoUnit = new()
    {
        [0] = Length.Auto(),
        [1] = Length.Auto()
    };
}
