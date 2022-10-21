// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Playables
{
    [NativeHeader("Modules/Director/ScriptBindings/DataPlayableOutputExtensions.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("DataPlayableOutputExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class DataPlayableOutputExtensions
    {
        [NativeThrows]
        extern internal static bool InternalCreateDataOutput(ref PlayableGraph graph, string name, Type type, out PlayableOutputHandle handle);
    }
}
