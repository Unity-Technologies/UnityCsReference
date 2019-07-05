// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Combines a package and a version
    /// </summary>
    internal class PackageVersion
    {
        public Package Package;
        public PackageInfo Version;

        public PackageVersion(Package package, PackageInfo packageInfo)
        {
            Package = package;
            Version = packageInfo;
        }
    }
}
