// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

enum StyleDataType
{
    InheritedDataType,
    LayoutDataType,
    RareDataType,
    TransformDataType,
    TransitionDataType,
    VisualDataType,
    ManagedDataType // only used for StyleDataRef to hold an index into a pool
}

[NativeHeader("Modules/UIElements/Core/Native/Unmanaged/StyleDataAllocator.bindings.h")]
static class StyleDataAllocator
{
    internal static StyleDataType GetType<T>() where T : unmanaged
    {
        if (typeof(T) == typeof(InheritedData))
            return StyleDataType.InheritedDataType;
        else if (typeof(T) == typeof(LayoutData))
            return StyleDataType.LayoutDataType;
        else if (typeof(T) == typeof(RareData))
            return StyleDataType.RareDataType;
        else if (typeof(T) == typeof(TransformData))
            return StyleDataType.TransformDataType;
        else if (typeof(T) == typeof(TransitionData))
            return StyleDataType.TransitionDataType;
        else if (typeof(T) == typeof(VisualData))
            return StyleDataType.VisualDataType;
        else
            throw new ArgumentException("T must be of type LayoutDataType, TransformDataType, VisualDataType");
    }

    internal static extern IntPtr Allocate(StyleDataType type);
    internal static extern void Free(IntPtr ptr, StyleDataType type);

    // For tests
    internal static extern int SizeOf(StyleDataType type);
}
