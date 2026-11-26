// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    // Reads text serialized files through YAMLRead and through the binary readers, and compares the results.
    [NativeHeader("Runtime/Serialize/TransferFunctions/YAMLReadABHarness.h")]
    internal static class YAMLReadABHarness
    {
        // How each object of the text serialized file at path compares, with a count per outcome.
        [FreeFunction("YAMLReadABHarnessBindings::CompareTextFile")]
        internal static extern string CompareTextFile(string path);

        // Tests only: -1 follows UNITY_YAML_READ_VIA_BINARY, 0 forces YAMLRead, 1 the binary readers with a fallback to
        // YAMLRead, 2 the binary readers alone, 3 as 1 and each object they read compared with YAMLRead's; returns the
        // previous value.
        [FreeFunction("YAMLReadABHarnessBindings::SetSwitchOverrideForTesting")]
        internal static extern int SetSwitchOverrideForTesting(int value);

        // Tests only: -1 follows UNITY_YAML_READ_VIA_BINARY_LOG_FALLBACKS, 0 forces the message of a fallback off, 1 on;
        // returns the previous value.
        [FreeFunction("YAMLReadABHarnessBindings::SetFallbackLogOverrideForTesting")]
        internal static extern int SetFallbackLogOverrideForTesting(int value);
    }
}
