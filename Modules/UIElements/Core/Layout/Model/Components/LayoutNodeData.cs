// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutNodeData
{
    [Flags]
    internal enum FlexStatus
    {
        // External States

        // Input suggesting the layout may have changed and should be recomputed.
        IsDirty = 1 << 0,

        // Output of the layout telling the node has a new layout that needs to be taken into account.
        // (generate GCO, dirty visuals)
        HasNewLayout = 1 << 2,

        // Cleared when the node is dirty, set when the node is first processed in the layout algorithm.
        DependsOnParentSize = 1 << 6,

        // Internal State
        // The Next 3 bits represent the status of the node for the flex algorithm.
        Fixed = 1 << 3,
        MinViolation = 1 << 4,
        MaxViolation = 1 << 5,
    }

    public FixedBuffer2<LayoutValue> ResolvedDimensions;
    float TargetSize;
    public int ManagedMeasureFunctionIndex;
    public int ManagedBaselineFunctionIndex;
    public int ManagedOwnerIndex;
    public int LineIndex;

    public LayoutHandle Config;
    public LayoutHandle Parent;
    public LayoutHandle NextChild;

    public LayoutList<LayoutHandle> Children;
    private FlexStatus Status;

    public bool HasNewLayout
    {
        get => (Status & FlexStatus.HasNewLayout) == FlexStatus.HasNewLayout;
        set => Status = value ? Status | FlexStatus.HasNewLayout : Status & ~FlexStatus.HasNewLayout;
    }

    public bool IsDirty
    {
        get => (Status & FlexStatus.IsDirty) == FlexStatus.IsDirty;
        set => Status = value ? Status | FlexStatus.IsDirty : Status & ~FlexStatus.IsDirty;
    }

    // FlexInternal states are just valid during the flex algorithm, has no meaning outside of it so it is not exposed.  
}


