// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEditor.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal interface IValueToUxmlConverter<T>
    {
        string Convert(T value);
    }

    internal interface IContravariantValueToUxmlConverter<in T>
    {
        string Convert(T value);
    }

    internal class ValueToUxmlConverter : IValueToUxmlConverter<bool>
        , IValueToUxmlConverter<float>
        , IValueToUxmlConverter<double>
        , IValueToUxmlConverter<Color>
        , IValueToUxmlConverter<string>
        , IContravariantValueToUxmlConverter<Object>
    {
        public string Convert<T>(T value)
        {
            switch (this)
            {
                case IValueToUxmlConverter<T> converter: return converter.Convert(value);
                case IContravariantValueToUxmlConverter<T> converter: return converter.Convert(value);
                default: return value.ToString();
            }
        }

        public string Convert(bool value)
        {
            return value.ToString().ToLower();
        }

        public string Convert(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        public string Convert(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }

        public string Convert(Color value)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(value);
        }

        public string Convert(string value)
        {
            return value;
        }

        public string Convert(Object value)
        {
            return value ? URIHelpers.MakeAssetUri(value) : null;
        }
    }

    /// <summary>
    /// This helper class converts a value into its uxml string representation.
    /// </summary>
    internal static class ValueToUxml
    {
        static readonly ValueToUxmlConverter s_Converter = new();
        public static string Convert<T>(T value)
        {
            return s_Converter.Convert(value);
        }
    }
}
