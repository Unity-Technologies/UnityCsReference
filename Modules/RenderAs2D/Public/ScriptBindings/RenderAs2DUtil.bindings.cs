// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/RenderAs2D/Public/RenderAs2DUtil.h")]
    internal struct RenderAs2DUtil
    {
        [FreeFunction("RenderAs2DUtil::InitializeCanRenderAs2D")]
        internal extern static void InitializeCanRenderAs2D();

        [FreeFunction("RenderAs2DUtil::DisposeCanRenderAs2D")]
        internal extern static void DisposeCanRenderAs2D();
    }
}
