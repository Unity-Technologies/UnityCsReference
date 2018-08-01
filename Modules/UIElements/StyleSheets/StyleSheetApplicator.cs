// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    struct FloatOrKeyword
    {
        public FloatOrKeyword(StyleValueKeyword kw)
        {
            isKeyword = true;
            keyword = kw;
            floatValue = 0;
        }

        public FloatOrKeyword(float v)
        {
            isKeyword = false;
            keyword = 0;
            floatValue = v;
        }

        public bool isKeyword { get; private set; }
        public StyleValueKeyword keyword { get; private set; }
        public float floatValue { get; private set; }
    }

    internal static class StyleSheetApplicator
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);
        internal static GetCursorIdFunction getCursorIdFunc = null;

        static void Apply<T>(T val, int specificity, ref StyleValue<T> property)
        {
            property.Apply(new StyleValue<T>(val, specificity), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public static void ApplyValue<T>(int specificity, ref StyleValue<T> property, T value = default(T))
        {
            Apply(value, specificity, ref property);
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

        public static void ApplyFloatOrKeyword(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<FloatOrKeyword> property)
        {
            var handle = handles[0];
            FloatOrKeyword fk;
            if (handle.valueType == StyleValueType.Keyword)
            {
                fk = new FloatOrKeyword((StyleValueKeyword)handle.valueIndex);
            }
            else
            {
                fk = new FloatOrKeyword(sheet.ReadFloat(handle));
            }
            Apply(fk, specificity, ref property);
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

        public static void CompileCursor(StyleSheet sheet, StyleValueHandle[] handles, out float hotspotX, out float hotspotY, out int cursorId, out Texture2D texture)
        {
            var handle = handles[0];
            int index = 0;
            bool isCustom = handle.valueType == StyleValueType.ResourcePath ||
                handle.valueType == StyleValueType.AssetReference;

            cursorId = 0;
            texture = null;
            hotspotX = 0f;
            hotspotY = 0f;

            if (isCustom)
            {
                if (TryGetSourceFromHandle(sheet, handles[index++], out texture))
                {
                    if (index < handles.Length && handles[index].valueType == StyleValueType.Float && sheet.TryReadFloat(handles, index++, out hotspotX))
                    {
                        if (!sheet.TryReadFloat(handles, index++, out hotspotY))
                        {
                        }
                    }
                }

                if (index < handles.Length)
                {
                    if (getCursorIdFunc != null)
                    {
                        cursorId = getCursorIdFunc(sheet, handles[index]);
                    }
                }
                else
                {
                }
            }
            else
            {
                // Default cursor
                if (getCursorIdFunc != null)
                {
                    cursorId = getCursorIdFunc(sheet, handle);
                }
            }
        }

        public static void ApplyCursor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<CursorStyle> property)
        {
            float hotspotX;
            float hotspotY;
            int cursorId;
            Texture2D texture;

            CompileCursor(sheet, handles, out hotspotX, out hotspotY, out cursorId, out texture);
            CursorStyle cursor = new CursorStyle() { texture = texture, hotspot = new Vector2(hotspotX, hotspotY), defaultCursorId = cursorId };
            Apply(cursor, specificity, ref property);
        }

        public static void ApplyFont(StyleSheet sheet, StyleValueHandle[] handles, int specificity,
            ref StyleValue<Font> property)
        {
            StyleValueHandle handle = handles[0];
            Font font = null;

            switch (handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = sheet.ReadResourcePath(handle);

                    if (!string.IsNullOrEmpty(path))
                    {
                        font = Panel.loadResourceFunc(path, typeof(Font)) as Font;
                    }

                    if (font == null)
                    {
                        Debug.LogWarning(string.Format("Font not found for path: {0}", path));
                    }
                }
                break;

                case StyleValueType.AssetReference:
                {
                    font = sheet.ReadAssetReference(handle) as Font;

                    if (font == null)
                    {
                        Debug.LogWarning("Invalid font reference");
                    }
                }
                break;

                default:
                    Debug.LogWarning("Invalid value for font " + handle.valueType);
                    break;
            }

            if (font != null)
            {
                Apply(font, specificity, ref property);
            }
        }

        public static void ApplyImage(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleValue<Texture2D> property)
        {
            Texture2D source = null;

            StyleValueHandle handle = handles[0];
            if (handle.valueType == StyleValueType.Keyword)
            {
                if (handle.valueIndex != (int)StyleValueKeyword.None)
                {
                    Debug.LogWarning("Invalid keyword for image source " + (StyleValueKeyword)handle.valueIndex);
                }
                else
                {
                    // it's OK, we let none be assigned to the source
                }
            }
            else if (TryGetSourceFromHandle(sheet, handle, out source) == false)
            {
                // Load a stand-in picture to make it easier to identify which image element is missing its picture
                source = Panel.loadResourceFunc("d_console.warnicon", typeof(Texture2D)) as Texture2D;
            }
            Apply(source, specificity, ref property);
        }

        static bool TryGetSourceFromHandle(StyleSheet sheet, StyleValueHandle handle, out Texture2D source)
        {
            source = null;

            switch (handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = sheet.ReadResourcePath(handle);

                    if (!string.IsNullOrEmpty(path))
                    {
                        source = Panel.loadResourceFunc(path, typeof(Texture2D)) as Texture2D;
                    }

                    if (source == null)
                    {
                        Debug.LogWarning(string.Format("Texture not found for path: {0}", path));
                        return false;
                    }
                }
                break;

                case StyleValueType.AssetReference:
                {
                    source = sheet.ReadAssetReference(handle) as Texture2D;

                    if (source  == null)
                    {
                        Debug.LogWarning("Invalid texture specified");

                        return false;
                    }
                }
                break;

                default:
                    Debug.LogWarning("Invalid value for image source " + handle.valueType);
                    return false;
            }

            return true;
        }

        public static bool CompileFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, out float grow, out float shrink, out FloatOrKeyword basis)
        {
            grow = 0f;
            shrink = 0f;
            basis = new FloatOrKeyword(StyleValueKeyword.Auto);
            bool valid = false;

            if (handles.Length == 1 && handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Unset)
            {
                valid = true;
                grow = 0f;
                shrink = 1f;
                basis = new FloatOrKeyword(StyleValueKeyword.Auto);
            }
            else if (handles.Length == 1 && handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.None)
            {
                valid = true;
                grow = 0f;
                shrink = 0f;
                basis = new FloatOrKeyword(StyleValueKeyword.Auto);
            }
            else if (handles.Length <= 3 && handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Auto)
            {
                valid = true;
                basis = new FloatOrKeyword(StyleValueKeyword.Auto);
                grow = 1f;
                shrink = 1f;

                if (handles.Length > 1)
                {
                    grow = sheet.ReadFloat(handles[1]);
                    if (handles.Length > 2)
                    {
                        shrink = sheet.ReadFloat(handles[2]);
                    }
                }
            }
            else if (handles.Length <= 3 && handles[0].valueType == StyleValueType.Float)
            {
                valid = true;

                // TODO: when support for units is implemented, basis must have units, grow and shrink are unitless.
                // This will remove ambiguities. For now we assume (when all values are number)
                //
                // flex: grow               (could be flex: basis)
                // flex: grow shrink        (could be flex: basis grow; or flex: grow basis)
                // flex: grow shrink basis  (could be flex: basis grow shrink)

                grow = sheet.ReadFloat(handles[0]);
                shrink = 1f;
                basis = new FloatOrKeyword(0f);

                if (handles.Length > 1)
                {
                    if (handles[1].valueType == StyleValueType.Float)
                    {
                        shrink = sheet.ReadFloat(handles[1]);

                        if (handles.Length > 2)
                        {
                            if (handles[2].valueType == StyleValueType.Keyword && handles[2].valueIndex == (int)StyleValueKeyword.Auto)
                            {
                                basis = new FloatOrKeyword(StyleValueKeyword.Auto);
                            }
                            else if (handles[2].valueType == StyleValueType.Float)
                            {
                                basis = new FloatOrKeyword(sheet.ReadFloat(handles[2]));
                            }
                        }
                    }
                    else if (handles[1].valueType == StyleValueType.Keyword && handles[1].valueIndex == (int)StyleValueKeyword.Auto)
                    {
                        basis = new FloatOrKeyword(StyleValueKeyword.Auto);
                    }
                }
            }

            return valid;
        }

        public static void ApplyFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            float grow;
            float shrink;
            FloatOrKeyword basis;
            bool valid = CompileFlexShorthand(sheet, handles, out grow, out shrink, out basis);

            if (valid)
            {
                ApplyValue(specificity, ref styleData.flexGrow, grow);
                ApplyValue(specificity, ref styleData.flexShrink, shrink);
                ApplyValue(specificity, ref styleData.flexBasis, basis);
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
