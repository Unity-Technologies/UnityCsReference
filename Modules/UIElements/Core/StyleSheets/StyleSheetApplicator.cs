// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    [VisibleToOtherModules]
    internal static partial class ShorthandApplicator
    {
        private static List<TimeValue> s_TransitionDelayList = new List<TimeValue>();
        private static List<TimeValue> s_TransitionDurationList = new List<TimeValue>();
        private static List<StylePropertyName> s_TransitionPropertyList = new List<StylePropertyName>();
        private static List<EasingFunction> s_TransitionTimingFunctionList = new List<EasingFunction>();

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

        private static void CompileBackgroundPosition(StylePropertyReader reader, out BackgroundPosition backgroundPositionX, out BackgroundPosition backgroundPositionY)
        {
            var valCount = reader.valueCount;

            var val1 = reader.GetValue(0);
            var val2 = valCount > 1 ? reader.GetValue(1) : default;
            var val3 = valCount > 2 ? reader.GetValue(2) : default;
            var val4 = valCount > 3 ? reader.GetValue(3) : default;

            backgroundPositionX = new BackgroundPosition();
            backgroundPositionY = new BackgroundPosition();

            if (valCount == 1)
            {
                var keyword = (BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 0);
                if (keyword == BackgroundPositionKeyword.Left)
                {
                    backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left);
                    backgroundPositionY = BackgroundPosition.Initial();
                }
                else if (keyword == BackgroundPositionKeyword.Right)
                {
                    backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Right);
                    backgroundPositionY = BackgroundPosition.Initial();
                }
                else if (keyword == BackgroundPositionKeyword.Top)
                {
                    backgroundPositionX = BackgroundPosition.Initial();
                    backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top);
                }
                else if (keyword == BackgroundPositionKeyword.Bottom)
                {
                    backgroundPositionX = BackgroundPosition.Initial();
                    backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Bottom);
                }
                else if (keyword == BackgroundPositionKeyword.Center)
                {
                    backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                }
            }
            else if (valCount == 2)
            {
                if (((val1.handle.valueType == StyleValueType.Dimension) || (val1.handle.valueType == StyleValueType.Float)) &&
                    ((val1.handle.valueType == StyleValueType.Dimension) || (val1.handle.valueType == StyleValueType.Float)))
                {
                    backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, val1.sheet.ReadDimension(val1.handle).ToLength());
                    backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, val2.sheet.ReadDimension(val2.handle).ToLength());
                }
                else if ((val1.handle.valueType == StyleValueType.Enum) && (val2.handle.valueType == StyleValueType.Enum))
                {
                    BackgroundPositionKeyword keyword1 = (BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 0);
                    BackgroundPositionKeyword keyword2 = (BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 1);

                    static void SwapKeyword(ref BackgroundPositionKeyword a, ref BackgroundPositionKeyword b)
                    {
                        BackgroundPositionKeyword temp = a;
                        a = b;
                        b = temp;
                    }

                    if (keyword2 == BackgroundPositionKeyword.Left) SwapKeyword(ref keyword1, ref keyword2);
                    if (keyword2 == BackgroundPositionKeyword.Right) SwapKeyword(ref keyword1, ref keyword2);

                    if (keyword1 == BackgroundPositionKeyword.Top) SwapKeyword(ref keyword1, ref keyword2);
                    if (keyword1 == BackgroundPositionKeyword.Bottom) SwapKeyword(ref keyword1, ref keyword2);

                    backgroundPositionX = new BackgroundPosition(keyword1);
                    backgroundPositionY = new BackgroundPosition(keyword2);
                }
            }
            else if (valCount == 3)
            {
                if ((val1.handle.valueType == StyleValueType.Enum) &&
                    (val2.handle.valueType == StyleValueType.Enum) &&
                    (val3.handle.valueType == StyleValueType.Dimension))
                {
                    backgroundPositionX = new BackgroundPosition((BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 0));
                    backgroundPositionY = new BackgroundPosition((BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 1), reader.ReadLength(2));
                }
                else if ((val1.handle.valueType == StyleValueType.Enum) &&
                         (val2.handle.valueType == StyleValueType.Dimension) &&
                         (val3.handle.valueType == StyleValueType.Enum))
                {
                    backgroundPositionX = new BackgroundPosition((BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 0), reader.ReadLength(1));
                    backgroundPositionY = new BackgroundPosition((BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 2));
                }
            }
            else if (valCount == 4)
            {
                if ((val1.handle.valueType == StyleValueType.Enum) &&
                    (val2.handle.valueType == StyleValueType.Dimension) &&
                    (val3.handle.valueType == StyleValueType.Enum) &&
                    (val4.handle.valueType == StyleValueType.Dimension))
                {
                    backgroundPositionX = new BackgroundPosition((BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 0), reader.ReadLength(1));
                    backgroundPositionY = new BackgroundPosition((BackgroundPositionKeyword)reader.ReadEnum(StyleEnumType.BackgroundPositionKeyword, 2), reader.ReadLength(3));
                }
            }
        }

        public static void CompileUnityBackgroundScaleMode(StylePropertyReader reader,
                                                           out BackgroundPosition backgroundPositionX,
                                                           out BackgroundPosition backgroundPositionY,
                                                           out BackgroundRepeat backgroundRepeat,
                                                           out BackgroundSize backgroundSize)
        {
            var scaleMode = (ScaleMode)reader.ReadEnum(StyleEnumType.ScaleMode, 0);
            backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(scaleMode);
            backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(scaleMode);
            backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(scaleMode);
            backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(scaleMode);
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

        // https://drafts.csswg.org/css-transitions/#transition-shorthand-property
        // [ none | <single-transition-property> ] || <time> || <easing-function> || <time>
        private static void CompileTransition(StylePropertyReader reader, out List<TimeValue> outDelay, out List<TimeValue> outDuration,
            out List<StylePropertyName> outProperty, out List<EasingFunction> outTimingFunction)
        {
            s_TransitionDelayList.Clear();
            s_TransitionDurationList.Clear();
            s_TransitionPropertyList.Clear();
            s_TransitionTimingFunctionList.Clear();

            bool isValid = true;
            bool noneFound = false;
            var valueCount = reader.valueCount;
            int transitionCount = 0;
            int i = 0;
            do
            {
                // If none is present and there are more transitions the shorthand is considered invalid
                if (noneFound)
                {
                    isValid = false;
                    break;
                }

                var transitionProperty = InitialStyle.transitionProperty[0];
                var transitionDuration = InitialStyle.transitionDuration[0];
                var transitionDelay = InitialStyle.transitionDelay[0];
                var transitionTimingFunction = InitialStyle.transitionTimingFunction[0];

                bool durationFound = false;
                bool delayFound = false;
                bool propertyFound = false;
                bool timingFunctionFound = false;
                bool commaFound = false;
                for (; i < valueCount && !commaFound; ++i)
                {
                    var valueType = reader.GetValueType(i);
                    switch (valueType)
                    {
                        case StyleValueType.Keyword:
                            if (reader.IsKeyword(i, StyleValueKeyword.None) && transitionCount == 0)
                            {
                                noneFound = true;
                                propertyFound = true;
                                transitionProperty = new StylePropertyName("none");
                            }
                            else
                            {
                                isValid = false;
                            }
                            break;
                        case StyleValueType.Dimension:
                            var time = reader.ReadTimeValue(i);
                            if (!durationFound)
                            {
                                // transition-duration
                                durationFound = true;
                                transitionDuration = time;
                            }
                            else if (!delayFound)
                            {
                                // transition-delay
                                delayFound = true;
                                transitionDelay = time;
                            }
                            else
                            {
                                isValid = false;
                            }
                            break;
                        case StyleValueType.Enum:
                            var str = reader.ReadAsString(i);
                            if (!timingFunctionFound && StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.EasingMode, str, out var intValue))
                            {
                                // transition-timing-function
                                timingFunctionFound = true;
                                transitionTimingFunction = (EasingMode)intValue;
                            }
                            else if (!propertyFound)
                            {
                                // transition-property
                                propertyFound = true;
                                transitionProperty = new StylePropertyName(str);
                            }
                            else
                            {
                                isValid = false;
                            }
                            break;
                        case StyleValueType.CommaSeparator:
                            commaFound = true;
                            ++transitionCount;
                            break;
                        default:
                            isValid = false;
                            break;
                    }
                }

                s_TransitionDelayList.Add(transitionDelay);
                s_TransitionDurationList.Add(transitionDuration);
                s_TransitionPropertyList.Add(transitionProperty);
                s_TransitionTimingFunctionList.Add(transitionTimingFunction);
            }
            while (i < valueCount && isValid);

            if (isValid)
            {
                outProperty = s_TransitionPropertyList;
                outDelay = s_TransitionDelayList;
                outDuration = s_TransitionDurationList;
                outTimingFunction = s_TransitionTimingFunctionList;
            }
            else
            {
                outProperty = InitialStyle.transitionProperty;
                outDelay = InitialStyle.transitionDelay;
                outDuration = InitialStyle.transitionDuration;
                outTimingFunction = InitialStyle.transitionTimingFunction;
            }
        }
    }
}
