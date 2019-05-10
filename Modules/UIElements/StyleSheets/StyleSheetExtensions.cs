// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    static class StyleSheetExtensions
    {
        public static string ReadAsString(this StyleSheet sheet, StyleValueHandle handle)
        {
            string value = string.Empty;

            switch (handle.valueType)
            {
                case StyleValueType.Float:
                    value = sheet.ReadFloat(handle).ToString(CultureInfo.InvariantCulture.NumberFormat);
                    break;
                case StyleValueType.Dimension:
                    value = sheet.ReadDimension(handle).ToString();
                    break;
                case StyleValueType.Color:
                    value = sheet.ReadColor(handle).ToString();
                    break;
                case StyleValueType.ResourcePath:
                    value = sheet.ReadResourcePath(handle);
                    break;
                case StyleValueType.String:
                    value = sheet.ReadString(handle);
                    break;
                case StyleValueType.Enum:
                    value = sheet.ReadEnum(handle);
                    break;
                case StyleValueType.Keyword:
                    value = sheet.ReadKeyword(handle).ToString();
                    break;
                case StyleValueType.AssetReference:
                    value = sheet.ReadAssetReference(handle).ToString();
                    break;
                case StyleValueType.Function:
                    value = sheet.ReadFunctionName(handle);
                    break;
                case StyleValueType.FunctionSeparator:
                    value = "Function Separator";
                    break;
                default:
                    value = "Error reading value type (" + handle.valueType + ") at index " + handle.valueIndex;
                    break;
            }

            return value;
        }

        public static StyleFloat ReadStyleFloat(this StyleSheet sheet, StyleValueHandle handle, int specificity)
        {
            return new StyleFloat(sheet.ReadFloat(handle)) {specificity = specificity};
        }

        public static StyleInt ReadStyleInt(this StyleSheet sheet, StyleValueHandle handle, int specificity)
        {
            return new StyleInt((int)sheet.ReadFloat(handle)) {specificity = specificity};
        }

        public static StyleInt ReadStyleEnum<T>(this StyleSheet sheet, StyleValueHandle handle, int specificity)
        {
            return new StyleInt(StyleSheetCache.GetEnumValue<T>(sheet, handle)) {specificity = specificity};
        }

        public static StyleColor ReadStyleColor(this StyleSheet sheet, StyleValueHandle handle, int specificity)
        {
            Color c = Color.clear;
            if (handle.valueType == StyleValueType.Enum)
            {
                var colorName = sheet.ReadAsString(handle);
                StyleSheetColor.TryGetColor(colorName.ToLower(), out c);
            }
            else
            {
                c = sheet.ReadColor(handle);
            }
            return new StyleColor(c) {specificity = specificity};
        }

        public static StyleLength ReadStyleLength(this StyleSheet sheet, StyleValueHandle handle, int specificity)
        {
            var keyword = TryReadKeyword(handle);

            StyleLength styleLength = new StyleLength(keyword) {specificity = specificity};
            if (keyword == StyleKeyword.Undefined)
            {
                var dimension = sheet.ReadDimension(handle);
                styleLength.value = dimension.ToLength();
            }

            return styleLength;
        }

        private static StyleKeyword TryReadKeyword(StyleValueHandle handle)
        {
            if (handle.valueType == StyleValueType.Keyword)
            {
                var keyword = (StyleValueKeyword)handle.valueIndex;
                switch (keyword)
                {
                    case StyleValueKeyword.Auto:
                        return StyleKeyword.Auto;
                    case StyleValueKeyword.None:
                        return StyleKeyword.None;
                    case StyleValueKeyword.Initial:
                        return StyleKeyword.Initial;
                }
            }

            return StyleKeyword.Undefined;
        }
    }
}
