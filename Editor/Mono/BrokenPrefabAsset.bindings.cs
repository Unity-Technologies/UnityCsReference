// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/BrokenPrefabAsset.h")]
    public class BrokenPrefabAsset : DefaultAsset
    {
        private BrokenPrefabAsset() {}

        public extern BrokenPrefabAsset brokenPrefabParent { get; }
        public extern bool isVariant { get; }
        public extern bool isPrefabFileValid { get; }
    }
}
