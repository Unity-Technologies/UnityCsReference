// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements.StyleSheets
{
    [VisibleToOtherModules]
    internal static partial class ShorthandApplicator
    {
        [NoAutoStaticsCleanup]
        private static List<TimeValue> s_TransitionDelayList = new List<TimeValue>();
        [NoAutoStaticsCleanup]
        private static List<TimeValue> s_TransitionDurationList = new List<TimeValue>();
        [NoAutoStaticsCleanup]
        private static List<StylePropertyName> s_TransitionPropertyList = new List<StylePropertyName>();
        [NoAutoStaticsCleanup]
        private static List<EasingFunction> s_TransitionTimingFunctionList = new List<EasingFunction>();

        [NoAutoStaticsCleanup]
        private static List<EntityId> s_AnimationClipList = new List<EntityId>();
        [NoAutoStaticsCleanup]
        private static List<float> s_AnimationDurationList = new List<float>();
        [NoAutoStaticsCleanup]
        private static List<float> s_AnimationDelayList = new List<float>();
        [NoAutoStaticsCleanup]
        private static List<AnimationIterationCount> s_AnimationIterationCountList = new List<AnimationIterationCount>();
        [NoAutoStaticsCleanup]
        private static List<AnimationDirection> s_AnimationDirectionList = new List<AnimationDirection>();
        [NoAutoStaticsCleanup]
        private static List<AnimationPlayState> s_AnimationPlayStateList = new List<AnimationPlayState>();

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

        // grid-column / grid-row shorthand -> (start line, end line). Grammar: "<n>", "<n> / <n>",
        // "span <n>", "<n> / span <n>". End encodes: an explicit end line, or (auto start) a span count.
        private static void CompileGridColumn(StylePropertyReader reader, out GridLine gridColumnStart, out GridLine gridColumnEnd)
            => CompileGridLine(reader, out gridColumnStart, out gridColumnEnd);

        private static void CompileGridRow(StylePropertyReader reader, out GridLine gridRowStart, out GridLine gridRowEnd)
            => CompileGridLine(reader, out gridRowStart, out gridRowEnd);

        // CSS Grid. Shorthand: "<line> [ / [ <line> | span <n> ] ]? | span <n>". Span is kept
        // as GridLine.Span (not baked into an absolute line) so authored values round-trip.
        private static void CompileGridLine(StylePropertyReader reader, out GridLine start, out GridLine end)
        {
            start = GridLine.Auto;
            end = GridLine.Auto;
            int valueCount = reader.valueCount;

            int slash = -1;
            for (int i = 0; i < valueCount; i++)
            {
                if (reader.GetValueType(i) == StyleValueType.String &&
                    string.Equals(reader.ReadAsString(i), "/", StringComparison.Ordinal))
                {
                    slash = i;
                    break;
                }
            }

            // Parse one side [from, to) into a GridLine.
            GridLine ParseSide(int from, int to)
            {
                bool spanKeyword = false;
                bool hasNumber = false;
                int number = 0;
                for (int i = from; i < to; i++)
                {
                    var t = reader.GetValueType(i);
                    if (t == StyleValueType.Float)
                    {
                        number = (int)reader.ReadFloat(i);
                        hasNumber = true;
                    }
                    else if (t == StyleValueType.Enum &&
                             string.Equals(reader.ReadAsString(i), "span", StringComparison.OrdinalIgnoreCase))
                    {
                        spanKeyword = true;
                    }
                }
                if (spanKeyword)
                {
                    int c = hasNumber ? number : 1; // "span" with the integer omitted defaults to 1
                    return c >= 1 ? GridLine.Span(c) : GridLine.Auto;
                }
                if (hasNumber)
                    return number >= 1 ? GridLine.AtLine(number) : GridLine.Auto;
                return GridLine.Auto; // auto / empty
            }

            if (slash < 0)
            {
                var side = ParseSide(0, valueCount);
                // "span n" applies to the end (auto start); a bare line applies to the start.
                if (side.isSpan) end = side;
                else start = side;
            }
            else
            {
                start = ParseSide(0, slash);
                end = ParseSide(slash + 1, valueCount);
            }
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
                  ((val2.handle.valueType == StyleValueType.Dimension) || (val2.handle.valueType == StyleValueType.Float)))
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

        private static void CompileGap(StylePropertyReader reader, out Length rowGap, out Length columnGap)
        {
            rowGap = 0f;
            columnGap = 0f;

            var valueCount = reader.valueCount;
            switch (valueCount)
            {
                case 0:
                    break;
                case 1:
                    rowGap = columnGap = reader.ReadLength(0);
                    break;
                default:
                    rowGap = reader.ReadLength(0);
                    columnGap = reader.ReadLength(1);
                    break;
            }
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

        private static void CompileBorderBoxArea(StylePropertyReader reader, out float top, out float right, out float bottom, out float left)
        {
            Length t;
            Length r;
            Length b;
            Length l;

            CompileBoxArea(reader, out t, out r, out b, out l);

            top = t.pixelValue;
            right = r.pixelValue;
            bottom = b.pixelValue;
            left = l.pixelValue;
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

        public static void ApplyTransition(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            ref var transitionData = ref computedStyle.transitionData.Write();
            CompileTransition(reader, ref transitionData.transitionDelay, ref transitionData.transitionDuration, ref transitionData.transitionProperty, ref transitionData.transitionTimingFunction);
        }

        // https://drafts.csswg.org/css-transitions/#transition-shorthand-property
        // [ none | <single-transition-property> ] || <time> || <easing-function> || <time>
        private static void CompileTransition(StylePropertyReader reader, ref UnmanagedRefCountedList<TimeValue> outDelay, ref UnmanagedRefCountedList<TimeValue> outDuration,
            ref UnmanagedRefCountedList<StylePropertyId> outProperty, ref UnmanagedRefCountedList<EasingFunction> outTimingFunction)
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

                var transitionProperty = new StylePropertyName(InitialStyle.transitionProperty[0]);
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
                outProperty.CopyFrom(s_TransitionPropertyList);
                outDelay.CopyFrom(s_TransitionDelayList);
                outDuration.CopyFrom(s_TransitionDurationList);
                outTimingFunction.CopyFrom(s_TransitionTimingFunctionList);
            }
            else
            {
                outProperty.CopyFrom(InitialStyle.transitionProperty);
                outDelay.CopyFrom(InitialStyle.transitionDelay);
                outDuration.CopyFrom(InitialStyle.transitionDuration);
                outTimingFunction.CopyFrom(InitialStyle.transitionTimingFunction);
            }
        }

        public static void ApplyAnimation(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            ref var anim = ref computedStyle.animationData.Write();
            CompileAnimation(reader, ref anim.animationNames, ref anim.animationDuration, ref anim.animationDelay,
                ref anim.animationIterationCount, ref anim.animationDirection, ref anim.animationPlayStates);
        }

        private static void CompileAnimation(StylePropertyReader reader,
            ref UnmanagedRefCountedList<EntityId> outClip, ref UnmanagedRefCountedList<float> outDuration,
            ref UnmanagedRefCountedList<float> outDelay, ref UnmanagedRefCountedList<AnimationIterationCount> outIterationCount,
            ref UnmanagedRefCountedList<AnimationDirection> outDirection, ref UnmanagedRefCountedList<AnimationPlayState> outPlayState)
        {
            s_AnimationClipList.Clear();
            s_AnimationDurationList.Clear();
            s_AnimationDelayList.Clear();
            s_AnimationIterationCountList.Clear();
            s_AnimationDirectionList.Clear();
            s_AnimationPlayStateList.Clear();

            bool isValid = true;
            var valueCount = reader.valueCount;
            int i = 0;
            do
            {
                var clip = InitialStyle.animationNames[0];
                var duration = InitialStyle.animationDuration[0];
                var delay = InitialStyle.animationDelay[0];
                var iterationCount = InitialStyle.animationIterationCount[0];
                var direction = InitialStyle.animationDirection[0];
                var playState = InitialStyle.animationPlayStates[0];

                bool durationFound = false;
                bool delayFound = false;
                bool commaFound = false;
                for (; i < valueCount && !commaFound; ++i)
                {
                    switch (reader.GetValueType(i))
                    {
                        case StyleValueType.Dimension:
                            var seconds = reader.ReadTimeValueAsSeconds(i);
                            if (!durationFound)
                            {
                                durationFound = true;
                                duration = seconds;
                            }
                            else if (!delayFound)
                            {
                                delayFound = true;
                                delay = seconds;
                            }
                            else
                            {
                                isValid = false;
                            }
                            break;

                        case StyleValueType.Float:
                            iterationCount = reader.ReadFloat(i);
                            break;

                        case StyleValueType.Keyword:
                            if (reader.IsKeyword(i, StyleValueKeyword.None))
                                clip = EntityId.None;
                            else
                                isValid = false;
                            break;

                        case StyleValueType.Enum:
                            var str = reader.ReadAsString(i);
                            if (string.Equals(str, "infinite", StringComparison.OrdinalIgnoreCase))
                                iterationCount = AnimationIterationCount.Infinite();
                            else if (StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.AnimationDirection, str, out var dirValue))
                                direction = (AnimationDirection)dirValue;
                            else if (StylePropertyUtil.TryGetEnumIntValue(StyleEnumType.AnimationPlayState, str, out var playValue))
                                playState = (AnimationPlayState)playValue;
                            else
                                isValid = false;
                            break;

                        case StyleValueType.ResourcePath:
                        case StyleValueType.AssetReference:
                        case StyleValueType.MissingAssetReference:
                            clip = reader.ReadUIAnimationClip(i);
                            break;

                        case StyleValueType.CommaSeparator:
                            commaFound = true;
                            break;

                        default:
                            isValid = false;
                            break;
                    }
                }

                s_AnimationClipList.Add(clip);
                s_AnimationDurationList.Add(duration);
                s_AnimationDelayList.Add(delay);
                s_AnimationIterationCountList.Add(iterationCount);
                s_AnimationDirectionList.Add(direction);
                s_AnimationPlayStateList.Add(playState);
            }
            while (i < valueCount && isValid);

            if (isValid)
            {
                outClip.CopyFrom(s_AnimationClipList);
                outDuration.CopyFrom(s_AnimationDurationList);
                outDelay.CopyFrom(s_AnimationDelayList);
                outIterationCount.CopyFrom(s_AnimationIterationCountList);
                outDirection.CopyFrom(s_AnimationDirectionList);
                outPlayState.CopyFrom(s_AnimationPlayStateList);
            }
            else
            {
                outClip.CopyFrom(InitialStyle.animationNames);
                outDuration.CopyFrom(InitialStyle.animationDuration);
                outDelay.CopyFrom(InitialStyle.animationDelay);
                outIterationCount.CopyFrom(InitialStyle.animationIterationCount);
                outDirection.CopyFrom(InitialStyle.animationDirection);
                outPlayState.CopyFrom(InitialStyle.animationPlayStates);
            }
        }

        public static void ApplyUnityAnimationClip(StylePropertyReader reader, ref ComputedStyle computedStyle)
        {
            ref var anim = ref computedStyle.animationData.Write();
            CompileUnityAnimationClipAlias(reader, ref anim.animationNames);
        }

        private static void CompileUnityAnimationClipAlias(StylePropertyReader reader, ref UnmanagedRefCountedList<EntityId> outClip)
        {
            s_AnimationClipList.Clear();

            bool isValid = true;
            var valueCount = reader.valueCount;
            int i = 0;
            do
            {
                var clip = InitialStyle.animationNames[0];
                bool commaFound = false;
                for (; i < valueCount && !commaFound; ++i)
                {
                    switch (reader.GetValueType(i))
                    {
                        case StyleValueType.Keyword:
                            if (reader.IsKeyword(i, StyleValueKeyword.None))
                                clip = EntityId.None;
                            else
                                isValid = false;
                            break;

                        case StyleValueType.ResourcePath:
                        case StyleValueType.AssetReference:
                        case StyleValueType.MissingAssetReference:
                            clip = reader.ReadUIAnimationClip(i);
                            break;

                        case StyleValueType.CommaSeparator:
                            commaFound = true;
                            break;

                        default:
                            isValid = false;
                            break;
                    }
                }

                s_AnimationClipList.Add(clip);
            }
            while (i < valueCount && isValid);

            if (isValid)
                outClip.CopyFrom(s_AnimationClipList);
            else
                outClip.CopyFrom(InitialStyle.animationNames);
        }
    }
}
