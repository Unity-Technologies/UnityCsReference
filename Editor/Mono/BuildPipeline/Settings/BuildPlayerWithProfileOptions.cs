// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Profile;

namespace UnityEditor
{
    public struct BuildPlayerWithProfileOptions
    {
        public BuildProfile buildProfile { get; set; }
        public string locationPathName { get; set; }
        public string assetBundleManifestPath { get; set; }
        public BuildOptions options { get; set; }
    }
}
