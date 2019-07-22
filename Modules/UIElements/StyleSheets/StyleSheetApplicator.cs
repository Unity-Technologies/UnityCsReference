// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static class StyleSheetApplicator
    {
        public static void ApplyAlign(IStylePropertyReader reader, ref StyleInt property)
        {
            if (reader.IsKeyword(0, StyleValueKeyword.Auto))
            {
                StyleInt auto = new StyleInt((int)Align.Auto) {specificity = reader.specificity};
                property = auto;
                return;
            }

            if (!reader.IsValueType(0, StyleValueType.Enum))
            {
                Debug.LogError("Invalid value for align property " + reader.ReadAsString(0));
                return;
            }

            property = reader.ReadStyleEnum<Align>(0);
        }

        public static void ApplyDisplay(IStylePropertyReader reader, ref StyleInt property)
        {
            if (reader.IsKeyword(0, StyleValueKeyword.None))
            {
                StyleInt none = new StyleInt((int)DisplayStyle.None) {specificity = reader.specificity};
                property = none;
                return;
            }

            if (!reader.IsValueType(0, StyleValueType.Enum))
            {
                Debug.LogError("Invalid value for display property " + reader.ReadAsString(0));
                return;
            }

            property = reader.ReadStyleEnum<DisplayStyle>(0);
        }
    }

    internal static class ShorthandApplicator
    {
        public static void ApplyBorderColor(StylePropertyReader reader, VisualElementStylesData styleData)
        {
            StyleColor top;
            StyleColor right;
            StyleColor bottom;
            StyleColor left;
            CompileBoxArea(reader, out top, out right, out bottom, out left);

            // border-color doesn't support any keyword, revert to Color.clear in that case
            if (top.keyword != StyleKeyword.Undefined)
                top.value = Color.clear;
            if (right.keyword != StyleKeyword.Undefined)
                right.value = Color.clear;
            if (bottom.keyword != StyleKeyword.Undefined)
                bottom.value = Color.clear;
            if (left.keyword != StyleKeyword.Undefined)
                left.value = Color.clear;

            styleData.borderTopColor.Apply(top, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderRightColor.Apply(right, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderBottomColor.Apply(bottom, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
            styleData.borderLeftColor.Apply(left, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity);
        }

        public static void ApplyBorderRadius(StylePropertyReader reader, VisualElementStylesData styleData)
        {
            StyleLength topLeft;
            StyleLength topRight;
            StyleLength bottomLeft;
            StyleLength bottomRight;
            CompileBoxArea(reader, out topLeft, out topRight, out bottomRight, out bottomLeft);

            // border-radius doesn't support any keyword, revert to 0 in that case
            if (topLeft.keyword != StyleKeyword.Undefined)
                topLeft.value = 0f;
            if (topRight.keyword != StyleKeyword.Undefined)
                topRight.value = 0f;
            if (bottomLeft.keyword != StyleKeyword.Undefined)
                bottomLeft.value = 0f;
            if (bottomRight.keyword != StyleKeyword.Undefined)
                bottomRight.value = 0f;

            styleData.borderTopLeftRadius = topLeft;
            styleData.borderTopRightRadius = topRight;
            styleData.borderBottomLeftRadius = bottomLeft;
            styleData.borderBottomRightRadius = bottomRight;
        }

        public static void ApplyBorderWidth(StylePropertyReader reader, VisualElementStylesData styleData)
        {
            StyleLength top;
            StyleLength right;
            StyleLength bottom;
            StyleLength left;
            CompileBoxArea(reader, out top, out right, out bottom, out left);

            // border-width doesn't support any keyword, revert to 0 in that case
            if (top.keyword != StyleKeyword.Undefined)
                top.value = 0f;
            if (right.keyword != StyleKeyword.Undefined)
                right.value = 0f;
            if (bottom.keyword != StyleKeyword.Undefined)
                bottom.value = 0f;
            if (left.keyword != StyleKeyword.Undefined)
                left.value = 0f;

            styleData.borderTopWidth = top.ToStyleFloat();
            styleData.borderRightWidth = right.ToStyleFloat();
            styleData.borderBottomWidth = bottom.ToStyleFloat();
            styleData.borderLeftWidth = left.ToStyleFloat();
        }

        public static void ApplyFlex(StylePropertyReader reader, VisualElementStylesData styleData)
        {
            StyleFloat grow;
            StyleFloat shrink;
            StyleLength basis;
            bool valid = CompileFlexShorthand(reader, out grow, out shrink, out basis);

            if (valid)
            {
                styleData.flexGrow = grow;
                styleData.flexShrink = shrink;
                styleData.flexBasis = basis;
            }
        }

        public static void ApplyMargin(StylePropertyReader reader, VisualElementStylesData styleData)
        {
            StyleLength top;
            StyleLength right;
            StyleLength bottom;
            StyleLength left;
            CompileBoxArea(reader, out top, out right, out bottom, out left);

            styleData.marginTop = top;
            styleData.marginRight = right;
            styleData.marginBottom = bottom;
            styleData.marginLeft = left;
        }

        public static void ApplyPadding(StylePropertyReader reader, VisualElementStylesData styleData)
        {
            StyleLength top;
            StyleLength right;
            StyleLength bottom;
            StyleLength left;
            CompileBoxArea(reader, out top, out right, out bottom, out left);

            styleData.paddingTop = top;
            styleData.paddingRight = right;
            styleData.paddingBottom = bottom;
            styleData.paddingLeft = left;
        }

        private static bool CompileFlexShorthand(StylePropertyReader reader, out StyleFloat grow, out StyleFloat shrink, out StyleLength basis)
        {
            grow = 0f;
            shrink = 1f;
            basis = StyleKeyword.Auto;

            bool valid = false;
            var valueCount = reader.valueCount;

            if (valueCount == 1 && reader.IsValueType(0, StyleValueType.Keyword))
            {
                // Handle none | auto
                if (reader.IsKeyword(0, StyleValueKeyword.None))
                {
                    valid = true;
                    grow = 0f;
                    shrink = 0f;
                    basis = StyleKeyword.Auto;
                }
                else if (reader.IsKeyword(0, StyleValueKeyword.Auto))
                {
                    valid = true;
                    grow = 1f;
                    shrink = 1f;
                    basis = StyleKeyword.Auto;
                }
            }
            else if (valueCount <= 3)
            {
                // Handle [ <'flex-grow'> <'flex-shrink'>? || <'flex-basis'> ]
                valid = true;

                grow = 0f;
                shrink = 1f;
                basis = Length.Percent(0);

                bool growFound = false;
                bool basisFound = false;
                for (int i = 0; i < valueCount && valid; i++)
                {
                    var valueType = reader.GetValueType(i);
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
                            if (reader.IsKeyword(i, StyleValueKeyword.Auto))
                                basis = StyleKeyword.Auto;
                        }
                        else if (valueType == StyleValueType.Dimension)
                        {
                            basis = reader.ReadStyleLength(i);
                        }

                        if (growFound && i != valueCount - 1)
                        {
                            // If grow is already processed basis must be the last value
                            valid = false;
                        }
                    }
                    else if (valueType == StyleValueType.Float)
                    {
                        var value = reader.ReadStyleFloat(i);
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

            grow.specificity = reader.specificity;
            shrink.specificity = reader.specificity;
            basis.specificity = reader.specificity;
            return valid;
        }

        private static void CompileBoxArea(StylePropertyReader reader, out StyleLength top, out StyleLength right, out StyleLength bottom, out StyleLength left)
        {
            top = 0f;
            right = 0f;
            bottom = 0f;
            left = 0f;

            var valueCount = reader.valueCount;
            switch (valueCount)
            {
                // apply to all four sides
                case 0:
                    break;
                case 1:
                {
                    top = right = bottom = left = reader.ReadStyleLength(0);
                    break;
                }
                // vertical | horizontal
                case 2:
                {
                    top = bottom = reader.ReadStyleLength(0);
                    left = right = reader.ReadStyleLength(1);
                    break;
                }
                // top | horizontal | bottom
                case 3:
                {
                    top = reader.ReadStyleLength(0);
                    left = right = reader.ReadStyleLength(1);
                    bottom = reader.ReadStyleLength(2);
                    break;
                }
                // top | right | bottom | left
                default:
                {
                    top = reader.ReadStyleLength(0);
                    right = reader.ReadStyleLength(1);
                    bottom = reader.ReadStyleLength(2);
                    left = reader.ReadStyleLength(3);
                    break;
                }
            }
        }

        private static void CompileBoxArea(StylePropertyReader reader, out StyleColor top, out StyleColor right, out StyleColor bottom, out StyleColor left)
        {
            top = Color.clear;
            right = Color.clear;
            bottom = Color.clear;
            left = Color.clear;

            var valueCount = reader.valueCount;
            switch (valueCount)
            {
                // apply to all four sides
                case 0:
                    break;
                case 1:
                {
                    top = right = bottom = left = reader.ReadStyleColor(0);
                    break;
                }
                // vertical | horizontal
                case 2:
                {
                    top = bottom = reader.ReadStyleColor(0);
                    left = right = reader.ReadStyleColor(1);
                    break;
                }
                // top | horizontal | bottom
                case 3:
                {
                    top = reader.ReadStyleColor(0);
                    left = right = reader.ReadStyleColor(1);
                    bottom = reader.ReadStyleColor(2);
                    break;
                }
                // top | right | bottom | left
                default:
                {
                    top = reader.ReadStyleColor(0);
                    right = reader.ReadStyleColor(1);
                    bottom = reader.ReadStyleColor(2);
                    left = reader.ReadStyleColor(3);
                    break;
                }
            }
        }
    }
}
