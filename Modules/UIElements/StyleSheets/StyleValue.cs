// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal enum StylePropertyID
    {
        Unknown = -1,
        // All float values
        MarginLeft,
        MarginTop,
        MarginRight,
        MarginBottom,
        PaddingLeft,
        PaddingTop,
        PaddingRight,
        PaddingBottom,
        BorderLeft,
        BorderTop,
        BorderRight,
        BorderBottom,
        PositionType,
        PositionLeft,
        PositionTop,
        PositionRight,
        PositionBottom,
        Width,
        Height,
        MinWidth,
        MinHeight,
        MaxWidth,
        MaxHeight,
        Flex,
        FlexBasis,
        FlexGrow,
        FlexShrink,
        BorderLeftWidth,
        BorderTopWidth,
        BorderRightWidth,
        BorderBottomWidth,
        BorderTopLeftRadius,
        BorderTopRightRadius,
        BorderBottomRightRadius,
        BorderBottomLeftRadius,
        // All enum values
        FlexDirection,
        FlexWrap,
        JustifyContent,
        AlignContent,
        AlignSelf,
        AlignItems,
        TextAlignment,
        TextClipping,
        Font,
        FontSize,
        FontStyle,
        BackgroundSize,
        Cursor,
        // All bool values
        WordWrap,
        // All string values
        BackgroundImage,
        // All color values
        TextColor,
        BackgroundColor,
        BorderColor,
        Overflow,
        SliceLeft,
        SliceTop,
        SliceRight,
        SliceBottom,
        Opacity,
        // Shorthand value
        BorderRadius,
        Margin,
        Padding,
        // Always leave as last value
        Custom
    }

    internal enum StylePropertyApplyMode
    {
        Copy,
        CopyIfEqualOrGreaterSpecificity,
        CopyIfNotInline
    }

    public struct StyleValue<T>
    {
        internal int specificity;
        public T value;

        static readonly StyleValue<T> defaultStyle = default(StyleValue<T>);

        public static StyleValue<T> nil
        {
            get { return defaultStyle; }
        }

        public StyleValue(T value)
        {
            this.value = value;
            this.specificity = 0;
        }

        internal StyleValue(T value, int specifity)
        {
            this.value = value;
            this.specificity = specifity;
        }

        public T GetSpecifiedValueOrDefault(T defaultValue)
        {
            if (specificity > 0)
            {
                defaultValue = value;
            }
            return defaultValue;
        }

        public static implicit operator T(StyleValue<T> sp)
        {
            return sp.value;
        }

        internal bool Apply(StyleValue<T> other, StylePropertyApplyMode mode)
        {
            return Apply(other.value, other.specificity, mode);
        }

        internal bool Apply(T otherValue, int otherSpecificity, StylePropertyApplyMode mode)
        {
            switch (mode)
            {
                case StylePropertyApplyMode.Copy:
                    value = otherValue;
                    specificity = otherSpecificity;
                    return true;
                case StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity:
                    if (otherSpecificity >= specificity)
                    {
                        value = otherValue;
                        specificity = otherSpecificity;
                        return true;
                    }
                    return false;
                case StylePropertyApplyMode.CopyIfNotInline:
                    if (specificity < int.MaxValue)
                    {
                        value = otherValue;
                        specificity = otherSpecificity;
                        return true;
                    }
                    return false;
                default:
                    Debug.Assert(false, "Invalid mode " + mode);
                    return false;
            }
        }

        public static implicit operator StyleValue<T>(T value)
        {
            return Create(value);
        }

        public static StyleValue<T> Create(T value)
        {
            return new StyleValue<T>(value, int.MaxValue);
        }

        public override string ToString()
        {
            return string.Format("[StyleProperty<{2}>: specifity={0}, value={1}]", specificity, value, typeof(T).Name);
        }
    }

    internal static class StyleValueUtils
    {
        public static bool ApplyAndCompare(ref StyleValue<float> current, StyleValue<float> other)
        {
            float oldValue = current.value;
            if (current.Apply(other, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
            {
                return oldValue != other.value;
            }
            return false;
        }

        public static bool ApplyAndCompare(ref StyleValue<int> current, StyleValue<int> other)
        {
            int oldValue = current.value;
            if (current.Apply(other, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
            {
                return oldValue != other.value;
            }
            return false;
        }

        public static bool ApplyAndCompare(ref StyleValue<bool> current, StyleValue<bool> other)
        {
            bool oldValue = current.value;
            if (current.Apply(other, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
            {
                return oldValue != other.value;
            }
            return false;
        }

        public static bool ApplyAndCompare(ref StyleValue<Color> current, StyleValue<Color> other)
        {
            Color oldValue = current.value;
            if (current.Apply(other, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
            {
                return oldValue != other.value;
            }
            return false;
        }

        public static bool ApplyAndCompare(ref StyleValue<CursorStyle> current, StyleValue<CursorStyle> other)
        {
            CursorStyle oldValue = current.value;
            if (current.Apply(other, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
            {
                return oldValue != other.value;
            }
            return false;
        }

        public static bool ApplyAndCompare<T>(ref StyleValue<T> current, StyleValue<T> other) where T : Object
        {
            T oldValue = current.value;
            if (current.Apply(other, StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity))
            {
                return oldValue != other.value;
            }
            return false;
        }
    }
}
