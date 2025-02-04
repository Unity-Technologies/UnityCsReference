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
using UnityEditor.Build.Profile;
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
        AssetBundleStripUnityVersion = 32768, // 1 << 15

        // Calculate bundle hash on the bundle content
        UseContentHash = 65536, // 1 << 16

        // Use when AssetBundle dependencies need to be calculated recursively, such as when you have a dependency chain of matching typed Scriptable Objects
        RecurseDependencies = 131072, // 1 << 17

        // Sprites are normally copied to all bundles that reference them. This flag prevents that behavior if the sprite is not in an atlas.
        StripUnatlasedSpriteCopies = 262144 // 1 << 18
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

    public struct BuildPlayerWithProfileOptions
    {
        public BuildProfile buildProfile { get; set; }
        public string locationPathName { get; set; }
        public string assetBundleManifestPath { get; set; }
        public BuildOptions options { get; set; }
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

        [NativeHeader("Editor/Src/BuildPipeline/BuildPlayerHelpers.h")]
        [FreeFunction]
        internal static extern void ShowBuildProfileWindow();

        [Obsolete("PushAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        [FreeFunction]
        public static extern void PushAssetDependencies();

        [Obsolete("PopAssetDependencies has been made obsolete. Please use the new AssetBundle build system introduced in 5.0 and check BuildAssetBundles documentation for details.", true)]
        [FreeFunction]
        public static extern void PopAssetDependencies();

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

        /// <summary>
        /// Builds a player.
        /// </summary>
        /// <param name="buildPlayerWithProfileOptions">The BuildPlayerWithProfileOptions to be built with.</param>
        /// <returns>A BuildReport giving build process information.</returns>
        /// <exception cref="ArgumentException">Throws if build profile is null.</exception>
        public static BuildReport BuildPlayer(BuildPlayerWithProfileOptions buildPlayerWithProfileOptions)
        {
            var buildProfile = buildPlayerWithProfileOptions.buildProfile;
            if (buildProfile == null)
                throw new ArgumentException("Build profile is invalid.");

            BuildProfileContext.activeProfile = buildProfile;
            var buildPlayerOptions = BuildProfileModuleUtil.GetBuildPlayerOptionsFromActiveProfile(
                buildPlayerWithProfileOptions.locationPathName, buildPlayerWithProfileOptions.assetBundleManifestPath, buildPlayerWithProfileOptions.options);
            return BuildPlayer(buildPlayerOptions);
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
            return BuildPlayer(buildPlayerOptions.scenes, buildPlayerOptions.locationPathName, buildPlayerOptions.assetBundleManifestPath, buildPlayerOptions.targetGroup, buildPlayerOptions.target, buildPlayerOptions.subtarget, buildPlayerOptions.options, buildPlayerOptions.extraScriptingDefines);
        }

        private static BuildReport BuildPlayer(string[] scenes, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot start a new build because there is already a build in progress.");

            if (buildTargetGroup == BuildTargetGroup.Unknown)
                buildTargetGroup = GetBuildTargetGroup(target);

            string locationPathNameError;
            if (!ValidateLocationPathNameForBuildTarget(locationPathName, target, subtarget, options, out locationPathNameError))
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

            if ((options & BuildOptions.Development) == 0)
            {
                if ((options & BuildOptions.AllowDebugging) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow debugging. Either add the Development build option, or remove the AllowDebugging build option.");
                }

                if ((options & BuildOptions.EnableDeepProfilingSupport) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow deep profiling support. Either add the Development build option, or remove the EnableDeepProfilingSupport build option.");
                }

                if ((options & BuildOptions.ConnectWithProfiler) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow auto-connecting the profiler. Either add the Development build option, or remove the ConnectWithProfiler build option.");
                }
            }

            try
            {
                return BuildPlayerInternal(scenes, locationPathName, assetBundleManifestPath, buildTargetGroup, target, subtarget, options, extraScriptingDefines);
            }
            catch (System.ArgumentException argumentException)
            {
                Debug.LogException(argumentException);
                return null;
            }
            catch (System.Exception exception)
            {
                // In some case BuildPlayer might let a null reference exception fall through. Prevent data loss by just exiting.
                LogBuildExceptionAndExit("BuildPipeline.BuildPlayer", exception);
                return null;
            }
        }

        internal static bool ValidateLocationPathNameForBuildTarget(string locationPathName, BuildTarget target, int subtarget, BuildOptions options, out string errorMessage)
        {
            if (string.IsNullOrEmpty(locationPathName))
            {
                var willInstallInBuildFolder = (options & BuildOptions.InstallInBuildFolder) != 0 &&
                    PostprocessBuildPlayer.SupportsInstallInBuildFolder(target);
                if (!willInstallInBuildFolder)
                {
                    errorMessage =
                        "The 'locationPathName' parameter for BuildPipeline.BuildPlayer should not be null or empty.";
                    return false;
                }
            }
            else if (string.IsNullOrEmpty(Path.GetFileName(locationPathName)))
            {
                var extensionForBuildTarget = PostprocessBuildPlayer.GetExtensionForBuildTarget(target, subtarget, options);

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

            if (buildTargetGroup == BuildTargetGroup.Standalone)
            {
                if (subtarget == (int)StandaloneBuildSubtarget.Default)
                    subtarget = (int)EditorUserBuildSettings.standaloneBuildSubtarget;

                EditorUserBuildSettings.standaloneBuildSubtarget = (StandaloneBuildSubtarget)subtarget;
            }

            return BuildPlayerInternalNoCheck(levels, locationPathName, assetBundleManifestPath, buildTargetGroup, target, subtarget, options, extraScriptingDefines, false);
        }

        // Is a player currently building?
        public static extern bool isBuildingPlayer { [FreeFunction("IsBuildingPlayer")] get; }

        // Just like BuildPlayer, but does not check for Pro license. Used from build player dialog.
        internal static extern BuildReport BuildPlayerInternalNoCheck(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines, bool delayToAfterScriptReload);

        internal static extern void BuildPlayerInternalPostBuild(BuildReport report);

        [FreeFunction("WriteBootConfigInternal", ThrowsException = true)]
        static extern void WriteBootConfigInternal(string outputFile, BuildTarget target, BuildOptions options);

        public static void WriteBootConfig(string outputFile, BuildTarget target, BuildOptions options)
        {
            WriteBootConfigInternal(outputFile,  target, options);
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

        // TODO: remove, superseded by IsBuildPlatformSupported()
        [FreeFunction]
        public static extern bool IsBuildTargetSupported(BuildTargetGroup buildTargetGroup, BuildTarget target);

        [FreeFunction]
        internal static extern bool IsBuildPlatformSupported(BuildTarget target);

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

        internal static string GetBuildToolsDirectory(BuildTarget target)
        {
            return Path.Combine(GetPlaybackEngineDirectory(target, BuildOptions.None, false), "Tools");
        }

        [FreeFunction]
        internal static extern string GetMonoRuntimeLibDirectory(BuildTarget target);

        [RequiredByNativeCode]
        internal static string CompatibilityProfileToClassLibFolder(ApiCompatibilityLevel compatibilityLevel)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var suffix = BuildTargetDiscovery.GetPlatformProfileSuffix(target);

            switch (compatibilityLevel)
            {
                case ApiCompatibilityLevel.NET_Unity_4_8:
                    return "unityjit-" + suffix;

                case ApiCompatibilityLevel.NET_Standard:
                    return "unityaot-" + suffix;

                default:
                    Debug.LogError($"Unknown API compatibility level: {compatibilityLevel}.");
                    return "unityjit";
            }
        }

        internal static string GetBuildTargetGroupName(BuildTarget target)
        {
            return GetBuildTargetGroupName(GetBuildTargetGroup(target));
        }

        [FreeFunction]
        internal static extern string GetBuildTargetGroupName(BuildTargetGroup buildTargetGroup);

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
            if (BuildTargetDiscovery.TryGetProperties(targetPlatform, out IPlayerConnectionPlatformProperties properties))
            {
                return properties.SupportsConnect;
            }
            return false;
        }

        [RequiredByNativeCode]
        private static bool DoesBuildTargetSupportPlayerConnectionListening(BuildTarget platform)
        {
            if (BuildTargetDiscovery.TryGetProperties(platform, out IPlayerConnectionPlatformProperties properties))
            {
                return properties.SupportsListen;
            }
            return true;
        }
    }
}
