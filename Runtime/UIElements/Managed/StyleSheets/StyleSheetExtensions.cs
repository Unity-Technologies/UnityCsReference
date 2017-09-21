// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal delegate void HandlesApplicatorFunction<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<T> property);
    internal delegate void ShorthandApplicatorFunction(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData);

    static class StyleSheetExtensions
    {
        public static void Apply<T>(this StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<T> property, HandlesApplicatorFunction<T> applicatorFunc)
        {
            if (handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Unset)
            {
                StyleSheetApplicator.ApplyDefault(specificity, ref property);
            }
            else
            {
                applicatorFunc(sheet, handles, specificity, ref property);
            }
        }

        public static void ApplyShorthand(this StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData, ShorthandApplicatorFunction applicatorFunc)
        {
            // Do not apply anything if shorthand is equal to unset
            if (handles[0].valueType != StyleValueType.Keyword && handles[0].valueIndex != (int)StyleValueKeyword.Unset)
            {
                applicatorFunc(sheet, handles, specificity, styleData);
            }
        }

        public static string ReadAsString(this StyleSheet sheet, StyleValueHandle handle)
        {
            string value = string.Empty;
            switch (handle.valueType)
            {
                case StyleValueType.Float:
                    value = sheet.ReadFloat(handle).ToString();
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
    }
}
