// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.Layout;

[NativeHeader("Modules/UIElements/Core/Layout/Native/LayoutNative.h")]
[StructLayout(LayoutKind.Sequential)]
struct LayoutCacheData
{
    public static LayoutCacheData Default = new()
    {
        CachedLayout = LayoutCachedMeasurement.Default
    };

    public LayoutCachedMeasurement CachedLayout;

    public override readonly string ToString()
    {
        return $"CacheCount: {MeasurementCacheCount()}\n" +
            $"CachedLayout: {CachedLayout}";
    }


    //The first is the layout cache, all subsequent are measurement caches
    public readonly int MeasurementCacheCount()
    {
        unsafe
        {
            int count = 0;
            LayoutCachedMeasurement* current = CachedLayout.NextMeasurementCache;
            while (current != null)
            {
                count++;
                current = current->NextMeasurementCache;
            }
            return count;
        }

    }

    public unsafe void ClearCachedMeasurements()
    {
        if(CachedLayout.NextMeasurementCache == null)
            return;

        fixed (LayoutCacheData* cachePtr = &(this))
        {
            LayoutCacheData.ClearCachedMeasurements(cachePtr);
        }
    }


    private static extern unsafe void ClearCachedMeasurements(void* LayoutCacheData);

}

[StructLayout(LayoutKind.Sequential)]
unsafe struct LayoutCachedMeasurement
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
        ComputedHeight = -1f,
        m_NextMeasurementCachePtr = null,
    };

    public float AvailableWidth;
    public float AvailableHeight;
    public float ParentWidth;
    public float ParentHeight;
    public LayoutMeasureMode WidthMeasureMode;
    public LayoutMeasureMode HeightMeasureMode;
    public float ComputedWidth;
    public float ComputedHeight;
    private void* m_NextMeasurementCachePtr;

    public LayoutCachedMeasurement* NextMeasurementCache => (LayoutCachedMeasurement*)m_NextMeasurementCachePtr;

    public override readonly string ToString()
    {
        return $"Available: {AvailableWidth}/{AvailableHeight}   Parent: {ParentWidth}/{ParentHeight}   MeasureMode: {WidthMeasureMode}/{HeightMeasureMode},   Computed: {ComputedWidth}/{ComputedHeight}";
    }
}
