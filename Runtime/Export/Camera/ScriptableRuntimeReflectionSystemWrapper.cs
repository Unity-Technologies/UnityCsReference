// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [RequiredByNativeCode]
    class ScriptableRuntimeReflectionSystemWrapper
    {
        internal IScriptableRuntimeReflectionSystem implementation { get; set; }

        [RequiredByNativeCode]
        bool Internal_ScriptableRuntimeReflectionSystemWrapper_TickRealtimeProbes_ReturnResult()
        {
            return implementation != null
                && implementation.TickRealtimeProbes();
        }

        [RequiredByNativeCode]
        void Internal_ScriptableRuntimeReflectionSystemWrapper_TickRealtimeProbes(out bool result)
        {
            result = implementation != null
                && implementation.TickRealtimeProbes();
        }
    }
}
