// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Provide various options to control the behavior of <see cref="BuildPipeline.BuildContentDirectory"/>.
    /// </summary>
    /// <seealso cref="BuildPipeline"/>
    /// <seealso cref="EditorUserBuildSettings"/>
    /// <seealso cref="EditorBuildSettings"/>
    [StructLayout(LayoutKind.Sequential)]
    /*UCBP-PUBLIC*/ internal struct BuildContentDirectoryParameters
    {
        /// <summary>
        /// Output path for the build.
        /// </summary>
        /// <remarks>
        /// This can be an absolute path, or a path relative to the current project. If the path does not exist 
        /// <see cref="BuildPipeline.BuildContentDirectory"/>  will attempt to create it.
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
        /// Compression settings for the build. When not specified the value is <see cref="BuildCompression.Uncompressed"/>.
        /// </summary>
        public BuildCompression compression { get; set; }

        /// <summary>
        /// The <see cref="BuildTarget"/> to build. (optional)
        /// </summary>
        /// <remarks>
        /// The output of the build is only compatible with the specific platform that it was built for, so you must produce
        /// different builds to use the assets on different platforms.
        ///
        /// If targetPlatform is not specified, e.g. it is 0, then the targetPlatform and subtarget fields will all be determined
        /// from the current build settings.
        /// 
        /// It is strongly recommended to switch to the target platform prior to calling <see cref="BuildPipeline.BuildContentDirectory"/>,
        /// to ensure that the Editor assemblies have been compiled to match the target platform and a domain reload has been performed.
        /// BuildContentDirectory can build a different target platform than the one currently selected in the build settings, but it may not
        /// work as expected because platform-specific conditional compilation will be applied and some build callbacks may not execute the expected code.
        /// See the topic build-command-line in the Unity Manual for more details.
        /// </remarks>
        /// <seealso cref="EditorUserBuildSettings.activeBuildTarget"/>
        public BuildTarget targetPlatform { get; set; }

        /// <summary>
        /// The subtarget to build. (optional)
        /// </summary>
        /// <remarks>
        /// For some BuildTargets the behaviour of a build can be influenced by using this field. The supported values are based on
        /// target-specific enums, for example <see cref="MobileTextureSubtarget"/>, <see cref="XboxBuildSubtarget"/>.
        ///
        /// The subtarget can be assigned using the build settings UI. To build with the current build settings, the
        /// <see cref="BuildContentDirectoryParameters.targetPlatform"/> field should be left unassigned (e.g. with the value 0).
        /// </remarks>
        /// <seealso cref="EditorUserBuildSettings.standaloneBuildSubtarget"/>
        /// <seealso cref="EditorUserBuildSettings.androidBuildSubtarget"/>
        /// <seealso cref="EditorUserBuildSettings.webGLBuildSubtarget"/>
        /// <seealso cref="EditorUserBuildSettings.ps4BuildSubtarget"/>
        public int subtarget { get; set; }

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
    }
}

