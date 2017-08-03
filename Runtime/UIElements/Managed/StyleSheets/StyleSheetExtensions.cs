// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    static class StyleSheetExtensions
    {
        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref StyleValue<float> property)
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(float), specificity, ref property);
            }
            else
            {
                Apply(sheet.ReadFloat(handle), specificity, ref property);
            }
        }

        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref StyleValue<Color> property)
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(Color), specificity, ref property);
            }
            else
            {
                Apply(sheet.ReadColor(handle), specificity, ref property);
            }
        }

        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref StyleValue<int> property)
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(int), specificity, ref property);
            }
            else
            {
                Apply((int)sheet.ReadFloat(handle), specificity, ref property);
            }
        }

        public static void Apply(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref StyleValue<bool> property)
        {
            bool val = sheet.ReadKeyword(handle) == StyleValueKeyword.True;

            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(bool), specificity, ref property);
            }
            else
            {
                Apply(val, specificity, ref property);
            }
        }

        public static void Apply<T>(this StyleSheet sheet, StyleValueHandle handle, int specificity, ref StyleValue<int> property) where T : struct
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(int), specificity, ref property);
            }
            else
            {
                Apply(StyleSheetCache.GetEnumValue<T>(sheet, handle), specificity, ref property);
            }
        }

        public static void Apply<T>(this StyleSheet sheet, StyleValueHandle handle, int specificity, LoadResourceFunction loadResourceFunc, ref StyleValue<T> property) where T : Object
        {
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.None)
            {
                Apply((T)null, specificity, ref property);
                return;
            }

            string path = sheet.ReadResourcePath(handle);

            if (!string.IsNullOrEmpty(path))
            {
                T resource = loadResourceFunc(path, typeof(T)) as T;

                if (resource != null)
                {
                    Apply(resource, specificity, ref property);
                }
                else
                {
                    // Load a stand-in picture to make it easier to identify which image element
                    // is missing its picture (but only if the referenced asset is a texture).
                    if (typeof(T) == typeof(Texture2D))
                    {
                        resource = loadResourceFunc("d_console.warnicon", typeof(T)) as T;
                        Apply(resource, specificity, ref property);
                    }

                    Debug.LogWarning(string.Format("{0} resource/file not found for path: {1}", typeof(T).Name, path));
                }
            }
        }

        static void Apply<T>(T val, int specificity, ref StyleValue<T> property)
        {
            property.Apply(new StyleValue<T>(val, specificity), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
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
