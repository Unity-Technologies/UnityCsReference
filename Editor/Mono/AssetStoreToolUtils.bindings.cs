// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Object = UnityEngine.Object;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Mono/AssetStore.bindings.h")]
    [StaticAccessor("AssetStoreScriptBindings", StaticAccessorType.DoubleColon)]
    public sealed partial class AssetStoreToolUtils
    {
        [System.Obsolete("BuildAssetStoreAssetBundle has been made obsolete. Please use BuildPipeline.BuildAssetBundles() or Addressables.", true)]
        extern public static bool BuildAssetStoreAssetBundle(Object targetObject, string targetPath);
    }
}
