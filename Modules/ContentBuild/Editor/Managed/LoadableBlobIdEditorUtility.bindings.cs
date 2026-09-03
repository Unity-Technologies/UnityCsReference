// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Loading;
using UnityEditor.AssetImporters;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Content
{
    // Mirrors BuildPipeline::LoadableBlobRegistrationInput. All fields must be non-null.
    [StructLayout(LayoutKind.Sequential)]
    internal struct LoadableBlobRegistrationInput
    {
        // Optional; the blob's artifact category becomes "loadableblob:<category>".
        public string category;
        public UCBPKeyValuePair[] keyValuePairs;
    }

    // Editor utility for creating LoadableBlobId values during asset import. Commits the bytes
    // and a BuildArtifactMetadata (category, key/value pairs) to UDS, anchors the metadata with
    // the import result, and returns the LoadableBlobId to store on a serialized Unity object.
    // The com.unity.content-build-load-preview package exposes this surface to users; the class
    // is shaped to become the public API once the feature leaves preview.
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/LoadableBlobRegistration.h")]
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/BuildArtifactMetadataCollectionBindings.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    internal static class LoadableBlobIdEditorUtility
    {
        [NativeMethod("RegisterLoadableBlob")]
        public static extern LoadableBlobId CreateLoadableBlobId(AssetImportContext context, ReadOnlySpan<byte> data,
            LoadableBlobRegistrationInput input);
    }
}
