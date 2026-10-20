// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Runtime/Export/Entities/EntitiesCoreCallbacks.bindings.h")]
    static class EntitiesCoreCallbacksBindings
    {
        // Fills the table through which the Entities module's icalls reach this module's native code.
        [FreeFunction("EntitiesCoreCallbacksBindings::Install")]
        internal static extern void Install();
    }
}
