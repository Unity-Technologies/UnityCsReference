// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    /// <summary>
    /// Build options for Content Directory builds. Multiple options can be combined together.
    /// </summary>
    /// <example>
    ///  <code source="../../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/Settings/BuildContentOptions_BuildContentDirectory.cs"/>
    /// </example>
    /// <seealso cref="BuildPipeline.BuildContentDirectory"/>
    [Flags]
    public enum BuildContentOptions
    {
        /// <summary>
        /// Perform the specified build without any special settings or extra tasks.
        /// </summary>
        None = 0,

        /// <summary>
        /// Build content using archive files (.archive) for storage.
        /// </summary>
        /// <remarks>
        /// When this flag is set, the build system will package content into archive files with the ".archive" extension,
        /// which can improve loading performance and reduce file system overhead.
        ///
        /// This flag is unnecessary if compression is enabled using the <see cref="BuildContentDirectoryParameters.compression"/> field.
        /// </remarks>
        UseArchive = 1 << 0,

        /// <summary>
        /// Do not include type tree information in the serialized data.
        /// </summary>
        /// <remarks>
        /// Disabling type trees reduces build size and build time, but removes the ability to load content built with different
        /// versions of scripts or Unity. Only use this option if you are certain the runtime environment will match the build
        /// environment exactly.
        /// </remarks>
        /// <seealso cref="BuildAssetBundleOptions.DisableWriteTypeTree"/>
        DisableWriteTypeTree = 1 << 3,

        /// <summary>
        /// Clear all cached build results, resulting in a full rebuild of content.
        /// </summary>
        /// <seealso cref="BuildPipeline.CleanBuildCache"/>
        /// <seealso cref="BuildOptions.CleanBuildCache"/>
        /// <seealso cref="BuildAssetBundleOptions.ForceRebuildAssetBundle"/>
        CleanBuildCache = 1 << 5,

        /// <summary>
        /// Fail the build if any errors are logged while it runs.
        /// </summary>
        /// <remarks>
        /// Without this flag, non-fatal errors - such as a failure to compile a shader for a particular platform - do not
        /// cause the build to fail, but may result in incorrect behaviour at runtime.
        /// When this flag is set, errors logged from build callbacks also
        /// fail the build, including these build callbacks:
        /// <see cref="Build.IPreprocessBuildWithContext.OnPreprocessBuild"/>,
        /// <see cref="Build.IPostprocessBuildWithContext.OnPostprocessBuild"/>,
        /// <see cref="Build.IProcessSceneWithReport.OnProcessScene"/>,
        /// <see cref="Build.IPreprocessShaders.OnProcessShader"/>, and
        /// <see cref="Build.IPreprocessComputeShaders.OnProcessComputeShader"/>.
        ///
        /// This flag is equivalent to <see cref="BuildOptions.StrictMode"/>.
        /// </remarks>
        FailBuildWhenErrorsLogged = 1 << 9,

        /// <summary>
        /// Include the Unity version information in the serialized build data.
        /// </summary>
        /// <remarks>
        /// When enabled, the build will include metadata about the Unity version used to create it, which can be useful for
        /// debugging and version tracking purposes.  However including the Unity version in the build data means that all your build output
        /// is certain to have new output if you upgrade and rebuild, even for a minor version upgrade.
        /// </remarks>
        SerializeUnityVersion = 1 << 15,

        /// <summary>
        /// Skip writing built artifacts and the archive to
        /// <see cref="BuildContentDirectoryParameters.outputPath"/>.
        /// Incompatible with <see cref="UseArchive"/> and any non-Uncompressed
        /// <see cref="BuildCompression"/>.
        /// </summary>
        SkipExportToOutputPath = 1 << 16,
    }
}

