// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine.Bindings;

namespace Unity.IO
{
    [NativeHeader("Runtime/VirtualFileSystem/UnityFile.bindings.h")]
    [VisibleToOtherModules("UnityEngine.ContentLoadModule")]
    internal static class UnityFile
    {
        [FreeFunction("UnityFile::ReadAllBytes", IsThreadSafe = true, ThrowsException = true)]
        public static extern byte[] ReadAllBytes(string path);

        [FreeFunction("UnityFile::Exists", IsThreadSafe = true)]
        public static extern bool Exists(string path);
    }
}
