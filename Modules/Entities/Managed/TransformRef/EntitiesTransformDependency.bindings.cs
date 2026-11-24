// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/EntitiesTransformDependency.bindings.h")]
    internal static unsafe class EntitiesTransformDependencyBindings
    {
        [FreeFunction("EntitiesTransformDependencyBindings::CheckManagedLayout")]
        internal static extern int CheckManagedLayout(int* values, int count);

        // Returns the byte that marks native transform access as possibly having to wait for Entities jobs, or null.
        internal static byte* RegisterWorld(WorldUnmanaged owner) => RegisterWorld(&owner);

        internal static void UnregisterWorld(WorldUnmanaged owner) => UnregisterWorld(&owner);

        // The bindings generator can't marshal WorldUnmanaged
        [FreeFunction("EntitiesTransformDependencyBindings::RegisterWorld")]
        static extern byte* RegisterWorld(void* owner);

        [FreeFunction("EntitiesTransformDependencyBindings::UnregisterWorld")]
        static extern void UnregisterWorld(void* owner);
    }
}
