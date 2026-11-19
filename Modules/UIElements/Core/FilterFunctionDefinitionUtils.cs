// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using UnityEngine.Rendering;

namespace UnityEngine.UIElements
{
    internal static class FilterFunctionDefinitionUtils
    {
        // Blur kernel cost and read margins are O(sigma); larger values hang the GPU (UUM-134044).
        internal const float k_MaxBlurSigma = 100.0f;

        // These definition are initialized once at first use and are shared across all filter functions.
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_BlurDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_TintDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_OpacityDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_InvertDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_GrayscaleDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_SepiaDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_ContrastDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_HueRotateDef;
        [NoAutoStaticsCleanup]
        private static FilterFunctionDefinition s_DropShadowDef;

        // Passes batch only when they share a material, so the built-in filters share one per shader
        [NoAutoStaticsCleanup]
        private static Material s_BlurMaterial;
        [NoAutoStaticsCleanup]
        private static Material s_ColorEffectMaterial;

        static Material GetSharedMaterial(ref Material material, string shaderName)
        {
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }

        public static string GetBuiltinFilterName(FilterFunctionType type)
        {
            switch (type)
            {
                case FilterFunctionType.Blur:
                    return StyleValueFunctionExtension.k_FilterBlur;
                case FilterFunctionType.Tint:
                    return StyleValueFunctionExtension.k_FilterTint;
                case FilterFunctionType.Opacity:
                    return StyleValueFunctionExtension.k_FilterOpacity;
                case FilterFunctionType.Invert:
                    return StyleValueFunctionExtension.k_FilterInvert;
                case FilterFunctionType.Grayscale:
                    return StyleValueFunctionExtension.k_FilterGrayscale;
                case FilterFunctionType.Sepia:
                    return StyleValueFunctionExtension.k_FilterSepia;
                case FilterFunctionType.Contrast:
                    return StyleValueFunctionExtension.k_FilterContrast;
                case FilterFunctionType.HueRotate:
                    return StyleValueFunctionExtension.k_FilterHueRotate;
                case FilterFunctionType.DropShadow:
                    return StyleValueFunctionExtension.k_FilterDropShadow;
            }

            return null;
        }

