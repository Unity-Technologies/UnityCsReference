using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class ShorthandApplicator
    {
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

        private static void CompileBoxAreaNoKeyword(StylePropertyReader reader, out StyleLength top, out StyleLength right, out StyleLength bottom, out StyleLength left)
        {
            CompileBoxArea(reader, out top, out right, out bottom, out left);

            if (top.keyword != StyleKeyword.Undefined)
                top.value = 0f;
            if (right.keyword != StyleKeyword.Undefined)
                right.value = 0f;
            if (bottom.keyword != StyleKeyword.Undefined)
                bottom.value = 0f;
            if (left.keyword != StyleKeyword.Undefined)
                left.value = 0f;
        }

        private static void CompileBoxAreaNoKeyword(StylePropertyReader reader, out StyleFloat top, out StyleFloat right, out StyleFloat bottom, out StyleFloat left)
        {
            StyleLength t;
            StyleLength r;
            StyleLength b;
            StyleLength l;

            CompileBoxAreaNoKeyword(reader, out t, out r, out b, out l);

            top = t.ToStyleFloat();
            right = r.ToStyleFloat();
            bottom = b.ToStyleFloat();
            left = l.ToStyleFloat();
        }

        private static void CompileBoxAreaNoKeyword(StylePropertyReader reader, out StyleColor top, out StyleColor right, out StyleColor bottom, out StyleColor left)
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

            if (top.keyword != StyleKeyword.Undefined)
                top.value = Color.clear;
            if (right.keyword != StyleKeyword.Undefined)
                right.value = Color.clear;
            if (bottom.keyword != StyleKeyword.Undefined)
                bottom.value = Color.clear;
            if (left.keyword != StyleKeyword.Undefined)
                left.value = Color.clear;
        }
    }
}
