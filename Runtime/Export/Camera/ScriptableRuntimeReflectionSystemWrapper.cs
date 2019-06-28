// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [RequiredByNativeCode]
    class ScriptableRuntimeReflectionSystemWrapper
    {
        internal IScriptableRuntimeReflectionSystem implementation { get; set; }

        [RequiredByNativeCode]
        unsafe void Internal_ScriptableRuntimeReflectionSystemWrapper_TickRealtimeProbes(IntPtr result)
        {
            *(bool*)result = implementation != null
                && implementation.TickRealtimeProbes();
        }
    }
}
