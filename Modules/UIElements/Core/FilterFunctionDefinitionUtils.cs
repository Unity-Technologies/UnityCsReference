// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Rendering;

namespace UnityEngine.UIElements
{
    internal static class FilterFunctionDefinitionUtils
    {
        // These definition are initialized once at first use and are shared across all filter functions.
        private static FilterFunctionDefinition s_BlurDef;
        private static FilterFunctionDefinition s_TintDef;
        private static FilterFunctionDefinition s_OpacityDef;
        private static FilterFunctionDefinition s_InvertDef;
        private static FilterFunctionDefinition s_GrayscaleDef;
        private static FilterFunctionDefinition s_SepiaDef;
        private static FilterFunctionDefinition s_ContrastDef;
        private static FilterFunctionDefinition s_HueRotateDef;

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
            }

            return null;
        }

        static FilterFunctionDefinition CreateBlurFilterFunctionDefinition()
        {
            var blurMaterial = new Material(Shader.Find("Hidden/UIR/GaussianBlur"));
            blurMaterial.hideFlags = HideFlags.HideAndDontSave;

            var filter = ScriptableObject.CreateInstance<FilterFunctionDefinition>();
            filter.hideFlags = HideFlags.HideAndDontSave;
            filter.filterName = GetBuiltinFilterName(FilterFunctionType.Blur);

            filter.parameters = new[]
            {
                // Gaussian-blur sigma
                new FilterParameterDeclaration {
                    interpolationDefaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f },
                    defaultValue = new FilterParameter { type = FilterParameterType.Float, floatValue = 0.0f }
                }
            };

            filter.passes = new[]
            {
                new PostProcessingPass
                {
                    material = blurMaterial,
                    passIndex = 0,
                    parameterBindings = new[]
                    {
                        new ParameterBinding { index = 0, name = "_Sigma" }
                    },
                    readMargins = new(), // Margins are set dynamically in computeRequiredReadMarginsCallback
                    writeMargins = new()
                },
                new PostProcessingPass
                {
                    material = blurMaterial,
                    passIndex = 1,
                    parameterBindings = new[]
                    {
                        new ParameterBinding { index = 0, name = "_Sigma" }
                    },
                    readMargins = new(), // Margins are set dynamically in computeRequiredReadMarginsCallback
                    writeMargins = new()
                }
            };

            filter.passes[0].computeRequiredReadMarginsCallback = ComputeHorizontalBlurMargins;
            filter.passes[0].computeRequiredWriteMarginsCallback = ComputeHorizontalBlurMargins;

            filter.passes[1].computeRequiredReadMarginsCallback = ComputeVerticalBlurMargins;
            filter.passes[1].computeRequiredWriteMarginsCallback = ComputeVerticalBlurMargins;

            return filter;
        }

        static FilterFunctionDefinition CreateColorEffectFilterFunctionDefinition(FilterFunctionType filterType)
        {
            var colorEffectMaterial = new Material(Shader.Find("Hidden/UIR/ColorEffect"));
            colorEffectMaterial.hideFlags = HideFlags.HideAndDontSave;

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
                    parameterBindings = new[] { // Parameters are set dynamically in prepareMaterialPropertyBlockCallback
                        new ParameterBinding { index = 0, name = "" },
                    },
                    readMargins = new PostProcessingMargins { left = 0, top = 0, right = 0, bottom = 0 },
                    writeMargins = new PostProcessingMargins { left = 0, top = 0, right = 0, bottom = 0 }
                },
            };

            filter.passes[0].applySettingsCallback = ApplySettings;

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

        static void ApplySettings(MaterialPropertyBlock mpb, FilterPassContext context)
        {
            var colorMatrix = Matrix4x4.identity;
            float colorOffset = 0.0f;
            float colorInvert = 0.0f;

            FilterFunction func = context.filterFunction;

            switch (func.type)
            {
                case FilterFunctionType.Tint:
                    Color tint = func.parameters[0].colorValue;
                    if (!context.writesGamma)
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

            mpb.SetMatrix("_ColorMatrix", colorMatrix);
            mpb.SetFloat("_ColorOffset", colorOffset);
            mpb.SetFloat("_ColorInvert", colorInvert);
        }
    }
}
