// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEditor.PackageManager;

namespace Unity.ProjectAuditor.Editor
{
    internal class ProjectAuditorRulesPackage
    {
        const string k_CanonicalPath = "Packages/" + Name;

        static ProjectAuditorRulesPackage()
        {
            var paths = AssetDatabase.FindAssets("t:asmdef", new string[] { "Packages" })
                .Select(AssetDatabase.GUIDToAssetPath);
            var asmDefPath = paths.FirstOrDefault(path => path.EndsWith("Unity.ProjectAuditor.Editor.asmdef"));
            Path = string.IsNullOrEmpty(asmDefPath) ?
                k_CanonicalPath :
                PathUtils.GetDirectoryName(PathUtils.GetDirectoryName(asmDefPath));

            var packageInfo = PackageUtils.GetClientPackages().FirstOrDefault(p => p.name == Name);

            IsInstalled = (packageInfo != null);
            if (IsInstalled)
            {
                IsLatest = packageInfo.versions.latest == packageInfo.version;
                IsLocal = packageInfo.source == PackageSource.Local;
                Version = packageInfo.version;
                var splitVersion = packageInfo.version.Split('.');
                VersionShort = splitVersion[0] + '.' + splitVersion[1];
            }
        }

        public static bool IsInstalled { get; }
        public static bool IsLatest { get; }
        public static bool IsLocal { get; }

        public const string Name = "com.unity.project-auditor-rules";

        public static string Path { get; }

        public static string Version { get; }

        public static string VersionShort { get; }
    }
}
