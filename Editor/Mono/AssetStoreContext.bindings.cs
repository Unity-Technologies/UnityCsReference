// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/AssetStore.bindings.h")]
    [StaticAccessor("AssetStoreScriptBindings", StaticAccessorType.DoubleColon)]
    internal partial class AssetStoreContext
    {
        extern public static void SessionSetString(string key, string value);
        extern public static string SessionGetString(string key);
        extern public static void SessionRemoveString(string key);
        extern public static bool SessionHasString(string key);
    }
}
