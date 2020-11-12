// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.AssetImporters
{
    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    public class ThreeDSMaterialDescriptionPreprocessor : AssetPostprocessor
    {
        static readonly uint k_Version = 1;
        static readonly int k_Order = 1;
        public override uint GetVersion()
        {
            return k_Version;
        }

        public override int GetPostprocessOrder()
        {
            return k_Order;
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var lowerCasePath = Path.GetExtension(assetPath).ToLower();
            if (lowerCasePath == ".3ds")
                CreateFrom3DSMaterial(description, material, clips);
        }

        void CreateFrom3DSMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                context.LogImportError("ThreeDSMaterialDescriptionPreprocessor cannot find a shader named 'Standard'.");
                return;
            }

            material.shader = shader;

            TexturePropertyDescription textureProperty;
            float floatProperty;
            Vector4 vectorProperty;

            description.TryGetProperty("diffuse", out vectorProperty);
            vectorProperty.x /= 255.0f;
            vectorProperty.y /= 255.0f;
            vectorProperty.z /= 255.0f;
            vectorProperty.w /= 255.0f;
            description.TryGetProperty("transparency", out floatProperty);

            bool isTransparent = vectorProperty.w <= 0.99f || floatProperty > .0f;
            if (isTransparent)
            {
                material.SetFloat("_Mode", (float)StandardShaderGUI.BlendMode.Transparent);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0.0f);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Mode", (float)StandardShaderGUI.BlendMode.Opaque);
                material.SetOverrideTag("RenderType", "");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1.0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }

            if (floatProperty > .0f)
                vectorProperty.w = 1.0f - floatProperty;

            Color diffuseColor = vectorProperty;

            material.SetColor("_Color", PlayerSettings.colorSpace == ColorSpace.Gamma ? diffuseColor : diffuseColor.linear);

            if (description.TryGetProperty("EmissiveColor", out vectorProperty))
            {
                material.SetColor("_EmissionColor", vectorProperty);
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                material.EnableKeyword("_EMISSION");
            }

            if (description.TryGetProperty("texturemap1", out textureProperty))
            {
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
            }

            if (description.TryGetProperty("bumpmap", out textureProperty))
            {
                if (material.HasProperty("_BumpMap"))
                {
                    SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                    material.EnableKeyword("_NORMALMAP");
                }
            }
        }

        static void SetMaterialTextureProperty(string propertyName, Material material, TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }
    }
}
