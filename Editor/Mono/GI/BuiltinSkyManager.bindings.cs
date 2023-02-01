// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/GI/BuiltinSkyManager.h")]
    internal class BuiltinSkyManager
    {
        private BuiltinSkyManager() {}
        extern public static bool StaticIsOnAutoMode();
        extern public static bool StaticIsDone();
        extern public static bool enabled { get; set; }
    }
}
