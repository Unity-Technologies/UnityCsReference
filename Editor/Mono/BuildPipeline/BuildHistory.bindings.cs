// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Build
{
    /// <summary>
    // Provides access to build history generated during ContentDirectory builds.
    /// </summary>
    /// <remarks>
    /// This mechanism is currently only used for ContentDirectory builds, not for AssetBundle or Player builds.
    ///
    /// By default, build history is stored in the Library folder. For each build, a new folder is created in the BuildHistory root
    /// directory that contains build metadata files.
    ///
    /// This folder is populated with files with information about various aspects of the build
    /// such as the BuildReport, profiling information, and type-usage (ScriptsOnlyCache.yaml).
    /// The precise content is influenced by flags passed through <see cref="BuildContentDirectoryParameters.options"/>
    /// and certain Editor Preference (e.g. Analysis -> Build Pipeline -> "Generate build performance profiling file").
    ///
    /// The files in this folder are for development and debugging purposes only. They are not meant to be shipped along with
    /// the content and are not required by the runtime.
    /// </remarks>
    /// <seealso cref="BuildPipeline.BuildContentDirectory"/>
    /// <seealso cref="BuildPlayerOptions.previousBuildMetadataLocations"/>
    [NativeHeader("Modules/ContentBuild/Editor/WriteMetadata/BuildHistory.h")]
    /*UCBP-PUBLIC*/ internal static class BuildHistory
    {
        /// <summary>
        /// Returns the default metadata directory, regardless of what the current build history directory is.
        /// </summary>
        /// <returns> The default build history directory path. </returns>
        public static string defaultRootDirectory
        {
            get { return GetDefaultRootDirectory(); }
        }

        extern private static string GetDirectoryForLatestBuild();

        /// <summary>
        /// Returns the path in the build history created by the most recent build.
        /// </summary>
        /// <returns>The path of the most recent build metadata directory</returns>
        public static string latestBuildDirectory
        {
            get { return GetDirectoryForLatestBuild(); }
        }

        extern private static string GetDefaultRootDirectory();

        /// <summary>
        /// Sets the path where the build history will be stored. Folders beneath the Assets and Temp folders are not allowed.
        /// </summary>
        /// <param name="buildHistoryPath"> The relative or absolute path to set as the new build history folder. </param>
        /// <returns>True if successfully set, false if not. </returns>
        extern public static bool SetRootDirectory(string buildHistoryPath);

        /// <summary>
        /// Returns the current build history root directory, as set by SetRootDirectory or within the Preferences > Build Pipeline window.
        /// </summary>
        /// <returns> The path of the root build history directory. </returns>
        extern public static string GetRootDirectory();

        /// <summary>
        /// Retrieves the directory in the build history associated with the passed in build output folder
        /// </summary>
        /// <param name="buildOutputLocation"> The output path of a build. </param>
        /// <returns> Returns an empty string if the build type does not generate build metadata,
        /// or if the folder has been erased.
        /// Otherwise returns the path of the build metadata directory associated with the build output location</returns>
        extern public static string GetDirectory(string buildOutputLocation);

        /// <summary>
        /// Deletes all recorded build history
        /// </summary>
        /// <returns>true if history was successfully deleted, false if there were only errors. </returns>
        extern public static bool DeleteHistory();
    }
}
