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
        void ApplyAlign(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property);
        void ApplyDisplay(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property);
    }

    internal class StyleSheetApplicator : IStyleSheetApplicator
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);
        internal static GetCursorIdFunction getCursorIdFunc = null;

        public void ApplyFloat(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleFloat property)
        {
            var value = sheet.ReadStyleFloat(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyFlexBasis(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            var value = sheet.ReadStyleLength(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyInt(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            var value = sheet.ReadStyleInt(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyEnum<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            var value = sheet.ReadStyleEnum<T>(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyLength(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            var value = sheet.ReadStyleLength(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyColor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleColor property)
        {
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
                            Debug.LogWarning("USS 'cursor' property requires two integers for the hot spot value.");
                        }
                    }
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
            StyleValueHandle handle = handles[0];
            Font font = null;

            switch (handle.valueType)
            {
                case StyleValueType.ResourcePath:
                {
                    string path = sheet.ReadResourcePath(handle);

                    if (!string.IsNullOrEmpty(path))
                    {
                        font = Panel.LoadResource(path, typeof(Font)) as Font;
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
                source = Panel.LoadResource("d_console.warnicon", typeof(Texture2D)) as Texture2D;
            }
            var value = new StyleBackground(source) {specificity = specificity};
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyAlign(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Auto)
            {
                StyleInt auto = new StyleInt((int)Align.Auto) {specificity = specificity};
                property.Apply(auto, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                return;
            }

            if (handles[0].valueType != StyleValueType.Enum)
            {
                Debug.LogError("Invalid value for align property " + sheet.ReadAsString(handles[0]));
                return;
            }

            var value = sheet.ReadStyleEnum<Align>(handles[0], specificity);
            property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public void ApplyDisplay(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.None)
            {
                StyleInt none = new StyleInt((int)DisplayStyle.None) {specificity = specificity};
                property.Apply(none, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                return;
            }

            if (handles[0].valueType != StyleValueType.Enum)
            {
                Debug.LogError("Invalid value for display property " + sheet.ReadAsString(handles[0]));
                return;
            }

            var value = sheet.ReadStyleEnum<DisplayStyle>(handles[0], specificity);
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
                        source = Panel.LoadResource(path, typeof(Texture2D)) as Texture2D;
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

        public void ApplyAlign(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (currentStyleValue.keyword == StyleKeyword.Auto)
            {
                property.Apply(new StyleInt((int)Align.Auto) { specificity = specificity }, StylePropertyApplyMode.Copy);
            }
            else
            {
                ApplyInt(sheet, handles, specificity, ref property);
            }
        }

        public void ApplyDisplay(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (currentStyleValue.keyword == StyleKeyword.None)
            {
                property.Apply(new StyleInt((int)DisplayStyle.None) { specificity = specificity }, StylePropertyApplyMode.Copy);
            }
            else
            {
                ApplyInt(sheet, handles, specificity, ref property);
            }
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
            shrink = 1f;
            basis = StyleKeyword.Auto;

            bool valid = false;

            if (handles.Length == 1 && handles[0].valueType == StyleValueType.Keyword)
            {
                // Handle none | auto
                if (handles[0].valueIndex == (int)StyleValueKeyword.None)
                {
                    valid = true;
                    grow = 0f;
                    shrink = 0f;
                    basis = StyleKeyword.Auto;
                }
                else if (handles[0].valueIndex == (int)StyleValueKeyword.Auto)
                {
                    valid = true;
                    grow = 1f;
                    shrink = 1f;
                    basis = StyleKeyword.Auto;
                }
            }
            else if (handles.Length <= 3)
            {
                // Handle [ <'flex-grow'> <'flex-shrink'>? || <'flex-basis'> ]
                valid = true;

                grow = 0f;
                shrink = 1f;
                basis = Length.Percent(0);

                bool growFound = false;
                bool basisFound = false;
                for (int i = 0; i < handles.Length && valid; i++)
                {
                    var handle = handles[i];
                    var valueType = handle.valueType;
                    if (valueType == StyleValueType.Dimension || valueType == StyleValueType.Keyword)
                    {
                        // Basis
                        if (basisFound)
                        {
                            valid = false;
                            break;
                        }

                        basisFound = true;
                        if (valueType == StyleValueType.Keyword)
                        {
                            if (handle.valueIndex == (int)StyleValueKeyword.Auto)
                                basis = StyleKeyword.Auto;
                        }
                        else if (valueType == StyleValueType.Dimension)
                        {
                            basis = sheet.ReadStyleLength(handle, specificity);
                        }

                        if (growFound && i != handles.Length - 1)
                        {
                            // If grow is already processed basis must be the last value
                            valid = false;
                        }
                    }
                    else if (valueType == StyleValueType.Float)
                    {
                        var value = sheet.ReadFloat(handle);
                        if (!growFound)
                        {
                            growFound = true;
                            grow = value;
                        }
                        else
                        {
                            shrink = value;
                        }
                    }
                    else
                    {
                        valid = false;
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
