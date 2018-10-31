// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.Build.Reporting;
using Mono.Cecil;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor
{
    // Building options. Multiple options can be combined together.
    [Flags]
    public enum BuildOptions
    {
        // Perform the specified build without any special settings or extra tasks.
        None = 0,

        // Build a development version of the standalone player.
        Development = 1 << 0,

        // Run the built player.
        AutoRunPlayer = 1 << 2,

        // Show the built player.
        ShowBuiltPlayer = 1 << 3,

        // Build a compressed asset bundle that contains streamed scenes loadable with the WWW class.
        BuildAdditionalStreamedScenes = 1 << 4,

        // Do not overwrite player directory, but accept user's modifications.
        AcceptExternalModificationsToPlayer = 1 << 5,

        //*undocumented*
        InstallInBuildFolder = 1 << 6,

        //*undocumented*
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("WebPlayer has been removed in 5.4", true)]
        WebPlayerOfflineDeployment = 1 << 7,

        // automatically connects the profiler when the build is ran
        ConnectWithProfiler = 1 << 8,

        // Allow script debuggers to attach to the player remotely.
        AllowDebugging = 1 << 9,

        // Symlink runtime libraries when generating iOS XCode project. (Faster iteration time).
        SymlinkLibraries = 1 << 10,

        // Don't compress the data when creating the asset bundle.
        UncompressedAssetBundle = 1 << 11,

        [Obsolete("Use BuildOptions.Development instead")]
        StripDebugSymbols = 0,
        [Obsolete("Texture Compression is now always enabled")]
        CompressTextures = 0,

        //Set the player to try to connect to the host
        ConnectToHost = 1 << 12,

        // Headless Mode
        EnableHeadlessMode = 1 << 14,

        // Build scripts only
        BuildScriptsOnly = 1 << 15,

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BuildOptions.IL2CPP is deprecated and has no effect. Use PlayerSettings.SetScriptingBackend() instead.", true)]
        Il2CPP = 0,

        // Include assertions in non development builds
        ForceEnableAssertions = 1 << 17,

        // Forces chunk-based LZ4 compression for the asset bundle. Such asset bundles can be decompressed on the fly.
        CompressWithLz4 = 1 << 18,

        CompressWithLz4HC = 1 << 19,

        //*undocumented*
        [Obsolete("Specify IL2CPP optimization level in Player Settings.")]
        ForceOptimizeScriptCompilation = 0,

        // Request that the CRC of the built output be computed and included in the build report
        ComputeCRC = 1 << 20,

        // Force the build to fail when any errors are encountered
        StrictMode = 1 << 21,

        IncludeTestAssemblies = 1 << 22,

        // Will forces the buildGUID to all zeros
        NoUniqueIdentifier = 1 << 23,

        // Don't add assets to the build. Assets will be downloaded on-demand from the editor.
        // Reserve this flag as it's used in the experimental namespace
        Reserved1 = 1 << 24, // DatalessPlayer
    }

    // Asset Bundle building options.
    [Flags]
    public enum BuildAssetBundleOptions
    {
        // Perform the build without any special option.
        None = 0,

        // Don't compress the data when creating the asset bundle.
        UncompressedAssetBundle = 1, // 1 << 0

        // Includes all dependencies.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.")]
        CollectDependencies = 2, // 1 << 1

        // Forces inclusion of the entire asset.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.")]
        CompleteAssets = 4, // 1 << 2

        // Do not include type information within the AssetBundle.
        DisableWriteTypeTree = 8, // 1 << 3

        // Builds an asset bundle using a hash for the id of the object stored in the asset bundle.
        DeterministicAssetBundle = 16, // 1 << 4

        // Force rebuild the asset bundle.
        ForceRebuildAssetBundle = 32, // 1 << 5

        // Ignore the type tree changes.
        IgnoreTypeTreeChanges = 64, // 1 << 6,

        // Append hash to the output name.
        AppendHashToAssetBundleName = 128, // 1 << 7

        // Forces chunk-based LZ4 compression for the asset bundle. Such asset bundles can be decompressed on the fly.
        ChunkBasedCompression = 256, // 1 << 8

        // Force the build to fail when any errors are encountered
        StrictMode = 512, // 1 << 9

        // Do a dry run build which doesn't actually build the asset bundles.
        DryRunBuild = 1024, // 1 << 10

        // Turns off loading an asset using file name. Results in faster AssetBundle.LoadFromFile.
        DisableLoadAssetByFileName = 4096, // 1 << 12,

        // Turns off loading an asset using file name + extension. Results in faster AssetBundle.LoadFromFile.
        DisableLoadAssetByFileNameWithExtension = 8192 // 1 << 13,
    }

    public struct AssetBundleBuild
    {
        public string   assetBundleName;
        public string   assetBundleVariant;
        public string[] assetNames;
        [NativeName("nameOverrides")]
        public string[] addressableNames;
    }

    public struct BuildPlayerOptions
    {
        public string[] scenes {get; set; }
        public string locationPathName {get; set; }
        public string assetBundleManifestPath {get; set; }
        public BuildTargetGroup targetGroup {get; set; }
        public BuildTarget target {get; set; }
        public BuildOptions options {get; set; }
    }

    // Lets you programmatically build players or AssetBundles which can be loaded from the web.
    [NativeHeader("Editor/Mono/BuildPipeline.bindings.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public class BuildPipeline
    {
        [FreeFunction(IsThreadSafe = true)]
        public static extern BuildTargetGroup GetBuildTargetGroup(BuildTarget platform);

        internal static extern BuildTargetGroup GetBuildTargetGroupByName(string platform);

        internal static extern BuildTarget GetBuildTargetByName(string platform);
        internal static extern EditorScriptCompilationOptions GetScriptCompileFlags(BuildOptions buildOptions, BuildTarget buildTarget);

        [FreeFunction]
        internal static extern string GetBuildTargetGroupDisplayName(BuildTargetGroup targetPlatformGroup);

        [FreeFunction("GetBuildTargetUniqueName", IsThreadSafe = true)]
        internal static extern string GetBuildTargetName(BuildTarget targetPlatform);

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetEditorTargetName();

        // Lets you manage cross-references and dependencies between different asset bundles and player builds.
        [Obsolete("PushAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        [FreeFunction]
        public static extern void PushAssetDependencies();

        // Lets you manage cross-references and dependencies between different asset bundles and player builds.
        [Obsolete("PopAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        [FreeFunction]
        public static extern void PopAssetDependencies();

        private static string[] InvokeCalculateBuildTags(BuildTarget target, BuildTargetGroup group)
        {
            // TODO: This is a temporary function until the new Package Manager comes online. (cases 849472)
            // When that happens, we will walk the list of installed packages, see which ones are active for the current build settings
            // and if they are unity official packages with metadata for analytics, we will append those tags to the returned vector.

            return new string[0];
        }

        private static void LogBuildExceptionAndExit(string buildFunctionName, System.Exception exception)
        {
            Debug.LogErrorFormat("Internal Error in {0}:", buildFunctionName);
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }

        public static BuildReport BuildPlayer(EditorBuildSettingsScene[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(levels);
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = options;
            return BuildPlayer(buildPlayerOptions);
        }

        public static BuildReport BuildPlayer(string[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(target);
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = levels;
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.targetGroup = buildTargetGroup;
            buildPlayerOptions.target = target;
            buildPlayerOptions.options = options;
            return BuildPlayer(buildPlayerOptions);
        }

        public static BuildReport BuildPlayer(BuildPlayerOptions buildPlayerOptions)
        {
            return BuildPlayer(buildPlayerOptions.scenes, buildPlayerOptions.locationPathName, buildPlayerOptions.assetBundleManifestPath, buildPlayerOptions.targetGroup, buildPlayerOptions.target, buildPlayerOptions.options);
        }

        private static BuildReport BuildPlayer(string[] scenes, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot start a new build because there is already a build in progress.");

            if (buildTargetGroup == BuildTargetGroup.Unknown)
                buildTargetGroup = GetBuildTargetGroup(target);

            string locationPathNameError;
            if (!ValidateLocationPathNameForBuildTargetGroup(locationPathName, buildTargetGroup, target, options, out locationPathNameError))
                throw new ArgumentException(locationPathNameError);

            try
            {
                return BuildPlayerInternal(scenes, locationPathName, assetBundleManifestPath, buildTargetGroup, target, options);
            }
            catch (System.Exception exception)
            {
                // In some case BuildPlayer might let a null reference exception fall through. Prevent data loss by just exiting.
                LogBuildExceptionAndExit("BuildPipeline.BuildPlayer", exception);
                return null;
            }
        }

        internal static bool ValidateLocationPathNameForBuildTargetGroup(string locationPathName, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, out string errorMessage)
        {
            if (string.IsNullOrEmpty(locationPathName))
            {
                var willInstallInBuildFolder = (options & BuildOptions.InstallInBuildFolder) != 0 &&
                    PostprocessBuildPlayer.SupportsInstallInBuildFolder(buildTargetGroup, target);
                if (!willInstallInBuildFolder)
                {
                    errorMessage =
                        "The 'locationPathName' parameter for BuildPipeline.BuildPlayer should not be null or empty.";
                    return false;
                }
            }
            else if (string.IsNullOrEmpty(Path.GetFileName(locationPathName)))
            {
                var extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(buildTargetGroup, target, options);

                if (!string.IsNullOrEmpty(extensionForBuildTarget))
                {
                    errorMessage = string.Format(
                        "For the '{0}' target the 'locationPathName' parameter for BuildPipeline.BuildPlayer should not end with a directory separator.\n" +
                        "Provided path: '{1}', expected a path with the extension '.{2}'.", target, locationPathName,
                        extensionForBuildTarget);
                    return false;
                }
            }

            errorMessage = "";

            return true;
        }

        [FreeFunction]
        internal static extern bool IsFeatureSupported(string define, BuildTarget platform);

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, BuildOptions options)
        {
            return BuildPlayer(levels, locationPath, target, options | BuildOptions.BuildAdditionalStreamedScenes).SummarizeErrors();
        }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target)
        {
            return BuildPlayer(levels, locationPath, target, BuildOptions.BuildAdditionalStreamedScenes).SummarizeErrors();
        }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, out uint crc, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            return BuildStreamedSceneAssetBundle(levels, locationPath, buildTargetGroup, target, out crc, options);
        }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        internal static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTargetGroup buildTargetGroup, BuildTarget target, out uint crc, BuildOptions options)
        {
            crc = 0;
            try
            {
                var report = BuildPlayerInternal(levels, locationPath, null, buildTargetGroup, target, options | BuildOptions.BuildAdditionalStreamedScenes | BuildOptions.ComputeCRC);
                crc = report.summary.crc;

                var summary = report.SummarizeErrors();
                UnityEngine.Object.DestroyImmediate(report, true);
                return summary;
            }
            catch (System.Exception exception)
            {
                // In some case BuildPlayer might let a null reference exception fall through. Prevent data loss by just exiting.
                LogBuildExceptionAndExit("BuildPipeline.BuildStreamedSceneAssetBundle", exception);
                return "";
            }
        }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, out uint crc)
        {
            return BuildStreamedSceneAssetBundle(levels, locationPath, target, out crc, 0);
        }

        private static BuildReport BuildPlayerInternal(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options)
        {
            if (!BuildPlayerWindow.DefaultBuildMethods.IsBuildPathValid(locationPathName))
                throw new Exception("Invalid Build Path: " + locationPathName);

            return BuildPlayerInternalNoCheck(levels, locationPathName, assetBundleManifestPath, buildTargetGroup, target, options, false);
        }

        // Is a player currently building?
        public static extern bool isBuildingPlayer {[FreeFunction("IsBuildingPlayer")] get; }

        // Just like BuildPlayer, but does not check for Pro license. Used from build player dialog.
        internal static extern BuildReport BuildPlayerInternalNoCheck(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, bool delayToAfterScriptReload);

#pragma warning disable 618

        // Builds an AssetBundle.
        [Obsolete("BuildAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static bool BuildAssetBundle(UnityEngine.Object mainAsset, UnityEngine.Object[] assets, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            uint crc;
            return BuildAssetBundle(mainAsset, assets, pathName, out crc, assetBundleOptions, targetPlatform);
        }

        [Obsolete("BuildAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static bool BuildAssetBundle(UnityEngine.Object mainAsset, UnityEngine.Object[] assets, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundle(mainAsset, assets, pathName, out crc, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }

        // Builds an AssetBundle.
        internal static bool BuildAssetBundle(UnityEngine.Object mainAsset, UnityEngine.Object[] assets, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            crc = 0;
            try
            {
                return BuildAssetBundleInternal(mainAsset, assets, null, pathName, assetBundleOptions, targetPlatformGroup, targetPlatform, out crc);
            }
            catch (System.Exception exception)
            {
                LogBuildExceptionAndExit("BuildPipeline.BuildAssetBundle", exception);
                return false;
            }
        }

        // Builds an AssetBundle, with custom names for the assets.
        [Obsolete("BuildAssetBundleExplicitAssetNames has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static bool BuildAssetBundleExplicitAssetNames(UnityEngine.Object[] assets, string[] assetNames, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            uint crc;
            return BuildAssetBundleExplicitAssetNames(assets, assetNames, pathName, out crc, assetBundleOptions, targetPlatform);
        }

        // Builds an AssetBundle, with custom names for the assets.
        [Obsolete("BuildAssetBundleExplicitAssetNames has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.")]
        public static bool BuildAssetBundleExplicitAssetNames(UnityEngine.Object[] assets, string[] assetNames, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundleExplicitAssetNames(assets, assetNames, pathName, out crc, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }

        // Builds an AssetBundle, with custom names for the assets.
        internal static bool BuildAssetBundleExplicitAssetNames(UnityEngine.Object[] assets, string[] assetNames, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            crc = 0;
            try
            {
                return BuildAssetBundleInternal(null, assets, assetNames, pathName, assetBundleOptions, targetPlatformGroup, targetPlatform, out crc);
            }
            catch (System.Exception exception)
            {
                LogBuildExceptionAndExit("BuildPipeline.BuildAssetBundleExplicitAssetNames", exception);
                return false;
            }
        }

#pragma warning restore 618

        private static extern bool BuildAssetBundleInternal(UnityEngine.Object mainAsset, UnityEngine.Object[] assets, string[] assetNames, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform, out uint crc);

        public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundles(outputPath, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }

        internal static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot build asset bundles while a build is in progress.");

            if (!System.IO.Directory.Exists(outputPath))
                throw new ArgumentException("The output path \"" + outputPath + "\" doesn't exist");

            return BuildAssetBundlesInternal(outputPath, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }

        [NativeThrows]
        private static extern AssetBundleManifest BuildAssetBundlesInternal(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform);

        public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildTargetGroup targetPlatformGroup = BuildPipeline.GetBuildTargetGroup(targetPlatform);
            return BuildAssetBundles(outputPath, builds, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }

        internal static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot build asset bundles while a build is in progress.");

            if (!System.IO.Directory.Exists(outputPath))
                throw new ArgumentException("The output path \"" + outputPath + "\" doesn't exist");

            if (builds == null)
                throw new ArgumentException("AssetBundleBuild cannot be null.");

            return BuildAssetBundlesWithInfoInternal(outputPath, builds, assetBundleOptions, targetPlatformGroup, targetPlatform);
        }

        [NativeThrows]
        private static extern AssetBundleManifest BuildAssetBundlesWithInfoInternal(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTargetGroup targetPlatformGroup, BuildTarget targetPlatform);

        [FreeFunction("GetPlayerDataSessionId")]
        internal static extern string GetSessionIdForBuildTarget(BuildTarget target);

        [FreeFunction("ExtractCRCFromAssetBundleManifestFile")]
        public static extern bool GetCRCForAssetBundle(string targetPath, out uint crc);

        [FreeFunction("ExtractHashFromAssetBundleManifestFile")]
        public static extern bool GetHashForAssetBundle(string targetPath, out Hash128 hash);

        [FreeFunction("BuildPlayerLicenseCheck", IsThreadSafe = true)]
        internal static extern bool LicenseCheck(BuildTarget target);

        [FreeFunction]
        public static extern bool IsBuildTargetSupported(BuildTargetGroup buildTargetGroup, BuildTarget target);

        [FreeFunction]
        internal static extern string GetBuildTargetAdvancedLicenseName(BuildTarget target);

        internal static string GetPlaybackEngineDirectory(BuildTarget target, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(target);
            return GetPlaybackEngineDirectory(buildTargetGroup, target, options);
        }

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetPlaybackEngineDirectory(BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options);

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetPlaybackEngineExtensionDirectory(BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options);

        internal static extern void SetPlaybackEngineDirectory(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options, string playbackEngineDirectory);

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetBuildToolsDirectory(BuildTarget target);

        [FreeFunction]
        internal static extern string GetMonoBinDirectory(BuildTarget target);

        [FreeFunction]
        internal static extern string GetMonoRuntimeLibDirectory(BuildTarget target);

        [FreeFunction]
        internal static extern string CompatibilityProfileToClassLibFolder(ApiCompatibilityLevel compatibilityLevel);

        internal static string GetBuildTargetGroupName(BuildTarget target)
        {
            return GetBuildTargetGroupName(GetBuildTargetGroup(target));
        }

        [FreeFunction]
        internal static extern string GetBuildTargetGroupName(BuildTargetGroup buildTargetGroup);

        [FreeFunction]
        internal static extern bool IsUnityScriptEvalSupported(BuildTarget target);

        internal static string[] GetReferencingPlayerAssembliesForDLL(string dllPath)
        {
            DefaultAssemblyResolver resolverRoot = new DefaultAssemblyResolver();
            resolverRoot.AddSearchDirectory(Path.GetDirectoryName(dllPath));
            AssemblyDefinition assemblyRoot = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { AssemblyResolver = resolverRoot });

            string[] assemblyPaths = BuildPipeline.GetManagedPlayerDllPaths();
            List<string> referencingAssemblies = new List<string>();

            // determine whether there is an assembly that is referencing the assembly path
            foreach (string assemblyPath in assemblyPaths)
            {
                DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
                AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { AssemblyResolver = resolver });

                foreach (AssemblyNameReference anr in assembly.MainModule.AssemblyReferences)
                {
                    if (anr.FullName == assemblyRoot.Name.FullName)
                    {
                        referencingAssemblies.Add(assemblyPath);
                    }
                }
            }

            return referencingAssemblies.ToArray();
        }

        internal static extern string[] GetManagedPlayerDllPaths();
    }
}
