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

        // Shorthand
        void ApplyFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData);
    }

    internal class StyleSheetApplicator : IStyleSheetApplicator
    {
        // Strategy to create default cursor must be provided in the context of Editor or Runtime
        internal delegate int GetCursorIdFunction(StyleSheet sheet, StyleValueHandle handle);
        internal static GetCursorIdFunction getCursorIdFunc = null;

        internal static void Apply<T, U>(U val, int specificity, ref T property) where T : struct, IStyleValue<U>
        {
            property.Apply(new T() {value = val, specificity = specificity}, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        private bool ApplyUnset<T, U>(StyleValueHandle[] handles, int specificity, ref T property) where T : struct, IStyleValue<U>
        {
            if (handles[0].valueType == StyleValueType.Keyword && handles[0].valueIndex == (int)StyleValueKeyword.Unset)
            {
                Apply(default(U), specificity, ref property);
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

            var value = sheet.ReadFloat(handles[0]);
            Apply(value, specificity, ref property);
        }

        public void ApplyFlexBasis(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            if (ApplyUnset<StyleLength, Length>(handles, specificity, ref property))
                return;

            var handle = handles[0];
            if (handle.valueType == StyleValueType.Keyword && handle.valueIndex == (int)StyleValueKeyword.Auto)
            {
                var value = new StyleLength(StyleKeyword.Auto) { specificity = specificity };
                property.Apply(value, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            }
            else
            {
                var value = new Length(sheet.ReadFloat(handles[0]));
                value = sheet.ReadFloat(handle);
                Apply(value, specificity, ref property);
            }
        }

        public void ApplyInt(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (ApplyUnset<StyleInt, int>(handles, specificity, ref property))
                return;

            var value = (int)sheet.ReadFloat(handles[0]);
            Apply(value, specificity, ref property);
        }

        public void ApplyEnum<T>(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleInt property)
        {
            if (ApplyUnset<StyleInt, int>(handles, specificity, ref property))
                return;

            var value = StyleSheetCache.GetEnumValue<T>(sheet, handles[0]);
            Apply(value, specificity, ref property);
        }

        public void ApplyLength(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleLength property)
        {
            if (ApplyUnset<StyleLength, Length>(handles, specificity, ref property))
                return;

            var value = new Length(sheet.ReadFloat(handles[0]));
            Apply(value, specificity, ref property);
        }

        public void ApplyColor(StyleSheet sheet, StyleValueHandle[] handles, int specificity, ref StyleColor property)
        {
            if (ApplyUnset<StyleColor, Color>(handles, specificity, ref property))
                return;

            var value = sheet.ReadColor(handles[0]);
            Apply(value, specificity, ref property);
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
            Cursor cursor = new Cursor() { texture = texture, hotspot = new Vector2(hotspotX, hotspotY), defaultCursorId = cursorId };
            Apply(cursor, specificity, ref property);
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
                Apply(font, specificity, ref property);
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
            Apply(new Background(source), specificity, ref property);
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

        private static bool CompileFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, out StyleFloat grow, out StyleFloat shrink, out StyleLength basis)
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
                            if (handles[2].valueType == StyleValueType.Keyword && handles[2].valueIndex == (int)StyleValueKeyword.Auto)
                            {
                                basis = StyleKeyword.Auto;
                            }
                            else if (handles[2].valueType == StyleValueType.Float)
                            {
                                basis = sheet.ReadFloat(handles[2]);
                            }
                        }
                    }
                    else if (handles[1].valueType == StyleValueType.Keyword && handles[1].valueIndex == (int)StyleValueKeyword.Auto)
                    {
                        basis = StyleKeyword.Auto;
                    }
                }
            }

            return valid;
        }

        public void ApplyFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            StyleFloat grow;
            StyleFloat shrink;
            StyleLength basis;
            bool valid = CompileFlexShorthand(sheet, handles, out grow, out shrink, out basis);

            if (valid)
            {
                grow.specificity = specificity;
                shrink.specificity = specificity;
                basis.specificity = specificity;

                styleData.flexGrow.Apply(grow, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                styleData.flexShrink.Apply(shrink, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
                styleData.flexBasis.Apply(basis, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            }
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

        public void ApplyFlexShorthand(StyleSheet sheet, StyleValueHandle[] handles, int specificity, VisualElementStylesData styleData)
        {
            // IStyle doesn't contain a Flex property so this should never be called!
            Debug.LogError("Cannot apply Flex shorthand on inline style value");
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
}
