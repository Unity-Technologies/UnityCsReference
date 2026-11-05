// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/World.bindings.h")]
    public unsafe partial class World
    {
        [FreeFunction("ActiveWorldsStack::SetStorage", IsThreadSafe = true)]
        static extern void SetActiveWorldsStackStorage(void* storage, int elementsOffset, int elementSize, int worldImplOffset);
    }
}
