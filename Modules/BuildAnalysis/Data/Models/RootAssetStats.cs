// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Per-root-asset statistics computed from a ContentLayout.
    /// Direct values cover what is loaded immediately with the root asset;
    /// Total values also cover what is reachable through loadable references.
    /// </summary>
    [Serializable]
    internal struct RootAssetStats
    {
        public string AssetPath;
        public int DirectAssets;
        public ulong DirectSize;
        public int TotalAssets;
        public ulong TotalSize;
    }
}
