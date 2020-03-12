// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace UnityEditor.Experimental.AssetImporters
{
    public class SketchupMaterialDescriptionPreprocessor : AssetPostprocessor
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
            if (lowerCasePath == ".skp")
                CreateFromSketchupMaterial(description, material, clips);
        }

        void CreateFromSketchupMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                context.LogImportError("SketchupMaterialDescriptionPreprocessor cannot find a shader named 'Standard'.");
                return;
            }
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;

            if (description.TryGetProperty("DiffuseColor", out vectorProperty))
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    vectorProperty.x = Mathf.LinearToGammaSpace(vectorProperty.x);
                    vectorProperty.y = Mathf.LinearToGammaSpace(vectorProperty.y);
                    vectorProperty.z = Mathf.LinearToGammaSpace(vectorProperty.z);
                }
                material.SetColor("_Color", vectorProperty);
            }

            if (description.TryGetProperty("DiffuseMap", out textureProperty))
            {
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                material.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
            }

            if (description.TryGetProperty("IsTransparent", out floatProperty) && floatProperty == 1.0f)
            {
                material.SetInt("_Mode", (int)StandardShaderGUI.BlendMode.Transparent);
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                material.SetInt("_Mode", (int)StandardShaderGUI.BlendMode.Opaque);
                material.SetOverrideTag("RenderType", "");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
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
