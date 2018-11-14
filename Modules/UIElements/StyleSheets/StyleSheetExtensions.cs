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
                default:
                    throw new ArgumentException("Unhandled type " + handle.valueType);
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
            return new StyleColor(sheet.ReadColor(handle)) {specificity = specificity};
        }

        public static StyleLength ReadStyleLength(this StyleSheet sheet, StyleValueHandle handle, int specificity)
        {
            StyleLength styleLength = new StyleLength() {specificity = specificity};
            if (handle.valueType == StyleValueType.Float)
            {
                styleLength.value = sheet.ReadFloat(handle);
            }
            else if (handle.valueType == StyleValueType.Keyword)
            {
                var keyword = (StyleValueKeyword)handle.valueIndex;
                if (keyword == StyleValueKeyword.Auto)
                    styleLength.keyword = StyleKeyword.Auto;
            }
            else
            {
                Debug.LogError($"Unexpected Length value of type {handle.valueType.ToString()}");
            }

            return styleLength;
        }
    }
}
