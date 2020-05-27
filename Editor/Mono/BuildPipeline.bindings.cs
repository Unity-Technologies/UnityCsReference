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
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

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

        // Patches the application package without recreating it, applicable to platforms like Android where app packaging is involved
        PatchPackage = 1 << 16,

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

        // Wait for player connection on start
        WaitForPlayerConnection = 1 << 25,

        // Enables Code Coverage. Can be used as a complimentary way of enabling code coverage on platforms
        // that do not support command line arguments
        EnableCodeCoverage = 1 << 26,

        // Only needed internally for AssetBundleStripUnityVersion
        //StripUnityVersion = 1 << 27

        // Enable C# code instrumentation for the player.
        EnableDeepProfilingSupport = 1 << 28,

        // The BuildReport object returned by BuildPipeline.BuildPlayer will contain more details (about build times and contents), at the cost of a slightly (typically, a few percents) longer build time
        DetailedBuildReport = 1 << 29
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
        DisableLoadAssetByFileNameWithExtension = 8192, // 1 << 13,

        //kAssetBundleAllowEditorOnlyScriptableObjects is defined in the native BuildAssetBundleOptions as 1 << 14
        //AssetBundleAllowEditorOnlyScriptableObjects = 1 << 14,

        //Removes the Unity Version number in the Archive File & Serialized File headers during the build.
        AssetBundleStripUnityVersion = 32768 // 1 << 15
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
        public string[] extraScriptingDefines { get; set; }
    }

    internal struct BuildPlayerDataOptions
    {
        public string[] scenes { get; set; }
        public BuildTargetGroup targetGroup { get; set; }
        public BuildTarget target { get; set; }
        public BuildOptions options { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }

    // Keep in sync with Runtime\Network\PlayerCommunicator\PlayerConnectionTypes.h
    public enum PlayerConnectionInitiateMode
    {
        None,
        PlayerConnectsToHost,
        PlayerListens
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
            return BuildPlayer(buildPlayerOptions.scenes, buildPlayerOptions.locationPathName, buildPlayerOptions.assetBundleManifestPath, buildPlayerOptions.targetGroup, buildPlayerOptions.target, buildPlayerOptions.options, buildPlayerOptions.extraScriptingDefines);
        }

        private static BuildReport BuildPlayer(string[] scenes, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, string[] extraScriptingDefines)
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
                return BuildPlayerInternal(scenes, locationPathName, assetBundleManifestPath, buildTargetGroup, target, options, extraScriptingDefines);
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
                var report = BuildPlayerInternal(levels, locationPath, null, buildTargetGroup, target, options | BuildOptions.BuildAdditionalStreamedScenes | BuildOptions.ComputeCRC, new string[] {});
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

        private static BuildReport BuildPlayerInternal(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, string[] extraScriptingDefines)
        {
            if (!BuildPlayerWindow.DefaultBuildMethods.IsBuildPathValid(locationPathName))
                throw new Exception("Invalid Build Path: " + locationPathName);

            return BuildPlayerInternalNoCheck(levels, locationPathName, assetBundleManifestPath, buildTargetGroup, target, options, extraScriptingDefines, false);
        }

        // Is a player currently building?
        public static extern bool isBuildingPlayer { [FreeFunction("IsBuildingPlayer")] get; }

        // Just like BuildPlayer, but does not check for Pro license. Used from build player dialog.
        internal static extern BuildReport BuildPlayerInternalNoCheck(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, string[] extraScriptingDefines, bool delayToAfterScriptReload);

        [StructLayout(LayoutKind.Sequential)]
        private struct BuildPlayerDataResult
        {
            internal BuildReport report;
            internal RuntimeClassRegistry usedClasses;
        }

        internal static BuildReport BuildPlayerData(BuildPlayerDataOptions buildPlayerDataOptions, out RuntimeClassRegistry usedClasses)
        {
            var result = BuildPlayerData(buildPlayerDataOptions);
            usedClasses = result.usedClasses;
            return result.report;
        }

        private static extern BuildPlayerDataResult BuildPlayerData(BuildPlayerDataOptions buildPlayerDataOptions);
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

        public static string GetPlaybackEngineDirectory(BuildTarget target, BuildOptions options)
        {
            BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(target);
            return GetPlaybackEngineDirectory(buildTargetGroup, target, options);
        }

        [FreeFunction(IsThreadSafe = true)]
        public static extern string GetPlaybackEngineDirectory(BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options);

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetPlaybackEngineExtensionDirectory(BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options);

        internal static extern void SetPlaybackEngineDirectory(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options, string playbackEngineDirectory);

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetBuildToolsDirectory(BuildTarget target);

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
        internal static extern bool SupportsReflectionEmit(BuildTarget target);

        internal static string[] GetReferencingPlayerAssembliesForDLL(string dllPath, string assembliesOutputPath)
        {
            DefaultAssemblyResolver resolverRoot = new DefaultAssemblyResolver();
            resolverRoot.AddSearchDirectory(Path.GetDirectoryName(dllPath));
            AssemblyDefinition assemblyRoot = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { AssemblyResolver = resolverRoot });

            string[] assemblyPaths = BuildPipeline.GetManagedPlayerDllPaths(assembliesOutputPath);
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

        internal static extern string[] GetManagedPlayerDllPaths(string assembliesOutputPath);

        [RequiredByNativeCode]
        public static PlayerConnectionInitiateMode GetPlayerConnectionInitiateMode(BuildTarget targetPlatform, BuildOptions buildOptions)
        {
            bool connectProfilerOnStartup = (buildOptions & BuildOptions.ConnectWithProfiler) != 0;

            bool connect = (buildOptions & BuildOptions.ConnectToHost) != 0 || (connectProfilerOnStartup && DoesBuildTargetSupportPlayerConnectionPlayerToEditor(targetPlatform));
            return connect ? PlayerConnectionInitiateMode.PlayerConnectsToHost : PlayerConnectionInitiateMode.PlayerListens;
        }

        [RequiredByNativeCode]
        private static bool DoesBuildTargetSupportPlayerConnectionPlayerToEditor(BuildTarget targetPlatform)
        {
            return
                // Android: support connection from player to Editor in both cases
                //          connecting to 127.0.0.1 (when both Editor and Android are on localhost using USB cable)
                //          connecting to <ip of machine where the Editor is running>, the Android and PC has to be on the same subnet
                targetPlatform == BuildTarget.Android ||
                // WebGL: only supports connecting from player to editor, so always use this when profiling is set up in WebGL.
                targetPlatform == BuildTarget.WebGL ||
                // WSA: When Editor and Windows Store Apps are running on the same device, only connection from Player-To-Editor works
                //      Editor-To-Player doesn't work, seems something is wrong with listening. For ex.,
                //      when player starts, it's starts listening to 10.37.1.227:55207, this is an ip of the machine the application is running on
                //      On the same machine editor is running, and editor simply cannot connect. Tried http://www.nirsoft.net/utils/cports.html, and shows that there's no application listening to that port...
                //      Update: This is actually mentioned in the docs https://msdn.microsoft.com/en-us/library/windows/apps/Hh780593.aspx -
                //         'Windows Runtime app can use an IP loopback only as the target address for a client network request. So a Windows Runtime app that uses a DatagramSocket or StreamSocketListener to listen on an IP loopback address is prevented from receiving any incoming packets.'
                //      Note: if application is launched on another device, and starts lisenting, then the Editor will be able to connect
                targetPlatform == BuildTarget.WSAPlayer;
        }

        [RequiredByNativeCode]
        private static bool DoesBuildTargetSupportPlayerConnectionListening(BuildTarget platform)
        {
            return platform != BuildTarget.WebGL;
        }
    }
}
