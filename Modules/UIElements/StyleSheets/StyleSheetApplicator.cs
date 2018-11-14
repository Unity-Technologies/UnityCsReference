// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    internal interface IStyleSheetApplicator
    {
        void ApplyFloat(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFloat property);
        void ApplyFlexBasis(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property);
        void ApplyInt(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property);
        void ApplyEnum<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property);
        void ApplyLength(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property);
        void ApplyColor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleColor property);
        void ApplyImage(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleBackground property);
        void ApplyFont(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFont property);
        void ApplyCursor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleCursor property);
    }

    internal class StyleSheetApplicator : IStyleSheetApplicator
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);
        internal static GetCursorIdFunction getCursorIdFunc = null;

        private bool ApplyUnset<T, U>(StyleValueHandle[] handles, int specificity, ref T property) where T : struct, IStyleValue<U>
        {
            if (handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Unset)
            {
                var defaultValue = default(T);
                property.Apply(defaultValue, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ApplyFloat(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFloat property)
        {
            if (ApplyUnset<StyleFloat, float>(handles, specificity, ref property))
                return;

            var value = sheet.ReadStyleFloat(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyFlexBasis(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            if (ApplyUnset<StyleLength, Length>(handles, specificity, ref property))
                return;

            var value = sheet.ReadStyleLength(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyInt(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (ApplyUnset<StyleInt, int>(handles, specificity, ref property))
                return;

            var value = sheet.ReadStyleInt(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyEnum<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (ApplyUnset<StyleInt, int>(handles, specificity, ref property))
                return;

            var value = sheet.ReadStyleEnum<T>(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyLength(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            if (ApplyUnset<StyleLength, Length>(handles, specificity, ref property))
                return;

            var value = sheet.ReadStyleLength(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyColor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleColor property)
        {
            if (ApplyUnset<StyleColor, Color>(handles, specificity, ref property))
                return;

            var value = sheet.ReadStyleColor(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        private void CompileCursor(StyleSheet sheet, StyleValueHandle[] handles, out float hotspotX, out float hotspotY, out int cursorId, out Texture2D texture)
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

        public void ApplyCursor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleCursor property)
        {
            if (ApplyUnset<StyleCursor, Cursor>(handles, specificity, ref property))
                return;

            float hotspotX;
            float hotspotY;
            int cursorId;
            Texture2D texture;

            CompileCursor(sheet, handles, out hotspotX, out hotspotY, out cursorId, out texture);
            var cursor = new Cursor() { texture = texture, hotspot = new Vector2(hotspotX, hotspotY), defaultCursorId = cursorId };
            var styleCursor = new StyleCursor(cursor) {specificity = specificity};
            property.Apply(styleCursor, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyFont(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFont property)
        {
            if (ApplyUnset<StyleFont, Font>(handles, specificity, ref property))
                return;

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
                var value = new StyleFont(font) {specificity = specificity};
                property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            }
        }

        public void ApplyImage(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleBackground property)
        {
            if (ApplyUnset<StyleBackground, Background>(handles, specificity, ref property))
                return;

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
            var value = new StyleBackground(source) {specificity = specificity};
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
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
    }

    internal class StyleValueApplicator : IStyleSheetApplicator
    {
        public StyleValue currentStyleValue { get; set; }
        public StyleCursor currentCursor { get; set; }

        public void ApplyColor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleColor property)
        {
            property.Apply(new StyleColor(currentStyleValue.color, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyCursor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleCursor property)
        {
            property.Apply(new StyleCursor(currentCursor.value, currentCursor.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyEnum<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            property.Apply(new StyleInt((int)currentStyleValue.number, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyLength(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            property.Apply(new StyleLength(currentStyleValue.length, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyFloat(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFloat property)
        {
            property.Apply(new StyleFloat(currentStyleValue.number, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyFlexBasis(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            property.Apply(new StyleLength(currentStyleValue.length, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyFont(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFont property)
        {
            Font font = null;
            if (currentStyleValue.resource.IsAllocated)
                font = currentStyleValue.resource.Target as Font;

            property.Apply(new StyleFont(font, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyImage(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleBackground property)
        {
            Texture2D texture = null;
            if (currentStyleValue.resource.IsAllocated)
                texture = currentStyleValue.resource.Target as Texture2D;

            property.Apply(new StyleBackground(texture, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }

        public void ApplyInt(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            property.Apply(new StyleInt((int)currentStyleValue.number, currentStyleValue.keyword) {specificity = specificity}, StylePropertyApplyMode.Copy);
        }
    }

    internal static class ShorthandApplicator
    {
        public static void ApplyBorderRadius(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            StyleLength topLeft;
            StyleLength topRight;
            StyleLength bottomLeft;
            StyleLength bottomRight;
            CompileBoxArea(sheet, handles, specificity, out topLeft, out topRight, out bottomRight, out bottomLeft);

            // border-radius doesn't support any keyword, revert to 0 in that case
            if (topLeft.keyword != StyleKeyword.Undefined)
                topLeft.value = 0f;
            if (topRight.keyword != StyleKeyword.Undefined)
                topRight.value = 0f;
            if (bottomLeft.keyword != StyleKeyword.Undefined)
                bottomLeft.value = 0f;
            if (bottomRight.keyword != StyleKeyword.Undefined)
                bottomRight.value = 0f;

            styleData.borderTopLeftRadius.Apply(topLeft, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderTopRightRadius.Apply(topRight, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderBottomLeftRadius.Apply(bottomLeft, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderBottomRightRadius.Apply(bottomRight, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public static void ApplyBorderWidth(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            StyleLength top;
            StyleLength right;
            StyleLength bottom;
            StyleLength left;
            CompileBoxArea(sheet, handles, specificity, out top, out right, out bottom, out left);

            // border-width doesn't support any keyword, revert to 0 in that case
            if (top.keyword != StyleKeyword.Undefined)
                top.value = 0f;
            if (right.keyword != StyleKeyword.Undefined)
                right.value = 0f;
            if (bottom.keyword != StyleKeyword.Undefined)
                bottom.value = 0f;
            if (left.keyword != StyleKeyword.Undefined)
                left.value = 0f;

            styleData.borderTopWidth.Apply(top.ToStyleFloat(), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderRightWidth.Apply(right.ToStyleFloat(), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderBottomWidth.Apply(bottom.ToStyleFloat(), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderLeftWidth.Apply(left.ToStyleFloat(), StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public static void ApplyFlex(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            StyleFloat grow;
            StyleFloat shrink;
            StyleLength basis;
            bool valid = CompileFlexShorthand(sheet, handles, specificity, out grow, out shrink, out basis);

            if (valid)
            {
                styleData.flexGrow.Apply(grow, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                styleData.flexShrink.Apply(shrink, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                styleData.flexBasis.Apply(basis, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            }
        }

        public static void ApplyMargin(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            StyleLength top;
            StyleLength right;
            StyleLength bottom;
            StyleLength left;
            CompileBoxArea(sheet, handles, specificity, out top, out right, out bottom, out left);

            styleData.marginTop.Apply(top, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.marginRight.Apply(right, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.marginBottom.Apply(bottom, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.marginLeft.Apply(left, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public static void ApplyPadding(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            StyleLength top;
            StyleLength right;
            StyleLength bottom;
            StyleLength left;
            CompileBoxArea(sheet, handles, specificity, out top, out right, out bottom, out left);

            styleData.paddingTop.Apply(top, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.paddingRight.Apply(right, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.paddingBottom.Apply(bottom, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.paddingLeft.Apply(left, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        private static bool CompileFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, int specificity, out StyleFloat grow, out StyleFloat shrink, out StyleLength basis)
        {
            grow = 0f;
            shrink = 0f;
            basis = StyleKeyword.Auto;

            bool valid = false;

            if (handles.Length == 1 && handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Unset)
            {
                valid = true;
                grow = 0f;
                shrink = 1f;
                basis = StyleKeyword.Auto;
            }
            else if (handles.Length == 1 && handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.None)
            {
                valid = true;
                grow = 0f;
                shrink = 0f;
                basis = StyleKeyword.Auto;
            }
            else if (handles.Length <= 3 && handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Auto)
            {
                valid = true;
                basis = StyleKeyword.Auto;
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
                basis = 0f;

                if (handles.Length > 1)
                {
                    if (handles[1].valueType == StyleValueType.Float)
                    {
                        shrink = sheet.ReadFloat(handles[1]);

                        if (handles.Length > 2)
                        {
                            basis = sheet.ReadStyleLength(handles[2], specificity);
                        }
                    }
                    else if (handles[1].valueType == StyleValueType.Keyword && handles[1].valueIndex == (int)StyleValueKeyword.Auto)
                    {
                        basis = StyleKeyword.Auto;
                    }
                }
            }

            grow.specificity = specificity;
            shrink.specificity = specificity;
            basis.specificity = specificity;
            return valid;
        }

        private static void CompileBoxArea(StyleSheet sheet, StyleValueHandle[] handles, int specificity, out StyleLength top, out StyleLength right, out StyleLength bottom, out StyleLength left)
        {
            top = 0f;
            right = 0f;
            bottom = 0f;
            left = 0f;
            switch (handles.Length)
            {
                // apply to all four sides
                case 0:
                    break;
                case 1:
                {
                    top = right = bottom = left = sheet.ReadStyleLength(handles[0], specificity);
                    break;
                }
                // vertical | horizontal
                case 2:
                {
                    top = bottom = sheet.ReadStyleLength(handles[0], specificity);
                    left = right = sheet.ReadStyleLength(handles[1], specificity);
                    break;
                }
                // top | horizontal | bottom
                case 3:
                {
                    top = sheet.ReadStyleLength(handles[0], specificity);
                    left = right = sheet.ReadStyleLength(handles[1], specificity);
                    bottom = sheet.ReadStyleLength(handles[2], specificity);
                    break;
                }
                // top | right | bottom | left
                default:
                {
                    top = sheet.ReadStyleLength(handles[0], specificity);
                    right = sheet.ReadStyleLength(handles[1], specificity);
                    bottom = sheet.ReadStyleLength(handles[2], specificity);
                    left = sheet.ReadStyleLength(handles[3], specificity);
                    break;
                }
            }
        }
    }
}
