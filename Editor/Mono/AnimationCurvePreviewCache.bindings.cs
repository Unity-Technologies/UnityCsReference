// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/AnimationCurvePreviewCache.h")]
    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    internal partial class AnimationCurvePreviewCache
    {
        public static extern Texture2D GenerateCurvePreview(int previewWidth, int previewHeight, Rect curveRanges, AnimationCurve curve, Color color, Texture2D existingTexture);
    }
}
