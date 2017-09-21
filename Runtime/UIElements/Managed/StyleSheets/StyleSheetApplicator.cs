// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal static class StyleSheetApplicator
    {
        static void Apply<T>(T val, int specificity, ref StyleValue<T> property)
        {
            property.Apply(new StyleValue<T>(val, specificity), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public static void ApplyDefault<T>(int specificity, ref StyleValue<T> property)
        {
            Apply(default(T), specificity, ref property);
        }

        public static void ApplyBool(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<bool> property)
        {
            bool value = sheet.ReadKeyword(handles[0]) == StyleValueKeyword.True;
            Apply(value, specificity, ref property);
        }

        public static void ApplyFloat(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<float> property)
        {
            var value = sheet.ReadFloat(handles[0]);
            Apply(value, specificity, ref property);
        }

        public static void ApplyInt(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<int> property)
        {
            var value = (int)sheet.ReadFloat(handles[0]);
            Apply(value, specificity, ref property);
        }

        public static void ApplyEnum<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<int> property)
        {
            var value = StyleSheetCache.GetEnumValue<T>(sheet, handles[0]);
            Apply(value, specificity, ref property);
        }

        public static void ApplyColor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<Color> property)
        {
            var value = sheet.ReadColor(handles[0]);
            Apply(value, specificity, ref property);
        }

        public static void ApplyResource<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<T> property) where T : Object
        {
            var handle = handles[0];
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.None)
            {
                Apply(null, specificity, ref property);
                return;
            }

            T resource = null;
            string path = sheet.ReadResourcePath(handle);

            if (!string.IsNullOrEmpty(path))
            {
                resource = Panel.loadResourceFunc(path, typeof(T)) as T;

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
                        resource = Panel.loadResourceFunc("d_console.warnicon", typeof(T)) as T;
                        Apply(resource, specificity, ref property);
                    }

                    Debug.LogWarning(string.Format("{0} resource/file not found for path: {1}", typeof(T).Name, path));
                }
            }
        }

        public static class Shorthand
        {
            private static void ReadFourSidesArea(StyleSheet sheet, StyleValueHandle[] handles, out float top, out float right, out float bottom, out float left)
            {
                top = 0;
                right = 0;
                bottom = 0;
                left = 0;
                switch (handles.Length)
                {
                    // apply to all four sides
                    case 0:
                        break;
                    case 1:
                    {
                        top = right = bottom = left = sheet.ReadFloat(handles[0]);
                        break;
                    }
                    // vertical | horizontal
                    case 2:
                    {
                        top = bottom = sheet.ReadFloat(handles[0]);
                        left = right = sheet.ReadFloat(handles[1]);
                        break;
                    }
                    // top | horizontal | bottom
                    case 3:
                    {
                        top = sheet.ReadFloat(handles[0]);
                        left = right = sheet.ReadFloat(handles[1]);
                        bottom = sheet.ReadFloat(handles[2]);
                        break;
                    }
                    // top | right | bottom | left
                    default:
                    {
                        top = sheet.ReadFloat(handles[0]);
                        right = sheet.ReadFloat(handles[1]);
                        bottom = sheet.ReadFloat(handles[2]);
                        left = sheet.ReadFloat(handles[3]);
                        break;
                    }
                }
            }

            public static void ApplyBorderRadius(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
            {
                float topLeft;
                float topRight;
                float bottomLeft;
                float bottomRight;
                ReadFourSidesArea(sheet, handles, out topLeft, out topRight, out bottomRight, out bottomLeft);

                Apply(topLeft, specificity, ref styleData.borderTopLeftRadius);
                Apply(topRight, specificity, ref styleData.borderTopRightRadius);
                Apply(bottomLeft, specificity, ref styleData.borderBottomLeftRadius);
                Apply(bottomRight, specificity, ref styleData.borderBottomRightRadius);
            }

            public static void ApplyMargin(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
            {
                float top;
                float right;
                float bottom;
                float left;
                ReadFourSidesArea(sheet, handles, out top, out right, out bottom, out left);

                Apply(top, specificity, ref styleData.marginTop);
                Apply(right, specificity, ref styleData.marginRight);
                Apply(bottom, specificity, ref styleData.marginBottom);
                Apply(left, specificity, ref styleData.marginLeft);
            }

            public static void ApplyPadding(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
            {
                float top;
                float right;
                float bottom;
                float left;
                ReadFourSidesArea(sheet, handles, out top, out right, out bottom, out left);

                Apply(top, specificity, ref styleData.paddingTop);
                Apply(right, specificity, ref styleData.paddingRight);
                Apply(bottom, specificity, ref styleData.paddingBottom);
                Apply(left, specificity, ref styleData.paddingLeft);
            }
        }
    }
}
