// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.PackageManager
{
    public static class BuildUtilities
    {
        private static Dictionary<string, IShouldIncludeInBuildCallback> m_PackageNameToCallback = new Dictionary<string, IShouldIncludeInBuildCallback>();

        public static void RegisterShouldIncludeInBuildCallback(IShouldIncludeInBuildCallback cb)
        {
            if (string.IsNullOrEmpty(cb.PackageName))
                throw new ArgumentException("PackageName is empty.", "cb");

            if (m_PackageNameToCallback.ContainsKey(cb.PackageName))
                throw new NotSupportedException("Only one callback per package is supported.");

            m_PackageNameToCallback[cb.PackageName] = cb;
        }

        internal static int ShouldIncludeInBuild(string packagePath, string packageFullPath)
        {
            string packageName = Regex.Match(packagePath, @"\/(.*)\/").Groups[1].Value;
            IShouldIncludeInBuildCallback callback;
            if (packageName != string.Empty && m_PackageNameToCallback.TryGetValue(packageName, out callback))
                return callback.ShouldIncludeInBuild(packageFullPath) ? 1 : 0;

            return -1;
        }
    }
}

