// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutState
{
    public IntPtr measureFunctionCallback;
    public IntPtr baselineFunctionCallback;

    public uint depth;
    public uint currentGenerationCount;

    [MarshalAs(UnmanagedType.U1)] public bool error;

    public static LayoutState Default => new LayoutState()
    {
        measureFunctionCallback = LayoutDelegates.s_InvokeMeasureFunction,
        baselineFunctionCallback = LayoutDelegates.s_InvokeBaselineFunction
    };
}

unsafe class LayoutProcessorNative : ILayoutProcessor
{
    LayoutState m_State = LayoutState.Default;

    void ILayoutProcessor.CalculateLayout(LayoutNode node, float parentWidth, float parentHeight, LayoutDirection parentDirection)
    {
        var pNode = (IntPtr)(&node);

        fixed (void* ptrState = &m_State)
        {
            var pState = (IntPtr) ptrState;
            LayoutNative.CalculateLayout(pNode, parentWidth, parentHeight, (int)parentDirection, pState);
        }
    }
}
