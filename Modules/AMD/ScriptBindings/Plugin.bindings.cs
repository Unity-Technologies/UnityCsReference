// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.AMD
{
    [NativeHeader("Modules/AMD/AMDPlugins.h")]
    public static class AMDUnityPlugin
    {
        extern public static bool Load();
        extern public static bool IsLoaded();
    }
}
