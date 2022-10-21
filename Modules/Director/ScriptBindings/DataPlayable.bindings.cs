// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Playables
{
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayable.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableGraph.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("DataPlayableBindings", StaticAccessorType.DoubleColon)]
    static class DataPlayableBindings
    {
        [NativeThrows]
        extern public static bool CreateHandleInternal(PlayableGraph graph, ref PlayableHandle handle);
    }
}
