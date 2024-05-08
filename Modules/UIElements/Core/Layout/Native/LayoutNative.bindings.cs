// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.Layout;

[NativeHeader("Modules/UIElements/Core/Layout/Native/LayoutNative.h")]
static class LayoutNative
{
    [NativeMethod(IsThreadSafe = false)]
    internal static extern void CalculateLayout(
        IntPtr node,
        float parentWidth,
        float parentHeight,
        int parentDirection,
        IntPtr state,
        IntPtr exceptionGCHandle);

    internal enum LayoutLogEventType
    {
        None = 0,
        Error = 1,
        Measure = 2,
        Layout = 3,
        CacheUsage = 4,
        BeginLayout = 5,
        EndLayout = 6,
    }

    internal class LayoutLogData
    {
        public LayoutNode node;
        public LayoutLogEventType eventType;
        public string message;
    }


    internal static event Action<LayoutLogData> onLayoutLog;

    [RequiredByNativeCode]
    private static void LayoutLog_Internal(IntPtr nodePtr, LayoutLogEventType type, string message)
    {
        LayoutLogData data = new LayoutLogData();
        unsafe
        {
            data.node = *(LayoutNode*)(nodePtr);
            data.message = message;
            data.eventType = type;
        }

        onLayoutLog(data);
    }
}

[RequiredByNativeCode]
[NativeHeader("Modules/UIElements/Core/Layout/Native/LayoutModel.h")]
[StructLayout(LayoutKind.Sequential)]
struct LayoutStyleData
{
    public static LayoutStyleData Default = new()
    {
        Direction = LayoutDirection.Inherit,
        FlexDirection = LayoutFlexDirection.Column,
        JustifyContent = LayoutJustify.FlexStart,
        AlignContent = LayoutAlign.Auto,
        AlignItems = LayoutAlign.Stretch,
        AlignSelf = LayoutAlign.Auto,
        PositionType = LayoutPositionType.Relative,
        AspectRatio = float.NaN,

        FlexWrap = LayoutWrap.NoWrap,
        Overflow = LayoutOverflow.Visible,
        Display = LayoutDisplay.Flex,
        FlexGrow = float.NaN,
        FlexShrink = float.NaN,
        FlexBasis = LayoutValue.Auto(),

        border = LayoutDefaults.EdgeValuesUnit,
        position = LayoutDefaults.EdgeValuesUnit,

        margin = LayoutDefaults.EdgeValuesUnit,
        padding = LayoutDefaults.EdgeValuesUnit,

        dimensions = LayoutDefaults.DimensionValuesAutoUnit,
        minDimensions = LayoutDefaults.DimensionValuesUnit,
    };

    public LayoutDirection Direction;
    public LayoutFlexDirection FlexDirection;
    public LayoutJustify JustifyContent;
    public LayoutAlign AlignContent;
    public LayoutAlign AlignItems;
    public LayoutAlign AlignSelf;
    public LayoutPositionType PositionType;
    public float AspectRatio;

    public LayoutWrap FlexWrap;
    public LayoutOverflow Overflow;
    public LayoutDisplay Display;
    public float FlexGrow;
    public float FlexShrink;
    public LayoutValue FlexBasis;

    public FixedBuffer9<LayoutValue> border;
    public FixedBuffer9<LayoutValue> position;

    public FixedBuffer9<LayoutValue> margin;
    public FixedBuffer9<LayoutValue> padding;

    public FixedBuffer2<LayoutValue> maxDimensions;
    public FixedBuffer2<LayoutValue> minDimensions;
    public FixedBuffer2<LayoutValue> dimensions;
}
