// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
        FilterBlur
    }

    internal static class StyleValueFunctionExtension
    {
        public const string k_Var = "var";
        public const string k_Env = "env";
        public const string k_LinearGradient = "linear-gradient";
        public const string k_NoneFilter = "none";
        public const string k_CustomFilter = "filter";
        public const string k_FilterTint = "tint";
        public const string k_FilterOpacity = "opacity";
        public const string k_FilterInvert = "invert";
        public const string k_FilterGrayscale = "grayscale";
        public const string k_FilterSepia = "sepia";
        public const string k_FilterBlur = "blur";

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
                default:
                    throw new ArgumentOutOfRangeException(nameof(svf), svf, $"Unknown {nameof(StyleValueFunction)}");
            }
        }
    }
}
