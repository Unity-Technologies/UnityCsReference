// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("ArtifactID is deprecated. Use ImportResultID instead (UnityUpgradable) -> ImportResultID", true)]
    public struct ArtifactID
    {
        public Hash128 value;
        public bool isValid => value.isValid;
    }
}
