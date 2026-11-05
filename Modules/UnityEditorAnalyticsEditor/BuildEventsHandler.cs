// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal partial class BuildEventsHandlerPostProcess : IPostprocessBuildWithReport
    {
        [Serializable]
        internal struct SceneViewInfo
        {
            public int total_scene_views;
            public int num_of_2d_views;
            public bool is_default_2d_mode;
        }

        [Serializable]
        internal struct BuildPackageIds
        {
            public string[] package_ids;
            // Only packages hooking into the build without shipping code in it, so never overlaps package_ids
            public string[] build_hook_package_ids;
        }

        [AutoStaticsCleanupOnCodeReload] // build-gate flag: persisting true after reload would permanently skip the event
        private static bool s_EventSent = false;
        [AutoStaticsCleanupOnCodeReload] // build-time accumulator: must reset so the post-build event reflects the new build
        private static int s_NumOfSceneViews = 0;
        [AutoStaticsCleanupOnCodeReload] // build-time accumulator: must reset so the post-build event reflects the new build
        private static int s_NumOf2dSceneViews = 0;

        public int callbackOrder {get { return 0; }}
        public void OnPostprocessBuild(BuildReport report)
        {
            ReportSceneViewInfo();
            ReportBuildPackageIds(report.GetFiles());

            if (BuildTargetDiscovery.TryGetBuildTarget(EditorUserBuildSettings.activeBuildTarget, out var iBuildTarget))
            {
                iBuildTarget.BuildPlatformProperties?.ReportBuildTargetPermissions(report.summary.options);
            }
        }

        private string SanitizePackageId(UnityEditor.PackageManager.PackageInfo packageInfo)
        {
            if (packageInfo.source == UnityEditor.PackageManager.PackageSource.Registry)
                return packageInfo.packageId;
            return packageInfo.name + "@" + Enum.GetName(typeof(UnityEditor.PackageManager.PackageSource), packageInfo.source).ToLower();
        }

        private void ReportBuildPackageIds(BuildFile[] buildFiles)
        {
            List<string> managedLibraries = new List<string>();
            foreach (BuildFile file in buildFiles)
            {
                if (file.role == "ManagedLibrary" || file.role == "dll")
                    managedLibraries.Add(file.path);
            }

            // IL2CPP builds that emit an IDE project (iOS/tvOS/visionOS/project export) ship no managed
            // assemblies in the build output, so buildFiles lists none; player assemblies exist regardless.
            foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies))
                managedLibraries.Add(assembly.outputPath);

            var matchingPackages = UnityEditor.PackageManager.PackageInfo.GetForAssemblyFilePaths(managedLibraries);
            #pragma warning disable UAC2001 // Avoid Linq
            var packageIds = matchingPackages.Select(SanitizePackageId).ToArray();
#pragma warning restore UAC2001
            var buildHookPackageIds = GetIdsOfPackagesOnlyHookingIntoBuild(matchingPackages);
            if (packageIds.Length > 0 || buildHookPackageIds.Length > 0)
                EditorAnalytics.SendEventBuildPackageList(new BuildPackageIds() { package_ids = packageIds, build_hook_package_ids = buildHookPackageIds });
        }

        private string[] GetIdsOfPackagesOnlyHookingIntoBuild(List<UnityEditor.PackageManager.PackageInfo> packagesShippingCode)
        {
            var packagesHookingIntoBuild = UnityEditor.PackageManager.PackageInfo.GetForAssemblyFilePaths(BuildPipelineInterfaces.GetAssemblyPathsDeclaringBuildCallbacks());
            var packageIds = new List<string>();
            foreach (var package in packagesHookingIntoBuild)
            {
                if (!packagesShippingCode.Exists(packageShippingCode => packageShippingCode.name == package.name))
                    packageIds.Add(SanitizePackageId(package));
            }
            return packageIds.ToArray();
        }

        private void ReportSceneViewInfo()
        {
            Object[] views = Resources.FindObjectsOfTypeAll(typeof(SceneView));
            int numOf2dSceneViews = 0;
            foreach (SceneView view in views)
            {
                if (view.in2DMode)
                    numOf2dSceneViews++;
            }
            if ((s_NumOfSceneViews != views.Length) || (s_NumOf2dSceneViews != numOf2dSceneViews) || !s_EventSent)
            {
                s_EventSent = true;
                s_NumOfSceneViews = views.Length;
                s_NumOf2dSceneViews = numOf2dSceneViews;
                EditorAnalytics.SendEventSceneViewInfo(new SceneViewInfo()
                {
                    total_scene_views = s_NumOfSceneViews, num_of_2d_views = s_NumOf2dSceneViews,
                    is_default_2d_mode = EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D
                });
            }
        }
    }

    internal class BuildCompletionEventsHandler
    {
        [Serializable]
        struct BuildLibrariesInfo
        {
            public bool ar_plugin_loaded;
            public string[] build_libraries;
        }

        public static void ReportPostBuildCompletionInfo(List<string> libraries)
        {
            if (libraries != null)
            {
                EditorAnalytics.SendEventBuildFrameworkList(new BuildLibrariesInfo()
                {
                    ar_plugin_loaded = libraries.Contains("System/Library/Frameworks/ARKit.framework"),
                    build_libraries = libraries.ToArray()
                });
            }
        }
    }
} // namespace
