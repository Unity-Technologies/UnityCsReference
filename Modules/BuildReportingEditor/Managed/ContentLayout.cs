// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Build
{
    /// <summary>
    /// Category strings used by <see cref="BinaryArtifact.Category"/>.
    /// Keep in sync with the kBuildArtifactCategory_* constants in native code.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "Unity.Modules.BuildAnalysis.Tests.Editor")]
    internal static class BuildArtifactCategory
    {
        /// <summary>Texture data, written to a streaming resource (.resS) file.</summary>
        public const string Texture = "texture";

        /// <summary>Mesh data, written to a streaming resource (.resS) file.</summary>
        public const string Mesh = "mesh";

        /// <summary>Audio data, written to a resource (.resource) file.</summary>
        public const string Audio = "audio";

        /// <summary>Video data, written to a resource (.resource) file.</summary>
        public const string Video = "video";

        /// <summary>A SerializedFile / Content File (.cf).</summary>
        public const string ContentFile = "contentfile";

        /// <summary>A manifest file (.json).</summary>
        public const string Manifest = "manifest";
    }

    /// <summary>
    /// Describes a SerializedFile in the build output.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "Unity.Modules.BuildAnalysis.Tests.Editor")]
    [Serializable]
    internal struct SerializedFileLayout
    {
        /// <summary>Index of this entry inside <see cref="ContentLayout.SerializedFiles"/>.</summary>
        public int Index;

        /// <summary>Stable identifier used to reference this SerializedFile from other SerializedFiles
        /// in a way that doesn't break when the content changes. Currently based on the cluster or guid of the source.</summary>
        public string ID;

        /// <summary>True for synthetic entries representing built-in Unity resources that are not produced
        /// by the build (currently only "Library/unity default resources"). Such entries have no ContentHash.</summary>
        public bool IsBuiltIn;

        /// <summary>The source assets included in this SerializedFile.</summary>
        public string[] SourceAssets;

        /// <summary>Indices into the containing <see cref="ContentLayout.SerializedFiles"/> array, identifying
        /// other SerializedFiles that need to be loaded prior to loading this SerializedFile.</summary>
        public int[] SerializedFileDependencies;

        /// <summary>ObjectIdHash values for loadable objects referenced from this SerializedFile.</summary>
        public string[] LoadableDependencies;

        /// <summary>Scene paths for scenes referenced from this SerializedFile.</summary>
        public string[] LoadableSceneDependencies;

        /// <summary>xxhash3 hash of the content, used for the filename (+".cf") and for lookup into UDS.
        /// Matches the <see cref="BinaryArtifact.ContentHash"/> of the corresponding entry in
        /// <see cref="ContentLayout.BinaryArtifacts"/>.</summary>
        public string ContentHash;
    }

    /// <summary>
    /// Records a loadable object in the build. Listed at the top level of the ContentLayout so that a
    /// loadable's identity is described independently of the SerializedFile that happens to contain it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "Unity.Modules.BuildAnalysis.Tests.Editor")]
    [Serializable]
    internal class LoadableObjectIdLayout
    {
        /// <summary>Hash of the GUID, LFID and IdentifierType.</summary>
        public string ObjectIdHash;

        /// <summary>AssetDatabase GUID of the source asset.</summary>
        public string GUID;

        /// <summary>Path of the source asset.</summary>
        public string AssetPath;

        /// <summary>Local file id of the source object.</summary>
        public long LFID;

        /// <summary>Identifier type of the source object.</summary>
        public int IdentifierType;

        /// <summary>Index into <see cref="ContentLayout.SerializedFiles"/> for the file that contains this
        /// loadable, or -1 if it was dropped (e.g. server build shader references).</summary>
        public int SerializedFile = -1;
    }

    /// <summary>
    /// Records a scene exposed as loadable in the build, and the SerializedFile that contains it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "Unity.Modules.BuildAnalysis.Tests.Editor")]
    [Serializable]
    internal class LoadableSceneIdLayout
    {
        /// <summary>AssetDatabase GUID of the scene.</summary>
        public string GUID;

        /// <summary>Scene path.</summary>
        public string Path;

        /// <summary>Index into <see cref="ContentLayout.SerializedFiles"/>, or -1 if not mapped.</summary>
        public int SerializedFile = -1;
    }

    /// <summary>
    /// Describes a binary artifact produced by the build (SerializedFile, streaming resource, manifest, etc.).
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "Unity.Modules.BuildAnalysis.Tests.Editor")]
    [Serializable]
    internal class BinaryArtifact
    {
        /// <summary>Index of this entry inside <see cref="ContentLayout.BinaryArtifacts"/>.</summary>
        public int Index;

        /// <summary>Content addressable hash. For ContentFile artifacts, the matching
        /// <see cref="SerializedFileLayout"/> can be found by ContentHash.</summary>
        public string ContentHash;

        /// <summary>One of the strings in <see cref="BuildArtifactCategory"/>.</summary>
        public string Category;

        /// <summary>Size in bytes.</summary>
        public ulong Size;

        /// <summary>Indices into <see cref="ContentLayout.BinaryArtifacts"/> identifying additional artifacts
        /// referenced from this one (e.g. mesh, audio).
        /// Note: this does not track references to other ContentFiles — those are recorded in
        /// <see cref="SerializedFileLayout.SerializedFileDependencies"/>.</summary>
        public int[] ArtifactReferences;

        /// <summary>The on-disk file extension for this artifact, derived from <see cref="Category"/>.
        /// Returns an empty string for unrecognized categories.</summary>
        public string FileExtension
        {
            get
            {
                switch (Category)
                {
                    case BuildArtifactCategory.Texture:
                    case BuildArtifactCategory.Mesh:
                        return ".resS";
                    case BuildArtifactCategory.Audio:
                    case BuildArtifactCategory.Video:
                        return ".resource";
                    case BuildArtifactCategory.ContentFile:
                        return ".cf";
                    case BuildArtifactCategory.Manifest:
                        return ".json";
                    default:
                        return "";
                }
            }
        }
    }

    /// <summary>
    /// In-memory representation of the ContentLayout.json file written by the build.
    ///
    /// The Layout is a companion to the BuildManifest, recording additional details about the build
    /// (including source assets and information about which object an ObjectId hash refers to).
    /// It is not shipped with the build; it exists for tools and tests that analyze build output.
    /// The schema is subject to change and there is currently no backward compatibility.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.BuildAnalysisModule", "Unity.Modules.BuildAnalysis.Tests.Editor")]
    [Serializable]
    internal class ContentLayout
    {
        // Keep in sync with kContentLayoutVersion in WriteBuildOutput.cpp.
        const int kContentLayoutVersion = 1;

        /// <summary>Schema version of the ContentLayout.json file.</summary>
        public int Version;

        /// <summary>Hash of the BuildManifest this layout corresponds to.</summary>
        public string BuildManifestHash;

        /// <summary>The SerializedFiles in the build output.</summary>
        public SerializedFileLayout[] SerializedFiles;

        /// <summary>ObjectIdHash values of the root assets; resolve via <see cref="LoadableObjectIds"/>.</summary>
        public string[] RootAssets;

        /// <summary>Loadable objects in the build.</summary>
        public LoadableObjectIdLayout[] LoadableObjectIds;

        /// <summary>Loadable scenes in the build.</summary>
        public LoadableSceneIdLayout[] LoadableSceneIds;

        /// <summary>Binary artifacts produced by the build.</summary>
        public BinaryArtifact[] BinaryArtifacts;

        /// <summary>Loads a <see cref="ContentLayout"/> from a JSON file at the specified path.</summary>
        /// <param name="jsonPath">The path to the JSON file to load.</param>
        /// <returns>The <see cref="ContentLayout"/> loaded from the file, or null if the file is not found.</returns>
        public static ContentLayout Load(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"Failed to load ContentLayout.json: File not found at {jsonPath}");
                return null;
            }

            return FromJson(File.ReadAllText(jsonPath));
        }

        /// <summary>
        /// Parse a ContentLayout from its JSON representation.
        /// Logs a warning if the file's <see cref="Version"/> does not match the expected schema version.
        /// </summary>
        public static ContentLayout FromJson(string manifestAsString)
        {
            ContentLayout layout = JsonUtility.FromJson<ContentLayout>(manifestAsString);

            if (layout != null && layout.Version != kContentLayoutVersion)
            {
                if (layout.Version == 0)
                {
                    Debug.LogWarning(
                        $"ContentLayout.json missing version field or has version 0 (legacy format). " +
                        $"Expected version: {kContentLayoutVersion}. " +
                        $"This file may be from an older Unity version. Consider rebuilding."
                    );
                }
                else
                {
                    Debug.LogWarning(
                        $"ContentLayout.json version mismatch. Expected: {kContentLayoutVersion}, Found: {layout.Version}. " +
                        $"This may indicate content built with a different Unity version. " +
                        $"Some tools may not work correctly."
                    );
                }
            }

            return layout;
        }
    }
}
