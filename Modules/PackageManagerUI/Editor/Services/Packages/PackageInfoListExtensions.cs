// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageInfoListExtensions
    {
        public static IEnumerable<PackageInfo> ByName(this IEnumerable<PackageInfo> list, string name)
        {
            return from package in list where package.Name == name select package;
        }

        public static void SetCurrent(this IEnumerable<PackageInfo> list, bool current = true)
        {
            foreach (var package in list)
            {
                package.IsInstalled = current;
            }
        }

        public static void SetLatest(this IEnumerable<PackageInfo> list, bool latest = true)
        {
            foreach (var package in list)
            {
                package.IsLatest = latest;
            }
        }

        public static void SetGroup(this IEnumerable<PackageInfo> list, string group)
        {
            foreach (var package in list)
            {
                package.Group = group;
            }
        }
    }
}
