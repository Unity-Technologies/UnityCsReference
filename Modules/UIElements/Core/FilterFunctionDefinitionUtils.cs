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

        public static string GetBuiltinFilterName(FilterFunctionType type)
        {
            switch (type)
            {
                case FilterFunctionType.Blur:
                    return "blur";
                case FilterFunctionType.Tint:
                    return "tint";
                case FilterFunctionType.Opacity:
                    return "opacity";
                case FilterFunctionType.Invert:
                    return "invert";
                case FilterFunctionType.Grayscale:
                    return "grayscale";
                case FilterFunctionType.Sepia:
                    return "sepia";
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
                    defaultVal = new FilterParameter { type = FilterParameterType.Float, floatValue = 1.0f };
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

            filter.passes[0].prepareMaterialPropertyBlockCallback = PrepareBuiltinColorEffectMaterialPropertyBlock;

            return filter;
        }

        static PostProcessingMargins ComputeHorizontalBlurMargins(FilterFunction func)
        {
            float sigma = Math.Max(0.0f, func.parameters[0].floatValue);
            float radius = sigma * 3.0f; // This is the kernel-size as defined in shader
            return new PostProcessingMargins() { left = radius, top = 0, right = radius, bottom = 0 };
        }

        static PostProcessingMargins ComputeVerticalBlurMargins(FilterFunction func)
        {
            float sigma = Math.Max(0.0f, func.parameters[0].floatValue);
            float radius = sigma * 3.0f; // This is the kernel-size as defined in shader
            return new PostProcessingMargins() { left = 0, top = radius, right = 0, bottom = radius };
        }

        static void PrepareBuiltinColorEffectMaterialPropertyBlock(MaterialPropertyBlock mpb, FilterFunction func)
        {
            var colorMatrix = Matrix4x4.identity;
            var colorTint = Color.white;
            float colorInvert = 0.0f;

            switch (func.type)
            {
                case FilterFunctionType.Tint:
                    colorTint = func.parameters[0].colorValue;
                    break;
                case FilterFunctionType.Opacity:
                    colorTint.a = Mathf.Clamp01(func.parameters[0].floatValue);
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
            }

            mpb.SetMatrix("_ColorMatrix", colorMatrix);
            mpb.SetColor("_ColorTint", colorTint);
            mpb.SetFloat("_ColorInvert", colorInvert);
        }
    }
}
