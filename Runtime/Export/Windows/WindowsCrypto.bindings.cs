// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    public static class Crypto
    {
        [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsCryptoBindings.h")]
        public extern static byte[] ComputeMD5Hash(byte[] buffer);

        [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsCryptoBindings.h")]
        public extern static byte[] ComputeSHA1Hash(byte[] buffer);
    }
}