        public static FilterFunctionDefinition GetBuiltinDefinition(FilterFunctionType type)
        {
            switch (type)
            {
                case FilterFunctionType.Blur:
                {
                    if (s_BlurDef == null)
                        s_BlurDef = CreateBlurFilterFunctionDefinition();
                    return s_BlurDef;
                }
                case FilterFunctionType.Tint:
                {
                    if (s_TintDef == null)
                        s_TintDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.Tint);
                    return s_TintDef;
                }
                case FilterFunctionType.Opacity:
                {
                    if (s_OpacityDef == null)
                        s_OpacityDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.Opacity);
                    return s_OpacityDef;
                }
                case FilterFunctionType.Invert:
                {
                    if (s_InvertDef == null)
                        s_InvertDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.Invert);
                    return s_InvertDef;
                }
                case FilterFunctionType.Grayscale:
                {
                    if (s_GrayscaleDef == null)
                        s_GrayscaleDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.Grayscale);
                    return s_GrayscaleDef;
                }
                case FilterFunctionType.Sepia:
                {
                    if (s_SepiaDef == null)
                        s_SepiaDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.Sepia);
                    return s_SepiaDef;
                }
                case FilterFunctionType.Contrast:
                {
                    if (s_ContrastDef == null)
                        s_ContrastDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.Contrast);
                    return s_ContrastDef;
                }
                case FilterFunctionType.HueRotate:
                {
                    if (s_HueRotateDef == null)
                        s_HueRotateDef = CreateColorEffectFilterFunctionDefinition(FilterFunctionType.HueRotate);
                    return s_HueRotateDef;
                }
                case FilterFunctionType.DropShadow:
                {
                    if (s_DropShadowDef == null)
                        s_DropShadowDef = CreateDropShadowFilterFunctionDefinition();
                    return s_DropShadowDef;
                }
            }

            return null;
        }

        static FilterFunctionDefinition CreateBlurFilterFunctionDefinition()
        {
            var blurMaterial = GetSharedMaterial(ref s_BlurMaterial, "Hidden/UIR/GaussianBlur");

            var filter = ScriptableObject.CreateInstance<FilterFunctionDefinition>();
            filter.hideFlags = HideFlags.HideAndDontSave;
            filter.filterName = GetBuiltinFilterName(FilterFunctionType.Blur);

            filter.parameters = new[]
            {
                // Gaussian-blur sigma
                new FilterParameterDeclaration {
                    interpolationDefaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    defaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    hasFloatRange = true,
                    minFloatValue = 0.0f,
                    maxFloatValue = k_MaxBlurSigma
                }
            };

            filter.passes = new[]
            {
                new PostProcessingPass
                {
                    material = blurMaterial,
                    passIndex = 0,
                    readMargins = new(), // Margins are set dynamically in computeRequiredReadMarginsCallback
                    writeMargins = new()
                },
                new PostProcessingPass
                {
                    material = blurMaterial,
                    passIndex = 1,
                    readMargins = new(), // Margins are set dynamically in computeRequiredReadMarginsCallback
                    writeMargins = new()
                }
            };

            filter.passes[0].computeRequiredReadMarginsCallback = ComputeHorizontalBlurMargins;
            filter.passes[0].computeRequiredWriteMarginsCallback = ComputeHorizontalBlurMargins;
            SetInstancedParameters(ref filter.passes[0], WriteBlurParameters);

            filter.passes[1].computeRequiredReadMarginsCallback = ComputeVerticalBlurMargins;
            filter.passes[1].computeRequiredWriteMarginsCallback = ComputeVerticalBlurMargins;
            SetInstancedParameters(ref filter.passes[1], WriteBlurParameters);

            return filter;
        }

        static FilterFunctionDefinition CreateColorEffectFilterFunctionDefinition(FilterFunctionType filterType)
        {
            var colorEffectMaterial = GetSharedMaterial(ref s_ColorEffectMaterial, "Hidden/UIR/ColorEffect");

            var filter = ScriptableObject.CreateInstance<FilterFunctionDefinition>();
            filter.hideFlags = HideFlags.HideAndDontSave;
            filter.filterName = GetBuiltinFilterName(filterType);

            FilterParameter interpolationDefault = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f };
            FilterParameter defaultVal = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f };

            switch (filterType)
            {
                case FilterFunctionType.Tint:
                    interpolationDefault = new FilterParameter { type = FilterParameterType.Color, colorValue = Color.white };
                    defaultVal = new FilterParameter { type = FilterParameterType.Color, colorValue = Color.white };
                    break;
                case FilterFunctionType.Opacity:
                    interpolationDefault = new FilterParameter { type = FilterParameterType.Float, floatValue = 1.0f };
                    defaultVal = new FilterParameter { type = FilterParameterType.Float, floatValue = 1.0f };
                    break;
                case FilterFunctionType.Invert:
                case FilterFunctionType.Grayscale:
                case FilterFunctionType.Sepia:
                case FilterFunctionType.Contrast:
                    defaultVal = new FilterParameter { type = FilterParameterType.Float, floatValue = 1.0f };
                    break;
                case FilterFunctionType.HueRotate:
                    break;
                default:
                    break;
            }

            filter.parameters = new[]
            {
                new FilterParameterDeclaration {
                    interpolationDefaultValue = interpolationDefault,
                    defaultValue = defaultVal
                }
            };

            filter.passes = new[]
            {
                new PostProcessingPass
                {
                    material = colorEffectMaterial,
                    passIndex = 0,
                    readMargins = new PostProcessingMargins { left = 0, top = 0, right = 0, bottom = 0 },
                    writeMargins = new PostProcessingMargins { left = 0, top = 0, right = 0, bottom = 0 }
                },
            };

            SetInstancedParameters(ref filter.passes[0], WriteColorEffectParameters);

            return filter;
        }

        static FilterFunctionDefinition CreateDropShadowFilterFunctionDefinition()
        {
            var blurMaterial = GetSharedMaterial(ref s_BlurMaterial, "Hidden/UIR/GaussianBlur");

            var compositeMaterial = new Material(Shader.Find("Hidden/UIR/DropShadowComposite"));
            compositeMaterial.hideFlags = HideFlags.HideAndDontSave;

            var filter = ScriptableObject.CreateInstance<FilterFunctionDefinition>();
            filter.hideFlags = HideFlags.HideAndDontSave;
            filter.filterName = GetBuiltinFilterName(FilterFunctionType.DropShadow);

            // Parameter layout: [0] offsetX (px), [1] offsetY (px), [2] sigma (px), [3] color
            // Offsets are range-limited to what the capped margins can express, so a large offset
            // clamps instead of silently truncating against UIR.FilterHelper.k_MaxPostProcessingMargin.
            filter.parameters = new[]
            {
                new FilterParameterDeclaration {
                    interpolationDefaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    defaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    name = "X",
                    hasFloatRange = true,
                    minFloatValue = -UIR.FilterHelper.k_MaxPostProcessingMargin,
                    maxFloatValue = UIR.FilterHelper.k_MaxPostProcessingMargin
                },
                new FilterParameterDeclaration {
                    interpolationDefaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    defaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    name = "Y",
                    hasFloatRange = true,
                    minFloatValue = -UIR.FilterHelper.k_MaxPostProcessingMargin,
                    maxFloatValue = UIR.FilterHelper.k_MaxPostProcessingMargin
                },
                new FilterParameterDeclaration {
                    interpolationDefaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    defaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    name = "Radius",
                    hasFloatRange = true,
                    minFloatValue = 0.0f,
                    maxFloatValue = k_MaxBlurSigma
                },
                new FilterParameterDeclaration {
                    interpolationDefaultValue = new FilterParameter { type = FilterParameterType.Color, colorValue = Color.clear },
                    defaultValue = new FilterParameter { type = FilterParameterType.Color, colorValue = Color.black },
                    name = "Color"
                },
            };

            filter.passes = new[]
            {
                // Pass 0: horizontal blur of alpha (composite reads only .a, so RGB bleed is irrelevant)
                new PostProcessingPass
                {
                    material = blurMaterial,
                    passIndex = 0,
                    readMargins = new(),
                    writeMargins = new(),
                    expandsBackdropCapture = true
                },
                // Pass 1: vertical blur
                new PostProcessingPass
                {
                    material = blurMaterial,
                    passIndex = 1,
                    readMargins = new(),
                    writeMargins = new(),
                    expandsBackdropCapture = true
                },
                // Pass 2: composite — reads V-blur output AND the texture that fed pass 0.
                new PostProcessingPass
                {
                    material = compositeMaterial,
                    passIndex = 0,
                    readMargins = new(),
                    writeMargins = new(),
                    requiredInputTextureName = "Source",
                    expandsBackdropCapture = true
                },
            };

            filter.passes[0].computeRequiredReadMarginsCallback = ComputeDropShadowHorizontalBlurMargins;
            filter.passes[0].computeRequiredWriteMarginsCallback = ComputeDropShadowHorizontalBlurMargins;
            SetInstancedParameters(ref filter.passes[0], WriteDropShadowBlurParameters);

            filter.passes[1].computeRequiredReadMarginsCallback = ComputeDropShadowVerticalBlurMargins;
            filter.passes[1].computeRequiredWriteMarginsCallback = ComputeDropShadowVerticalBlurMargins;
            SetInstancedParameters(ref filter.passes[1], WriteDropShadowBlurParameters);

            filter.passes[2].computeRequiredReadMarginsCallback = ComputeDropShadowCompositeReadMargins;
            filter.passes[2].computeRequiredWriteMarginsCallback = ComputeDropShadowCompositeWriteMargins;
            SetInstancedParameters(ref filter.passes[2], WriteDropShadowCompositeParameters);

            return filter;
        }

        static PostProcessingMargins ComputeHorizontalBlurMargins(FilterFunction func)
        {
            float sigma = Math.Max(0.0f, func.parameters[0].floatValue);
            int kernelSize = Mathf.CeilToInt(sigma * 3.0f + 1.0f); // This is the kernel-size as defined in shader
            return new PostProcessingMargins() { left = kernelSize, top = 0, right = kernelSize, bottom = 0 };
        }

        static PostProcessingMargins ComputeVerticalBlurMargins(FilterFunction func)
        {
            float sigma = Math.Max(1.0f, func.parameters[0].floatValue);
            int kernelSize = Mathf.CeilToInt(sigma * 3.0f + 1.0f); // This is the kernel-size as defined in shader
            return new PostProcessingMargins() { left = 0, top = kernelSize, right = 0, bottom = kernelSize };
        }

        static readonly int k_SigmaId = Shader.PropertyToID("_Sigma");
        static readonly int k_ShadowOffsetId = Shader.PropertyToID("_ShadowOffset");
        static readonly int k_ShadowColorId = Shader.PropertyToID("_ShadowColor");
        static readonly int k_ColorMatrixId = Shader.PropertyToID("_ColorMatrix");
        static readonly int k_ColorOffsetInvertId = Shader.PropertyToID("_ColorOffsetInvert");

        static void SetInstancedParameters(ref PostProcessingPass pass, PostProcessingPass.WriteParametersDelegate callback)
        {
            pass.material.enableInstancing = true;
            pass.writeParametersCallback = callback;
        }

        static float ComputeBlurSigma(FilterPassContext context, int sigmaParameterIndex)
        {
            float sigma = Math.Max(0.0f, context.filterFunction.parameters[sigmaParameterIndex].floatValue);
            // Pass 0 blurs horizontally, pass 1 vertically: each takes its own points-to-pixels axis,
            // so a non-uniform content scale yields the matching elliptical Gaussian.
            var ppp = context.perAxisScaledPixelsPerPoint;
            return sigma * (context.filterPassIndex == 0 ? ppp.x : ppp.y);
        }

        static void WriteBlurParameters(FilterParameterWriter writer, FilterPassContext context)
        {
            writer.SetFloat(k_SigmaId, ComputeBlurSigma(context, 0));
        }

        static PostProcessingMargins ComputeDropShadowHorizontalBlurMargins(FilterFunction func)
        {
            float sigma = Math.Max(0.0f, func.parameters[2].floatValue);
            int kernelSize = Mathf.CeilToInt(sigma * 3.0f + 1.0f);
            return new PostProcessingMargins() { left = kernelSize, top = 0, right = kernelSize, bottom = 0 };
        }

        static PostProcessingMargins ComputeDropShadowVerticalBlurMargins(FilterFunction func)
        {
            float sigma = Math.Max(0.0f, func.parameters[2].floatValue);
            int kernelSize = Mathf.CeilToInt(sigma * 3.0f + 1.0f);
            return new PostProcessingMargins() { left = 0, top = kernelSize, right = 0, bottom = kernelSize };
        }

        static void WriteDropShadowBlurParameters(FilterParameterWriter writer, FilterPassContext context)
        {
            writer.SetFloat(k_SigmaId, ComputeBlurSigma(context, 2));
        }

        static PostProcessingMargins ComputeDropShadowCompositeReadMargins(FilterFunction func)
        {
            float ox = func.parameters[0].floatValue;
            float oy = func.parameters[1].floatValue;

            // Read margins inflate the V-blur child by |offset| per side, so the composite can
            // safely sample _MainTex at (p - offset) anywhere within its own output extent.
            return new PostProcessingMargins {
                left   = Mathf.Max(0.0f, Mathf.Ceil(ox)),
                top    = Mathf.Max(0.0f, Mathf.Ceil(oy)),
                right  = Mathf.Max(0.0f, Mathf.Ceil(-ox)),
                bottom = Mathf.Max(0.0f, Mathf.Ceil(-oy)),
            };
        }

        static PostProcessingMargins ComputeDropShadowCompositeWriteMargins(FilterFunction func)
        {
            float ox = func.parameters[0].floatValue;
            float oy = func.parameters[1].floatValue;

            // Write margins extend the visible composite output to fit the offset shadow extent
            return new PostProcessingMargins {
                left   = Mathf.Max(0.0f, Mathf.Ceil(-ox)),
                top    = Mathf.Max(0.0f, Mathf.Ceil(-oy)),
                right  = Mathf.Max(0.0f, Mathf.Ceil(ox)),
                bottom = Mathf.Max(0.0f, Mathf.Ceil(oy)),
            };
        }

        static void WriteDropShadowCompositeParameters(FilterParameterWriter writer, FilterPassContext context)
        {
            ComputeDropShadowComposite(context, out Vector4 offset, out Color color);
            writer.SetVector(k_ShadowOffsetId, offset);
            writer.SetColor(k_ShadowColorId, color);
        }

        static void ComputeDropShadowComposite(FilterPassContext context, out Vector4 offset, out Color color)
        {
            var func = context.filterFunction;
            float ox = func.parameters[0].floatValue;
            float oy = func.parameters[1].floatValue;
            color = func.parameters[3].colorValue;

            // Offset is in points; the shader expects physical pixels (it converts to UV with
            // the source texel size). Per-axis: a translation routed through the scalar approximation
            // would visibly misplace the shadow under a non-uniform content scale.
            var scale = context.perAxisScaledPixelsPerPoint;
            offset = new Vector4(ox * scale.x, oy * scale.y, 0, 0);

            if (!context.readsGamma)
                color = color.linear;
        }

        static void WriteColorEffectParameters(FilterParameterWriter writer, FilterPassContext context)
        {
            ComputeColorEffect(context, out writer.GetOrAddMatrixRef(k_ColorMatrixId), out float colorOffset, out float colorInvert);
            writer.SetVector(k_ColorOffsetInvertId, new Vector4(colorOffset, colorInvert, 0, 0));
        }

        static void ComputeColorEffect(FilterPassContext context, out Matrix4x4 colorMatrix, out float colorOffset, out float colorInvert)
        {
            colorMatrix = Matrix4x4.identity;
            colorOffset = 0.0f;
            colorInvert = 0.0f;

            FilterFunction func = context.filterFunction;

            switch (func.type)
            {
                case FilterFunctionType.Tint:
                    Color tint = func.parameters[0].colorValue;
                    if (!context.readsGamma)
                        tint = tint.linear;

                    tint.a = Mathf.Clamp01(tint.a);
                    tint.r = Mathf.Clamp01(tint.r * tint.a);
                    tint.g = Mathf.Clamp01(tint.g * tint.a);
                    tint.b = Mathf.Clamp01(tint.b * tint.a);
                    colorMatrix = new Matrix4x4(
                        new Vector4(tint.r, 0, 0, 0),
                        new Vector4(0, tint.g, 0, 0),
                        new Vector4(0, 0, tint.b, 0),
                        new Vector4(0, 0, 0, tint.a));
                    break;
                case FilterFunctionType.Opacity:
                    float opacity = Mathf.Clamp01(func.parameters[0].floatValue);
                    colorMatrix = new Matrix4x4(
                        new Vector4(opacity, 0, 0, 0),
                        new Vector4(0, opacity, 0, 0),
                        new Vector4(0, 0, opacity, 0),
                        new Vector4(0, 0, 0, opacity));
                    break;
                case FilterFunctionType.Invert:
                    colorInvert = Mathf.Clamp01(func.parameters[0].floatValue);
                    break;
                case FilterFunctionType.Grayscale:
                    float grayscale = Mathf.Clamp01(func.parameters[0].floatValue);
                    colorMatrix = new Matrix4x4(
                        new Vector4(0.2126f + 0.7874f * (1 - grayscale), 0.2126f - 0.2126f * (1 - grayscale), 0.2126f - 0.2126f * (1 - grayscale), 0),
                        new Vector4(0.7152f - 0.7152f * (1 - grayscale), 0.7152f + 0.2848f * (1 - grayscale), 0.7152f - 0.7152f * (1 - grayscale), 0),
                        new Vector4(0.0722f - 0.0722f * (1 - grayscale), 0.0722f - 0.0722f * (1 - grayscale), 0.0722f + 0.9278f * (1 - grayscale), 0),
                        new Vector4(0, 0, 0, 1));
                    break;
                case FilterFunctionType.Sepia:
                    float sepia = Mathf.Clamp01(func.parameters[0].floatValue);
                    colorMatrix = new Matrix4x4(
                        new Vector4(0.393f + 0.607f * (1 - sepia), 0.349f - 0.349f * (1 - sepia), 0.272f - 0.272f * (1 - sepia), 0),
                        new Vector4(0.769f - 0.769f * (1 - sepia), 0.686f + 0.314f * (1 - sepia), 0.534f - 0.534f * (1 - sepia), 0),
                        new Vector4(0.189f - 0.189f * (1 - sepia), 0.168f - 0.168f * (1 - sepia), 0.131f + 0.869f * (1 - sepia), 0),
                        new Vector4(0, 0, 0, 1));
                    break;
                case FilterFunctionType.Contrast:
                    float contrast = Mathf.Max(0.0f, func.parameters[0].floatValue);
                    colorOffset = (1.0f - contrast) * 0.5f;
                    colorMatrix = new Matrix4x4(
                        new Vector4(contrast, 0, 0, 0),
                        new Vector4(0, contrast, 0, 0),
                        new Vector4(0, 0, contrast, 0),
                        new Vector4(0, 0, 0, 1));
                    break;
                case FilterFunctionType.HueRotate:
                    float angle = func.parameters[0].floatValue;
                    float cosA = Mathf.Cos(angle);
                    float sinA = Mathf.Sin(angle);

                    float lumR = 0.213f;
                    float lumG = 0.715f;
                    float lumB = 0.072f;

                    colorMatrix = new Matrix4x4(
                        new Vector4(
                            lumR + cosA * (1 - lumR) + sinA * (-lumR),
                            lumR + cosA * (-lumR) + sinA * 0.143f,
                            lumR + cosA * (-lumR) + sinA * (-(1 - lumR)),
                            0
                        ),
                        new Vector4(
                            lumG + cosA * (-lumG) + sinA * (-lumG),
                            lumG + cosA * (1 - lumG) + sinA * 0.140f,
                            lumG + cosA * (-lumG) + sinA * lumG,
                            0
                        ),
                        new Vector4(
                            lumB + cosA * (-lumB) + sinA * (1 - lumB),
                            lumB + cosA * (-lumB) + sinA * -0.283f,
                            lumB + cosA * (1 - lumB) + sinA * lumB,
                            0
                        ),
                        new Vector4(0, 0, 0, 1));
                    break;
            }
        }
    }
}
