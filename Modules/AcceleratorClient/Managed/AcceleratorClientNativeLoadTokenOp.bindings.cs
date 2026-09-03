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
        [FreeFunction("AcceleratorClientInvokeLoadToken")]
        internal static extern long InvokeLoadToken();
    }
}
