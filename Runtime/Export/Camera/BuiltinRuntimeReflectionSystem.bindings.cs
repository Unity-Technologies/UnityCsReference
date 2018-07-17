// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Camera/ReflectionProbes.h")]
    class BuiltinRuntimeReflectionSystem : IScriptableRuntimeReflectionSystem
    {
        public bool TickRealtimeProbes()
        {
            return BuiltinUpdate();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
        }

        [StaticAccessor("GetReflectionProbes()", Type = StaticAccessorType.Dot)]
        static extern bool BuiltinUpdate();

        [RequiredByNativeCode]
        static BuiltinRuntimeReflectionSystem Internal_BuiltinRuntimeReflectionSystem_New()
        {
            return new BuiltinRuntimeReflectionSystem();
        }
    }
}
