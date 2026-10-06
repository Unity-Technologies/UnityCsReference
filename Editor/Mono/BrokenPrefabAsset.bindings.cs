// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/BrokenPrefabAsset.h")]
    [UnityEngine.NativeClass("BrokenPrefabAsset", PersistentTypeId = 0x672E287B)]
    public class BrokenPrefabAsset : DefaultAsset
    {
        internal BrokenPrefabAsset(global::UnityEngine.EntityId id) : base(id) {}
        private BrokenPrefabAsset() {}

        public extern BrokenPrefabAsset brokenPrefabParent { get; }
        public extern bool isVariant { get; }
        public extern bool isPrefabFileValid { get; }
    }
}
