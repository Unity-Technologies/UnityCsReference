// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Localization.Editor;

// Connects a collection to a file on disk so it can be pushed and pulled without picking the path each time.
[Serializable]
abstract class FileCollectionExtension : IResourceCollectionExtension
{
    [SerializeField] string m_ConnectedFile;
    [SerializeField] bool m_RemoveMissingPulledKeys;

    public string File { get => m_ConnectedFile; set => m_ConnectedFile = value; }

    public bool RemoveMissingPulledKeys { get => m_RemoveMissingPulledKeys; set => m_RemoveMissingPulledKeys = value; }

    public abstract string DisplayName { get; }

    public abstract ITableCollectionExporter Exporter { get; }

    public abstract ITableCollectionImporter Importer { get; }
}

