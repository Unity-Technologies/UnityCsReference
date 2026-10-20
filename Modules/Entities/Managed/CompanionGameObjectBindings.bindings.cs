// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/CompanionGameObjectBindings.bindings.h")]
    internal static unsafe class CompanionGameObjectBindings
    {
        // Instantiates count copies of source into its scene, writing the new GameObject and Transform ids.
        [FreeFunction("CompanionGameObjectBindings::InstantiateCompanionGameObjects")]
        internal static extern void InstantiateCompanionGameObjects(EntityId source, void* newEntityIds, void* newTransformEntityIds, int count);
    }
}
