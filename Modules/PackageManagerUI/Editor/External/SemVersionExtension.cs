// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿namespace UnityEditor.PackageManager.UI
{
    internal static class SemVersionExtension
    {
        public static string VersionOnly(this SemVersion version)
        {
            return "" + version.Major + "." + version.Minor + "." + version.Patch;
        }
        
        public static string ShortVersion(this SemVersion version)
        {
            return version.Major + "." + version.Minor;
        }                
    }
}
