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
    // Keep in sync with CanAppendBuild in EditorUtility.h
    public enum CanAppendBuild
    {
        Unsupported = 0,
        Yes = 1,
        No = 2,
    }

    // Keep in sync with Runtime\Network\PlayerCommunicator\PlayerConnectionTypes.h
    public enum PlayerConnectionInitiateMode
    {
        None,
        PlayerConnectsToHost,
        PlayerListens
    }

    // Lets you programmatically build players or AssetBundles which can be loaded from the web.
    [NativeHeader("Editor/Mono/BuildPipeline/BuildPipeline.bindings.h")]
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

        [NativeHeader("Editor/Src/BuildPipeline/BuildPlayerHelpers.h")]
        [FreeFunction]
        internal static extern void ShowBuildProfileWindowAndRequireActiveProfile();

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

        // Legacy signature for calling BuildPlayer()
        // Do not add any more overloads of BuildPlayer, or arguments to this method.  Functionality should be added by extending BuildPlayerOptions
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

        // Legacy signature for calling BuildPlayer()
        // Do not add any more overloads of BuildPlayer, or arguments to this method.  Functionality should be added by extending BuildPlayerOptions
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

        // Main entry point for scripts to invoke player builds.
        public static BuildReport BuildPlayer(BuildPlayerOptions buildPlayerOptions)
        {
            if (isBuildingPlayer)
                throw new InvalidOperationException("Cannot start a new build because there is already a build in progress.");

            if (buildPlayerOptions.targetGroup == BuildTargetGroup.Unknown)
                buildPlayerOptions.targetGroup = GetBuildTargetGroup(buildPlayerOptions.target);

            string locationPathNameError;
            if (!ValidateLocationPathNameForBuildTarget(buildPlayerOptions.locationPathName, buildPlayerOptions.target, buildPlayerOptions.subtarget, buildPlayerOptions.options, out locationPathNameError))
                throw new ArgumentException(locationPathNameError);

            string scenesError;
            if (!ValidateScenePaths(buildPlayerOptions.scenes, out scenesError))
                throw new ArgumentException(scenesError);

            if ((buildPlayerOptions.options & BuildOptions.AcceptExternalModificationsToPlayer) == BuildOptions.AcceptExternalModificationsToPlayer)
            {
                CanAppendBuild canAppend = BuildCanBeAppended(buildPlayerOptions.target, buildPlayerOptions.locationPathName);
                if (canAppend == CanAppendBuild.Unsupported)
                    throw new InvalidOperationException("The build target does not support build appending.");
                if (canAppend == CanAppendBuild.No)
                    throw new InvalidOperationException("The build cannot be appended.");
            }

            if (buildPlayerOptions.scenes != null)
            {
                for (int i = 0; i < buildPlayerOptions.scenes.Length; i++)
                    buildPlayerOptions.scenes[i] = buildPlayerOptions.scenes[i].Replace('\\', '/').Replace("//", "/");
            }

            if ((buildPlayerOptions.options & BuildOptions.Development) == 0)
            {
                if ((buildPlayerOptions.options & BuildOptions.AllowDebugging) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow debugging. Either add the Development build option, or remove the AllowDebugging build option.");
                }

                if ((buildPlayerOptions.options & BuildOptions.EnableCodeCoverage) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow code coverage. Either add the Development build option, or remove the EnableCodeCoverage build option.");
                }

                if ((buildPlayerOptions.options & BuildOptions.EnableDeepProfilingSupport) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow deep profiling support. Either add the Development build option, or remove the EnableDeepProfilingSupport build option.");
                }

                if ((buildPlayerOptions.options & BuildOptions.ConnectWithProfiler) != 0)
                {
                    throw new ArgumentException("Non-development build cannot allow auto-connecting the profiler. Either add the Development build option, or remove the ConnectWithProfiler build option.");
                }
            }

            try
            {
                if (!BuildPlayerWindow.DefaultBuildMethods.IsBuildPathValid(buildPlayerOptions.locationPathName, out var msg))
                    throw new ArgumentException($"Invalid build path: '{buildPlayerOptions.locationPathName}'. {msg}");

                if (buildPlayerOptions.targetGroup == BuildTargetGroup.Standalone)
                {
                    if (buildPlayerOptions.subtarget == (int)StandaloneBuildSubtarget.Default)
                        buildPlayerOptions.subtarget = (int)EditorUserBuildSettings.standaloneBuildSubtarget;

                    EditorUserBuildSettings.standaloneBuildSubtarget = (StandaloneBuildSubtarget)buildPlayerOptions.subtarget;
                }

                return BuildPlayerInternal(
                    buildPlayerOptions.scenes,
                    buildPlayerOptions.locationPathName,
                    buildPlayerOptions.assetBundleManifestPath,
                    buildPlayerOptions.targetGroup,
                    buildPlayerOptions.target,
                    buildPlayerOptions.subtarget,
                    buildPlayerOptions.options,
                    buildPlayerOptions.extraScriptingDefines,
                    buildPlayerOptions.previousBuildMetadataLocations,
                    false);
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

        internal static BuildReport BuildPlayerFromUI(BuildPlayerOptions buildPlayerOptions, bool delayToAfterScriptReload)
        {
            // The entry point used by the UI (see DefaultBuildMethods.BuildPlayer())
            // TODO: unify more of the error handling / logging behavior between the UI and script-based entry points
            // to reduce accidental inconsistencies.
            return BuildPlayerInternal(
                buildPlayerOptions.scenes,
                buildPlayerOptions.locationPathName,
                buildPlayerOptions.assetBundleManifestPath,
                buildPlayerOptions.targetGroup,
                buildPlayerOptions.target,
                buildPlayerOptions.subtarget,
                buildPlayerOptions.options,
                buildPlayerOptions.extraScriptingDefines,
                buildPlayerOptions.previousBuildMetadataLocations,
                delayToAfterScriptReload
                );
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

        // Is a player currently building?
        public static extern bool isBuildingPlayer { [FreeFunction("IsBuildingPlayer")] get; }


        // Entry point into the C++ implementation of BuildPlayer()
        internal static extern BuildReport BuildPlayerInternal(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines, string[] previousBuildMetadataLocations, bool delayToAfterScriptReload);

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

        // For testing purposes - perform only the "content build" portion of a Player Build.
        // Rather than supporting a build output folder, the generated "Data" directory will remain in the kPlayerDataCache location
        // In practice this performs most of the same setup steps as regular BuildPlayer, but not the finalizing packaging stages.
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

        /*UCBP-PUBLIC*/ internal static BuildReport BuildContentDirectory(BuildContentDirectoryParameters buildParameters)
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

            if (string.IsNullOrEmpty(buildParameters.outputPath))
                throw new ArgumentException("BuildContentDirectoryParameters.outputPath cannot be empty");

            if (!System.IO.Directory.Exists(buildParameters.outputPath))
            {
                // Attempt an on the fly creation of the directory.
                try
                {
                    System.IO.Directory.CreateDirectory(buildParameters.outputPath);
                }
                catch(System.Exception ex)
                {
                    Debug.LogError($"BuildContentDirectory failed. The output path '{buildParameters.outputPath}' does not exist and cannot be created.");
                    // Pass up the specific exception so that the cause (permissions, invalid filename etc) is not swallowed.
                    throw ex;
                }
            }
            buildParameters.outputPath = buildParameters.outputPath.Replace('\\', '/');
            return BuildContentDirectoryInternal(buildParameters);
        }

        [NativeThrows]
        private static extern BuildReport BuildContentDirectoryInternal(BuildContentDirectoryParameters buildParameters);

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

        [FreeFunction("GetPlayerDataCache")]
        internal static extern string GetDataCacheForBuildTarget(BuildTarget target, int subtarget);

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

        [FreeFunction]
        internal static extern bool IsServerBuildPlatformSupported(BuildTarget target);

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

        /*UCBP-PUBLIC*/ internal static extern void CleanBuildCache();
    }
}
