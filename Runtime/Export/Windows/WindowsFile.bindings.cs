// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    public static class File
    {
        [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsFileBindings.h")]
        public extern static byte[] ReadAllBytes(string path);

        [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsFileBindings.h")]
        public extern static void WriteAllBytes(string path, byte[] bytes);

        [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsFileBindings.h")]
        public extern static bool Exists(string path);

        [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsFileBindings.h")]
        public extern static void Delete(string path);
    }
}
