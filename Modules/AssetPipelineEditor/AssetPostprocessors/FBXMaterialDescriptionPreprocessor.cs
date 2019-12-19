// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace UnityEditor.Experimental.AssetImporters
{
    public class FBXMaterialDescriptionPreprocessor : AssetPostprocessor
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
            var lowerCaseExtension = Path.GetExtension(assetPath).ToLower();
            if (lowerCaseExtension == ".fbx" || lowerCaseExtension == ".dae" || lowerCaseExtension == ".obj" || lowerCaseExtension == ".blend" || lowerCaseExtension == ".mb" || lowerCaseExtension == ".ma" || lowerCaseExtension == ".max")
            {
                if (IsAutodeskInteractiveMaterial(description))
                    CreateFromAutodeskInteractiveMaterial(description, material, clips);
                else if (IsMayaArnoldStandardSurfaceMaterial(description))
                    CreateFromMayaArnoldStandardSurfaceMaterial(description, material, clips);
                else if (Is3DsMaxArnoldStandardSurfaceMaterial(description))
                    CreateFrom3DsMaxArnoldStandardSurfaceMaterial(description, material, clips);
                else if (Is3DsMaxPhysicalMaterial(description))
                    CreateFrom3DsMaxPhysicalMaterial(description, material, clips);
                else
                    CreateFromStandardMaterial(description, material, clips);
            }
        }

        static bool Is3DsMaxPhysicalMaterial(MaterialDescription description)
        {
            float classIdA;
            float classIdB;
            description.TryGetProperty("ClassIDa", out classIdA);
            description.TryGetProperty("ClassIDb", out classIdB);
            return classIdA == 1030429932 && classIdB == -559038463;
        }

        static bool IsMayaArnoldStandardSurfaceMaterial(MaterialDescription description)
        {
            float typeId;
            description.TryGetProperty("TypeId", out typeId);
            return typeId == 1138001;
        }

        static bool Is3DsMaxArnoldStandardSurfaceMaterial(MaterialDescription description)
        {
            float classIdA;
            float classIdB;
            description.TryGetProperty("ClassIDa", out classIdA);
            description.TryGetProperty("ClassIDb", out classIdB);
            return classIdA == 2121471519 && classIdB == 1660373836;
        }

        static bool IsAutodeskInteractiveMaterial(MaterialDescription description)
        {
            string stringValue;
            return description.TryGetProperty("renderAPI", out stringValue) && stringValue == "SFX_PBS_SHADER";
        }

        void CreateFrom3DsMaxArnoldStandardSurfaceMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            // 3DsMax does not export material animations for Arnold materials.
            var shader = Shader.Find("Autodesk Interactive");
            if (shader == null)
            {
                context.LogImportError("FBXMaterialDescriptionPreprocessor cannot find a shader named 'Autodesk Interactive'.");
                return;
            }
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;

            if (description.TryGetProperty("transmission", out floatProperty) && floatProperty > 0.0f)
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
            if (description.TryGetProperty("base_color.shader", out textureProperty))
            {
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                Color baseColor = new Color(1.0f, 1.0f, 1.0f, 1.0f - floatProperty);
                material.SetColor("_Color", baseColor);
            }
            else if (description.TryGetProperty("base_color", out vectorProperty))
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    vectorProperty.x = Mathf.LinearToGammaSpace(vectorProperty.x);
                    vectorProperty.y = Mathf.LinearToGammaSpace(vectorProperty.y);
                    vectorProperty.z = Mathf.LinearToGammaSpace(vectorProperty.z);
                }

                vectorProperty.w = 1.0f - floatProperty;
                material.SetColor("_Color", vectorProperty);
            }

            if (description.TryGetProperty("normal.shader", out textureProperty))
            {
                SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                material.EnableKeyword("_NORMALMAP");
            }
            if (description.TryGetProperty("specular_roughness", out textureProperty))
            {
                SetMaterialTextureProperty("_SpecGlossMap", material, textureProperty);
                material.EnableKeyword("_SPECGLOSSMAP");
            }
            else if (description.TryGetProperty("specular_roughness", out floatProperty))
            {
                material.SetFloat("_Glossiness", floatProperty);
            }
            if (description.TryGetProperty("metalness", out textureProperty))
            {
                SetMaterialTextureProperty("_MetallicGlossMap", material, textureProperty);
                material.EnableKeyword("_METALLICGLOSSMAP");
            }

            if (description.TryGetProperty("emission", out floatProperty) && floatProperty > 0.0f)
            {
                Color emissiveColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                if (description.TryGetProperty("emission_color.shader", out textureProperty))
                {
                    emissiveColor *= floatProperty;
                    SetMaterialTextureProperty("_EmissionMap", material, textureProperty);
                    material.SetColor("_EmissionColor", emissiveColor);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else if (description.TryGetProperty("emission_color", out vectorProperty))
                {
                    emissiveColor = vectorProperty * floatProperty;
                    material.SetColor("_EmissionColor", emissiveColor);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
        }

        void CreateFromMayaArnoldStandardSurfaceMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var shader = Shader.Find("Autodesk Interactive");
            if (shader == null)
            {
                context.LogImportError("FBXMaterialDescriptionPreprocessor cannot find a shader named 'Autodesk Interactive'.");
                return;
            }
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;

            if (description.TryGetProperty("transmission", out floatProperty) && floatProperty > 0.0f)
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
            if (description.TryGetProperty("baseColor", out textureProperty))
            {
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                Color baseColor = new Color(1.0f, 1.0f, 1.0f, 1.0f - floatProperty);
                material.SetColor("_Color", baseColor);
            }
            else if (description.TryGetProperty("baseColor", out vectorProperty))
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                {
                    vectorProperty.x = Mathf.LinearToGammaSpace(vectorProperty.x);
                    vectorProperty.y = Mathf.LinearToGammaSpace(vectorProperty.y);
                    vectorProperty.z = Mathf.LinearToGammaSpace(vectorProperty.z);
                }

                vectorProperty.w = 1.0f - floatProperty;
                material.SetColor("_Color", vectorProperty);

                RemapColorCurves(description, clips, "baseColor", "_Color");
            }

            if (description.HasAnimationCurve("transmission"))
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationCurve opacityCurve;
                    description.TryGetAnimationCurve(clips[i].name, "transmission", out opacityCurve);
                    clips[i].SetCurve("", typeof(Material), "_Color.a", opacityCurve);

                    if (!description.HasAnimationCurveInClip(clips[i].name, "baseColor.x"))
                    {
                        Vector4 diffuseColor;
                        description.TryGetProperty("baseColor", out diffuseColor);
                        clips[i].SetCurve("", typeof(Material), "_Color.r", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.x));
                        clips[i].SetCurve("", typeof(Material), "_Color.g", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.y));
                        clips[i].SetCurve("", typeof(Material), "_Color.b", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.z));
                    }
                }
            }

            if (description.TryGetProperty("normalCamera", out textureProperty))
            {
                SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                material.EnableKeyword("_NORMALMAP");
            }
            if (description.TryGetProperty("specularRoughness", out textureProperty))
            {
                SetMaterialTextureProperty("_SpecGlossMap", material, textureProperty);
                material.EnableKeyword("_SPECGLOSSMAP");
            }
            else if (description.TryGetProperty("specularRoughness", out floatProperty))
            {
                material.SetFloat("_Glossiness", floatProperty);
                RemapCurve(description, clips, "specularRoughness", "_Glossiness");
            }
            if (description.TryGetProperty("metalness", out textureProperty))
            {
                SetMaterialTextureProperty("_MetallicGlossMap", material, textureProperty);
                material.EnableKeyword("_METALLICGLOSSMAP");
                RemapCurve(description, clips, "metalness", "_MetallicGlossMap");
            }

            if (description.TryGetProperty("emission", out floatProperty) && floatProperty > 0.0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;

                Color emissiveColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                if (description.TryGetProperty("emissionColor", out textureProperty))
                {
                    emissiveColor *= floatProperty;
                    SetMaterialTextureProperty("_EmissionMap", material, textureProperty);
                    material.SetColor("_EmissionColor", emissiveColor);
                }
                else if (description.TryGetProperty("emissionColor", out vectorProperty))
                {
                    emissiveColor = vectorProperty * floatProperty;
                    material.SetColor("_EmissionColor", emissiveColor);
                }
            }

            if (description.HasAnimationCurve("emissionColor.x"))
            {
                if (description.HasAnimationCurve("emission"))
                {
                    // combine color and intensity.
                    AnimationCurve curve;
                    for (int i = 0; i < clips.Length; i++)
                    {
                        AnimationCurve intensityCurve;
                        description.TryGetAnimationCurve(clips[i].name, "emission", out intensityCurve);

                        description.TryGetAnimationCurve(clips[i].name, "emissionColor.x", out curve);
                        MultiplyCurves(curve, intensityCurve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curve);

                        description.TryGetAnimationCurve(clips[i].name, "emissionColor.y", out curve);
                        MultiplyCurves(curve, intensityCurve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curve);

                        description.TryGetAnimationCurve(clips[i].name, "emissionColor.z", out curve);
                        MultiplyCurves(curve, intensityCurve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curve);
                    }
                }
                else
                {
                    RemapColorCurves(description, clips, "emission", "_EmissionColor");
                }
            }
            else if (description.HasAnimationCurve("emission"))
            {
                Vector4 emissiveColor;
                description.TryGetProperty("emissionColor", out emissiveColor);
                AnimationCurve curve;
                for (int i = 0; i < clips.Length; i++)
                {
                    description.TryGetAnimationCurve(clips[i].name, "emission", out curve);
                    // remap emissive intensity to emission color
                    AnimationCurve curveR = new AnimationCurve();
                    ConvertAndCopyKeys(curveR, curve, value => ConvertFloatMultiply(emissiveColor.x, value));
                    clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curveR);

                    AnimationCurve curveG = new AnimationCurve();
                    ConvertAndCopyKeys(curveG, curve, value => ConvertFloatMultiply(emissiveColor.y, value));
                    clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curveG);

                    AnimationCurve curveB = new AnimationCurve();
                    ConvertAndCopyKeys(curveB, curve, value => ConvertFloatMultiply(emissiveColor.z, value));
                    clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curveB);
                }
            }
        }

        void CreateFrom3DsMaxPhysicalMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var shader = Shader.Find("Autodesk Interactive");
            if (shader == null)
            {
                context.LogImportError("FBXMaterialDescriptionPreprocessor cannot find a shader named 'Autodesk Interactive'.");
                return;
            }
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;
            float transparency = 0.0f;
            description.TryGetProperty("transparency", out transparency);

            if (transparency > 0.0f)
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

            if (description.TryGetProperty("base_color_map", out textureProperty))
            {
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                material.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 1.0f - transparency));
            }
            else if (description.TryGetProperty("base_color", out vectorProperty))
            {
                vectorProperty.w = 1.0f - transparency;
                material.SetColor("_Color", vectorProperty);
            }
            if (description.TryGetProperty("bump_map", out textureProperty))
            {
                if (description.TryGetProperty("bump_map_amt", out floatProperty))
                {
                    material.SetFloat("_BumpScale", floatProperty);
                }
                SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                material.EnableKeyword("_NORMALMAP");
            }
            if (description.TryGetProperty("roughness_map", out textureProperty))
            {
                SetMaterialTextureProperty("_SpecGlossMap", material, textureProperty);
                material.EnableKeyword("_SPECGLOSSMAP");
            }
            if (description.TryGetProperty("metalness_map", out textureProperty))
            {
                SetMaterialTextureProperty("_MetallicGlossMap", material, textureProperty);
                material.EnableKeyword("_METALLICGLOSSMAP");
            }

            if (description.TryGetProperty("emit_color", out vectorProperty))
            {
                if (description.TryGetProperty("emission", out floatProperty))
                {
                    vectorProperty *= floatProperty;
                }
                material.SetColor("_EmissionColor", vectorProperty);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }

        void CreateFromAutodeskInteractiveMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var shader = Shader.Find("Autodesk Interactive");
            if (shader == null)
            {
                context.LogImportError("FBXMaterialDescriptionPreprocessor cannot find a shader named 'Autodesk Interactive'.");
                return;
            }
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;
            AnimationCurve curve;

            Vector2 uvOffset = new Vector2(0.0f, 0.0f);
            Vector2 uvScale = new Vector2(1.0f, 1.0f);

            if (description.TryGetProperty("uv_offset", out vectorProperty))
            {
                uvOffset.x = vectorProperty.x;
                uvOffset.y = -vectorProperty.y;
            }
            if (description.TryGetProperty("uv_scale", out vectorProperty))
            {
                uvScale.x = vectorProperty.x;
                uvScale.y = vectorProperty.y;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (description.HasAnimationCurveInClip(clips[i].name, "uv_scale.x") ||
                    description.HasAnimationCurveInClip(clips[i].name, "uv_scale.y") ||
                    description.HasAnimationCurveInClip(clips[i].name, "uv_offset.x") ||
                    description.HasAnimationCurveInClip(clips[i].name, "uv_offset.y")
                )
                {
                    if (description.TryGetAnimationCurve(clips[i].name, "uv_scale.x", out curve))
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.x", curve);
                    else
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.x", AnimationCurve.Constant(0.0f, 1.0f, 1.0f));

                    if (description.TryGetAnimationCurve(clips[i].name, "uv_scale.y", out curve))
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.y", curve);
                    else
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.y", AnimationCurve.Constant(0.0f, 1.0f, 1.0f));

                    if (description.TryGetAnimationCurve(clips[i].name, "uv_offset.x", out curve))
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.z", curve);
                    else
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.z", AnimationCurve.Constant(0.0f, 1.0f, 0.0f));

                    if (description.TryGetAnimationCurve(clips[i].name, "uv_offset.y", out curve))
                    {
                        ConvertKeys(curve, ConvertFloatNegate);
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.w", curve);
                    }
                    else
                        clips[i].SetCurve("", typeof(Material), "_MainTex_ST.w", AnimationCurve.Constant(0.0f, 1.0f, 0.0f));
                }
            }

            float opacity = 1.0f;
            float alphaThreshold = 0.0f;

            description.TryGetProperty("opacity", out opacity);
            description.TryGetProperty("mask_threshold", out alphaThreshold);
            if (alphaThreshold > 0.0f || description.HasAnimationCurve("mask_threshold"))
            {
                material.SetInt("_Mode", (int)StandardShaderGUI.BlendMode.Cutout);
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else if (opacity < 1.0f ||
                     description.HasAnimationCurve("opacity") ||
                     (description.TryGetProperty("use_opacity_map", out floatProperty) && floatProperty == 1.0f))
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

            if (description.TryGetProperty("use_color_map", out floatProperty) && floatProperty == 1.0f ||
                description.TryGetProperty("use_opacity_map", out floatProperty) && floatProperty == 1.0f)
            {
                if (description.TryGetProperty("TEX_color_map", out textureProperty))
                {
                    material.SetTexture("_MainTex", textureProperty.texture);
                    material.SetTextureOffset("_MainTex", uvOffset);
                    material.SetTextureScale("_MainTex", uvScale);
                    material.SetColor("_Color", new Vector4(1.0f, 1.0f, 1.0f, opacity));

                    if (alphaThreshold > 0.0f || description.HasAnimationCurve("mask_threshold"))
                    {
                        material.SetFloat("_Cutoff", alphaThreshold);
                        RemapCurve(description, clips, "mask_threshold", "_Cutoff");
                    }
                }
            }
            else
            {
                description.TryGetProperty("base_color", out vectorProperty);
                vectorProperty.w = opacity;
                material.SetColor("_Color", vectorProperty);
                RemapColorCurves(description, clips, "base_color", "_Color");
            }

            if (description.HasAnimationCurve("opacity"))
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationCurve opacityCurve;
                    description.TryGetAnimationCurve(clips[i].name, "opacity", out opacityCurve);
                    clips[i].SetCurve("", typeof(Material), "_Color.a", opacityCurve);

                    if (!description.HasAnimationCurveInClip(clips[i].name, "base_color.x"))
                    {
                        Vector4 diffuseColor;
                        description.TryGetProperty("base_color", out diffuseColor);
                        clips[i].SetCurve("", typeof(Material), "_Color.r", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.x));
                        clips[i].SetCurve("", typeof(Material), "_Color.g", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.y));
                        clips[i].SetCurve("", typeof(Material), "_Color.b", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.z));
                    }
                }
            }

            if (description.TryGetProperty("use_normal_map", out floatProperty) && floatProperty == 1.0f)
            {
                if (description.TryGetProperty("TEX_normal_map", out textureProperty))
                {
                    material.SetTexture("_BumpMap", textureProperty.texture);
                    material.SetTextureOffset("_BumpMap", uvOffset);
                    material.SetTextureScale("_BumpMap", uvScale);
                    material.EnableKeyword("_NORMALMAP");
                }
            }

            if (description.TryGetProperty("use_metallic_map", out floatProperty) && floatProperty == 1.0f)
            {
                if (description.TryGetProperty("TEX_metallic_map", out textureProperty))
                {
                    material.SetTexture("_MetallicGlossMap", textureProperty.texture);
                    material.SetTextureOffset("_MetallicGlossMap", uvOffset);
                    material.SetTextureScale("_MetallicGlossMap", uvScale);
                    material.EnableKeyword("_METALLICGLOSSMAP");
                }
            }
            else
            {
                if (description.TryGetProperty("metallic", out floatProperty))
                {
                    material.SetFloat("_Metallic", floatProperty);
                    RemapCurve(description, clips, "metallic", "_Metallic");
                }
            }

            if (description.TryGetProperty("use_roughness_map", out floatProperty) && floatProperty == 1.0f)
            {
                if (description.TryGetProperty("TEX_roughness_map", out textureProperty))
                {
                    material.SetTexture("_SpecGlossMap", textureProperty.texture);
                    material.SetTextureOffset("_SpecGlossMap", uvOffset);
                    material.SetTextureScale("_SpecGlossMap", uvScale);
                    material.EnableKeyword("_SPECGLOSSMAP");
                }
            }
            else
            {
                if (description.TryGetProperty("roughness", out floatProperty))
                {
                    material.SetFloat("_Glossiness", floatProperty);
                    RemapCurve(description, clips, "roughness", "_Glossiness");
                }
            }

            if (description.TryGetProperty("use_emissive_map", out floatProperty) && floatProperty == 1.0f)
            {
                if (description.TryGetProperty("TEX_emissive_map", out textureProperty))
                {
                    Vector4 emissiveColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                    if (description.TryGetProperty("emissive_intensity", out floatProperty))
                    {
                        emissiveColor *= floatProperty;
                    }
                    material.SetColor("_EmissionColor", emissiveColor);
                    material.SetTexture("_EmissionMap", textureProperty.texture);
                    material.SetTextureOffset("_EmissionMap", uvOffset);
                    material.SetTextureScale("_EmissionMap", uvScale);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                if (description.HasAnimationCurve("emissive_intensity"))
                {
                    Vector4 emissiveColor;
                    description.TryGetProperty("emissive", out emissiveColor);

                    for (int i = 0; i < clips.Length; i++)
                    {
                        description.TryGetAnimationCurve(clips[i].name, "emissive_intensity", out curve);
                        // remap emissive intensity to emission color
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curve);
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curve);
                    }
                }
            }
            else if (description.TryGetProperty("emissive", out vectorProperty))
            {
                if (vectorProperty.x > 0.0f || vectorProperty.y > 0.0f || vectorProperty.z > 0.0f)
                {
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.EnableKeyword("_EMISSION");
                }

                if (description.TryGetProperty("emissive_intensity", out floatProperty))
                {
                    vectorProperty *= floatProperty;
                }

                material.SetColor("_EmissionColor", vectorProperty);


                if (description.HasAnimationCurve("emissive.x"))
                {
                    if (description.HasAnimationCurve("emissive_intensity"))
                    {
                        // combine color and intensity.
                        for (int i = 0; i < clips.Length; i++)
                        {
                            AnimationCurve intensityCurve;
                            description.TryGetAnimationCurve(clips[i].name, "emissive_intensity", out intensityCurve);

                            description.TryGetAnimationCurve(clips[i].name, "emissive.x", out curve);
                            MultiplyCurves(curve, intensityCurve);
                            clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curve);

                            description.TryGetAnimationCurve(clips[i].name, "emissive.y", out curve);
                            MultiplyCurves(curve, intensityCurve);
                            clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curve);

                            description.TryGetAnimationCurve(clips[i].name, "emissive.z", out curve);
                            MultiplyCurves(curve, intensityCurve);
                            clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curve);
                        }
                    }
                    else
                    {
                        RemapColorCurves(description, clips, "emissive", "_EmissionColor");
                    }
                }
                else if (description.HasAnimationCurve("emissive_intensity"))
                {
                    Vector4 emissiveColor;
                    description.TryGetProperty("emissive", out emissiveColor);

                    for (int i = 0; i < clips.Length; i++)
                    {
                        description.TryGetAnimationCurve(clips[i].name, "emissive_intensity", out curve);
                        // remap emissive intensity to emission color
                        AnimationCurve curveR = new AnimationCurve();
                        ConvertAndCopyKeys(curveR, curve, value => ConvertFloatMultiply(emissiveColor.x, value));
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.r", curveR);

                        AnimationCurve curveG = new AnimationCurve();
                        ConvertAndCopyKeys(curveG, curve, value => ConvertFloatMultiply(emissiveColor.y, value));
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.g", curveG);

                        AnimationCurve curveB = new AnimationCurve();
                        ConvertAndCopyKeys(curveB, curve, value => ConvertFloatMultiply(emissiveColor.z, value));
                        clips[i].SetCurve("", typeof(Material), "_EmissionColor.b", curveB);
                    }
                }
            }

            if (description.TryGetProperty("use_ao_map", out floatProperty) && floatProperty == 1.0f)
            {
                if (description.TryGetProperty("TEX_ao_map", out textureProperty))
                {
                    material.SetTexture("_OcclusionMap", textureProperty.texture);
                    material.SetTextureOffset("_OcclusionMap", uvOffset);
                    material.SetTextureScale("_OcclusionMap", uvScale);
                }
            }
        }

        void CreateFromStandardMaterial(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                context.LogImportError("FBXMaterialDescriptionPreprocessor cannot find a shader named 'Standard'.");
                return;
            }
            material.shader = shader;

            Vector4 vectorProperty;
            float floatProperty;
            TexturePropertyDescription textureProperty;

            bool isTransparent = false;

            float opacity;
            float transparencyFactor;
            if (!description.TryGetProperty("Opacity", out opacity))
            {
                if (description.TryGetProperty("TransparencyFactor", out transparencyFactor))
                {
                    opacity = transparencyFactor == 1.0f ? 1.0f : 1.0f - transparencyFactor;
                }
                if (opacity == 1.0f && description.TryGetProperty("TransparentColor", out vectorProperty))
                {
                    opacity = vectorProperty.x == 1.0f ? 1.0f : 1.0f - vectorProperty.x;
                }
            }
            if (opacity < 1.0f || (opacity == 1.0f && description.TryGetProperty("TransparentColor", out textureProperty)))
            {
                isTransparent = true;
            }
            else if (description.HasAnimationCurve("TransparencyFactor") || description.HasAnimationCurve("TransparentColor"))
            {
                isTransparent = true;
            }

            if (isTransparent)
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

            if (description.TryGetProperty("DiffuseColor", out textureProperty))
            {
                Color diffuseColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                if (description.TryGetProperty("DiffuseFactor", out floatProperty))
                    diffuseColor *= floatProperty;
                diffuseColor.a = opacity;

                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                material.SetColor("_Color", PlayerSettings.colorSpace == ColorSpace.Gamma ? diffuseColor.gamma : diffuseColor);
            }
            else if (description.TryGetProperty("DiffuseColor", out vectorProperty))
            {
                Color diffuseColor = vectorProperty;
                if (description.TryGetProperty("DiffuseFactor", out floatProperty))
                    diffuseColor *= floatProperty;
                diffuseColor.a = opacity;
                material.SetColor("_Color", PlayerSettings.colorSpace == ColorSpace.Gamma ? diffuseColor.gamma : diffuseColor);
            }

            if (description.TryGetProperty("Bump", out textureProperty))
            {
                SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                material.EnableKeyword("_NORMALMAP");

                if (description.TryGetProperty("BumpFactor", out floatProperty))
                    material.SetFloat("_BumpScale", floatProperty);
            }
            else if (description.TryGetProperty("NormalMap", out textureProperty))
            {
                SetMaterialTextureProperty("_BumpMap", material, textureProperty);
                material.EnableKeyword("_NORMALMAP");

                if (description.TryGetProperty("BumpFactor", out floatProperty))
                    material.SetFloat("_BumpScale", floatProperty);
            }

            if (description.TryGetProperty("EmissiveColor", out textureProperty))
            {
                Color emissiveColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

                material.SetColor("_EmissionColor", emissiveColor);
                SetMaterialTextureProperty("_EmissionMap", material, textureProperty);

                if (description.TryGetProperty("EmissiveFactor", out floatProperty) && floatProperty > 0.0f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
            else if (
                description.TryGetProperty("EmissiveColor", out vectorProperty) && vectorProperty.magnitude > vectorProperty.w
                || description.HasAnimationCurve("EmissiveColor.x"))
            {
                if (description.TryGetProperty("EmissiveFactor", out floatProperty))
                    vectorProperty *= floatProperty;

                material.SetColor("_EmissionColor", vectorProperty);
                if (floatProperty > 0.0f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }

            material.SetFloat("_Glossiness", 0.0f);

            if (PlayerSettings.colorSpace == ColorSpace.Gamma)
                RemapAndTransformColorCurves(description, clips, "DiffuseColor", "_Color", ConvertFloatLinearToGamma);
            else
                RemapColorCurves(description, clips, "DiffuseColor", "_Color");

            RemapTransparencyCurves(description, clips);

            RemapColorCurves(description, clips, "EmissiveColor", "_EmissionColor");
        }

        static void RemapTransparencyCurves(MaterialDescription description, AnimationClip[] clips)
        {
            // For some reason, Opacity is never animated, we have to use TransparencyFactor and TransparentColor
            for (int i = 0; i < clips.Length; i++)
            {
                bool foundTransparencyCurve = false;
                AnimationCurve curve;
                if (description.TryGetAnimationCurve(clips[i].name, "TransparencyFactor", out curve))
                {
                    ConvertKeys(curve, ConvertFloatOneMinus);
                    clips[i].SetCurve("", typeof(Material), "_Color.a", curve);
                    foundTransparencyCurve = true;
                }
                else if (description.TryGetAnimationCurve(clips[i].name, "TransparentColor.x", out curve))
                {
                    ConvertKeys(curve, ConvertFloatOneMinus);
                    clips[i].SetCurve("", typeof(Material), "_Color.a", curve);
                    foundTransparencyCurve = true;
                }

                if (foundTransparencyCurve && !description.HasAnimationCurveInClip(clips[i].name, "DiffuseColor"))
                {
                    Vector4 diffuseColor;
                    description.TryGetProperty("DiffuseColor", out diffuseColor);
                    clips[i].SetCurve("", typeof(Material), "_Color.r", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.x));
                    clips[i].SetCurve("", typeof(Material), "_Color.g", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.y));
                    clips[i].SetCurve("", typeof(Material), "_Color.b", AnimationCurve.Constant(0.0f, 1.0f, diffuseColor.z));
                }
            }
        }

        static void RemapCurve(MaterialDescription description, AnimationClip[] clips, string originalPropertyName, string newPropertyName)
        {
            AnimationCurve curve;
            for (int i = 0; i < clips.Length; i++)
            {
                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName, out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName, curve);
                }
            }
        }

        static void RemapColorCurves(MaterialDescription description, AnimationClip[] clips, string originalPropertyName, string newPropertyName)
        {
            AnimationCurve curve;
            for (int i = 0; i < clips.Length; i++)
            {
                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".x", out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".r", curve);
                }

                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".y", out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".g", curve);
                }

                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".z", out curve))
                {
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".b", curve);
                }
            }
        }

        static void RemapAndTransformColorCurves(MaterialDescription description, AnimationClip[] clips, string originalPropertyName, string newPropertyName, System.Func<float, float> converter)
        {
            AnimationCurve curve;
            for (int i = 0; i < clips.Length; i++)
            {
                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".x", out curve))
                {
                    ConvertKeys(curve, converter);
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".r", curve);
                }

                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".y", out curve))
                {
                    ConvertKeys(curve, converter);
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".g", curve);
                }

                if (description.TryGetAnimationCurve(clips[i].name, originalPropertyName + ".z", out curve))
                {
                    ConvertKeys(curve, converter);
                    clips[i].SetCurve("", typeof(Material), newPropertyName + ".b", curve);
                }
            }
        }

        static float ConvertFloatMultiply(float value, float multiplier)
        {
            return value * multiplier;
        }

        static float ConvertFloatLinearToGamma(float value)
        {
            return Mathf.LinearToGammaSpace(value);
        }

        static float ConvertFloatOneMinus(float value)
        {
            return 1.0f - value;
        }

        static float ConvertFloatNegate(float value)
        {
            return -value;
        }

        static void MultiplyCurves(AnimationCurve curve, AnimationCurve curveMultiplier)
        {
            Keyframe[] keyframes = curve.keys;
            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i].value *= curveMultiplier.Evaluate(keyframes[i].time);
            }
            curve.keys = keyframes;
        }

        static void ConvertAndCopyKeys(AnimationCurve curveDest, AnimationCurve curveSource, System.Func<float, float> convertionDelegate)
        {
            for (int i = 0; i < curveSource.keys.Length; i++)
            {
                var sourceKey = curveSource.keys[i];
                curveDest.AddKey(new Keyframe(sourceKey.time, convertionDelegate(sourceKey.value), sourceKey.inTangent, sourceKey.outTangent, sourceKey.inWeight, sourceKey.outWeight));
            }
        }

        static void ConvertKeys(AnimationCurve curve, System.Func<float, float> convertionDelegate)
        {
            Keyframe[] keyframes = curve.keys;
            for (int i = 0; i < keyframes.Length; i++)
            {
                keyframes[i].value = convertionDelegate(keyframes[i].value);
            }
            curve.keys = keyframes;
        }

        static void SetMaterialTextureProperty(string propertyName, Material material, TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }
    }
}
