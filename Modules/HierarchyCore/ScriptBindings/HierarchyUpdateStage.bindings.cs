// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyUpdateStage.h")]
    enum HierarchyUpdateStage : uint
    {
        Idle = 0,
        UpdateBegin = 1,
        ExecuteCommands = 2,
        UpdateEnd = 3,
    }
}
