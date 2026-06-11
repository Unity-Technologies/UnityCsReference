// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.Build.Reporting;
using UnityEditor.Scripting;
using UnityEditor.Scripting.ScriptCompilation;
using System.Runtime.InteropServices;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditorInternal;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // Keep in sync with CanAppendBuild in EditorUtility.h
    ///<summary>Whether you can append an existing build using <see cref="BuildOptions.AcceptExternalModificationsToPlayer" />.</summary>
    public enum CanAppendBuild
    {
        ///<summary>The target platform does not support appending builds.</summary>
        Unsupported = 0,
        ///<summary>The target platform supports appending builds, and the build can be appended.</summary>
        Yes = 1,
        ///<summary>The target platform supports appending builds, and the build is not in a valid state.</summary>
        No = 2,
    }

    /// <summary>
    /// Describes how the player connects to the Editor.
    /// </summary>
    public enum PlayerConnectionInitiateMode
    {
        /// <summary>Player connection mode not set.</summary>
        None,
        /// <summary>Player connection is initiated by the player connecting to the host, usually the host is the Editor.</summary>
        PlayerConnectsToHost,
        /// <summary>Player connection is initiated by the player broadcasting its IP address, and then Editor connecting to the player.</summary>
        PlayerListens
    }

    // Lets you programmatically build players or AssetBundles which can be loaded from the web.
    ///<summary>API for building players or AssetBundles.</summary>
    ///<remarks>The BuildPipeline class in the Unity Editor namespace provides essential tools to programmatically <see cref="BuildPipeline.BuildPlayer">Build Players</see> and <see cref="BuildPipeline.BuildAssetBundles">Build AssetBundles</see>.
    ///AssetBundles can be loaded from external sources such as the web, enhancing the flexibility and scalability of content delivery in Unity applications.
    ///The class contains several static properties and methods to facilitate building workflows.</remarks>
    ///<seealso href="xref:um-asset-bundles-intro">AssetBundles</seealso>
    ///<seealso href="xref:um-build-player-pipeline">Build Player Pipeline</seealso>
    [NativeHeader("Editor/Mono/BuildPipeline/BuildPipeline.bindings.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public class BuildPipeline
    {
        /// <summary>
        /// Given a BuildTarget will return the <see cref="BuildTargetGroup" /> for the build target platform.
        /// </summary>
        /// <param name="platform">An instance of the <see cref="BuildTarget" /> enum.</param>
        /// <returns>The <see cref="BuildTargetGroup" /> represented by the passed in <see cref="BuildTarget" />.</returns>
        [FreeFunction(IsThreadSafe = true)]
        public static extern BuildTargetGroup GetBuildTargetGroup(BuildTarget platform);

        // Uses the implementation in "BuildPipeline.bindings.h"
        internal static extern BuildTargetGroup GetBuildTargetGroupByName(string platform);

        internal static extern BuildTarget GetBuildTargetByName(string platform);
        internal static extern EditorScriptCompilationOptions GetScriptCompileFlags(BuildOptions buildOptions, BuildTarget buildTarget);

        [FreeFunction]
        internal static extern string GetBuildTargetGroupDisplayName(BuildTargetGroup targetPlatformGroup);

        ///<summary>Given a BuildTarget will return the well known string representation for the build target platform.</summary>
        ///<param name="targetPlatform">An instance of the <see cref="BuildTarget" /> enum.</param>
        ///<returns>Target platform name represented by the passed in <see cref="BuildTarget" />.</returns>
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

        ///<summary>Checks if Unity can append the build.</summary>
        ///<param name="target">The <see cref="BuildTarget" /> to build.</param>
        ///<param name="location">The path where Unity builds the application.</param>
        ///<returns>Returns a UnityEditor.CanAppendBuild enum that indicates whether Unity can append the build.</returns>
        [FreeFunction]
        public extern static CanAppendBuild BuildCanBeAppended(BuildTarget target, string location);

        [RequiredByNativeCode]
        internal static BuildPlayerContext PreparePlayerBuild(IntPtr buildPlayerOptionsPtr)
        {
            var buildPlayerOptions = BuildPlayerOptions.GetBuildPlayerOptions(buildPlayerOptionsPtr);
            var buildPlayerContext = new BuildPlayerContext(buildPlayerOptions);
            BuildPipelineInterfaces.PreparePlayerBuild(buildPlayerContext);
            return buildPlayerContext;
        }


        [RequiredByNativeCode]
        internal static string[] RetrieveAdditionalBuildReportDirectoriesFromPlayerContext()
        {
            if (BuildPlayerContext.ActiveInstance == null)
                return Array.Empty<string>();
            return BuildPlayerContext.ActiveInstance.RetrieveAdditionalBuildReportDirectories();
        }

        ///<summary>Builds a player from a specific build profile.</summary>
        ///<param name="buildPlayerWithProfileOptions">Provide various options to control the behavior of <see cref="BuildPipeline.BuildPlayer" /> when using a <see cref="BuildProfile">build profile</see>.</param>
        ///<returns>A <see cref="BuildReport" /> object containing build process information.</returns>
        /// <exception cref="ArgumentException">Throws if build profile is null.</exception>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildPipeline_BuildPlayerWithBuildProfile.cs"/>
        ///</example>
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
        ///<summary>Builds a Player. These overloads are still supported, but will be replaced. Please use BuildPlayer(<see cref="BuildPlayerOptions" /> buildPlayerOptions) and BuildPlayer(<see cref="BuildPlayerWithProfileOptions" /> buildPlayerWithProfileOptions) instead.</summary>
        ///<param name="levels">The scenes to include in the build. If empty, the build includes only the current open scene. Paths are relative to the project folder, for example <c>Assets/MyLevels/MyScene.unity</c>.</param>
        ///<param name="locationPathName">The path where the application will be built. For information on the platform extensions to include in the path, refer to [Build path requirements for target platforms](xref:um-build-path-requirements).</param>
        ///<param name="target">The <see cref="BuildTarget" /> to build.</param>
        ///<param name="options">Additional <see cref="BuildOptions" />, like whether to run the built player.</param>
        ///<returns>A <see cref="BuildReport" /> object containing build process information.</returns>
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
        ///<summary>Builds a player.</summary>
        ///<remarks>Use this function to programatically create a build of your project.
        ///
        ///When working with <see cref="BuildProfile"/>, use the overload of <see cref="BuildPipeline.BuildPlayer(UnityEditor.BuildPlayerWithProfileOptions)"/> that accepts <see cref="BuildPlayerWithProfileOptions"/> instead. That overload applies the settings from the specified build profile to the build process.
        ///
        ///Calling this method will invalidate any variables in the editor script that reference GameObjects, so they will need to be reacquired after the call.
        ///
        ///Scripts can run at strategic points during the build by implementing one of the supported callback interfaces, for example <see cref="BuildPlayerProcessor" />, <see cref="IPreprocessBuildWithContext" />, <see cref="IProcessSceneWithReport" /> and <see cref="IPostprocessBuildWithContext" />.
        ///
        ///Note: Be aware that changes to [scripting symbols](xref:um-platform-dependent-compilation) only take effect at the next domain reload, when scripts are recompiled.
        ///
        ///This means if you make changes to the defined scripting symbols via code using <see cref="PlayerSettings.SetDefineSymbolsForGroup" /> without a domain reload before calling this function, those changes won't take effect.
        ///
        ///It also means that the built-in scripting symbols defined for the current active target platform (such as UNITY_STANDALONE_WIN, or UNITY_ANDROID) remain in place even if you try to build for a different target platform, which can result in the wrong code being compiled into your build.</remarks>
        ///<param name="buildPlayerOptions">Provide various options to control the behavior of <see cref="BuildPipeline.BuildPlayer" />.</param>
        ///<returns>A <see cref="BuildReport" /> object containing build process information.</returns>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildPipeline_BuildPlayer.cs"/>
        ///</example>
        ///<seealso cref="BuildPlayerWindow.DefaultBuildMethods.BuildPlayer" />
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
            else
            {
                if ((buildPlayerOptions.options & BuildOptions.EnableCodeCoverage) != 0)
                {
                    if (!BuildProfileModuleUtil.IsBuildTargetSupportedByCoverage(buildPlayerOptions.target))
                        throw new ArgumentException("Code coverage is unavailable for the selected build target. Remove the EnableCodeCoverage build option.");
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
                    buildPlayerOptions.previousBuildReportDirectories,
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
                buildPlayerOptions.previousBuildReportDirectories,
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
        ///<summary>Returns true when Unity is actively building a Player or AssetBundles</summary>
        ///<remarks>This returns true during Player builds (<see cref="BuildPipeline.BuildPlayer" />) and AssetBundle builds (<see cref="BuildPipeline.BuildAssetBundles" />).
        ///It can be used to check the context inside script code that could be triggered during a build, for example when <see cref="ExecuteAlways" /> is being used on a <see cref="MonoBehaviour" />.</remarks>
        public static extern bool isBuildingPlayer { [FreeFunction("IsBuildingPlayer")] get; }


        // Entry point into the C++ implementation of BuildPlayer()
        internal static extern BuildReport BuildPlayerInternal(string[] levels, string locationPathName, string assetBundleManifestPath, BuildTargetGroup buildTargetGroup, BuildTarget target, int subtarget, BuildOptions options, string[] extraScriptingDefines, string[] previousBuildReportDirectories, bool delayToAfterScriptReload);

        internal static extern void BuildPlayerInternalPostBuild(BuildReport report);

        [FreeFunction("WriteBootConfigInternal", ThrowsException = true)]
        static extern void WriteBootConfigInternal(string outputFile, BuildTarget target, BuildOptions options);

        [ExcludeFromDocs]
        public static void WriteBootConfig(string outputFile, BuildTarget target, BuildOptions options)
        {
            WriteBootConfigInternal(outputFile, target, options);
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
                buildPlayerDataOptions.subtarget = (int)EditorUserBuildSettings.standaloneBuildSubtarget;
            }

            var result = BuildPlayerData(buildPlayerDataOptions);
            usedClasses = result.usedClasses;
            return result.report;
        }

        private static extern BuildPlayerDataResult BuildPlayerData(BuildPlayerDataOptions buildPlayerDataOptions);

        // For testing: Populate RuntimeClassRegistry from ScriptsOnlyCache.yaml in metadataDirectory
        [NativeMethod(ThrowsException = false)]
        internal static extern bool PopulateRuntimeClassRegistry(string metadataDirectory, RuntimeClassRegistry runtimeClassRegistry);

        /// <summary>
        /// Builds a content directory (serialized assets and scenes plus a manifest) at a defined output path.
        /// </summary>
        /// <remarks>
        /// Register the folder at runtime with <see cref="Unity.Loading.ContentLoadManager.RegisterContentDirectory"/>.
        /// Each entry in <see cref="BuildContentDirectoryParameters.rootAssetPaths"/> must be a <c>ScriptableObject</c>; the build includes those roots and
        /// everything they reference (including <see cref="Unity.Loading.LoadableObjectId"/>, <see cref="Unity.Loading.Loadable{T}"/>, and <see cref="Unity.Loading.LoadableSceneId"/>).
        /// Creates an <c>outputPath</c> if missing, normalizes path separators, and defaults <see cref="BuildContentDirectoryParameters.name"/> to the output folder name.
        ///
        /// The build uses <see cref="EditorUserBuildSettings.activeBuildTarget"/> and the active subtarget configured for that target in the build settings.
        /// Select the intended platform in the **Build Profile** window or through [command line arguments](xref:um-command-line-arguments) so that the active target is set to the desired setting prior to calling this method.
        /// </remarks>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildPipeline_BuildContentDirectory.cs"/>
        ///</example>
        /// <param name="buildParameters">The build settings to use for the build, such as paths, roots, options, and compression.</param>
        /// <returns>The <see cref="BuildReport"/> that contains the results and details of the build.</returns>
        /// <exception cref="ArgumentException"><see cref="BuildContentDirectoryParameters.outputPath"/> is null or empty.</exception>
        /// <seealso cref="BuildContentDirectoryParameters"/>
        /// <seealso cref="Unity.Loading.ContentLoadManager"/>
        public static BuildReport BuildContentDirectory(BuildContentDirectoryParameters buildParameters)
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

            if (!Directory.Exists(buildParameters.outputPath))
            {
                // Attempt an on the fly creation of the directory.
                try
                {
                    Directory.CreateDirectory(buildParameters.outputPath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"BuildContentDirectory failed. The output path '{buildParameters.outputPath}' does not exist and cannot be created.");
                    // Pass up the specific exception so that the cause (permissions, invalid filename etc) is not swallowed.
                    throw ex;
                }
            }

            buildParameters.outputPath = buildParameters.outputPath.Replace('\\', '/');

            // Default the build name to the output folder leaf if not explicitly set
            if (string.IsNullOrEmpty(buildParameters.name))
                buildParameters.name = Path.GetFileName(buildParameters.outputPath.TrimEnd('/'));

            var buildSessionGuid = GUID.Generate();
            buildParameters.buildStartTimeTicks = DateTime.UtcNow.Ticks;
            buildParameters.metadataPath = BuildHistory.ReserveBuildReportDirectory(
                buildSessionGuid.ToString(), buildParameters.buildStartTimeTicks);

            var report = BuildContentDirectoryInternal(buildParameters, buildSessionGuid);

            BuildHistory.FinalizeBuild(report);

            return report;
        }

        [NativeMethod(ThrowsException = true)]
        private static extern BuildReport BuildContentDirectoryInternal(BuildContentDirectoryParameters buildParameters, GUID buildSessionGuid);

        ///<summary>Build all AssetBundles.</summary>
        ///<remarks>Use this function to build AssetBundles based on the AssetBundle and Label settings you have configured in the Editor. (See the Manual page about [AssetBundles workflow](xref:um-asset-bundles-workflow) for further details.)
        ///
        ///Set <c>outputPath</c> to the folder within your project folder where you want to save the built
        ///bundles (for example: "Assets/MyBundleFolder"). The folder is not created automatically
        ///and the function simply fails if it doesn't already exist.
        ///
        ///Use the optional <c>assetBundleOptions</c> argument to specify bundle build options.
        ///
        ///The <c>targetPlatform</c> argument selects which deployment target (Windows Standalone, Android, iOS, and so on) to build the bundles for. An AssetBundle is only compatible with the specific platform
        ///that it was built for, so you must produce different builds of a given bundle to use the assets on different platforms.
        ///
        ///For new code, the signature of BuildAssetBundles accepting a BuildAssetBundlesParameters structure is recommended instead of this one.
        ///To match the behaviour of this signature leave the <see cref="BuildAssetBundlesParameters.bundleDefinitions" /> field unassigned so that
        ///the Editor's AssetBundle assignments are used.</remarks>
        ///<param name="outputPath">Output path for the AssetBundles.</param>
        ///<param name="assetBundleOptions">AssetBundle building options.</param>
        ///<param name="targetPlatform">Chosen target build platform.</param>
        ///<returns>The manifest listing all AssetBundles included in this build.</returns>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildPipeline_BuildAssetBundles.cs"/>
        ///</example>
        ///<seealso cref="EditorUserBuildSettings.activeBuildTarget" />
        ///<seealso cref="AssetDatabase.GetAssetPathsFromAssetBundle" />
        ///<seealso cref="AssetDatabase.GetImplicitAssetBundleName" />
        ///<seealso cref="AssetImporter.assetBundleName" />
        ///<seealso cref="AssetBundle" />
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

        ///<summary>Build AssetBundles from a building map.</summary>
        ///<remarks>This signature of BuildAssetBundles lets you specify the names and contents
        ///                    of the bundles programmatically, using a "build map" rather than with the details set in the editor.
        ///                    The map is simply an array of <see cref="AssetBundleBuild" /> objects, each of which contains
        ///                    a bundle name and a list of the names of asset files to be added to the named bundle.
        ///
        ///                    For new code, the signature of BuildAssetBundles accepting a BuildAssetBundlesParameters structure is recommended
        ///                    instead of this one.  When using that signature the build map is assigned to <see cref="BuildAssetBundlesParameters.bundleDefinitions" />.</remarks>
        ///<param name="outputPath">Output path for the AssetBundles.</param>
        ///<param name="builds">AssetBundle building map.</param>
        ///<param name="assetBundleOptions">AssetBundle building options.</param>
        ///<param name="targetPlatform">Target build platform.</param>
        ///<returns>The manifest listing all AssetBundles included in this build.</returns>
        ///<example>
        ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildPipeline_BuildAssetBundles2.cs"/>
        ///</example>
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

        ///<summary>Builds the AssetBundles in your project.</summary>
        ///<remarks>
        ///  <para>This signature of BuildAssetBundles is recommended and exposes the most functionality. The other signatures, documented below,
        ///are retained for backward compatibility and convenience.
        ///
        ///During the <see cref="AssetBundle" /> build process, classes that implement <see cref="IProcessSceneWithReport" /> are called as scenes are processed. Similarly the way shaders are built can be influenced by implementing <see cref="IPreprocessShaders" /> or <see cref="IPreprocessComputeShaders" />.
        ///
        ///This method returns an <see cref="AssetBundleManifest" /> instance that describes the AssetBundles produced.
        ///
        ///If the build fails then this method returns null or throws an exception. You can find details about what went wrong in the exception message or in errors logged to the console.
        ///
        ///For example, if you pass an invalid or non-existent path as the <c>outputPath</c> parameter,
        ///the method throws an <c>ArgumentException</c> to indicate that the supplied argument was invalid.
        ///
        ///Specifying a <c>targetPlatform</c> that isn't the active platform causes Unity to change to the new platform and reimport assets. This can cause issues if you create a build in batch mode. To avoid this issue, use the <c>-buildTarget</c> command line argument to set the target platform and then call <c>BuildAssetBundles</c>.
        ///
        ///</para>
        ///  <para>In addition to the AssetBundle files, the build will produce several other files:
        ///
        ///                    * The <see cref="AssetBundleManifest" /> is stored in a small AssetBundle that has the same name as the output folder, for example "MyBundleFolder".
        ///                    This is the same object that is returned from the BuildAssetBundles call, and the serialized form can be useful at runtime,
        ///                    for example to determine the expected hashes of the other AssetBundles.
        ///
        ///                    * The main ".manifest" file, which is a text format file.  It has the same name as the output folder, but using ".manifest" as its extension (for example: "MyBundleFolder.manifest").
        ///                    Assign the path to this manifest file to
        ///                    <see cref="BuildPlayerOptions.assetBundleManifestPath" /> before calling <see cref="BuildPipeline.BuildPlayer" /> to make sure that any types appearing in the AssetBundles are not stripped from the build. (See [Managed code stripping](xref:um-managed-code-stripping) for more information about code stripping.)
        ///
        ///                    * There is also a separate ".manifest" file written for each AssetBundle, based on the name of the AssetBundle.
        ///
        ///                    * Finally, the <see cref="BuildReport" /> for the build is written to "Library/LastBuild.buildreport".</para>
        ///</remarks>
        ///<param name="buildParameters">Configuration of the build.</param>
        ///<returns>The manifest summarizing all AssetBundles generated by the build.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections.Generic;
        ///using System.IO;
        ///using System.Linq;
        ///using UnityEngine;
        ///using UnityEditor;
        ///
        ///public class BuildAssetBundlesExample
        ///{
        ///    [MenuItem("Example/Build AssetBundles")]
        ///    static void BuildBundles()
        ///    {
        ///        List<AssetBundleBuild> assetBundleDefinitionList = new();
        ///
        ///        // Define two asset bundles, populated based on file system structure
        ///
        ///        // The first bundle is all the scene files in the Scenes directory (non-recursive)
        ///        {
        ///            AssetBundleBuild ab = new();
        ///            ab.assetBundleName = "Scenes";
        ///            ab.assetNames = Directory.EnumerateFiles("Assets/" + ab.assetBundleName, "*.unity", SearchOption.TopDirectoryOnly).ToArray();
        ///            assetBundleDefinitionList.Add(ab);
        ///        }
        ///
        ///        // The second bundle is all the asset files found recursively in the Meshes directory
        ///        {
        ///            AssetBundleBuild ab = new();
        ///            ab.assetBundleName = "Meshes";
        ///            ab.assetNames = RecursiveGetAllAssetsInDirectory("Assets/" + ab.assetBundleName).ToArray();
        ///            assetBundleDefinitionList.Add(ab);
        ///        }
        ///
        ///        string outputPath = "MyBuild";  // Subfolder of the current project
        ///
        ///        if (!Directory.Exists(outputPath))
        ///            Directory.CreateDirectory(outputPath);
        ///
        ///        // Assemble all the input needed to perform the build in this structure.
        ///        // The project's current build settings will be used because target and subtarget fields are not filled in
        ///        BuildAssetBundlesParameters buildInput = new()
        ///        {
        ///            outputPath = outputPath,
        ///            options = BuildAssetBundleOptions.AssetBundleStripUnityVersion,
        ///            bundleDefinitions = assetBundleDefinitionList.ToArray()
        ///        };
        ///        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildInput);
        ///
        ///        // Look at the results
        ///        if (manifest != null)
        ///        {
        ///            foreach (var bundleName in manifest.GetAllAssetBundles())
        ///            {
        ///                string projectRelativePath = buildInput.outputPath + "/" + bundleName;
        ///                Debug.Log($"Size of AssetBundle {projectRelativePath} is {new FileInfo(projectRelativePath).Length}");
        ///            }
        ///        }
        ///        else
        ///        {
        ///            Debug.Log("Build failed, see Console and Editor log for details");
        ///        }
        ///    }
        ///
        ///    static List<string> RecursiveGetAllAssetsInDirectory(string path)
        ///    {
        ///        List<string> assets = new();
        ///        foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        ///            if (Path.GetExtension(f) != ".meta" &&
        ///                Path.GetExtension(f) != ".cs" &&  // Scripts are not supported in AssetBundles
        ///                Path.GetExtension(f) != ".unity") // Scenes cannot be mixed with other file types in a bundle
        ///                assets.Add(f);
        ///
        ///        return assets;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using System.IO;
        ///using UnityEngine;
        ///using UnityEditor;
        ///
        ///public class BuildAssetBundlesOutputFileExample
        ///{
        ///    [MenuItem("Example/AssetBundle Output File Example")]
        ///    static void BuildAndPrintOutputFiles()
        ///    {
        ///        var bundleDefinitions = new AssetBundleBuild[]
        ///        {
        ///            new AssetBundleBuild
        ///            {
        ///                assetBundleName = "mybundle",
        ///                assetNames = new string[] { "Assets/Scenes/Scene1.unity" }
        ///            }
        ///        };
        ///
        ///        string buildOutputDirectory = "build";
        ///        Directory.CreateDirectory(buildOutputDirectory);
        ///
        ///        BuildAssetBundlesParameters buildInput = new()
        ///        {
        ///            outputPath = buildOutputDirectory,
        ///            bundleDefinitions = bundleDefinitions
        ///        };
        ///
        ///        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(buildInput);
        ///        if (manifest != null)
        ///        {
        ///            var outputFiles = Directory.EnumerateFiles(buildOutputDirectory, "*", SearchOption.TopDirectoryOnly);
        ///
        ///            //Expected output (on Windows):
        ///            //  Output of the build:
        ///            //      build\build
        ///            //      build\build.manifest
        ///            //      build\mybundle
        ///            //      build\mybundle.manifest
        ///            Debug.Log("Output of the build:\n\t" + string.Join("\n\t", outputFiles));
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="xref:um-asset-bundles-intro">AssetBundles</seealso>
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

        [NativeMethod(ThrowsException = true)]
        private static extern AssetBundleManifest BuildAssetBundlesInternal(BuildAssetBundlesParameters buildParameters);

        [FreeFunction("GetPlayerDataCache")]
        internal static extern string GetDataCacheForBuildTarget(BuildTarget target, int subtarget);

        [FreeFunction("GetPlayerDataSessionId")]
        internal static extern string GetSessionIdForBuildTarget(BuildTarget target, int subtarget);

        ///<summary>Extract the CRC checksum for the given AssetBundle.</summary>
        ///<remarks>A 32-bit checksum is generated during the AssetBundle build process and recorded in the .manifest file and exposed by this method.</remarks>
        ///<param name="targetPath">Asset bundle path.</param>
        ///<param name="crc">A resulting checksum of the given asset bundle.</param>
        ///<returns>True if the method successfully reads the manifest file and extracts the CRC value. False if it cannot find the .manifest file associated to the <c>targetPath</c> or fails to parse the CRC value, which might happen due to incorrect paths.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEditor;
        ///
        ///public class CheckAssetBundleCRCExample
        ///{
        ///    [MenuItem("Debug/Check AssetBundle CRC")]
        ///    static public void CheckAssetBundleCRC()
        ///    {
        ///        string targetPath = EditorUtility.OpenFilePanel("Pick AssetBundle", "", "");
        ///        uint crc;
        ///        if (targetPath.Length == 0 || !BuildPipeline.GetCRCForAssetBundle(targetPath, out crc))
        ///            return;
        ///
        ///        Debug.Log($"AssetBundle {targetPath} has CRC equal to {crc}");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="xref:um-asset-bundles-integrity">CRC Checksums</seealso>
        ///<seealso cref="AssetBundleManifest.GetAssetBundleHash" />
        [FreeFunction("ExtractCRCFromAssetBundleManifestFile")]
        public static extern bool GetCRCForAssetBundle(string targetPath, out uint crc);

        ///<summary>Extract the hash for the given AssetBundle.</summary>
        [FreeFunction("ExtractHashFromAssetBundleManifestFile")]
        public static extern bool GetHashForAssetBundle(string targetPath, out Hash128 hash);

        [FreeFunction("BuildPlayerLicenseCheck", IsThreadSafe = true)]
        internal static extern bool LicenseCheck(BuildTarget target);

        // TODO: remove, superseded by IsBuildPlatformSupported()
        ///<summary>Returns true if the specified build target is currently available in the Editor.</summary>
        ///<param name="buildTargetGroup">build target group</param>
        ///<param name="target">build target</param>
        [FreeFunction]
        public static extern bool IsBuildTargetSupported(BuildTargetGroup buildTargetGroup, BuildTarget target);

        [FreeFunction]
        internal static extern bool IsBuildPlatformSupported(BuildTarget target);

        ///<summary>Returns the path of a player directory. For ex., Editor\Data\PlaybackEngines\AndroidPlayer.</summary>
        ///<remarks>In some cases the player directory path can be affected by <see cref="BuildOptions.Development" />.</remarks>
        ///<param name="target">Build target.</param>
        ///<param name="options">Build options.</param>
        ///<param name="buildTargetGroup">Build target group.</param>
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
        static string[] GetSystemAssemblies(BuildTarget target, BuildOptions buildOptions)
        {
            var namedTarget = NamedBuildTarget.FromActiveSettings(target);
            var scriptingBackend = PlayerSettings.GetScriptingBackend(namedTarget);
            var apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(namedTarget);

            var directories = GetBclReferenceDirectoriesForBackend(target, namedTarget, buildOptions, scriptingBackend, apiCompatibilityLevel);
            var assemblies = new List<string>();
            foreach (var directory in directories)
                assemblies.AddRange(Directory.GetFiles(directory, "*.dll"));
            string[] result = new string[assemblies.Count];
            for (var index = 0; index < assemblies.Count; index++)
            {
                // The native side expects all paths to be using forward slashes
                result[index] = assemblies[index].Replace("\\", "/");
            }

            return result;
        }

        static List<string> GetBclReferenceDirectoriesForBackend(BuildTarget target, NamedBuildTarget namedTarget, BuildOptions buildOptions, ScriptingImplementation scriptingBackend,
            ApiCompatibilityLevel apiCompatibilityLevel)
        {
#pragma warning disable CS0618
            if (scriptingBackend == ScriptingImplementation.CoreCLR)
#pragma warning restore CS0618
                return GetCoreCLRReferenceDirectories(target, namedTarget, buildOptions);

            if (scriptingBackend == ScriptingImplementation.IL2CPP)
                return IL2CPPUtils.GetIL2CPPReferenceDirectories(target, namedTarget, buildOptions, apiCompatibilityLevel);

            return GetMonoReferenceDirectories(target);
        }

        static List<string> GetCoreCLRReferenceDirectories(BuildTarget target, NamedBuildTarget namedTarget, BuildOptions buildOptions)
        {
            var result = new List<string>();
            result.Add(BCLExtensions.CoreCLRRuntimeDirectory());

            string engineDirectory = GetPlaybackEngineDirectory(target, buildOptions, true);

            var gotBuildTarget = BuildTargetDiscovery.TryGetBuildTarget(target, out var ibuildTarget);
            if (!gotBuildTarget || ibuildTarget.ScriptingPlatformProperties == null)
            {
                throw new BuildFailedException("ScriptingPlatformProperties does not contain a target directory for CoreCLR.");
            }
            result.Add(Path.Combine(engineDirectory, ibuildTarget.ScriptingPlatformProperties.CoreCLRBCLDirectory, "CoreCLR/lib"));

            return result;
        }

        internal static string GetVariationDirectory(BuildTarget target, BuildOptions buildOptions)
        {
            return Path.Combine(GetPlaybackEngineDirectory(target, buildOptions, true), "Variations");
        }

        private static List<string> GetMonoReferenceDirectories(BuildTarget target)
        {
            var result = new List<string>();
            result.Add(BCLExtensions.NetstandardRuntimeDirectory());

            string monoLibDirectory = GetMonoRuntimeLibDirectory(target);
            result.Add(monoLibDirectory);

            string facadeDirectory = Path.Combine(monoLibDirectory, "Facades");
            if (Directory.Exists(facadeDirectory))
                result.Add(facadeDirectory);

            return result;
        }

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

        ///<summary>Returns the mode currently used by players to initiate a connect to the host.</summary>
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

        /// <summary>
        /// Clears cached unified content-build data so the next build recomputes it.
        /// </summary>
        /// <remarks>
        /// <see cref="BuildPipeline.BuildContentDirectory"/> performs the same cleanup when <see cref="BuildContentOptions.CleanBuildCache"/> is set.
        /// Use this to clear that cache between builds without running a content build.
        /// </remarks>
        /// <seealso cref="BuildContentOptions.CleanBuildCache"/>
        /// <seealso cref="BuildPipeline.BuildContentDirectory"/>
        public static extern void CleanBuildCache();

        [RequiredByNativeCode]
        internal static bool InvokeGenerateBuildTimeAssets()
        {
            try
            {
                // Invoke BuildTimeAssetGeneration.GenerateAssets()
                var buildTimeAssetGenerationClass = Type.GetType("UnityEditor.BuildTimeAssetGeneration, Assembly-CSharp-Editor-firstpass-testable, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null", true);
                var generateAssetsMethod = buildTimeAssetGenerationClass.GetMethod("GenerateAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                return (bool)generateAssetsMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception while trying to generate build time assets: " + e.Message);
                return false;
            }
        }
    }
}
