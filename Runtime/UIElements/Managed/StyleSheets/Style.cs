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
        BorderWidth,
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

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal class StylePropertyAttribute : Attribute
    {
        // Specify a custom property name (otherwise field name is used)
        internal string propertyName;

        internal StylePropertyID propertyID;

        internal StylePropertyAttribute(string propertyName, StylePropertyID propertyID)
        {
            this.propertyName = propertyName;
            this.propertyID = propertyID;
        }

        public StylePropertyAttribute(string propertyName) : this(propertyName, StylePropertyID.Custom)
        {
        }

        public StylePropertyAttribute() : this(string.Empty)
        {
        }
    }

    internal enum StylePropertyApplyMode
    {
        Copy,
        CopyIfMoreSpecific,
        CopyIfNotInline
    }

    public struct Style<T>
    {
        internal int specificity;
        public T value;

        static readonly Style<T> defaultStyle = default(Style<T>);

        public static Style<T> nil
        {
            get { return defaultStyle; }
        }

        public Style(T value)
        {
            this.value = value;
            this.specificity = 0;
        }

        internal Style(T value, int specifity)
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

        public static implicit operator T(Style<T> sp)
        {
            return sp.value;
        }

        internal void Apply(Style<T> other, StylePropertyApplyMode mode)
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

        public static implicit operator Style<T>(T value)
        {
            return new Style<T>(value);
        }

        public static Style<T> Create(T value)
        {
            return new Style<T>(value, int.MaxValue);
        }

        public override string ToString()
        {
            return string.Format("[StyleProperty<{2}>: specifity={0}, value={1}]", specificity, value, typeof(T).Name);
        }
    }
}
