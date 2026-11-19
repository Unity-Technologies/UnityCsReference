// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal enum StyleValueFunction
    {
        Unknown,
        Var,
        Env,
        LinearGradient,
        NoneFilter,
        CustomFilter,
        FilterTint,
        FilterOpacity,
        FilterInvert,
        FilterGrayscale,
        FilterSepia,
        FilterBlur,
        FilterContrast,
        FilterHueRotate,
        FilterDropShadow,
        MaterialProperty,
        RadialGradient,
        // CSS Grid track functions.
        Minmax,
        Repeat,
        FitContent
    }

    internal static class StyleValueFunctionExtension
    {
        public const string k_Var = "var";
        public const string k_Env = "env";
        public const string k_LinearGradient = "linear-gradient";
        public const string k_RadialGradient = "radial-gradient";
        public const string k_NoneFilter = "none";
        public const string k_CustomFilter = "filter";
        public const string k_FilterTint = "tint";
        public const string k_FilterOpacity = "opacity";
        public const string k_FilterInvert = "invert";
        public const string k_FilterGrayscale = "grayscale";
        public const string k_FilterSepia = "sepia";
        public const string k_FilterBlur = "blur";
        public const string k_FilterContrast = "contrast";
        public const string k_FilterHueRotate = "hue-rotate";
        public const string k_FilterDropShadow = "drop-shadow";
        public const string k_MaterialProperty = "prop";
        public const string k_Minmax = "minmax";
        public const string k_Repeat = "repeat";
        public const string k_FitContent = "fit-content";

        public static StyleValueFunction FromUssString(string ussValue)
        {
#pragma warning disable CA1308
            ussValue = ussValue.ToLowerInvariant();
#pragma warning restore CA1308
            switch (ussValue)
            {
                case k_Var:
                    return StyleValueFunction.Var;
                case k_Env:
                    return StyleValueFunction.Env;
                case k_LinearGradient:
                    return StyleValueFunction.LinearGradient;
                case k_RadialGradient:
                    return StyleValueFunction.RadialGradient;
                case k_NoneFilter:
                    return StyleValueFunction.NoneFilter;
                case k_FilterTint:
                    return StyleValueFunction.FilterTint;
                case k_FilterOpacity:
                    return StyleValueFunction.FilterOpacity;
                case k_FilterInvert:
                    return StyleValueFunction.FilterInvert;
                case k_FilterGrayscale:
                    return StyleValueFunction.FilterGrayscale;
                case k_FilterSepia:
                    return StyleValueFunction.FilterSepia;
                case k_FilterBlur:
                    return StyleValueFunction.FilterBlur;
                case k_FilterContrast:
                    return StyleValueFunction.FilterContrast;
                case k_FilterHueRotate:
                    return StyleValueFunction.FilterHueRotate;
                case k_FilterDropShadow:
                    return StyleValueFunction.FilterDropShadow;
                case k_MaterialProperty:
                    return StyleValueFunction.MaterialProperty;
                case k_Minmax:
                    return StyleValueFunction.Minmax;
                case k_Repeat:
                    return StyleValueFunction.Repeat;
                case k_FitContent:
                    return StyleValueFunction.FitContent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ussValue), ussValue, "Unknown function name");
            }
        }

        public static string ToUssString(this StyleValueFunction svf)
        {
            switch (svf)
            {
                case StyleValueFunction.Var:
                    return k_Var;
                case StyleValueFunction.Env:
                    return k_Env;
                case StyleValueFunction.LinearGradient:
                    return k_LinearGradient;
                case StyleValueFunction.RadialGradient:
                    return k_RadialGradient;
                case StyleValueFunction.NoneFilter:
                    return k_NoneFilter;
                case StyleValueFunction.CustomFilter:
                    return k_CustomFilter;
                case StyleValueFunction.FilterTint:
                    return k_FilterTint;
                case StyleValueFunction.FilterOpacity:
                    return k_FilterOpacity;
                case StyleValueFunction.FilterInvert:
                    return k_FilterInvert;
                case StyleValueFunction.FilterGrayscale:
                    return k_FilterGrayscale;
                case StyleValueFunction.FilterSepia:
                    return k_FilterSepia;
                case StyleValueFunction.FilterBlur:
                    return k_FilterBlur;
                case StyleValueFunction.FilterContrast:
                    return k_FilterContrast;
                case StyleValueFunction.FilterHueRotate:
                    return k_FilterHueRotate;
                case StyleValueFunction.FilterDropShadow:
                    return k_FilterDropShadow;
                case StyleValueFunction.MaterialProperty:
                    return k_MaterialProperty;
                case StyleValueFunction.Minmax:
                    return k_Minmax;
                case StyleValueFunction.Repeat:
                    return k_Repeat;
                case StyleValueFunction.FitContent:
                    return k_FitContent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(svf), svf, $"Unknown {nameof(StyleValueFunction)}");
            }
        }
    }
}
