// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal partial class ArtifactInfo : IDisposable
    {
        [Obsolete("artifactID is deprecated. Use importResultID instead (UnityUpgradeable) -> importResultID", true)]
        internal string artifactID => m_ImportResultID;
    }
}
