// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/AssetStore/AssetStoreCachePathManager.h")]
    internal partial class AssetStoreCachePathManager
    {
        [FreeFunction("AssetStoreCachePathManager::GetConfig")]
        public static extern CachePathConfig GetConfig();

        // Int return the status of setting the config
        [FreeFunction("AssetStoreCachePathManager::SetConfig")]
        public static extern ConfigStatus SetConfig(string newPath);

        [FreeFunction("AssetStoreCachePathManager::ResetConfig")]
        public static extern ConfigStatus ResetConfig();
    }
}
