// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace UnityEditor.UIElements;

class UxmlAttributeStyleConverter<TStyleProperty, TProperty, TConverter> : UxmlAttributeConverter<TStyleProperty>
    where TStyleProperty : IStyleValue<TProperty>, IEquatable<TStyleProperty>, new()
    where TConverter : UxmlAttributeConverter<TProperty>, new()
{
    readonly TConverter K_Converter = new();

    public override TStyleProperty FromString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return new TStyleProperty();

        if (Enum.TryParse<StyleKeyword>(value, true, out var keyword))
            return new TStyleProperty { keyword = keyword };

        return new TStyleProperty { value = K_Converter.FromString(value) };
    }

    public override string ToString(TStyleProperty value)
    {
        return value.keyword != StyleKeyword.Undefined
            ? value.keyword.ToString().ToLower()
            : K_Converter.ToString(value.value);
    }
}

internal class StyleBackgroundAttributeConverter : UxmlAttributeStyleConverter<StyleBackground, Background, BackgroundAttributeConverter> { };

internal class StyleBackgroundPositionAttributeConverter : UxmlAttributeStyleConverter<StyleBackgroundPosition, BackgroundPosition, BackgroundPositionAttributeConverter> { };

internal class StyleBackgroundSizeAttributeConverter : UxmlAttributeStyleConverter<StyleBackgroundSize, BackgroundSize, BackgroundSizeAttributeConverter> { };

internal class StyleBackgroundRepeatAttributeConverter : UxmlAttributeStyleConverter<StyleBackgroundRepeat, BackgroundRepeat, BackgroundRepeatAttributeConverter> {};

internal class StyleColorAttributeConverter : UxmlAttributeStyleConverter<StyleColor, Color, ColorAttributeConverter> {};

internal class StyleFloatAttributeConverter : UxmlAttributeStyleConverter<StyleFloat, float, FloatAttributeConverter> {};

internal class StyleIntAttributeConverter : UxmlAttributeStyleConverter<StyleInt, int, IntAttributeConverter> {};

internal class StyleLengthAttributeConverter : UxmlAttributeStyleConverter<StyleLength, Length, LengthAttributeConverter> {};

internal class StyleListAttributeConverter<T> : IUxmlAttributeConverter
{
    public object FromString(string value, CreationContext cc)
    {
        if (value == null)
            return null;

        if (value.AsSpan().Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
            return StyleKeyword.None;

        var items = value.Split(',');
        if (typeof(T) == typeof(Color) && value.Contains("("))
        {
            // Using the lookahead assertion to split the string by commas that are not inside parentheses
            items = Regex.Split(value, @"(?<=\)),");
        }

        if (items.Length <= 0 || !UxmlAttributeConverter.TryGetConverter<T>(out var converter))
            return null;

        var result = new StyleList<T>();
        for (var i = 0; i < items.Length; i++)
        {
            var s = items[i].Trim();
            if (string.IsNullOrEmpty(s))
                continue;

            var decoded = UxmlUtility.DecodeListItem(s);
            result.value.Add((T)converter.FromString(decoded, cc));
        }

        return result;
    }

    public string ToString(object value, VisualTreeAsset visualTreeAsset)
    {
        var list = (StyleList<T>) value;
        if (!UxmlAttributeConverter.TryGetConverter<T>(out var converter))
            return string.Empty;

        if (list.keyword != StyleKeyword.Undefined)
            return list.keyword.ToString().ToLower();

        if (list.value == null)
            return "none";

        var sb = StringBuilderPool.Get();
        for (int i = 0; i < list.value.Count; i++)
        {
            var s = converter.ToString(list.value[i], visualTreeAsset);
            if (typeof(T) == typeof(Color) && s.Contains("("))
            {
                sb.Append(s);
            }
            else
            {
                var encoded = UxmlUtility.EncodeListItem(s);
                sb.Append(encoded);
            }

            if (i + 1 != list.value.Count)
                sb.Append(",");
        }

        var result = sb.ToString();
        StringBuilderPool.Release(sb);
        return result;
    }
}

internal class StyleRotateAttributeConverter : UxmlAttributeStyleConverter<StyleRotate, Rotate, RotateAttributeConverter> {};

internal class StyleScaleAttributeConverter : UxmlAttributeStyleConverter<StyleScale, Scale, ScaleAttributeConverter> {};

internal class StyleTextAutoSizeAttributeConverter : UxmlAttributeStyleConverter<StyleTextAutoSize, TextAutoSize, TextAutoSizeAttributeConverter> {};

internal class StyleTextShadowAttributeConverter : UxmlAttributeStyleConverter<StyleTextShadow, TextShadow, TextShadowAttributeConverter> {};

internal class StyleTransformOriginAttributeConverter : UxmlAttributeStyleConverter<StyleTransformOrigin, TransformOrigin, TransformOriginAttributeConverter> {};

internal class StyleTranslateAttributeConverter : UxmlAttributeStyleConverter<StyleTranslate, Translate, TranslateAttributeConverter> {};

internal class StyleCursorAttributeConverter : UxmlAttributeStyleConverter<StyleCursor, Cursor, CursorAttributeConverter> {};

internal class StyleFontAttributeConverter : UxmlAttributeStyleConverter<StyleFont, Font, FontAttributeConverter> {};

internal class StyleFontDefinitionAttributeConverter : UxmlAttributeStyleConverter<StyleFontDefinition, FontDefinition, FontDefinitionAttributeConverter> {};

internal class StyleEnumAttributeConverter<T> : UxmlAttributeConverter<StyleEnum<T>> where T : struct, IConvertible
{
    public override StyleEnum<T> FromString(string value)
    {
        var keyword = StyleKeyword.Auto;
        if (string.IsNullOrEmpty(value) || Enum.TryParse(value, true, out keyword))
            return keyword;

        return StyleEnum<T>.TryParseString(value, out var result) ? result : default;
    }

    public override string ToString(StyleEnum<T> value)
    {
        return value.keyword != StyleKeyword.Undefined ? value.keyword.ToString().ToLower() : value.ToString().ToLower();
    }
}

internal class StyleMaterialDefinitionAttributeConverter : UxmlAttributeStyleConverter<StyleMaterialDefinition, MaterialDefinition, MaterialDefinitionAttributeConverter> {};

internal class StyleRatioAttributeConverter : UxmlAttributeStyleConverter<StyleRatio, Ratio, RatioAttributeConverter> {};
