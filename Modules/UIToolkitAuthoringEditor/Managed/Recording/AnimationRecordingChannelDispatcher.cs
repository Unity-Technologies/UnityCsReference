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

                case StylePropertyRecordingChannel.Translate3:
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

                case StylePropertyRecordingChannel.Translate3:
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

                default:
                    return false;
            }
        }

        internal static bool TryBuildModifications<T>(
            StylePropertyRecordingChannel channel,
            PanelRenderer panelRenderer,
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
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Unsafe.As<T, float>(ref p), Unsafe.As<T, float>(ref c));
                        return true;
                    }
                    if (typeof(T) == typeof(StyleFloat))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Unsafe.As<T, StyleFloat>(ref p).value, Unsafe.As<T, StyleFloat>(ref c).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Rotate:
                    if (typeof(T) == typeof(StyleRotate))
                    {
                        var prevDeg = Unsafe.As<T, StyleRotate>(ref p).value.angle.ToDegrees();
                        var curDeg = Unsafe.As<T, StyleRotate>(ref c).value.angle.ToDegrees();
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, prevDeg, curDeg);
                        return true;
                    }
                    if (typeof(T) == typeof(Rotate))
                    {
                        var prevDeg = Unsafe.As<T, Rotate>(ref p).angle.ToDegrees();
                        var curDeg = Unsafe.As<T, Rotate>(ref c).angle.ToDegrees();
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, prevDeg, curDeg);
                        return true;
                    }
                    if (typeof(T) == typeof(float))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Unsafe.As<T, float>(ref p), Unsafe.As<T, float>(ref c));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Length1:
                    // Delegates to AddLengthSubMods so ".value" / ".unit" live in exactly
                    // one place. containerSuffix is empty because Length1 is a top-level
                    // Length (no ".offset" / ".x" wrapper).
                    if (typeof(T) == typeof(StyleLength))
                    {
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, panelRenderer, elementPath, propName, "",
                            Unsafe.As<T, StyleLength>(ref p).value, Unsafe.As<T, StyleLength>(ref c).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Length))
                    {
                        AnimationRecordingStyleBridge.AddLengthSubMods(list, panelRenderer, elementPath, propName, "",
                            Unsafe.As<T, Length>(ref p), Unsafe.As<T, Length>(ref c));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Color4:
                    if (typeof(T) == typeof(StyleColor))
                    {
                        AnimationRecordingStyleBridge.AddColorMods(list, panelRenderer, elementPath, propName, Unsafe.As<T, StyleColor>(ref p).value, Unsafe.As<T, StyleColor>(ref c).value);
                        return true;
                    }
                    if (typeof(T) == typeof(Color))
                    {
                        AnimationRecordingStyleBridge.AddColorMods(list, panelRenderer, elementPath, propName, Unsafe.As<T, Color>(ref p), Unsafe.As<T, Color>(ref c));
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Int1:
                    if (typeof(T) == typeof(StyleInt))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Unsafe.As<T, StyleInt>(ref p).value, Unsafe.As<T, StyleInt>(ref c).value);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Translate3:
                    if (typeof(T) == typeof(StyleTranslate))
                    {
                        var tPrev = Unsafe.As<T, StyleTranslate>(ref p).value;
                        var tCur = Unsafe.As<T, StyleTranslate>(ref c).value;
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", tPrev.x.value, tCur.x.value);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", tPrev.y.value, tCur.y.value);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".z", tPrev.z, tCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Translate))
                    {
                        var tPrev = Unsafe.As<T, Translate>(ref p);
                        var tCur = Unsafe.As<T, Translate>(ref c);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", tPrev.x.value, tCur.x.value);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", tPrev.y.value, tCur.y.value);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".z", tPrev.z, tCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Vector2))
                    {
                        var v2Prev = Unsafe.As<T, Vector2>(ref p);
                        var v2Cur = Unsafe.As<T, Vector2>(ref c);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", v2Prev.x, v2Cur.x);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", v2Prev.y, v2Cur.y);
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3Prev = Unsafe.As<T, Vector3>(ref p);
                        var v3Cur = Unsafe.As<T, Vector3>(ref c);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", v3Prev.x, v3Cur.x);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", v3Prev.y, v3Cur.y);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".z", v3Prev.z, v3Cur.z);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Scale3:
                    if (typeof(T) == typeof(StyleScale))
                    {
                        var vPrev = Unsafe.As<T, StyleScale>(ref p).value.value;
                        var vCur = Unsafe.As<T, StyleScale>(ref c).value.value;
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", vPrev.x, vCur.x);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", vPrev.y, vCur.y);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".z", vPrev.z, vCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Scale))
                    {
                        var vPrev = Unsafe.As<T, Scale>(ref p).value;
                        var vCur = Unsafe.As<T, Scale>(ref c).value;
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", vPrev.x, vCur.x);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", vPrev.y, vCur.y);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".z", vPrev.z, vCur.z);
                        return true;
                    }
                    if (typeof(T) == typeof(Vector3))
                    {
                        var v3Prev = Unsafe.As<T, Vector3>(ref p);
                        var v3Cur = Unsafe.As<T, Vector3>(ref c);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".x", v3Prev.x, v3Cur.x);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".y", v3Prev.y, v3Cur.y);
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, ".z", v3Prev.z, v3Cur.z);
                        return true;
                    }
                    return false;

                case StylePropertyRecordingChannel.Ratio1:
                    if (typeof(T) == typeof(StyleRatio))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Unsafe.As<T, StyleRatio>(ref p).value.value, Unsafe.As<T, StyleRatio>(ref c).value.value);
                        return true;
                    }
                    if (typeof(T) == typeof(Ratio))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Unsafe.As<T, Ratio>(ref p).value, Unsafe.As<T, Ratio>(ref c).value);
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
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Convert.ToInt32(sePrev.value), Convert.ToInt32(seCur.value));
                        return true;
                    }
                    if (typeof(T) == typeof(Visibility))
                    {
                        AnimationRecordingStyleBridge.AddModification(list, panelRenderer, elementPath, propName, null, Convert.ToInt32(Unsafe.As<T, Visibility>(ref p)), Convert.ToInt32(Unsafe.As<T, Visibility>(ref c)));
                        return true;
                    }
                    return AnimationRecordingStyleBridge.TryBuildModificationsForStyleEnumGeneric(panelRenderer.gameObject, elementPath, stylePropertyId, typeof(T), in previousValue, in currentValue, list);

                case StylePropertyRecordingChannel.BackgroundPosition3:
                {
                    // All three channels (align, offset.value, offset.unit) are emitted by
                    // AddBackgroundPositionMods so the bridge owns the composite layout.
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
                    AnimationRecordingStyleBridge.AddBackgroundPositionMods(list, panelRenderer, elementPath, propName, prevBp, curBp);
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
                    AnimationRecordingStyleBridge.AddBackgroundRepeatMods(list, panelRenderer, elementPath, propName, prevBr, curBr);
                    return true;
                }

                case StylePropertyRecordingChannel.BackgroundSize5:
                {
                    // All five channels (type, x.value, x.unit, y.value, y.unit) are emitted
                    // by AddBackgroundSizeMods which itself goes through AddLengthSubMods for
                    // the two Length sub-blocks.
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
                    AnimationRecordingStyleBridge.AddBackgroundSizeMods(list, panelRenderer, elementPath, propName, prevBs, curBs);
                    return true;
                }

                default:
                    return false;
            }
        }
    }
}
