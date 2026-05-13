// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Maps a USS property name to the set of <see cref="StyleValueType"/>s that are
/// compatible with it, using <see cref="StyleDebug.GetComputedStyleType"/> to
/// resolve the property's computed type.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal static class StyleValueTypeResolver
{
    static readonly StyleValueType[] s_FloatTypes = { StyleValueType.Float, StyleValueType.Keyword };
    static readonly StyleValueType[] s_DimensionTypes = { StyleValueType.Dimension, StyleValueType.Keyword };
    static readonly StyleValueType[] s_ColorTypes = { StyleValueType.Color, StyleValueType.Keyword };
    static readonly StyleValueType[] s_FontTypes = { StyleValueType.AssetReference, StyleValueType.ResourcePath, StyleValueType.Keyword };
    static readonly StyleValueType[] s_BackgroundTypes = { StyleValueType.ScalableImage, StyleValueType.AssetReference, StyleValueType.ResourcePath, StyleValueType.Keyword };
    static readonly StyleValueType[] s_CursorTypes = { StyleValueType.Enum, StyleValueType.ScalableImage, StyleValueType.AssetReference, StyleValueType.ResourcePath, StyleValueType.Keyword };
    static readonly StyleValueType[] s_EnumTypes = { StyleValueType.Enum, StyleValueType.Keyword };
    static readonly StyleValueType[] s_InvalidTypes = { StyleValueType.Invalid, StyleValueType.Keyword };
    static readonly StyleValueType[] s_InvalidOnly = { StyleValueType.Invalid };

    public static ReadOnlySpan<StyleValueType> GetCompatibleTypes(string styleName)
    {
        var computedType = StyleDebug.GetComputedStyleType(styleName);
        if (computedType == null)
            return s_InvalidOnly;

        if (computedType == typeof(float) || computedType == typeof(int))
            return s_FloatTypes;

        if (computedType == typeof(Length) || computedType == typeof(Rotate) || computedType == typeof(List<TimeValue>))
            return s_DimensionTypes;

        if (computedType == typeof(Color))
            return s_ColorTypes;

        if (computedType == typeof(Font) || computedType == typeof(FontDefinition))
            return s_FontTypes;

        if (computedType == typeof(Background))
            return s_BackgroundTypes;

        if (computedType == typeof(UnityEngine.UIElements.Cursor) || computedType == typeof(List<StylePropertyName>))
            return s_CursorTypes;

        if (IsEnum(computedType) || computedType == typeof(List<EasingFunction>))
            return s_EnumTypes;

        return s_InvalidTypes;
    }

    static bool IsEnum(Type t)
    {
        return t.IsEnum || t == typeof(StyleEnum<>);
    }
}
