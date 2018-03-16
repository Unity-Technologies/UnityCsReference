// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/GradientPreviewCache.h")]
    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    [NativeHeader("Runtime/Math/Gradient.h")]
    internal partial class GradientPreviewCache
    {
        public static extern Texture2D GenerateGradientPreview(Gradient gradient, Texture2D existingTexture);
    }
}
