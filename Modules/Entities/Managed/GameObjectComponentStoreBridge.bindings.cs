// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/GameObjectEntityTypes.h")]
    static unsafe partial class GameObjectComponentStoreBridge
    {
        [FreeFunction("GameObjectEntityTypes::Set")]
        static extern void SetGameObjectEntityTypes(uint* typeIds, int count);
    }
}
