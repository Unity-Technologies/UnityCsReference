// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Unity.ProjectAuditor.Editor.UI;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Unity.ProjectAuditor.Editor
{
    internal partial class ProjectAuditorRulesPackage
    {
        const string k_CanonicalPath = "Packages/" + Name;

        static ProjectAuditorRulesPackage()
        {
            Initialize();
        }

        public static void Initialize()
        {
            RulesPackageRefresher.s_StateInitialized = true;

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

        [NoAutoStaticsCleanup] // recomputed by RefreshOnCodeLoaded, which runs after every code reload
        public static bool IsInstalled { get; private set; }
        [NoAutoStaticsCleanup] // same as IsInstalled
        public static bool IsLatest { get; private set; }
        [NoAutoStaticsCleanup] // same as IsInstalled
        public static bool IsLocal { get; private set; }

        public const string Name = "com.unity.project-auditor-rules";

        public const string NotInstalledMessage = "Install the Project Auditor Rules package (" + Name + ") before using Project Auditor.";

        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string Path { get; private set; }

        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string Version { get; private set; }
        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string LatestVersion { get; private set; }

        [NoAutoStaticsCleanup] // same as IsInstalled
        public static string VersionShort { get; private set; }
    }

    // Separate class so running the callback does not trigger ProjectAuditorRulesPackage's static constructor, which fails in native tests
    internal static partial class RulesPackageRefresher
    {
        [NoAutoStaticsCleanup] // survives code reloads like the state it tracks
        internal static bool s_StateInitialized;

        // Installing or removing the package triggers a code reload, but this long-living module's static constructor does not run again.
        [OnCodeLoaded]
        static void RefreshOnCodeLoaded()
        {
            if (!s_StateInitialized)
                return;

            var wasInstalled = ProjectAuditorRulesPackage.IsInstalled;
            var previousVersion = ProjectAuditorRulesPackage.Version;

            ProjectAuditorRulesPackage.Initialize();

            if (ProjectAuditorRulesPackage.IsInstalled != wasInstalled || ProjectAuditorRulesPackage.Version != previousVersion)
                ProjectAuditorWindow.OnRulesPackageChanged();
        }
    }
}
