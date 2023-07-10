// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements;

[NativeType(Header = "Modules/UIElements/VisualNodeImguiData.h")]
[StructLayout(LayoutKind.Sequential)]
struct VisualNodeImguiData
{
    [MarshalAs(UnmanagedType.U1)] public bool IsContainer;
    public int DescendantCount;
}
