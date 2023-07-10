// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;

namespace UnityEngine.UIElements;

[System.Flags]
enum VisualNodeCallbackInterest
{
    None         = 0,
    ChildAdded   = 1 << 0,
    ChildRemoved = 1 << 1,
};

[NativeType(Header = "Modules/UIElements/VisualNodeData.h")]
[StructLayout(LayoutKind.Sequential)]
struct VisualNodeData
{
    public VisualPanelHandle Panel;
    public VisualNodeHandle LogicalParent;
    public VisualElementFlags Flags;
    public VisualNodeCallbackInterest CallbackInterest;
    public LayoutNode LayoutNode;
    public uint ControlId;
    [MarshalAs(UnmanagedType.U1)] public bool Enabled;
    [MarshalAs(UnmanagedType.U1)] public bool IsRootVisualContainer;
}
