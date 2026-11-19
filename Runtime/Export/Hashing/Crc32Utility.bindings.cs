// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/Hashing/Crc32.bindings.h")]
    [VisibleToOtherModules("UnityEngine.AnimationCoreModule")]
    internal static class Crc32Utility
    {
        // CRC32 of the UTF-8 bytes of the string, like native ComputeCRC32.
        public static uint Compute(string s)
        {
            return Append(0u, s);
        }

        // Extends a finished CRC32 with the UTF-8 bytes of the string, so Append(Compute(a), b) equals Compute(a + b).
        [FreeFunction("Crc32Bindings::Append", IsThreadSafe = true)]
        public static extern uint Append(uint crc, string s);
    }
}
