// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutNodeData
{
    public FixedBuffer2<LayoutValue> ResolvedDimensions;

    public bool IsDirty;
    public bool HasNewLayout;
    public int ManagedMeasureFunctionIndex;
    public int ManagedBaselineFunctionIndex;
    public int ManagedOwnerIndex;
    public int LineIndex;

    public LayoutHandle Config;
    public LayoutHandle Parent;
    public LayoutHandle NextChild;

    public LayoutList<LayoutHandle> Children;
}
