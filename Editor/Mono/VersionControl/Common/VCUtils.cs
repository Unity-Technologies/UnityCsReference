// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;

namespace UnityEditor.VersionControl
{
    public static class VersionControlUtils
    {
        static readonly string k_ProjectPath;

        internal static bool isVersionControlConnected => VersionControlManager.isConnected || Provider.isActive;

        static VersionControlUtils()
        {
            k_ProjectPath = Directory.GetCurrentDirectory().Replace('\\', '/') + '/';
        }

        public static bool IsPathVersioned(string path)
        {
            var vco = VersionControlManager.activeVersionControlObject;
            return vco != null ? IsPathVersionedInternal(path) : Provider.PathIsVersioned(path);
        }

        static bool IsPathVersionedInternal(string path)
        {
            path = Path.GetFullPath(path).Replace('\\', '/');

            // Paths that are outside of the project are not versioned.
            if (!path.StartsWith(k_ProjectPath, StringComparison.OrdinalIgnoreCase))
                return false;

            path = path.Substring(k_ProjectPath.Length);

            // Relative paths in Assets directory are versioned.
            if (IsPathInDirectory(path, "Assets"))
                return true;

            var meta = path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);

            if (meta)
            {
                // Meta files in Packages directory are versioned...
                if (IsPathInDirectory(path, "Packages"))
                {
                    var index = path.LastIndexOf('/');
                    if (index != -1)
                        path = path.Substring(0, index);

                    // ...except if they are in Packages directory itself (i.e. not in a subdirectory).
                    return !string.Equals(path, "Packages", StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                // Relative paths in ProjectSettings and local Packages directories are versioned.
                return IsPathInDirectory(path, "ProjectSettings") || IsPathInDirectory(path, "Packages");
            }

            // Nothing else is versioned.
            return false;
        }

        static bool IsPathInDirectory(string path, string directory)
        {
            return path.StartsWith(directory + '/', StringComparison.OrdinalIgnoreCase) || string.Equals(path, directory, StringComparison.OrdinalIgnoreCase);
        }
    }
}
