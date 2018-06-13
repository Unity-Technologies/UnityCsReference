// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    public static class Directory
    {
        [NativeHeader("Runtime/Export/Windows/WindowsDirectoryBindings.h")]
        public extern static string temporaryFolder { get; }

        [NativeHeader("Runtime/Export/Windows/WindowsDirectoryBindings.h")]
        public extern static string localFolder { get; }

        [NativeHeader("Runtime/Export/Windows/WindowsDirectoryBindings.h")]
        public extern static string roamingFolder { get; }

        [NativeHeader("Runtime/Export/Windows/WindowsDirectoryBindings.h")]
        public extern static void CreateDirectory(string path);

        [NativeHeader("Runtime/Export/Windows/WindowsDirectoryBindings.h")]
        public extern static bool Exists(string path);

        [NativeHeader("Runtime/Export/Windows/WindowsDirectoryBindings.h")]
        public extern static void Delete(string path);
    }
}
