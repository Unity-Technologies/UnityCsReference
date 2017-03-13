// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    partial class Application
    {
        [Obsolete("absoluteUrl is deprecated. Please use absoluteURL instead (UnityUpgradable) -> absoluteURL", true)]
        public static string absoluteUrl { get { return absoluteURL; } }

        [Obsolete("bundleIdentifier is deprecated. Please use identifier instead (UnityUpgradable) -> identifier", true)]
        public static string bundleIdentifier { get { return identifier; } }
    }
}
