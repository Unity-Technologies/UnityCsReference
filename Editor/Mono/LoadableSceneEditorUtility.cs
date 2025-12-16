// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    /// <summary>
    /// Utility class to create LoadableScene objects for use in the Editor. A typical use would be to populate fields of type
    /// <see cref="LoadableScene"/> on a class derived from ScriptableObject (or MonoBehaviour).
    /// </summary>
    /*UCBP-PUBLIC*/ internal static class LoadableSceneEditorUtility
    {
        /// <summary>
        /// Create a LoadableScene object based using a scene's GUID.
        /// </summary>
        /// <param name="guid">The scene's GUID.</param>
        /// <returns>A LoadableScene handle populated to reference the provided scene.</returns>
        public static LoadableScene CreateLoadableScene(GUID guid)
        {
            return new LoadableScene(guid);
        }

        /// <summary>
        /// Extracts the GUID from a LoadableScene.
        /// </summary>
        /// <param name="loadableScene">The LoadableScene to extract the GUID from.</param>
        /// <returns>The GUID of the scene.</returns>
        public static GUID LoadableSceneToGuid(LoadableScene loadableScene)
        {
            return loadableScene.m_SceneGUID;
        }

        /// <summary>
        /// Create a LoadableScene object based on a scene's path.
        /// </summary>
        /// <param name="scenePath">The scene's path within the project. Must end with .unity extension.</param>
        /// <returns>A LoadableScene handle populated to reference the provided scene.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if scenePath is null or empty.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown if the path doesn't end with .unity extension or if the scene asset cannot be found at the specified path.
        /// </exception>
        public static LoadableScene CreateLoadableScene(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentNullException(nameof(scenePath), "Scene path cannot be null or empty.");
            if (!scenePath.EndsWith(".unity", StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentException($"Path {scenePath} is not a scene path. Must end with .unity extension.");
            var guid = new GUID(AssetDatabase.AssetPathToGUID(scenePath));
            if (guid.Empty())
                throw new ArgumentException($"Couldn't locate scene asset at path {scenePath}");
            return CreateLoadableScene(guid);
        }
    }
}
