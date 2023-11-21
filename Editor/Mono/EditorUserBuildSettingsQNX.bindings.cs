// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeType(Header = "Editor/Src/EditorUserBuildSettings.h")]
    public enum QNXOsVersion
    {
        [System.Obsolete("Neutrino RTOS 7.0 has been removed in 2023.2")]
        Neutrino70 = 0,

        [UnityEngine.InspectorName("Neutrino RTOS 7.1")]
        Neutrino71 = 1,
    }
}
