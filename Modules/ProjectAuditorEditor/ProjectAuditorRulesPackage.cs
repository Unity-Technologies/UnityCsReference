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
            Initialize();
        }

        public static void Initialize()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var paths = AssetDatabase.FindAssets("t:asmdef", ["Packages"])
                .Select(AssetDatabase.GUIDToAssetPath);
            var asmDefPath = paths.FirstOrDefault(path => path.EndsWith("Unity.ProjectAuditor.Editor.asmdef"));
#pragma warning restore RS0030
            Path = string.IsNullOrEmpty(asmDefPath) ?
                k_CanonicalPath :
                PathUtils.GetDirectoryName(PathUtils.GetDirectoryName(asmDefPath));

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var packageInfo = PackageUtils.GetClientPackages().FirstOrDefault(p => p.name == Name);
#pragma warning restore RS0030

            IsInstalled = (packageInfo != null);
            if (IsInstalled)
            {
                LatestVersion = packageInfo.versions.latest;
                IsLatest = LatestVersion == packageInfo.version;
                IsLocal = packageInfo.source == PackageSource.Local;
                Version = packageInfo.version;
                var splitVersion = packageInfo.version.Split('.');
                VersionShort = splitVersion[0] + '.' + splitVersion[1];
            }
            else
            {
                IsLatest = false;
                IsLocal = false;
                Version = string.Empty;
                LatestVersion = string.Empty;
                VersionShort = string.Empty;
            }
        }

        public static bool IsInstalled { get; private set; }
        public static bool IsLatest { get; private set; }
        public static bool IsLocal { get; private set; }

        public const string Name = "com.unity.project-auditor-rules";

        public static string Path { get; private set; }

        public static string Version { get; private set; }
        public static string LatestVersion { get; private set; }

        public static string VersionShort { get; private set; }
    }
}
