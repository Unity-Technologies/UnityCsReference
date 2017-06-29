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
        BorderLeftWidth,
        BorderTopWidth,
        BorderRightWidth,
        BorderBottomWidth,
        BorderRadius,
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
        // Always leave as last value
        Custom
    }

    internal enum StylePropertyApplyMode
    {
        Copy,
        CopyIfMoreSpecific,
        CopyIfNotInline
    }

    [Obsolete("Style<T> is deprecated, use StyleValue<T> instead", false)]
    public struct Style<T>
    {
        public T GetSpecifiedValueOrDefault(T defaultValue)
        {
            return default(T);
        }
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

        internal void Apply(StyleValue<T> other, StylePropertyApplyMode mode)
        {
            switch (mode)
            {
                case StylePropertyApplyMode.Copy:
                    value = other.value;
                    specificity = other.specificity;
                    break;
                case StylePropertyApplyMode.CopyIfMoreSpecific:
                    if (other.specificity >= specificity)
                    {
                        value = other.value;
                        specificity = other.specificity;
                    }
                    break;
                case StylePropertyApplyMode.CopyIfNotInline:
                    if (specificity < int.MaxValue)
                    {
                        value = other.value;
                        specificity = other.specificity;
                    }
                    break;
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
}
