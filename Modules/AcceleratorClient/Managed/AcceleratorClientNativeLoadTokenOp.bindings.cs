// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.AcceleratorClient
{
    // Test/proof surface only: compares tokens before/after reload to prove the client-owned
    // AppDomain survived (created + owned by AcceleratorClientModuleRegistration.cpp).
    [NativeHeader("Modules/AcceleratorClient/AcceleratorClientModuleRegistration.h")]
    internal static class AcceleratorClientNativeLoadTokenOp
    {
        // Returns 0 on failure.
        //
        // NativeConditional makes the bindings generator wrap the generated glue in
        // #if ENABLE_UNIT_TESTS, with a stub returning 0 otherwise, so the native implementation can be
        // compiled out of installer builds (ENABLE_UNIT_TESTS=0 there) without leaving the glue calling a
        // symbol that no longer exists.
        [FreeFunction("AcceleratorClientInvokeLoadToken")]
        [NativeConditional("ENABLE_UNIT_TESTS")]
        internal static extern long InvokeLoadToken();

        // Whether the client's hosting domain exists RIGHT NOW, without creating it -- unlike
        // InvokeLoadToken above, which ensures it. Exists so a test can assert that a session which did
        // not select the V2 client never paid for one. Always false on CoreCLR, which creates no domain.
        [FreeFunction("AcceleratorClientHasHostingDomain")]
        [NativeConditional("ENABLE_UNIT_TESTS")]
        internal static extern bool HasHostingDomain();
    }
}
