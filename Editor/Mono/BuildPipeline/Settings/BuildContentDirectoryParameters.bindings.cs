// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Provides various options to control the behavior of <see cref="BuildPipeline.BuildContentDirectory"/>.
    /// </summary>
    /// <example>
    ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildPipeline_BuildContentDirectory.cs"/>
    /// </example>
    /// <seealso cref="BuildPipeline"/>
    /// <seealso cref="EditorUserBuildSettings"/>
    /// <seealso cref="EditorBuildSettings"/>
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildContentDirectoryParameters
    {
        /// <summary>
        /// The output path for the content directory build.
        /// </summary>
        /// <remarks>
        /// The path can be an absolute path, or a path relative to the project folder. If the path doesn't exist,
        /// <see cref="BuildPipeline.BuildContentDirectory"/> attempts to create it.
        /// </remarks>
        public string outputPath { get; set; }

        /// <summary>
        /// Array of paths to the root assets to include in the build.
        /// </summary>
        /// <remarks>
        /// Set this property to project-relative paths of existing <see cref="ScriptableObject"/>-derived assets. The build
        /// includes each specified asset along with any dependency it references. After you register the content directory, retrieve
        /// the root assets at runtime with <see cref="Unity.Loading.ContentLoadManager.GetRootAssets{T}()"/>.
        ///
        /// Only ScriptableObject-derived assets are permitted as roots, to prevent accidental misuse such as using large assets
        /// like Textures or Meshes as root assets.
        /// </remarks>
        /// <seealso cref="Unity.Loading.ContentLoadManager.GetRootAssets{T}()"/>
        public string[] rootAssetPaths { get; set; }

        /// <summary>
        /// Flags from the <see cref="BuildContentOptions"/> enum. (optional)
        /// </summary>
        public BuildContentOptions options { get; set; }

        /// <summary>
        /// The compression settings for the build. Defaults to <see cref="BuildCompression.Uncompressed"/>.
        /// </summary>
        /// <remarks>
        /// With the default <see cref="BuildCompression.Uncompressed"/>, the build writes the content as individual loose files
        /// without an archive wrapper. Set <see cref="BuildContentOptions.UseArchive"/> to wrap the output in archive (.archive)
        /// files instead. Any other compression setting always produces archive files.
        ///
        /// <see cref="Unity.Loading.ContentLoadManager.RegisterContentDirectory(string)"/> can load a content directory whether
        /// its content is stored as loose files or wrapped in archive files. You can also create archive files from loose-file
        /// output as a separate step after the build with
        /// <see cref="Build.Content.ContentBuildInterface.ArchiveAndCompress(Build.Content.ResourceFile[], string, BuildCompression)"/>.
        /// </remarks>
        /// <seealso cref="BuildContentOptions.UseArchive"/>
        public BuildCompression compression { get; set; }

        // Internal: optional BuildTarget. When unset at default, native code takes both platform and subtarget from current Editor build settings.
        // Output is platform-specific. Building a non-active target can diverge from Editor conditional compilation and callbacks; prefer switching active target first (see Manual: build-command-line).
        internal BuildTarget targetPlatform { get; set; }

        // Internal: optional subtarget for targets that support it (values from target-specific enums, e.g. MobileTextureSubtarget). Used with targetPlatform when that is explicitly set; otherwise follows active build settings (see EditorUserBuildSettings *BuildSubtarget).
        internal int subtarget { get; set; }

        /// <summary>
        /// User-specified preprocessor defines used while compiling assemblies during the build. (optional)
        /// </summary>
        /// <remarks>
        /// Preprocessor defines may be used to exclude serialized fields from class definitions, so this can have an influence on
        /// how objects are serialized during the build process. Typically values passed here should match any extra scripting
        /// defines passed during the player build.
        /// </remarks>
        /// <seealso cref="BuildPlayerOptions.extraScriptingDefines"/>
        public string[] extraScriptingDefines { get; set; }

        /// <summary>
        /// Optional name for the build.
        /// </summary>
        /// <remarks>
        /// This name is stored in the BuildReport and BuildManifest for identification purposes. It is reported as
        /// <see cref="Build.Reporting.BuildSummary.buildName"/> in the build's <see cref="Build.Reporting.BuildReport"/>, and as
        /// <see cref="Build.BuildReportSummary.BuildName"/> in the lightweight build summary.
        /// If not specified, the leaf folder name of <see cref="outputPath"/> is used as the default.
        /// </remarks>
        /// <seealso cref="Build.Reporting.BuildSummary.buildName"/>
        /// <seealso cref="Build.BuildReportSummary.BuildName"/>
        /// <seealso cref="Unity.Loading.ContentDirectoryHandle.BuildName"/>
        public string name { get; set; }

        /// <summary>
        /// Internal: path to the build report directory, set by BuildHistory.ReserveBuildReportDirectory
        /// before calling into native code.
        /// </summary>
        internal string metadataPath { get; set; }

        /// <summary>
        /// Internal: official build start time, set by the managed entry point and consumed
        /// by native code. UTC ticks (System.DateTime.Ticks / C++ DateTime::ticks).
        /// </summary>
        internal long buildStartTimeTicks { get; set; }

        /// <summary>
        /// Internal: pre-compiled TypeDB to use instead of compiling player scripts during the build.
        /// When set, script compilation is skipped and the provided TypeDB is serialized to disk for use by the build pipeline.
        /// </summary>
        internal Build.Player.TypeDB precompiledTypeDB { get; set; }
    }
}

