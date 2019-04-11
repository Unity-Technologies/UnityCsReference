// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageListExtensions
    {
        public static IEnumerable<Package> Current(this IEnumerable<Package> list)
        {
            return (from package in list where package.Current != null select package);
        }
    }
}
