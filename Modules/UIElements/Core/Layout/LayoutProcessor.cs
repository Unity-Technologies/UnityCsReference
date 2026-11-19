// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Profiling;

namespace UnityEngine.UIElements.Layout;

interface ILayoutProcessor
{
    void CalculateLayout(
        LayoutNode node,
        float parentWidth,
        float parentHeight,
        LayoutDirection parentDirection);

    LayoutSize Measure(
        LayoutNode node,
        float availableWidth,
        LayoutMeasureMode widthMode,
        float availableHeight,
        LayoutMeasureMode heightMode,
        LayoutDirection parentDirection);
}

static partial class LayoutProcessor
{
    [AutoStaticsCleanupOnCodeReload]
    static ILayoutProcessor s_Processor = new LayoutProcessorNative();

    [AutoStaticsCleanupOnCodeReload]
    static int s_CalculateLayoutDepth;

    // The end of a layout pass clears the dirty flag of every node it reached, so marks made during the pass are replayed after it.
    [AutoStaticsCleanupOnCodeReload]
    [IgnoreForUAL0015("Only filled during CalculateLayout and emptied before it returns, so no constructor can leave entries across a code reload")]
    static readonly List<LayoutNode> s_DirtiedDuringLayout = new();

    public static ILayoutProcessor Processor
    {
        get => s_Processor;
        set => s_Processor = value ?? new LayoutProcessorNative();
    }

    internal static bool isCalculatingLayout => s_CalculateLayoutDepth > 0;

    internal static void RecordDirtiedDuringLayout(LayoutNode node)
    {
        s_DirtiedDuringLayout.Add(node);
    }

    public static void CalculateLayout(
        LayoutNode node,
        float parentWidth,
        float parentHeight,
        LayoutDirection parentDirection)
    {
        s_CalculateLayoutDepth++;
        try
        {
            s_Processor.CalculateLayout(node, parentWidth, parentHeight, parentDirection);
        }
        finally
        {
            if (--s_CalculateLayoutDepth == 0 && s_DirtiedDuringLayout.Count > 0)
                ReplayDirtiedDuringLayout();
        }
    }

    static void ReplayDirtiedDuringLayout()
    {
        foreach (var dirtied in s_DirtiedDuringLayout)
        {
            if (!dirtied.IsUndefined)
                dirtied.MarkDirty();
        }
        s_DirtiedDuringLayout.Clear();
    }

    public static LayoutSize Measure(
        LayoutNode node,
        float availableWidth,
        LayoutMeasureMode widthMode,
        float availableHeight,
        LayoutMeasureMode heightMode,
        LayoutDirection parentDirection)
    {
        return s_Processor.Measure(node, availableWidth, widthMode, availableHeight, heightMode, parentDirection);
    }
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void InvokeMeasureFunctionDelegate(
    ref LayoutNode node,
    float width,
    LayoutMeasureMode widthMode,
    float height,
    LayoutMeasureMode heightMode,
    ref IntPtr exception,
    out LayoutSize result);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate float InvokeBaselineFunctionDelegate(
    ref LayoutNode node,
    float width,
    float height);

static class LayoutDelegates
{
    static readonly ProfilerMarker s_InvokeMeasureFunctionMarker = new(ProfilerCategory.UIToolkit, "InvokeMeasureFunction");
    static readonly ProfilerMarker s_InvokeBaselineFunctionMarker = new(ProfilerCategory.UIToolkit, "InvokeBaselineFunction");

    [AOT.MonoPInvokeCallback(typeof(InvokeMeasureFunctionDelegate))]
    static void InvokeMeasureFunction(
        ref LayoutNode node,
        float width,
        LayoutMeasureMode widthMode,
        float height,
        LayoutMeasureMode heightMode,
        ref IntPtr exception,
        out LayoutSize result)
    {
        var config = node.Config;
        var measureFunction = config.Measure;

        if (measureFunction == null)
        {
            Debug.Assert(false, "No measure function set in this node's config");
            result = default;
            return;
        }

        // Fix for UUM-48790:
        // AddressSanitizer (ASAN) is lost when we throw an exception from c#
        // which is called from c++, which in turn is called from c#.
        // C# : Measure Function <-- Exception
        // C++: LayoutNative
        // C# : LayoutProcessorNative <-- Catch
        // To solve this issue we return the exception using a GCHandle
        // to LayoutProcessorNative using intptr_t pointer in c++.
        try
        {
            using (s_InvokeMeasureFunctionMarker.Auto())
                measureFunction(ref node, width, widthMode, height, heightMode, out result);
        }
        catch (Exception e)
        {
            GCHandle handle = GCHandle.Alloc(e);
            exception = GCHandle.ToIntPtr(handle);
            result = default;
        }
    }

    [AOT.MonoPInvokeCallback(typeof(InvokeBaselineFunctionDelegate))]
    static float InvokeBaselineFunction(
        ref LayoutNode node,
        float width,
        float height)
    {
        var baselineFunction = node.Config.Baseline;
        if (baselineFunction == null)
        {
            Debug.Assert(false, "No baselineFunction function set in this node's config");
            return 0f;
        }

        using (s_InvokeBaselineFunctionMarker.Auto())
            return baselineFunction(ref node, width, height);
    }

    [NoAutoStaticsCleanup]
    static readonly InvokeMeasureFunctionDelegate s_InvokeMeasureDelegate = InvokeMeasureFunction;
    [NoAutoStaticsCleanup]
    static readonly InvokeBaselineFunctionDelegate s_InvokeBaselineDelegate = InvokeBaselineFunction;

    internal static readonly IntPtr s_InvokeMeasureFunction = Marshal.GetFunctionPointerForDelegate(s_InvokeMeasureDelegate);
    internal static readonly IntPtr s_InvokeBaselineFunction = Marshal.GetFunctionPointerForDelegate(s_InvokeBaselineDelegate);
}
