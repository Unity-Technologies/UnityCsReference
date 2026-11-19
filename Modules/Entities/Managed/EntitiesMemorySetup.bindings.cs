// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/Entities/Public/EntitiesMemorySetup.h")]
    [StaticAccessor("EntitiesMemorySetup", StaticAccessorType.DoubleColon)]
    internal static class EntitiesMemorySetup
    {
        internal static extern long GetArchetypeAllocatorBudget();

        internal static extern long GetQueryAllocatorBudget();

        internal static extern void SetArchetypeAllocatorBudget(long budget);

        internal static extern void SetQueryAllocatorBudget(long budget);
    }
}
