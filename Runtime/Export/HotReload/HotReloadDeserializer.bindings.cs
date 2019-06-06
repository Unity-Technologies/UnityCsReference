// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine
{
    /// <summary>
    /// Used by com.unity.hotreload package
    /// </summary>
    [NativeType(Header = "Runtime/Export/HotReload/HotReload.bindings.h")]
    [NativeConditional("HOT_RELOAD_AVAILABLE")]
    internal static class HotReloadDeserializer
    {
        [FreeFunction("HotReload::Prepare")]
        internal extern static void PrepareHotReload();

        [FreeFunction("HotReload::Finish")]
        internal extern static void FinishHotReload(Type[] typesToReset);

        [NativeThrows]
        [FreeFunction("HotReload::CreateEmptyAsset")]
        internal extern static UnityEngine.Object CreateEmptyAsset(Type type);

        [NativeThrows]
        [FreeFunction("HotReload::DeserializeAsset")]
        internal extern static void DeserializeAsset(UnityEngine.Object asset, byte[] data);

        [NativeThrows]
        [FreeFunction("HotReload::RemapInstanceIds")]
        private extern static void RemapInstanceIds(UnityEngine.Object editorAsset, int[] editorToPlayerInstanceIdMapKeys, int[] editorToPlayerInstanceIdMapValues);

        internal static void RemapInstanceIds(UnityEngine.Object editorAsset, Dictionary<int, int> editorToPlayerInstanceIdMap)
        {
            RemapInstanceIds(editorAsset, editorToPlayerInstanceIdMap.Keys.ToArray(), editorToPlayerInstanceIdMap.Values.ToArray());
        }

        [FreeFunction("HotReload::FinalizeAssetCreation")]
        internal extern static void FinalizeAssetCreation(UnityEngine.Object asset);

        [FreeFunction("HotReload::GetDependencies")]
        internal extern static UnityEngine.Object[] GetDependencies(UnityEngine.Object asset);

        [FreeFunction("HotReload::GetNullDependencies")]
        internal extern static int[] GetNullDependencies(UnityEngine.Object asset);
    }
}
