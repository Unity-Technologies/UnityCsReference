// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements;

[NativeHeader("Modules/UIElements/Core/Native/Transform/NativeTransformUtils.h")]
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeTransformUtils
{
    public static extern void SetInitialStyleTransformData(IntPtr initialTransformData);

    public static extern void SetDataAccess(IntPtr nodesPtr);

    public static extern void UpdateWorldTransform(UnmanagedDataHandle handle, bool duringLayoutPhase, bool panelIsFlat);
    public static extern void UpdateWorldTransformHierarchy(UnmanagedDataHandle handle, bool panelIsFlat);

    // For performance tests only
    public static extern int CountHierarchy(UnmanagedDataHandle handle);

    // Assumes flat panel.
    public static extern void UpdateBoundingBox(UnmanagedDataHandle handle);
}
