// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    public struct AssetBundleBuild
    {
        public string assetBundleName;
        public string assetBundleVariant;
        public string[] assetNames;
        [NativeName("nameOverrides")]
        public string[] addressableNames;
    }
}
