// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEditor.Build.Reporting;

namespace UnityEditor.Build.Content
{
    /// <summary>
    /// This ScriptableObject-derived class makes it possible to create persisted definitions for content-only builds, similar to a Build Profile.
    /// Assets of this type can be edited through the Inspector and the Inspector UI makes it possible to invoke the build directly.
    /// This makes it possible to define and launch content-only builds without writing a custom script.
    /// </summary>
    /// <remarks>
    /// If a custom build script is used then it is not necessary to use this class. The script can call <see cref="BuildPipeline.BuildContentDirectory"/> directly.
    /// However, this class can still be useful as part of a custom build pipeline, as a way to serialize the specific configuration of each content-only build.
    /// See ContentDirectoryBuild.GetContentBuildDirectoryParameters for more information.
    /// </remarks>
    [CreateAssetMenu(fileName = "ContentDirectoryProfile", menuName = "Scriptable Objects/Content Directory Profile")]
    /*UCBP-PUBLIC*/ internal sealed class ContentDirectoryProfile : ScriptableObject
    {
        [SerializeField]
        internal string outputPath;
        [SerializeField]
        internal List<Object> rootAssets;
        [SerializeField]
        internal BuildContentOptions options;
        [SerializeField]
        internal CompressionType compressionType;
        [SerializeField]
        internal BuildTarget targetPlatform;
        [SerializeField]
        internal int subtarget;
        [SerializeField]
        internal string[] extraScriptingDefines;

        internal BuildCompression compression
        {
            get
            {
                switch (compressionType)
                {
                    case CompressionType.None:
                        return BuildCompression.Uncompressed;
                    case CompressionType.Lzma:
                        return BuildCompression.LZMA;
                    case CompressionType.Lz4:
                        return BuildCompression.LZ4Runtime;
                    case CompressionType.Lz4HC:
                        return BuildCompression.LZ4;
                    default:
                        return BuildCompression.Uncompressed;
                }
            }
        }

        /// <summary>
        /// Retrieves the current build parameters as a BuildContentDirectoryParameters struct.
        /// </summary>
        /// <returns>A BuildContentDirectoryParameters struct containing the current build configuration.</returns>
        public BuildContentDirectoryParameters GetBuildContentDirectoryParameters()
        {
            var rootAssetsPaths = new string[rootAssets.Count];
            GetAssetPaths(rootAssets, rootAssetsPaths);
            return new BuildContentDirectoryParameters
            {
                outputPath = outputPath,
                rootAssetPaths = rootAssetsPaths,
                options = options,
                compression = compression,
                targetPlatform = targetPlatform,
                subtarget = subtarget,
                extraScriptingDefines = extraScriptingDefines
            };
        }

        /// <summary>
        /// Sets the build parameters from a BuildContentDirectoryParameters struct.
        /// </summary>
        /// <param name="buildParameters">The BuildContentDirectoryParameters struct containing the new build configuration.</param>
        public void SetBuildContentDirectoryParameters(BuildContentDirectoryParameters buildParameters)
        {
            outputPath = buildParameters.outputPath;
            if (rootAssets != null)
            {
                rootAssets.Clear();
            }
            else
            {
                rootAssets = new List<Object>();
            }
            LoadAssetsFromPaths(buildParameters.rootAssetPaths, rootAssets);
            options = buildParameters.options;
            compressionType = buildParameters.compression.compression;
            targetPlatform = buildParameters.targetPlatform;
            subtarget = buildParameters.subtarget;
            extraScriptingDefines = buildParameters.extraScriptingDefines;
        }

        public BuildReport BuildContentDirectory(string oneTimeOutputPath = null)
        {
            var buildParams = GetBuildContentDirectoryParameters();
            if (oneTimeOutputPath != null)
            {
                buildParams.outputPath = oneTimeOutputPath;
            }

            return BuildPipeline.BuildContentDirectory(buildParams);
        }

        private static void GetAssetPaths(List<UnityEngine.Object> rootAssets, string[] rootAssetPaths)
        {
            for (int i = 0; i < rootAssets.Count; i++)
            {
                string assetPath = AssetDatabase.GetAssetPath(rootAssets[i]);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Failed to determine path for Root Asset at EntityId {rootAssets[i].GetEntityId()}");
                }
                else
                {
                    rootAssetPaths[i] = assetPath;
                }
            }
        }

        private static void LoadAssetsFromPaths(string[] rootAssetPaths, List<UnityEngine.Object> rootAssets)
        {
            for (int i = 0; i < rootAssetPaths.Length; i++)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(rootAssetPaths[i]);
                if (asset != null)
                {
                    rootAssets.Add(asset);
                }
                else
                {
                    Debug.LogError($"Failed to load asset at path: {rootAssetPaths[i]}");
                }
            }
        }
    }
}

