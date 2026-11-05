// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Core
{
    // UnityEngine.Time.fixedDeltaTime, which the Entities module sits below. An icall, because Burst can call one but
    // not the CoreModuleLightShim interface.
    [NativeHeader("Modules/Entities/Public/EntitiesTimeData.bindings.h")]
    static class TimeDataBindings
    {
        [FreeFunction("EntitiesTimeData::GetFixedDeltaTime")]
        internal static extern float GetFixedDeltaTime();
    }
}
