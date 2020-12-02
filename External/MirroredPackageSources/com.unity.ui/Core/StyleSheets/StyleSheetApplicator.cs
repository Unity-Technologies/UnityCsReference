using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace UnityEngine.UIElements.StyleSheets
{
    internal static partial class ShorthandApplicator
    {
        private static bool CompileFlexShorthand(StylePropertyReader reader, out float grow, out float shrink, out Length basis)
        {
            grow = 0f;
            shrink = 1f;
            basis = Length.Auto();

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
                    basis = Length.Auto();
                }
                else if (reader.IsKeyword(0, StyleValueKeyword.Auto))
                {
                    valid = true;
                    grow = 1f;
                    shrink = 1f;
                    basis = Length.Auto();
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
                                basis = Length.Auto();
                        }
                        else if (valueType == StyleValueType.Dimension)
                        {
                            basis = reader.ReadLength(i);
                        }

                        if (growFound && i != valueCount - 1)
                        {
                            // If grow is already processed basis must be the last value
                            valid = false;
                        }
                    }
                    else if (valueType == StyleValueType.Float)
                    {
                        var value = reader.ReadFloat(i);
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

        private static void CompileBorderRadius(StylePropertyReader reader, out Length top, out Length right, out Length bottom, out Length left)
        {
            CompileBoxArea(reader, out top, out right, out bottom, out left);

            // Border radius doesn't support any keyword, reset to 0 in this case.
            if (top.IsAuto() || top.IsNone())
                top = 0f;
            if (right.IsAuto() || right.IsNone())
                right = 0f;
            if (bottom.IsAuto() || bottom.IsNone())
                bottom = 0f;
            if (left.IsAuto() || left.IsNone())
                left = 0f;
        }

        private static void CompileBoxArea(StylePropertyReader reader, out Length top, out Length right, out Length bottom, out Length left)
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
                    top = right = bottom = left = reader.ReadLength(0);
                    break;
                }
                // vertical | horizontal
                case 2:
                {
                    top = bottom = reader.ReadLength(0);
                    left = right = reader.ReadLength(1);
                    break;
                }
                // top | horizontal | bottom
                case 3:
                {
                    top = reader.ReadLength(0);
                    left = right = reader.ReadLength(1);
                    bottom = reader.ReadLength(2);
                    break;
                }
                // top | right | bottom | left
                default:
                {
                    top = reader.ReadLength(0);
                    right = reader.ReadLength(1);
                    bottom = reader.ReadLength(2);
                    left = reader.ReadLength(3);
                    break;
                }
            }
        }

        private static void CompileBoxArea(StylePropertyReader reader, out float top, out float right, out float bottom, out float left)
        {
            Length t;
            Length r;
            Length b;
            Length l;

            CompileBoxArea(reader, out t, out r, out b, out l);

            top = t.value;
            right = r.value;
            bottom = b.value;
            left = l.value;
        }

        private static void CompileBoxArea(StylePropertyReader reader, out Color top, out Color right, out Color bottom, out Color left)
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
                    top = right = bottom = left = reader.ReadColor(0);
                    break;
                }
                // vertical | horizontal
                case 2:
                {
                    top = bottom = reader.ReadColor(0);
                    left = right = reader.ReadColor(1);
                    break;
                }
                // top | horizontal | bottom
                case 3:
                {
                    top = reader.ReadColor(0);
                    left = right = reader.ReadColor(1);
                    bottom = reader.ReadColor(2);
                    break;
                }
                // top | right | bottom | left
                default:
                {
                    top = reader.ReadColor(0);
                    right = reader.ReadColor(1);
                    bottom = reader.ReadColor(2);
                    left = reader.ReadColor(3);
                    break;
                }
            }
        }

        private static void CompileTextOutline(StylePropertyReader reader, out Color outlineColor, out float outlineWidth)
        {
            outlineColor = Color.clear;
            outlineWidth = 0.0f;

            var valueCount = reader.valueCount;
            for (int i = 0; i < valueCount; i++)
            {
                var valueType = reader.GetValueType(i);
                if (valueType == StyleValueType.Dimension)
                    outlineWidth = reader.ReadFloat(i);
                else if (valueType == StyleValueType.Enum || valueType == StyleValueType.Color)
                    outlineColor = reader.ReadColor(i);
            }
        }
    }
}
