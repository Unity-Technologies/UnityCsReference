// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
}

static class LayoutProcessor
{
    static ILayoutProcessor s_Processor = new LayoutProcessorNative();

    public static ILayoutProcessor Processor
    {
        get => s_Processor;
        set => s_Processor = value ?? new LayoutProcessorNative();
    }

    public static void CalculateLayout(
        LayoutNode node,
        float parentWidth,
        float parentHeight,
        LayoutDirection parentDirection)
    {
        s_Processor.CalculateLayout(node, parentWidth, parentHeight, parentDirection);
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
    static readonly ProfilerMarker s_InvokeMeasureFunctionMarker = new("InvokeMeasureFunction");
    static readonly ProfilerMarker s_InvokeBaselineFunctionMarker = new("InvokeBaselineFunction");

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
        var measureFunction = node.Measure;

        if (measureFunction == null)
        {
            Debug.Assert(false, "Measure called on a null method");
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
                measureFunction(node.GetOwner(), ref node, width, widthMode, height, heightMode, out result);
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
        var baselineFunction = node.Baseline;
        if (baselineFunction == null)
        {
            return 0f;
        }

        using (s_InvokeBaselineFunctionMarker.Auto())
            return baselineFunction(ref node, width, height);
    }

    static readonly InvokeMeasureFunctionDelegate s_InvokeMeasureDelegate = InvokeMeasureFunction;
    static readonly InvokeBaselineFunctionDelegate s_InvokeBaselineDelegate = InvokeBaselineFunction;

    internal static readonly IntPtr s_InvokeMeasureFunction = Marshal.GetFunctionPointerForDelegate(s_InvokeMeasureDelegate);
    internal static readonly IntPtr s_InvokeBaselineFunction = Marshal.GetFunctionPointerForDelegate(s_InvokeBaselineDelegate);
}
