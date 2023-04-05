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
using UnityEditor.Build;
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

        // For internal use. Used when BuildAssetBundles implementation triggers part of the player build code path when packaging Scenes into AssetBundles.
        BuildAdditionalStreamedScenes = 1 << 4,

        // Do not overwrite player directory, but accept user's modifications.
        AcceptExternalModificationsToPlayer = 1 << 5,

        //*undocumented*
        InstallInBuildFolder = 1 << 6,

        // Do a non-incremental, clean cache build
        CleanBuildCache = 1 << 7,

        // automatically connects the profiler when the build is ran
        ConnectWithProfiler = 1 << 8,

        // Allow script debuggers to attach to the player remotely.
        AllowDebugging = 1 << 9,

        // Symlink runtime libraries when generating iOS XCode project. (Faster iteration time).
        [Obsolete("BuildOptions.SymlinkLibraries is obsolete. Use BuildOptions.SymlinkSources instead (UnityUpgradable) -> [UnityEditor] BuildOptions.SymlinkSources", false)]

        SymlinkLibraries = 1 << 10,

        // Symlink runtime libraries and reference externally .m, .mm, .c, .cpp, .swift files from Unity project when generating iOS XCode project.(Faster iteration time).
        // Reference externally .java, .kt files when generating Android gradle project
        SymlinkSources = 1 << 10,

        // Don't compress the data when creating the asset bundle.
        UncompressedAssetBundle = 1 << 11,

        [Obsolete("Use BuildOptions.Development instead")]
        StripDebugSymbols = 0,
        [Obsolete("Texture Compression is now always enabled")]
        CompressTextures = 0,

        //Set the player to try to connect to the host
        ConnectToHost = 1 << 12,

        //custom connection id
        CustomConnectionID = 1 << 13,

        // Headless Mode
        [Obsolete("Use StandaloneBuildSubtarget.Server instead")]
        EnableHeadlessMode = 1 << 14,

        // Build scripts only
        BuildScriptsOnly = 1 << 15,

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
        DetailedBuildReport = 1 << 29,

        [Obsolete("Shader LiveLink is no longer supported.")]
        ShaderLivelinkSupport = 0,

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
        [Obsolete("This has been made obsolete. It is always disabled in the new AssetBundle build system introduced in 5.0.")]
        CompleteAssets = 4, // 1 << 2

        // Do not include type information within the AssetBundle.
        DisableWriteTypeTree = 8, // 1 << 3

        // Builds an asset bundle using a hash for the id of the object stored in the asset bundle.
        [Obsolete("This has been made obsolete. It is always enabled in the new AssetBundle build system introduced in 5.0.")]
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

    // Keep in sync with CanAppendBuild in EditorUtility.h
    public enum CanAppendBuild
    {
        Unsupported = 0,
        Yes = 1,
        No = 2,
    }

    public struct AssetBundleBuild
    {
        public string   assetBundleName;
        public string   assetBundleVariant;
        public string[] assetNames;
        [NativeName("nameOverrides")]
        public string[] addressableNames;
    }

    // NB! Keep in sync with BuildPlayerOptionsManagedStruct in Editor/Src/BuildPipeline/BuildPlayerOptions.h
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildPlayerOptions
    {
        public string[] scenes {get; set; }
        public string locationPathName {get; set; }
        public string assetBundleManifestPath {get; set; }
        public BuildTargetGroup targetGroup {get; set; }
        public BuildTarget target {get; set; }
        public int subtarget { get; set; }
        public BuildOptions options {get; set; }
        public string[] extraScriptingDefines { get; set; }
    }

    internal struct BuildPlayerDataOptions
    {
        public string[] scenes { get; set; }
        public BuildTargetGroup targetGroup { get; set; }
        public BuildTarget target { get; set; }
        public int subtarget { get; set; }
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


    [StructLayout(LayoutKind.Sequential)]
    public struct BuildAssetBundlesParameters
    {
        public string outputPath { get; set; }
        public AssetBundleBuild[] bundleDefinitions { get; set; }
        public BuildAssetBundleOptions options { get; set; }
        public BuildTarget targetPlatform { get; set; }
        public int subtarget { get; set; }
        public string[] extraScriptingDefines { get; set; }
    }

    // Lets you programmatically build players or AssetBundles which can be loaded from the web.
    [NativeHeader("Editor/Mono/BuildPipeline.bindings.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public class BuildPipeline
    {
        [FreeFunction(IsThreadSafe = true)]
        public static extern BuildTargetGroup GetBuildTargetGroup(BuildTarget platform);

        // Uses the implementation in "BuildPipeline.bindings.h"
        internal static extern BuildTargetGroup GetBuildTargetGroupByName(string platform);

        internal static extern BuildTarget GetBuildTargetByName(string platform);
        internal static extern EditorScriptCompilationOptions GetScriptCompileFlags(BuildOptions buildOptions, BuildTarget buildTarget);

        [FreeFunction]
        internal static extern string GetBuildTargetGroupDisplayName(BuildTargetGroup targetPlatformGroup);

        [FreeFunction("GetBuildTargetUniqueName", IsThreadSafe = true)]
        public static extern string GetBuildTargetName(BuildTarget targetPlatform);

        [FreeFunction(IsThreadSafe = true)]
        internal static extern string GetEditorTargetName();

        [Obsolete("PushAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        [FreeFunction]
        public static extern void PushAssetDependencies();

        [Obsolete("PopAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
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

        [FreeFunction]
        public extern static CanAppendBuild BuildCanBeAppended(BuildTarget target, string location);

        [RequiredByNativeCode]
        internal static BuildPlayerContext PreparePlayerBuild(BuildPlayerOptions buildPlayerOptions)
        {
            var buildPlayerContext = new BuildPlayerContext(buildPlayerOptions);
            BuildPipelineInterfaces.PreparePlayerBuild(buildPlayerContext);
            return buildPlayerContext;
        }

        public static BuildReport BuildPlayer(EditorBuildSettingsScene[] levels, string locationPathName, BuildTarget target, BuildOptions options)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = EditorBuildSettingsScene.GetActiveSceneList(levels);
            buildPlayerOptions.locationPathName = locationPathName;
            buildPlayerOptions.target = target;
            buildPlayerOptions.subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(target);
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
            buildPlayerOptions.subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(target);
            buildPlayerOptions.options = options;
            return BuildPlayer(buildPlayerOptions);
        }

        public static BuildReport BuildPlayer(BuildPlayerOptions buildPlayerOptions)
        {
            if (GetBuildTargetGroup(buildPlayerOptions.target) == BuildTargetGroup.Standalone &&
                buildPlayerOptions.subtarget == (int)StandaloneBuildSubtarget.Default)
            {
                buildPlayerOptions.subtarget = (int) EditorUserBuildSettings.standaloneBuildSubtarget;
            }
            EditorUserBuildSettings.standaloneBuildSubtarget = (StandaloneBuildSubtarget) buildPlayerOptions.subtarget;

            return BuildPlayer(buildPlayerOptions.scenes, buildPlayerOptions.locationPathName, buildPlayerOptions.assetBundleManifestPath, buildPlayerOptions.targetGroup, buildPlayerOptions.target, buildPlayerOptions.subtarget, buildPlayerOptions.options, buildPlayerOptions.extraScriptingDefines);
        }

        private static BuildReport BuildPlayer(string[] scenes, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot start a new build because there is already a build in progress.");

            if (buildTargetGroup == BuildTargetGroup.Unknown)
                buildTargetGroup = GetBuildTargetGroup(target);

            string locationPathNameError;
            if (!ValidateLocationPathNameForBuildTargetGroup(locationPathName, buildTargetGroup, target, subtarget, options, out locationPathNameError))
                throw new ArgumentException(locationPathNameError);

            string scenesError;
            if (!ValidateScenePaths(scenes, out scenesError))
                throw new ArgumentException(scenesError);

            if ((options & BuildOptions.AcceptExternalModificationsToPlayer) == BuildOptions.AcceptExternalModificationsToPlayer)
            {
                CanAppendBuild canAppend = BuildCanBeAppended(target, locationPathName);
                if (canAppend == CanAppendBuild.Unsupported)
                    throw new InvalidOperationException("The build target does not support build appending.");
                if (canAppend == CanAppendBuild.No)
                    throw new InvalidOperationException("The build cannot be appended.");
            }

            if (scenes != null)
            {
                for (int i = 0; i < scenes.Length; i++)
                    scenes[i] = scenes[i].Replace('\\', '/').Replace("//", "/");
            }

            try
            {
                return BuildPlayerInternal(scenes, locationPathName, assetBundleManifestPath, buildTargetGroup, target, subtarget, options, extraScriptingDefines);
            }
            catch (System.ArgumentException argumentException)
            {
                Debug.LogException(argumentException);
                EditorApplication.Exit(1);
                return null;
            }
            catch (System.Exception exception)
            {
                // In some case BuildPlayer might let a null reference exception fall through. Prevent data loss by just exiting.
                LogBuildExceptionAndExit("BuildPipeline.BuildPlayer", exception);
                return null;
            }
        }

        internal static bool ValidateLocationPathNameForBuildTargetGroup(string locationPathName, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, out string errorMessage)
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
                var extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(buildTargetGroup, target, subtarget, options);

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

        internal static bool ValidateScenePaths(string[] scenes, out string errorMessage)
        {
            if (scenes != null)
            {
                for (int i = 0; i < scenes.Length; i++)
                {
                    string scenePath = scenes[i].Replace('\\', '/');

                    if (scenePath.Contains("///"))
                    {
                        errorMessage = string.Format("Scene path \"{0}\" contains invalid directory separators.", scenes[i]);
                        return false;
                    }
                }
            }

            errorMessage = "";

            return true;
        }

        [FreeFunction]
        internal static extern bool IsFeatureSupported(string define, BuildTarget platform);

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, BuildOptions options) { return ""; }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target) { return ""; }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, out uint crc, BuildOptions options)
        {
            crc = 0;
            return "";
        }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        internal static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, out uint crc, BuildOptions options)
        {
            crc = 0;
            return "";
        }

        // Builds one or more scenes and all their dependencies into a compressed asset bundle.
        [Obsolete("BuildStreamedSceneAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static string BuildStreamedSceneAssetBundle(string[] levels, string locationPath, BuildTarget target, out uint crc)
        {
            crc = 0;
            return "";
        }

        private static BuildReport BuildPlayerInternal(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines)
        {
            if (!BuildPlayerWindow.DefaultBuildMethods.IsBuildPathValid(locationPathName, out var msg))
                throw new ArgumentException($"Invalid build path: '{locationPathName}'. {msg}");

            return BuildPlayerInternalNoCheck(levels, locationPathName, assetBundleManifestPath, buildTargetGroup, target, subtarget, options, extraScriptingDefines, false);
        }

        // Is a player currently building?
        public static extern bool isBuildingPlayer { [FreeFunction("IsBuildingPlayer")] get; }

        // Just like BuildPlayer, but does not check for Pro license. Used from build player dialog.
        internal static extern BuildReport BuildPlayerInternalNoCheck(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines, bool delayToAfterScriptReload);

        internal static extern void BuildPlayerInternalPostBuild(BuildReport report);


        [FreeFunction("WriteBootConfig", ThrowsException = true)]
        static extern void WriteBootConfig(string outputFile, BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options);

        public static void WriteBootConfig(string outputFile, BuildTarget target, BuildOptions options)
        {
            WriteBootConfig(outputFile, BuildPipeline.GetBuildTargetGroup(target), target, options);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BuildPlayerDataResult
        {
            internal BuildReport report;
            internal RuntimeClassRegistry usedClasses;
        }

        internal static BuildReport BuildPlayerData(BuildPlayerDataOptions buildPlayerDataOptions, out RuntimeClassRegistry usedClasses)
        {
            if (GetBuildTargetGroup(buildPlayerDataOptions.target) == BuildTargetGroup.Standalone &&
                buildPlayerDataOptions.subtarget == (int)StandaloneBuildSubtarget.Default)
            {
                buildPlayerDataOptions.subtarget = (int) EditorUserBuildSettings.standaloneBuildSubtarget;
            }

            var result = BuildPlayerData(buildPlayerDataOptions);
            usedClasses = result.usedClasses;
            return result.report;
        }

        private static extern BuildPlayerDataResult BuildPlayerData(BuildPlayerDataOptions buildPlayerDataOptions);
#pragma warning disable 618

        // Builds an AssetBundle.
        [Obsolete("BuildAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static bool BuildAssetBundle(UnityEngine.Object mainAsset, UnityEngine.Object[] assets, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform) { return false; }

        [Obsolete("BuildAssetBundle has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static bool BuildAssetBundle(UnityEngine.Object mainAsset, UnityEngine.Object[] assets, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            crc = 0;
            return false;
        }

        // Builds an AssetBundle, with custom names for the assets.
        [Obsolete("BuildAssetBundleExplicitAssetNames has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static bool BuildAssetBundleExplicitAssetNames(UnityEngine.Object[] assets, string[] assetNames, string pathName, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            return false;
        }

        // Builds an AssetBundle, with custom names for the assets.
        [Obsolete("BuildAssetBundleExplicitAssetNames has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        public static bool BuildAssetBundleExplicitAssetNames(UnityEngine.Object[] assets, string[] assetNames, string pathName, out uint crc, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            crc = 0;
            return false;
        }

#pragma warning restore 618


        public static AssetBundleManifest BuildAssetBundles(string outputPath, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            BuildAssetBundlesParameters input = new BuildAssetBundlesParameters
            {
                outputPath = outputPath,
                bundleDefinitions = null, // Bundle assignment will be read from AssetDatabase
                options = assetBundleOptions,
                targetPlatform = targetPlatform,
                subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(targetPlatform),
            };

            return BuildAssetBundles(input);
        }

        public static AssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds, BuildAssetBundleOptions assetBundleOptions, BuildTarget targetPlatform)
        {
            if (builds == null)
                // This signature is specifically meant for specifying the bundle definition array, so it is not optional
                throw new ArgumentException("AssetBundleBuild cannot be null.");

            BuildAssetBundlesParameters input = new BuildAssetBundlesParameters
            {
                outputPath = outputPath,
                bundleDefinitions = builds,
                options = assetBundleOptions,
                targetPlatform = targetPlatform,
                subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(targetPlatform),
            };

            return BuildAssetBundles(input);
        }

        public static AssetBundleManifest BuildAssetBundles(BuildAssetBundlesParameters buildParameters)
        {
            if (buildParameters.targetPlatform == 0 || buildParameters.targetPlatform == BuildTarget.NoTarget)
            {
                buildParameters.targetPlatform = EditorUserBuildSettings.activeBuildTarget;

                // Note: subtarget is associated with multiple enums, and 0 may have a specific meaning,
                // so we only auto-set it when the target is also coming from the build settings
                buildParameters.subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(buildParameters.targetPlatform);
            }

            // For Standalone platforms, the Default subtarget means to use the current active one
            if (GetBuildTargetGroup(buildParameters.targetPlatform) == BuildTargetGroup.Standalone &&
                (StandaloneBuildSubtarget)buildParameters.subtarget == StandaloneBuildSubtarget.Default)
            {
                buildParameters.subtarget = EditorUserBuildSettings.GetActiveSubtargetFor(buildParameters.targetPlatform);
            }

            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot build asset bundles while a build is in progress.");

            if (!System.IO.Directory.Exists(buildParameters.outputPath))
                throw new ArgumentException("The output path \"" + buildParameters.outputPath + "\" doesn't exist");

            return BuildAssetBundlesInternal(buildParameters);
        }

        [NativeThrows]
        private static extern AssetBundleManifest BuildAssetBundlesInternal(BuildAssetBundlesParameters buildParameters);

        [FreeFunction("GetPlayerDataSessionId")]
        internal static extern string GetSessionIdForBuildTarget(BuildTarget target, int subtarget);

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
            return GetPlaybackEngineDirectory(target, options, true);
        }

        public static string GetPlaybackEngineDirectory(BuildTarget target, BuildOptions options, bool assertUnsupportedPlatforms)
        {
            BuildTargetGroup buildTargetGroup = GetBuildTargetGroup(target);
            return GetPlaybackEngineDirectory(buildTargetGroup, target, options, assertUnsupportedPlatforms);
        }

        public static string GetPlaybackEngineDirectory(BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options)
        {
            return GetPlaybackEngineDirectory(buildTargetGroup, target, options, true);
        }

        [FreeFunction(IsThreadSafe = true)]
        public static extern string GetPlaybackEngineDirectory(BuildTargetGroup buildTargetGroup, BuildTarget target, BuildOptions options, bool assertUnsupportedPlatforms);

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
                targetPlatform == BuildTarget.StandaloneOSX ||
                targetPlatform == BuildTarget.StandaloneWindows ||
                targetPlatform == BuildTarget.StandaloneWindows64 ||
                targetPlatform == BuildTarget.StandaloneLinux64 ||
                targetPlatform == BuildTarget.iOS ||
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
