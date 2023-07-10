// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutCacheData
{
    public static LayoutCacheData Default = new()
    {
        NextCachedMeasurementsIndex = 0,
        CachedLayout = LayoutCachedMeasurement.Default
    };

    public uint NextCachedMeasurementsIndex;
    public FixedBuffer16<LayoutCachedMeasurement> cachedMeasurements;
    public LayoutCachedMeasurement CachedLayout;
}

[StructLayout(LayoutKind.Sequential)]
struct LayoutCachedMeasurement
{
    public static LayoutCachedMeasurement Default = new()
    {
        AvailableWidth = 0f,
        AvailableHeight = 0f,
        ParentWidth = 0f,
        ParentHeight = 0f,
        WidthMeasureMode = (LayoutMeasureMode) (-1),
        HeightMeasureMode = (LayoutMeasureMode) (-1),
        ComputedWidth = -1f,
        ComputedHeight = -1f
    };

    public float AvailableWidth;
    public float AvailableHeight;
    public float ParentWidth;
    public float ParentHeight;
    public LayoutMeasureMode WidthMeasureMode;
    public LayoutMeasureMode HeightMeasureMode;
    public float ComputedWidth;
    public float ComputedHeight;
}
