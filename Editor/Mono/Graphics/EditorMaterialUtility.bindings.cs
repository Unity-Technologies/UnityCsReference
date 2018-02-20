// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/Graphics/EditorMaterialUtility.bindings.h")]
    [NativeHeader("Runtime/Shaders/MaterialIsBackground.h")]
    public sealed partial class EditorMaterialUtility
    {
        [FreeFunction("EditorMaterialUtilityBindings::ResetDefaultTextures")]
        extern public static void ResetDefaultTextures([NotNull] Material material, bool overrideSetTextures);

        [FreeFunction]
        extern public static bool IsBackgroundMaterial([NotNull] Material material);

        [FreeFunction("EditorMaterialUtilityBindings::SetShaderDefaults")]
        extern public static void SetShaderDefaults([NotNull] Shader shader, string[] name, Texture[] textures);

        [FreeFunction("EditorMaterialUtilityBindings::SetShaderNonModifiableDefaults")]
        extern public static void SetShaderNonModifiableDefaults([NotNull] Shader shader, string[] name, Texture[] textures);
    }
}
