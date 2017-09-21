// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;

namespace UnityEngine.Experimental.Playables
{

    [NativeHeader("Runtime/Export/Director/TexturePlayableGraphExtensions.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("TexturePlayableGraphExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class TexturePlayableGraphExtensions
    {
        extern internal static bool InternalCreateTextureOutput(ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
    }

}
