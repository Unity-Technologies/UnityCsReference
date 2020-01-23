// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Mono/GradientPreviewCache.bindings.h")]
    internal partial class GradientPreviewCache
    {
        public static extern Texture2D GenerateGradientPreview(Gradient gradient, Texture2D existingTexture);

        [StaticAccessor("GradientPreviewCache::Get()", StaticAccessorType.Dot)]
        public static extern void ClearCache();

        [FreeFunction("GradientPreviewCache_GetPreview_Internal<SerializedProperty>")]
        public static extern Texture2D GetPropertyPreview(SerializedProperty property);

        [FreeFunction("GradientPreviewCache_GetPreview_Internal<Gradient>")]
        public static extern Texture2D GetGradientPreview(Gradient curve);
    }
}
