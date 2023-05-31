// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Linker/BlockLinker.h")]
    // This is needed for registration to work with the container being in a namespace!
    [NativeClass("ShaderFoundry::BlockLinker")]
    [FoundryAPI]
    internal sealed partial class BlockLinkerInternal
    {
        internal enum Mode : ushort { Normal, Debug }
        internal extern static string Build(ShaderContainer container, FoundryHandle blockShaderHandle, Mode mode);
        internal static string Build(ShaderContainer container, FoundryHandle blockShaderHandle) => Build(container, blockShaderHandle, Mode.Normal);
    }
}
