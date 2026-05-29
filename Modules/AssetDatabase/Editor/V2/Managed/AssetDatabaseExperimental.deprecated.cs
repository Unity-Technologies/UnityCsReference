// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental
{
    [Obsolete("OnDemandState has been renamed. Use OutOfProcessImportState instead. (UnityUpgradable) -> OutOfProcessImportState", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public enum OnDemandState
    {
        Unavailable = 0,
        Processing = 1,
        Downloading = 2,
        Available = 3,
        Failed = 4
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("ArtifactID is deprecated. Use ImportResultID instead (UnityUpgradable) -> ImportResultID", true)]
    public struct ArtifactID
    {
        public Hash128 value;
        public bool isValid => value.isValid;
    }

    [Obsolete("OnDemandProgress has been renamed. Use OutOfProcessImportProgress instead. (UnityUpgradable) -> OutOfProcessImportProgress", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public struct OnDemandProgress {}

    public sealed partial class AssetDatabaseExperimental
    {
        [Obsolete("GetOnDemandArtifactProgress has been renamed. Use GetOutOfProcessImportProgress instead. (UnityUpgradable) -> GetOutOfProcessImportProgress(*)", true)]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static OnDemandProgress GetOnDemandArtifactProgress(ArtifactKey artifactKey) { return default; }
    }
}
