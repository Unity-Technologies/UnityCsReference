// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.VFX
{
    [RequiredByNativeCode]
    [NativeType(Header = "Modules/VFX/Public/VFXExpressionTextureFunctions.h")]
    [StaticAccessor("VFXExpressionTextureFunctions", StaticAccessorType.DoubleColon)]
    internal class VFXExpressionTexture
    {
        extern static internal uint GetTextureFormat(Texture texture);
    }
}
