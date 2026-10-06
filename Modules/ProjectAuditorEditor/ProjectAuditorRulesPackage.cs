// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

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
            #pragma warning disable UAC2001 // Avoid Linq
            var paths = AssetDatabase.FindAssets("t:asmdef", ["Packages"])
                .Select(AssetDatabase.GUIDToAssetPath);
            var asmDefPath = paths.FirstOrDefault(path => path.EndsWith("Unity.ProjectAuditor.Editor.asmdef"));
#pragma warning restore UAC2001
            Path = string.IsNullOrEmpty(asmDefPath) ?
                k_CanonicalPath :
                PathUtils.GetDirectoryName(PathUtils.GetDirectoryName(asmDefPath));

            #pragma warning disable UAC2001 // Avoid Linq
            var packageInfo = PackageInfo.GetAllRegisteredPackages().FirstOrDefault(p => p.name == Name);
#pragma warning restore UAC2001

            IsInstalled = (packageInfo != null);
            if (IsInstalled)
            {
                LatestVersion = packageInfo.versions.latest;
                IsLatest = Utility.CompareVersions(LatestVersion, packageInfo.version) <= 0;
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

        [NoAutoStaticsCleanup] // package state is set by static ctor at startup; packages don't change during script-only reloads, so the value is safe to persist
        public static bool IsInstalled { get; private set; }
        [NoAutoStaticsCleanup] // same as IsInstalled
        public static bool IsLatest { get; private set; }
        [NoAutoStaticsCleanup] // same as IsInstalled
        public static bool IsLocal { get; private set; }

        public const string Name = "com.unity.project-auditor-rules";

        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string Path { get; private set; }

        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string Version { get; private set; }
        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string LatestVersion { get; private set; }

        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string VersionShort { get; private set; }
    }
}
