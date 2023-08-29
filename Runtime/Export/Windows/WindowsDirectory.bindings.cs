// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine.Bindings;

namespace UnityEngine.Windows
{
    [NativeHeader("PlatformDependent/MetroPlayer/Bindings/WindowsDirectoryBindings.h")]
    public static class Directory
    {
        // We create fake directories in the project mimicking UWP application local storage folders when running in the editor
        private static string GetNamedProjectFolder(string name)
        {
            var path = Path.Combine(Path.GetDirectoryName(Application.dataPath), name);
            if (!Exists(path))
                CreateDirectory(path);

            return path;
        }

        public static string temporaryFolder => GetNamedProjectFolder("TempState");

        public static string localFolder => GetNamedProjectFolder("LocalState");

        public static string roamingFolder => GetNamedProjectFolder("RoamingState");


        public extern static void CreateDirectory(string path);

        public extern static bool Exists(string path);

        public extern static void Delete(string path);
    }
}
