// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEditor
{
    /// <summary>
    /// Used by com.unity.hotreload package
    /// </summary>
    [NativeType(Header = "Runtime/Export/HotReload/HotReload.bindings.h")]
    [NativeConditional("HOT_RELOAD_AVAILABLE")]
    internal static class HotReloadSerializer
    {
        [NativeThrows]
        [FreeFunction("HotReload::SerializeAsset")]
        internal extern static byte[] SerializeAsset(UnityEngine.Object asset, BuildTarget buildTarget);
    }
}
