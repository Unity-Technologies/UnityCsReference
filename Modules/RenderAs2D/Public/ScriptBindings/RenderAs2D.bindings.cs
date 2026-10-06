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
    [AddComponentMenu("")]                                              // This is just here for clarity. This is a builtin component and doesn't show in the editor because of changes made to ComponentRequirement.cpp.
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/RenderAs2D/Public/RenderAs2D.h")]
    [NativeClass("RenderAs2D", PersistentTypeId = 0x42CAB754)]
    internal sealed class RenderAs2D : Renderer
    {
        internal RenderAs2D(global::UnityEngine.EntityId id) : base(id) {}
        internal extern void Init(Component owner);
        internal extern bool IsOwner(Component owner);
    }
}
