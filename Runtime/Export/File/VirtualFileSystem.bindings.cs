// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace Unity.IO.LowLevel.Unsafe
{
    [NativeHeader("Runtime/VirtualFileSystem/VirtualFileSystem.h")]
    [StaticAccessor("GetFileSystem()", StaticAccessorType.Dot)]
    public static class VirtualFileSystem
    {
        [FreeFunction(IsThreadSafe = true)]
        public extern static bool GetLocalFileSystemName(string vfsFileName, out string localFileName, out ulong localFileOffset, out ulong localFileSize);

        internal extern static string ToLogicalPath(string physicalPath);
    }
}
