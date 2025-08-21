// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections;

namespace UnityEngine
{
    [RequireComponent(typeof(Transform))]
    [NativeType(Header = "Modules/RenderAs2D/Public/RenderAs2D.h")]
    internal sealed class RenderAs2D : Renderer
    {
        internal extern void Init(Component owner);
        internal extern bool IsOwner(Component owner);
    }
}
