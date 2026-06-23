// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    internal static class AnimationRecordingChannelDispatcher
    {
        internal static bool TryApply<T>(StylePropertyRecordingChannel channel, VisualElement element, StylePropertyId id, in T value)
        {
            ref var cs = ref element.computedStyle;
            ref var v = ref Unsafe.AsRef(in value);

            switch (channel)
            {
                case StylePropertyRecordingChannel.Float1:
                    if (typeof(T) == typeof(float))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, float>(ref v));
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFloat))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleFloat>(ref v).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Rotate:
                    if (typeof(T) == typeof(float))
                    {
                        cs.ApplyPropertyAnimation(element, id, new Rotate(Angle.Degrees(Unsafe.As<T, float>(ref v))));
                        return true;
                    }
                    if (typeof(T) == typeof(StyleRotate))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleRotate>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Rotate))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, Rotate>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Length1:
                    if (typeof(T) == typeof(StyleLength))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleLength>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Length))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, Length>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Color4:
                    if (typeof(T) == typeof(StyleColor))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleColor>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Color))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, Color>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Int1:
                    if (typeof(T) == typeof(StyleInt))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleInt>(ref v).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Translate5:
                    if (typeof(T) == typeof(StyleTranslate))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleTranslate>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Translate))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, Translate>(ref v));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector2))
                    {
                        var v2 = Unsafe.As<T, Vector2>(ref v);
                        var cur = cs.ReadPropertyAnimationTranslate(id);
                        cs.ApplyPropertyAnimation(element, id, new Translate(Length.Pixels(v2.x), Length.Pixels(v2.y), cur.z));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3 = Unsafe.As<T, Vector3>(ref v);
                        cs.ApplyPropertyAnimation(element, id, new Translate(Length.Pixels(v3.x), Length.Pixels(v3.y), v3.z));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.TransformOrigin4:
                    // 4 channels: x.value, x.unit, y.value, y.unit. Z is preserved from the
                    // current computed style because UIElements doesn't animate Z origin.
                    if (typeof(T) == typeof(StyleTransformOrigin))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleTransformOrigin>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(TransformOrigin))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, TransformOrigin>(ref v));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector2))
                    {
                        var v2 = Unsafe.As<T, Vector2>(ref v);
                        var cur = cs.ReadPropertyAnimationTransformOrigin(id);
                        cs.ApplyPropertyAnimation(element, id, new TransformOrigin(Length.Pixels(v2.x), Length.Pixels(v2.y), cur.z));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3 = Unsafe.As<T, Vector3>(ref v);
                        cs.ApplyPropertyAnimation(element, id, new TransformOrigin(Length.Pixels(v3.x), Length.Pixels(v3.y), v3.z));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Scale3:
                    if (typeof(T) == typeof(StyleScale))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleScale>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Scale))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, Scale>(ref v));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3 = Unsafe.As<T, Vector3>(ref v);
                        cs.ApplyPropertyAnimation(element, id, new Scale(v3));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Ratio1:
                    if (typeof(T) == typeof(StyleRatio))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, StyleRatio>(ref v).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Ratio))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, Ratio>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.EnumInt:
                    if (typeof(T) == typeof(StyleEnum<Visibility>))
                    {
                        var se = Unsafe.As<T, StyleEnum<Visibility>>(ref v);
                        if (se.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, Convert.ToInt32(se.value));
                        return true;
                    }
                    if (typeof(T) == typeof(Visibility))
                    {
                        cs.ApplyPropertyAnimation(element, id, Convert.ToInt32(Unsafe.As<T, Visibility>(ref v)));
                        return true;
                    }
                    return AnimationRecordingStyleBridge.TryApplyStyleEnumGeneric(element, id, typeof(T), in value);

                case StylePropertyRecordingChannel.BackgroundPosition3:
                    if (typeof(T) == typeof(StyleBackgroundPosition))
                    {
                        var sbp = Unsafe.As<T, StyleBackgroundPosition>(ref v);
                        if (sbp.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sbp.value);
                        return true;
                    }
                    if (typeof(T) == typeof(BackgroundPosition))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, BackgroundPosition>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.BackgroundRepeat2:
                    if (typeof(T) == typeof(StyleBackgroundRepeat))
                    {
                        var sbr = Unsafe.As<T, StyleBackgroundRepeat>(ref v);
                        if (sbr.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sbr.value);
                        return true;
                    }
                    if (typeof(T) == typeof(BackgroundRepeat))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, BackgroundRepeat>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.BackgroundSize5:
                    if (typeof(T) == typeof(StyleBackgroundSize))
                    {
                        var sbs = Unsafe.As<T, StyleBackgroundSize>(ref v);
                        if (sbs.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sbs.value);
                        return true;
                    }
                    if (typeof(T) == typeof(BackgroundSize))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, BackgroundSize>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Filter:
                    // Filter recordings carry the whole list. ApplyPropertyAnimation copies it
                    // into rareData via filter.CopyFrom, matching the per-channel sample path.
                    if (typeof(T) == typeof(StyleList<FilterFunction>))
                    {
                        var sl = Unsafe.As<T, StyleList<FilterFunction>>(ref v);
                        if (sl.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sl.value);
                        return true;
                    }
                    if (typeof(T) == typeof(List<FilterFunction>))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, List<FilterFunction>>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Object1:
                    // PPtr style values flow through ComputedStyle as EntityId.
                    if (typeof(T) == typeof(StyleBackground))
                    {
                        var sb = Unsafe.As<T, StyleBackground>(ref v);
                        if (sb.keyword != StyleKeyword.Undefined)
                            return false;
                        Background.To(sb.value, out var sbId);
                        cs.ApplyPropertyAnimation(element, id, sbId);
                        return true;
                    }
                    if (typeof(T) == typeof(Background))
                    {
                        Background.To(Unsafe.As<T, Background>(ref v), out var bgId);
                        cs.ApplyPropertyAnimation(element, id, bgId);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFont))
                    {
                        var sf = Unsafe.As<T, StyleFont>(ref v);
                        if (sf.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sf.value?.GetEntityId() ?? EntityId.None);
                        return true;
                    }
                    if (typeof(T) == typeof(Font))
                    {
                        var font = Unsafe.As<T, Font>(ref v);
                        cs.ApplyPropertyAnimation(element, id, font?.GetEntityId() ?? EntityId.None);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFontDefinition))
                    {
                        var sfd = Unsafe.As<T, StyleFontDefinition>(ref v);
                        if (sfd.keyword != StyleKeyword.Undefined)
                            return false;
                        FontDefinition.To(sfd.value, out var sfdId);
                        cs.ApplyPropertyAnimation(element, id, sfdId);
                        return true;
                    }
                    if (typeof(T) == typeof(FontDefinition))
                    {
                        FontDefinition.To(Unsafe.As<T, FontDefinition>(ref v), out var fdId);
                        cs.ApplyPropertyAnimation(element, id, fdId);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleMaterialDefinition))
                    {
                        var smd = Unsafe.As<T, StyleMaterialDefinition>(ref v);
                        if (smd.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, smd.value);
                        return true;
                    }
                    if (typeof(T) == typeof(MaterialDefinition))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, MaterialDefinition>(ref v));
                        return true;
                    }
                    if (typeof(T) == typeof(Material))
                    {
                        // MaterialDefinition's EntityId-only path leaves UnmanagedMaterialDefinition's
                        // property-value buffer empty; route through the typed overload so the
                        // material's property block survives.
                        cs.ApplyPropertyAnimation(element, id, new MaterialDefinition(Unsafe.As<T, Material>(ref v)));
                        return true;
                    }
                    if (typeof(T) == typeof(EntityId))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, EntityId>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.TextShadow7:
                    if (typeof(T) == typeof(StyleTextShadow))
                    {
                        var sts = Unsafe.As<T, StyleTextShadow>(ref v);
                        if (sts.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sts.value);
                        return true;
                    }
                    if (typeof(T) == typeof(TextShadow))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, TextShadow>(ref v));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Cursor3:
                    if (typeof(T) == typeof(StyleCursor))
                    {
                        var sc = Unsafe.As<T, StyleCursor>(ref v);
                        if (sc.keyword != StyleKeyword.Undefined)
                            return false;
                        cs.ApplyPropertyAnimation(element, id, sc.value);
                        return true;
                    }
                    if (typeof(T) == typeof(UnityEngine.UIElements.Cursor))
                    {
                        cs.ApplyPropertyAnimation(element, id, Unsafe.As<T, UnityEngine.UIElements.Cursor>(ref v));
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        internal static bool TryReadPrevious<T>(StylePropertyRecordingChannel channel, VisualElement element, StylePropertyId stylePropertyId, out T previousValue)
        {
            previousValue = default;
            var cs = element.computedStyle;

            switch (channel)
            {
                case StylePropertyRecordingChannel.Float1:
                    if (typeof(T) == typeof(float))
                    {
                        var tmp = cs.ReadPropertyAnimationFloat(stylePropertyId);
                        previousValue = Unsafe.As<float, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFloat))
                    {
                        var tmp = new StyleFloat(cs.ReadPropertyAnimationFloat(stylePropertyId));
                        previousValue = Unsafe.As<StyleFloat, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Rotate:
                    if (typeof(T) == typeof(StyleRotate))
                    {
                        var tmp = new StyleRotate(cs.ReadPropertyAnimationRotate(stylePropertyId));
                        previousValue = Unsafe.As<StyleRotate, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Rotate))
                    {
                        var tmp = cs.ReadPropertyAnimationRotate(stylePropertyId);
                        previousValue = Unsafe.As<Rotate, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(float))
                    {
                        var tmp = cs.ReadPropertyAnimationFloat(stylePropertyId);
                        previousValue = Unsafe.As<float, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Length1:
                    if (typeof(T) == typeof(StyleLength))
                    {
                        var tmp = new StyleLength(cs.ReadPropertyAnimationLength(stylePropertyId));
                        previousValue = Unsafe.As<StyleLength, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Length))
                    {
                        var tmp = cs.ReadPropertyAnimationLength(stylePropertyId);
                        previousValue = Unsafe.As<Length, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Color4:
                    if (typeof(T) == typeof(StyleColor))
                    {
                        var tmp = new StyleColor(cs.ReadPropertyAnimationColor(stylePropertyId));
                        previousValue = Unsafe.As<StyleColor, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Color))
                    {
                        var tmp = cs.ReadPropertyAnimationColor(stylePropertyId);
                        previousValue = Unsafe.As<Color, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Int1:
                    if (typeof(T) == typeof(StyleInt))
                    {
                        var tmp = new StyleInt(cs.ReadPropertyAnimationInt(stylePropertyId));
                        previousValue = Unsafe.As<StyleInt, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Translate5:
                    if (typeof(T) == typeof(StyleTranslate))
                    {
                        var tmp = new StyleTranslate(cs.ReadPropertyAnimationTranslate(stylePropertyId));
                        previousValue = Unsafe.As<StyleTranslate, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Translate))
                    {
                        var tmp = cs.ReadPropertyAnimationTranslate(stylePropertyId);
                        previousValue = Unsafe.As<Translate, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.TransformOrigin4:
                    if (typeof(T) == typeof(StyleTransformOrigin))
                    {
                        var tmp = new StyleTransformOrigin(cs.ReadPropertyAnimationTransformOrigin(stylePropertyId));
                        previousValue = Unsafe.As<StyleTransformOrigin, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(TransformOrigin))
                    {
                        var tmp = cs.ReadPropertyAnimationTransformOrigin(stylePropertyId);
                        previousValue = Unsafe.As<TransformOrigin, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Scale3:
                    if (typeof(T) == typeof(StyleScale))
                    {
                        var tmp = new StyleScale(cs.ReadPropertyAnimationScale(stylePropertyId));
                        previousValue = Unsafe.As<StyleScale, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Scale))
                    {
                        var tmp = cs.ReadPropertyAnimationScale(stylePropertyId);
                        previousValue = Unsafe.As<Scale, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Ratio1:
                    if (typeof(T) == typeof(StyleRatio))
                    {
                        var tmp = new StyleRatio(cs.ReadPropertyAnimationRatio(stylePropertyId));
                        previousValue = Unsafe.As<StyleRatio, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Ratio))
                    {
                        var tmp = cs.ReadPropertyAnimationRatio(stylePropertyId);
                        previousValue = Unsafe.As<Ratio, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.EnumInt:
                {
                    var valueType = typeof(T);
                    if (valueType.IsEnum)
                    {
                        try
                        {
                            var i = cs.ReadPropertyAnimationInt(stylePropertyId);
                            previousValue = (T)Enum.ToObject(valueType, i);
                            return true;
                        }
                        catch (ArgumentException)
                        {
                            return false;
                        }
                    }
                    if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(StyleEnum<>))
                        return AnimationRecordingStyleBridge.TryReadPreviousStyleEnumGeneric(element, stylePropertyId, valueType, out previousValue);
                    return false;
                }

                case StylePropertyRecordingChannel.BackgroundPosition3:
                    if (typeof(T) == typeof(StyleBackgroundPosition))
                    {
                        var tmp = new StyleBackgroundPosition(cs.ReadPropertyAnimationBackgroundPosition(stylePropertyId));
                        previousValue = Unsafe.As<StyleBackgroundPosition, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(BackgroundPosition))
                    {
                        var tmp = cs.ReadPropertyAnimationBackgroundPosition(stylePropertyId);
                        previousValue = Unsafe.As<BackgroundPosition, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.BackgroundRepeat2:
                    if (typeof(T) == typeof(StyleBackgroundRepeat))
                    {
                        var tmp = new StyleBackgroundRepeat(cs.ReadPropertyAnimationBackgroundRepeat(stylePropertyId));
                        previousValue = Unsafe.As<StyleBackgroundRepeat, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(BackgroundRepeat))
                    {
                        var tmp = cs.ReadPropertyAnimationBackgroundRepeat(stylePropertyId);
                        previousValue = Unsafe.As<BackgroundRepeat, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.BackgroundSize5:
                    if (typeof(T) == typeof(StyleBackgroundSize))
                    {
                        var tmp = new StyleBackgroundSize(cs.ReadPropertyAnimationBackgroundSize(stylePropertyId));
                        previousValue = Unsafe.As<StyleBackgroundSize, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(BackgroundSize))
                    {
                        var tmp = cs.ReadPropertyAnimationBackgroundSize(stylePropertyId);
                        previousValue = Unsafe.As<BackgroundSize, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Filter:
                    // Snapshot lives on UIAnimationBinder; the underlying RareData /
                    // UnmanagedRefCountedList<UnmanagedFilterFunction> types are not visible here.
                    if (typeof(T) == typeof(StyleList<FilterFunction>))
                    {
                        var snapshot = UIAnimationBinder.ReadFilterListSnapshot(element);
                        var tmp = new StyleList<FilterFunction>(snapshot);
                        previousValue = Unsafe.As<StyleList<FilterFunction>, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(List<FilterFunction>))
                    {
                        var snapshot = UIAnimationBinder.ReadFilterListSnapshot(element);
                        previousValue = Unsafe.As<List<FilterFunction>, T>(ref snapshot);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Object1:
                    if (typeof(T) == typeof(StyleBackground))
                    {
                        var tmp = new StyleBackground(Background.From(cs.ReadPropertyAnimationEntityId(stylePropertyId)));
                        previousValue = Unsafe.As<StyleBackground, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Background))
                    {
                        var tmp = Background.From(cs.ReadPropertyAnimationEntityId(stylePropertyId));
                        previousValue = Unsafe.As<Background, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFont))
                    {
                        var tmp = new StyleFont((Font)Resources.EntityIdToObject(cs.ReadPropertyAnimationEntityId(stylePropertyId)));
                        previousValue = Unsafe.As<StyleFont, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Font))
                    {
                        var tmp = (Font)Resources.EntityIdToObject(cs.ReadPropertyAnimationEntityId(stylePropertyId));
                        previousValue = Unsafe.As<Font, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFontDefinition))
                    {
                        var tmp = new StyleFontDefinition(FontDefinition.From(cs.ReadPropertyAnimationEntityId(stylePropertyId)));
                        previousValue = Unsafe.As<StyleFontDefinition, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(FontDefinition))
                    {
                        var tmp = FontDefinition.From(cs.ReadPropertyAnimationEntityId(stylePropertyId));
                        previousValue = Unsafe.As<FontDefinition, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(StyleMaterialDefinition))
                    {
                        var tmp = new StyleMaterialDefinition(MaterialDefinition.From(cs.unityMaterial));
                        previousValue = Unsafe.As<StyleMaterialDefinition, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(MaterialDefinition))
                    {
                        var tmp = MaterialDefinition.From(cs.unityMaterial);
                        previousValue = Unsafe.As<MaterialDefinition, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(Material))
                    {
                        var tmp = (Material)Resources.EntityIdToObject(cs.unityMaterial.material);
                        previousValue = Unsafe.As<Material, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(EntityId))
                    {
                        // unity-material is the only MaterialDefinition-kind property today;
                        // ReadPropertyAnimationEntityId throws for it because the channel value
                        // is fetched from UnmanagedMaterialDefinition rather than a plain
                        // EntityId field.
                        var tmp = stylePropertyId == StylePropertyId.UnityMaterial
                            ? cs.unityMaterial.material
                            : cs.ReadPropertyAnimationEntityId(stylePropertyId);
                        previousValue = Unsafe.As<EntityId, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.TextShadow7:
                    if (typeof(T) == typeof(StyleTextShadow))
                    {
                        var tmp = new StyleTextShadow(cs.ReadPropertyAnimationTextShadow(stylePropertyId));
                        previousValue = Unsafe.As<StyleTextShadow, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(TextShadow))
                    {
                        var tmp = cs.ReadPropertyAnimationTextShadow(stylePropertyId);
                        previousValue = Unsafe.As<TextShadow, T>(ref tmp);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Cursor3:
                    if (typeof(T) == typeof(StyleCursor))
                    {
                        var tmp = new StyleCursor(cs.ReadPropertyAnimationCursor(stylePropertyId));
                        previousValue = Unsafe.As<StyleCursor, T>(ref tmp);
                        return true;
                    }
                    if (typeof(T) == typeof(UnityEngine.UIElements.Cursor))
                    {
                        var tmp = cs.ReadPropertyAnimationCursor(stylePropertyId);
                        previousValue = Unsafe.As<UnityEngine.UIElements.Cursor, T>(ref tmp);
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        internal static bool TryBuildModifications<T>(
            StylePropertyRecordingChannel channel,
            UnityEngine.Object target,
            string elementPath,
            StylePropertyId stylePropertyId,
            string propName,
            in T previousValue,
            in T currentValue,
            List<UndoPropertyModification> list)
        {
            ref var p = ref Unsafe.AsRef(in previousValue);
            ref var c = ref Unsafe.AsRef(in currentValue);

            switch (channel)
            {
                case StylePropertyRecordingChannel.Float1:
                    if (typeof(T) == typeof(float))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Unsafe.As<T, float>(ref p), Unsafe.As<T, float>(ref c));
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFloat))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Unsafe.As<T, StyleFloat>(ref p).value, Unsafe.As<T, StyleFloat>(ref c).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Rotate:
                    if (typeof(T) == typeof(StyleRotate))
                    {
                        var prevDeg = Unsafe.As<T, StyleRotate>(ref p).value.angle.ToDegrees();
                        var curDeg = Unsafe.As<T, StyleRotate>(ref c).value.angle.ToDegrees();
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, prevDeg, curDeg);
                        return true;
                    }
                    if (typeof(T) == typeof(Rotate))
                    {
                        var prevDeg = Unsafe.As<T, Rotate>(ref p).angle.ToDegrees();
                        var curDeg = Unsafe.As<T, Rotate>(ref c).angle.ToDegrees();
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, prevDeg, curDeg);
                        return true;
                    }
                    if (typeof(T) == typeof(float))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Unsafe.As<T, float>(ref p), Unsafe.As<T, float>(ref c));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Length1:
                    // Top-level Length (no container wrapper); funnel through AddLengthSubMods.
                    if (typeof(T) == typeof(StyleLength))
                    {
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, "",
                            Unsafe.As<T, StyleLength>(ref p).value, Unsafe.As<T, StyleLength>(ref c).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Length))
                    {
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, "",
                            Unsafe.As<T, Length>(ref p), Unsafe.As<T, Length>(ref c));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Color4:
                    if (typeof(T) == typeof(StyleColor))
                    {
                        AnimationRecordingStyleBridge.AddColorMods(list, target, elementPath, propName, Unsafe.As<T, StyleColor>(ref p).value, Unsafe.As<T, StyleColor>(ref c).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Color))
                    {
                        AnimationRecordingStyleBridge.AddColorMods(list, target, elementPath, propName, Unsafe.As<T, Color>(ref p), Unsafe.As<T, Color>(ref c));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Int1:
                    if (typeof(T) == typeof(StyleInt))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Unsafe.As<T, StyleInt>(ref p).value, Unsafe.As<T, StyleInt>(ref c).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Translate5:
                    if (typeof(T) == typeof(StyleTranslate))
                    {
                        var tPrev = Unsafe.As<T, StyleTranslate>(ref p).value;
                        var tCur = Unsafe.As<T, StyleTranslate>(ref c).value;
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", tPrev.x, tCur.x);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", tPrev.y, tCur.y);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".z", tPrev.z, tCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Translate))
                    {
                        var tPrev = Unsafe.As<T, Translate>(ref p);
                        var tCur = Unsafe.As<T, Translate>(ref c);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", tPrev.x, tCur.x);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", tPrev.y, tCur.y);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".z", tPrev.z, tCur.z);
                        return true;
                    }
                    // Vector2 / Vector3 are the implicit-pixel shapes used when the inspector
                    // forwards a numeric Translate without explicit units; pin .x.unit and
                    // .y.unit to Pixel so the curves capture the unit alongside the values.
                    if (typeof(T) == typeof(Vector2))
                    {
                        var v2Prev = Unsafe.As<T, Vector2>(ref p);
                        var v2Cur = Unsafe.As<T, Vector2>(ref c);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", Length.Pixels(v2Prev.x), Length.Pixels(v2Cur.x));
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", Length.Pixels(v2Prev.y), Length.Pixels(v2Cur.y));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3Prev = Unsafe.As<T, Vector3>(ref p);
                        var v3Cur = Unsafe.As<T, Vector3>(ref c);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", Length.Pixels(v3Prev.x), Length.Pixels(v3Cur.x));
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", Length.Pixels(v3Prev.y), Length.Pixels(v3Cur.y));
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".z", v3Prev.z, v3Cur.z);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.TransformOrigin4:
                    // Mirror Translate5 minus the .z modification: TransformOrigin's z is not
                    // animated, so only x/y length sub-channels are emitted.
                    if (typeof(T) == typeof(StyleTransformOrigin))
                    {
                        var toPrev = Unsafe.As<T, StyleTransformOrigin>(ref p).value;
                        var toCur = Unsafe.As<T, StyleTransformOrigin>(ref c).value;
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", toPrev.x, toCur.x);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", toPrev.y, toCur.y);
                        return true;
                    }
                    if (typeof(T) == typeof(TransformOrigin))
                    {
                        var toPrev = Unsafe.As<T, TransformOrigin>(ref p);
                        var toCur = Unsafe.As<T, TransformOrigin>(ref c);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", toPrev.x, toCur.x);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", toPrev.y, toCur.y);
                        return true;
                    }
                    if (typeof(T) == typeof(Vector2))
                    {
                        var v2Prev = Unsafe.As<T, Vector2>(ref p);
                        var v2Cur = Unsafe.As<T, Vector2>(ref c);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", Length.Pixels(v2Prev.x), Length.Pixels(v2Cur.x));
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", Length.Pixels(v2Prev.y), Length.Pixels(v2Cur.y));
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3Prev = Unsafe.As<T, Vector3>(ref p);
                        var v3Cur = Unsafe.As<T, Vector3>(ref c);
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".x", Length.Pixels(v3Prev.x), Length.Pixels(v3Cur.x));
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, target, elementPath, propName, ".y", Length.Pixels(v3Prev.y), Length.Pixels(v3Cur.y));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Scale3:
                    if (typeof(T) == typeof(StyleScale))
                    {
                        var vPrev = Unsafe.As<T, StyleScale>(ref p).value.value;
                        var vCur = Unsafe.As<T, StyleScale>(ref c).value.value;
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".x", vPrev.x, vCur.x);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".y", vPrev.y, vCur.y);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".z", vPrev.z, vCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Scale))
                    {
                        var vPrev = Unsafe.As<T, Scale>(ref p).value;
                        var vCur = Unsafe.As<T, Scale>(ref c).value;
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".x", vPrev.x, vCur.x);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".y", vPrev.y, vCur.y);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".z", vPrev.z, vCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3Prev = Unsafe.As<T, Vector3>(ref p);
                        var v3Cur = Unsafe.As<T, Vector3>(ref c);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".x", v3Prev.x, v3Cur.x);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".y", v3Prev.y, v3Cur.y);
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".z", v3Prev.z, v3Cur.z);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Ratio1:
                    if (typeof(T) == typeof(StyleRatio))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Unsafe.As<T, StyleRatio>(ref p).value.value, Unsafe.As<T, StyleRatio>(ref c).value.value);
                        return true;
                    }
                    if (typeof(T) == typeof(Ratio))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Unsafe.As<T, Ratio>(ref p).value, Unsafe.As<T, Ratio>(ref c).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.EnumInt:
                    if (typeof(T) == typeof(StyleEnum<Visibility>))
                    {
                        var sePrev = Unsafe.As<T, StyleEnum<Visibility>>(ref p);
                        var seCur = Unsafe.As<T, StyleEnum<Visibility>>(ref c);
                        if (sePrev.keyword != StyleKeyword.Undefined || seCur.keyword != StyleKeyword.Undefined)
                            return false;
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Convert.ToInt32(sePrev.value), Convert.ToInt32(seCur.value));
                        return true;
                    }
                    if (typeof(T) == typeof(Visibility))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, null, Convert.ToInt32(Unsafe.As<T, Visibility>(ref p)), Convert.ToInt32(Unsafe.As<T, Visibility>(ref c)));
                        return true;
                    }
                    // StyleEnum<TGeneric> fallback. Targets the supplied UnityEngine.Object
                    // directly (PanelRenderer for the panel-wide path, UIAnimationClip for the
                    // per-element path), so both routings share the same builder.
                    return AnimationRecordingStyleBridge.TryBuildModificationsForStyleEnumGeneric(target, elementPath, stylePropertyId, typeof(T), in previousValue, in currentValue, list);

                case StylePropertyRecordingChannel.BackgroundPosition3:
                {
                    BackgroundPosition prevBp;
                    BackgroundPosition curBp;
                    if (typeof(T) == typeof(StyleBackgroundPosition))
                    {
                        prevBp = Unsafe.As<T, StyleBackgroundPosition>(ref p).value;
                        curBp = Unsafe.As<T, StyleBackgroundPosition>(ref c).value;
                    }
                    else if (typeof(T) == typeof(BackgroundPosition))
                    {
                        prevBp = Unsafe.As<T, BackgroundPosition>(ref p);
                        curBp = Unsafe.As<T, BackgroundPosition>(ref c);
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddBackgroundPositionMods(list, target, elementPath, propName, prevBp, curBp);
                    return true;
                }

                case StylePropertyRecordingChannel.BackgroundRepeat2:
                {
                    BackgroundRepeat prevBr;
                    BackgroundRepeat curBr;
                    if (typeof(T) == typeof(StyleBackgroundRepeat))
                    {
                        prevBr = Unsafe.As<T, StyleBackgroundRepeat>(ref p).value;
                        curBr = Unsafe.As<T, StyleBackgroundRepeat>(ref c).value;
                    }
                    else if (typeof(T) == typeof(BackgroundRepeat))
                    {
                        prevBr = Unsafe.As<T, BackgroundRepeat>(ref p);
                        curBr = Unsafe.As<T, BackgroundRepeat>(ref c);
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddBackgroundRepeatMods(list, target, elementPath, propName, prevBr, curBr);
                    return true;
                }

                case StylePropertyRecordingChannel.BackgroundSize5:
                {
                    BackgroundSize prevBs;
                    BackgroundSize curBs;
                    if (typeof(T) == typeof(StyleBackgroundSize))
                    {
                        prevBs = Unsafe.As<T, StyleBackgroundSize>(ref p).value;
                        curBs = Unsafe.As<T, StyleBackgroundSize>(ref c).value;
                    }
                    else if (typeof(T) == typeof(BackgroundSize))
                    {
                        prevBs = Unsafe.As<T, BackgroundSize>(ref p);
                        curBs = Unsafe.As<T, BackgroundSize>(ref c);
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddBackgroundSizeMods(list, target, elementPath, propName, prevBs, curBs);
                    return true;
                }

                case StylePropertyRecordingChannel.Object1:
                {
                    UnityEngine.Object prevObj;
                    UnityEngine.Object curObj;
                    if (typeof(T) == typeof(StyleBackground))
                    {
                        var prevSb = Unsafe.As<T, StyleBackground>(ref p);
                        var curSb = Unsafe.As<T, StyleBackground>(ref c);
                        if (prevSb.keyword != StyleKeyword.Undefined || curSb.keyword != StyleKeyword.Undefined)
                            return false;
                        prevObj = prevSb.value.GetSelectedImage();
                        curObj = curSb.value.GetSelectedImage();
                    }
                    else if (typeof(T) == typeof(Background))
                    {
                        prevObj = Unsafe.As<T, Background>(ref p).GetSelectedImage();
                        curObj = Unsafe.As<T, Background>(ref c).GetSelectedImage();
                    }
                    else if (typeof(T) == typeof(StyleFont))
                    {
                        var prevSf = Unsafe.As<T, StyleFont>(ref p);
                        var curSf = Unsafe.As<T, StyleFont>(ref c);
                        if (prevSf.keyword != StyleKeyword.Undefined || curSf.keyword != StyleKeyword.Undefined)
                            return false;
                        prevObj = prevSf.value;
                        curObj = curSf.value;
                    }
                    else if (typeof(T) == typeof(Font))
                    {
                        prevObj = Unsafe.As<T, Font>(ref p);
                        curObj = Unsafe.As<T, Font>(ref c);
                    }
                    else if (typeof(T) == typeof(StyleFontDefinition))
                    {
                        var prevSfd = Unsafe.As<T, StyleFontDefinition>(ref p);
                        var curSfd = Unsafe.As<T, StyleFontDefinition>(ref c);
                        if (prevSfd.keyword != StyleKeyword.Undefined || curSfd.keyword != StyleKeyword.Undefined)
                            return false;
                        prevObj = prevSfd.value.GetSelectedFont();
                        curObj = curSfd.value.GetSelectedFont();
                    }
                    else if (typeof(T) == typeof(FontDefinition))
                    {
                        prevObj = Unsafe.As<T, FontDefinition>(ref p).GetSelectedFont();
                        curObj = Unsafe.As<T, FontDefinition>(ref c).GetSelectedFont();
                    }
                    else if (typeof(T) == typeof(StyleMaterialDefinition))
                    {
                        var prevSmd = Unsafe.As<T, StyleMaterialDefinition>(ref p);
                        var curSmd = Unsafe.As<T, StyleMaterialDefinition>(ref c);
                        if (prevSmd.keyword != StyleKeyword.Undefined || curSmd.keyword != StyleKeyword.Undefined)
                            return false;
                        prevObj = prevSmd.value.material;
                        curObj = curSmd.value.material;
                    }
                    else if (typeof(T) == typeof(MaterialDefinition))
                    {
                        prevObj = Unsafe.As<T, MaterialDefinition>(ref p).material;
                        curObj = Unsafe.As<T, MaterialDefinition>(ref c).material;
                    }
                    else if (typeof(T) == typeof(Material))
                    {
                        prevObj = Unsafe.As<T, Material>(ref p);
                        curObj = Unsafe.As<T, Material>(ref c);
                    }
                    else if (typeof(T) == typeof(EntityId))
                    {
                        prevObj = UnityEngine.Resources.EntityIdToObject(Unsafe.As<T, EntityId>(ref p));
                        curObj = UnityEngine.Resources.EntityIdToObject(Unsafe.As<T, EntityId>(ref c));
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddObjectModification(list, target, elementPath, propName, null, prevObj, curObj);
                    return true;
                }

                case StylePropertyRecordingChannel.TextShadow7:
                {
                    // 7 channels via AddTextShadowMods: .color.r/.g/.b/.a + .offset.x/.y + .blurRadius.
                    TextShadow prevTs;
                    TextShadow curTs;
                    if (typeof(T) == typeof(StyleTextShadow))
                    {
                        var stsPrev = Unsafe.As<T, StyleTextShadow>(ref p);
                        var stsCur = Unsafe.As<T, StyleTextShadow>(ref c);
                        if (stsPrev.keyword != StyleKeyword.Undefined || stsCur.keyword != StyleKeyword.Undefined)
                            return false;
                        prevTs = stsPrev.value;
                        curTs = stsCur.value;
                    }
                    else if (typeof(T) == typeof(TextShadow))
                    {
                        prevTs = Unsafe.As<T, TextShadow>(ref p);
                        curTs = Unsafe.As<T, TextShadow>(ref c);
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddTextShadowMods(list, target, elementPath, propName, prevTs, curTs);
                    return true;
                }

                case StylePropertyRecordingChannel.Filter:
                {
                    // AddFilterMods owns the per-slot sub-channel layout (.p<n> / .type / .customDefinition).
                    List<FilterFunction> prevFilters;
                    List<FilterFunction> curFilters;
                    if (typeof(T) == typeof(StyleList<FilterFunction>))
                    {
                        var slPrev = Unsafe.As<T, StyleList<FilterFunction>>(ref p);
                        var slCur = Unsafe.As<T, StyleList<FilterFunction>>(ref c);
                        if (slPrev.keyword != StyleKeyword.Undefined || slCur.keyword != StyleKeyword.Undefined)
                            return false;
                        prevFilters = slPrev.value;
                        curFilters = slCur.value;
                    }
                    else if (typeof(T) == typeof(List<FilterFunction>))
                    {
                        prevFilters = Unsafe.As<T, List<FilterFunction>>(ref p);
                        curFilters = Unsafe.As<T, List<FilterFunction>>(ref c);
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddFilterMods(list, target, elementPath, propName, prevFilters, curFilters);
                    return true;
                }

                case StylePropertyRecordingChannel.Cursor3:
                {
                    // 3 channels: .image (Texture2D PPtr) + .hotspot.x / .hotspot.y (Float).
                    UnityEngine.UIElements.Cursor prevCursor;
                    UnityEngine.UIElements.Cursor curCursor;
                    if (typeof(T) == typeof(StyleCursor))
                    {
                        var scPrev = Unsafe.As<T, StyleCursor>(ref p);
                        var scCur = Unsafe.As<T, StyleCursor>(ref c);
                        if (scPrev.keyword != StyleKeyword.Undefined || scCur.keyword != StyleKeyword.Undefined)
                            return false;
                        prevCursor = scPrev.value;
                        curCursor = scCur.value;
                    }
                    else if (typeof(T) == typeof(UnityEngine.UIElements.Cursor))
                    {
                        prevCursor = Unsafe.As<T, UnityEngine.UIElements.Cursor>(ref p);
                        curCursor = Unsafe.As<T, UnityEngine.UIElements.Cursor>(ref c);
                    }
                    else
                    {
                        return false;
                    }
                    AnimationRecordingStyleBridge.AddObjectModification(list, target, elementPath, propName, ".image", prevCursor.texture, curCursor.texture);
                    AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".hotspot.x", prevCursor.hotspot.x, curCursor.hotspot.x);
                    AnimationRecordingStyleBridge.AddModification(list, target, elementPath, propName, ".hotspot.y", prevCursor.hotspot.y, curCursor.hotspot.y);
                    return true;
                }

                default:
                    return false;
            }
        }
    }
}
