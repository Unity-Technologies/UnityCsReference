// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;

namespace UnityEditor.Utils
{
    class NetStandardFinder
    {
        public const string NetStandardInstallation = "NetStandard";

        public static string GetReferenceDirectory()
        {
            var prefix = GetNetStandardInstallation();
            return Path.Combine(prefix, Path.Combine("ref", "2.0.0"));
        }

        public static string GetCompatShimsDirectory()
        {
            return Path.Combine("compat", Path.Combine("2.0.0", "shims"));
        }

        public static string GetNetStandardCompatShimsDirectory()
        {
            var prefix = GetNetStandardInstallation();
            return Path.Combine(prefix, Path.Combine(GetCompatShimsDirectory(), "netstandard"));
        }

        public static string GetDotNetFrameworkCompatShimsDirectory()
        {
            var prefix = GetNetStandardInstallation();
            return Path.Combine(prefix, Path.Combine(GetCompatShimsDirectory(), "netfx"));
        }

        public static string GetNetStandardInstallation()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), NetStandardInstallation);
        }
    }
}
