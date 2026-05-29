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
        /// Array of paths to the root Assets that should be included in the build.
        /// </summary>
        /// <remarks>
        /// This property should contain project-relative paths to existing ScriptableObject-derived Assets. Each specified
        /// Asset will be included in the build, and available for direct load. Any dependency referenced from the Asset will also
        /// be included in the build. Root assets are automatically loaded when a Content Directory is registered, so only
        /// ScriptableObject-derived assets are permitted to prevent accidental misuse (such as attempting to use large assets
        /// like Textures or Meshes as root assets).
        /// </remarks>
        public string[] rootAssetPaths { get; set; }

        /// <summary>
        /// Flags from the <see cref="BuildContentOptions"/> enum. (optional)
        /// </summary>
        public BuildContentOptions options { get; set; }

        /// <summary>
        /// The compression settings for the build. Defaults to <see cref="BuildCompression.Uncompressed"/>.
        /// </summary>
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
        /// This name is stored in the BuildReport and BuildManifest for identification purposes.
        /// If not specified, the leaf folder name of <see cref="outputPath"/> is used as the default.
        /// </remarks>
        public string name { get; set; }

        /// <summary>
        /// Internal: path to the build metadata directory, set by BuildHistory.ReserveBuildMetadataPath
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

