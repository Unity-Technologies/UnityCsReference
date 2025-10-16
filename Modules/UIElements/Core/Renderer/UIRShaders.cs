// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEngine.UIElements.UIR
{
    static class Shaders
    {
        public static readonly string k_AtlasBlit = "Hidden/Internal-UIRAtlasBlitCopy";
        public static readonly string k_Default = "Hidden/Internal-UIRDefault";
        public static readonly string k_RuntimeGaussianBlur = "Hidden/UIR/GaussianBlur";
        public static readonly string k_RuntimeColorEffect = "Hidden/UIR/ColorEffect";
        public static readonly string k_ColorConversionBlit = "Hidden/Internal-UIE-ColorConversionBlit";
        public static readonly string k_ForceGammaKeyword = "_UIE_FORCE_GAMMA";
        public static readonly string k_TextureSlotCount1 = "_UIE_TEXTURE_SLOT_COUNT_1";
        public static readonly string k_TextureSlotCount2 = "_UIE_TEXTURE_SLOT_COUNT_2";
        public static readonly string k_TextureSlotCount4 = "_UIE_TEXTURE_SLOT_COUNT_4";
        public static readonly string k_ForceRenderTypeSolid = "_UIE_RENDER_TYPE_SOLID";
        public static readonly string k_ForceRenderTypeTextured = "_UIE_RENDER_TYPE_TEXTURE";
        public static readonly string k_ForceRenderTypeText = "_UIE_RENDER_TYPE_TEXT";
        public static readonly string k_ForceRenderTypeSvgGradient = "_UIE_RENDER_TYPE_GRADIENT";

        static Material s_DefaultMaterial;

        public static Material defaultMaterial => GetOrCreateMaterial(ref s_DefaultMaterial, k_Default);

        static Material GetOrCreateMaterial(ref Material material, string shaderName)
        {
            if (material == null)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogError($"Could not find shader '{shaderName}'");
                    return null;
                }

                material = new Material(shader);
                material.hideFlags = HideFlags.DontSave;
            }

            return material;
        }

        static int s_RefCount;
        public static void Acquire() => ++s_RefCount;

        public static void Release()
        {
            --s_RefCount;

            Debug.Assert(s_RefCount >= 0, "UIR materials acquire/release don't match.");

            if (s_RefCount < 1)
            {
                s_RefCount = 0;
                UIRUtility.Destroy(s_DefaultMaterial);

                s_DefaultMaterial = null;
            }
        }
    }
}
