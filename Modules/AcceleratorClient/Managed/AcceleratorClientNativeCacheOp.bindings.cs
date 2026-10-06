// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.AcceleratorClient
{
    // Managed handle for the native cache-op binding. The native side does the real work from
    // its (possibly background) calling thread under ScopedThreadAttach + UseDomain, then calls
    // back into AsyncClientTestApi.NativeCacheRoundTrip in the client-owned AppDomain. This
    // extern is the reachable entry that lets a script-domain runner drive that native path
    // before and after a reload. Test/proof surface only -- internal, no public API.
    [NativeHeader("Modules/AcceleratorClient/AcceleratorClientModuleRegistration.h")]
    internal static class AcceleratorClientNativeCacheOp
    {
        // Returns 1 on a verified Put/Get round-trip against `endpoint`, 0 on any failure.
        [FreeFunction("AcceleratorClientInvokeCacheRoundTrip")]
        [NativeConditional("ENABLE_UNIT_TESTS")]
        internal static extern long InvokeCacheRoundTrip(string endpoint);
    }

    // Managed handle for the adapter gate. The native entry (defined in the AssetDatabase
    // module) drives AcceleratorClientV2 end-to-end against `endpoint` and returns a packed verdict.
    // Reachable by a script-domain gate runner via reflection into this module assembly.
    [NativeHeader("Modules/AcceleratorClient/AcceleratorClientModuleRegistration.h")]
    internal static class AcceleratorClientV2AdapterGate
    {
        [FreeFunction("AcceleratorClientV2RunAdapterGate")]
        [NativeConditional("ENABLE_UNIT_TESTS")]
        internal static extern long RunAdapterGate(string endpoint);
    }

    // Managed handle for the identity push. Applies an identity to the live V2 client and fans it out to
    // every live import worker, entering at the same point a worker-side push does
    // (SetV2CacheClientIdentity). NOT identical to a UnityConnect refresh: that path re-resolves the rung
    // list first and leaves the token refreshable, whereas this marks it non-refreshable. Exists because a
    // real refresh waits on a ~1h token expiry and cannot be driven in-session. Test/proof surface only.
    [NativeHeader("Modules/AcceleratorClient/AcceleratorClientModuleRegistration.h")]
    internal static class AcceleratorClientV2Identity
    {
        [FreeFunction("AcceleratorClientV2PushIdentity")]
        [NativeConditional("ENABLE_UNIT_TESTS")]
        internal static extern bool PushIdentity(string organization, string projectId, string token);
    }
}
