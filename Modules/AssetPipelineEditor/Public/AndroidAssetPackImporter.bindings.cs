// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/AssetPipelineEditor/Public/AndroidAssetPackImporter.h")]
    [NativeClass("AndroidAssetPackImporter", PersistentTypeId = 0x6783E580)]
    [ExcludeFromPreset]
    public class AndroidAssetPackImporter : AssetImporter
    {
        internal AndroidAssetPackImporter(global::UnityEngine.EntityId id) : base(id) {}
        public AndroidAssetPackImporter() {}
        extern public static AndroidAssetPackImporter[] GetAllImporters();
    }
}
