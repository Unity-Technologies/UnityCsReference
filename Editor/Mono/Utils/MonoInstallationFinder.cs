// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;

namespace UnityEditor.Utils
{
    class MonoInstallationFinder
    {
        public const string MonoInstallation = "Mono";
        public const string MonoBleedingEdgeInstallation = "MonoBleedingEdge";

        public static string GetFrameWorksFolder()
        {
            var editorAppPath = FileUtil.NiceWinPath(EditorApplication.applicationPath);
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return Path.Combine(Path.GetDirectoryName(editorAppPath), "Data");
            else if (Application.platform == RuntimePlatform.OSXEditor)
                return Path.Combine(editorAppPath, "Contents");
            else // Linux...?
                return Path.Combine(Path.GetDirectoryName(editorAppPath), "Data");
        }

        public static string GetProfileDirectory(string profile)
        {
            var monoprefix = GetMonoInstallation();
            return Path.Combine(monoprefix, Path.Combine("lib", Path.Combine("mono", profile)));
        }

        public static string GetProfileDirectory(string profile, string monoInstallation)
        {
            var monoprefix = GetMonoInstallation(monoInstallation);
            return Path.Combine(monoprefix, Path.Combine("lib", Path.Combine("mono", profile)));
        }

        public static string GetProfilesDirectory(string monoInstallation)
        {
            var monoprefix = GetMonoInstallation(monoInstallation);
            return Path.Combine(monoprefix, Path.Combine("lib", "mono"));
        }

        public static string GetEtcDirectory(string monoInstallation)
        {
            var monoprefix = GetMonoInstallation(monoInstallation);
            return Path.Combine(monoprefix, Path.Combine("etc", "mono"));
        }

        public static string GetMonoInstallation()
        {
            return GetMonoInstallation(MonoInstallation);
        }

        public static string GetMonoBleedingEdgeInstallation()
        {
            return GetMonoInstallation(MonoBleedingEdgeInstallation);
        }

        public static string GetMonoInstallation(string monoName)
        {
            return Path.Combine(GetFrameWorksFolder(), monoName);
        }
    }
}
