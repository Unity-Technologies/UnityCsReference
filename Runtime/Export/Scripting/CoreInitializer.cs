// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Initializes the core scripting systems in a controlled order at the earliest managed startup hook.
    // Scripting wrappers can be created as early as LoadDefaultResourcesFromEditor / InitialRefresh, which
    // run after [OnAssemblyLoaded] but before [OnCodeLoaded], so these systems must be initialized under
    // [OnAssemblyLoaded] (not lazily, and not under the later [OnCodeLoaded]). CoreModule loads first, so
    // this runs before any other assembly's code and before wrapper creation.
    internal static partial class CoreInitializer
    {
        // The editor also calls Initialize natively before initialDomainReloadingComplete, so it runs
        // twice per reload. Guards the second call. Reset each domain generation.
        [NoAutoStaticsCleanup] static bool s_Initialized;

        [OnAssemblyLoaded]
        [RequiredByNativeCode]
        internal static void Initialize()
        {
            bool runOnceOnly = !s_Initialized;
            s_Initialized = true;

            // 0. Wrapper-marshalling offsets and scripted-object base-class table. Must run before any
            //    wrapper is resolved (Object.MarshalledUnityObject reads these when resolving a wrapper).
            //    An explicit init rather than a static ctor keeps MarshalledUnityObject .cctor-free, so
            //    resolving pays no per-call class-init check.
            if (runOnceOnly)
                Object.MarshalledUnityObject.Initialize();

            // 1. EntityIdStore FIRST: every UnityEngine.Object resolves its native object through
            //    EntityIdStore.GetNativeObject(id), which reads the store's native tables.
            //    Runs every reload: Burst zeroes its SharedStatic on each one, whatever the ALC layout.
            EntityIdStore.Initialize();

            // 2. TypeManagerV2: fetches the native RTTI layout used by CreateProxyObject's base-chain walk.
            if (runOnceOnly)
                TypeManagerV2.Initialize();
        }
    }
}
