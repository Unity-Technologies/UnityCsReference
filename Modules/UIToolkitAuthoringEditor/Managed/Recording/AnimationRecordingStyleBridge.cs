// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// When animation recording is active (AnimationMode.InAnimationRecording) and the inspector target is a VisualElement with a unique
    /// animatable path, style value changes are sent to the active clip via the recording pipeline (synthetic UndoPropertyModification)
    /// so they use the same path as serialized properties. Binding format matches UIAnimationBinder: path = "",
    /// type = PanelRenderer, propertyName = elementPath/stylePropertyId (e.g. #MyElement/Width, #Box/Opacity).
    /// </summary>
    internal static partial class AnimationRecordingStyleBridge
    {
        internal static bool CanRecordStylePropertyChange(VisualElement element, StylePropertyId stylePropertyId)
        {
            if (element == null || !AnimationMode.InAnimationRecording())
                return false;
            return VisualElementRecordability.Probe(element, stylePropertyId).CanRecord;
        }

        /// <summary>
        /// Resolves the element's animation path via the recordability probe (the
        /// <see cref="UIAnimationBinder"/>'s path cache is the source of truth).
        /// </summary>
        static bool TryGetRecordingPath(VisualElement element, out string elementPath)
        {
            elementPath = null;
            var recordability = VisualElementRecordability.ProbeElement(element);
            if (!recordability.CanRecord)
                return false;
            elementPath = recordability.Path;
            return true;
        }

        internal static bool ApplyStyleValueToElementForRecording<T>(VisualElement element, StylePropertyId id, in T value)
        {
            if (element == null)
                return false;

            if (TryApplyStyleValueToElementForRecordingTyped(element, id, in value))
                return true;

            if (!typeof(T).IsValueType && ReferenceEquals(value, null))
                return false;

            return ApplyStyleValueToElementForRecordingObject(element, id, value);
        }

        static bool TryApplyStyleValueToElementForRecordingTyped<T>(VisualElement element, StylePropertyId id, in T value)
        {
            var channel = GetRecordingChannelKind(id);
            if (channel != StylePropertyRecordingChannel.Unsupported)
                return AnimationRecordingChannelDispatcher.TryApply(channel, element, id, in value);
            if (!typeof(T).IsValueType && ReferenceEquals(value, null))
                return false;
            return TryApplyStyleEnumGeneric(element, id, typeof(T), in value);
        }

        internal static bool TryApplyStyleEnumGeneric<T>(VisualElement element, StylePropertyId id, Type valueType, in T value)
        {
            if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(StyleEnum<>))
                return false;
            var enumType = valueType.GetGenericArguments()[0];
            if (!enumType.IsEnum)
                return false;
            object boxed = value;
            var iface = typeof(IStyleValue<>).MakeGenericType(enumType);
            var keyword = (StyleKeyword)iface.GetProperty("keyword").GetValue(boxed);
            if (keyword != StyleKeyword.Undefined)
                return false;
            var enumVal = iface.GetProperty("value").GetValue(boxed);
            ref var cs = ref element.computedStyle;
            cs.ApplyPropertyAnimation(element, id, Convert.ToInt32(enumVal));
            return true;
        }

        static bool ApplyStyleValueToElementForRecordingObject(VisualElement element, StylePropertyId id, object value)
        {
            if (element == null || value == null)
                return false;
            ref var cs = ref element.computedStyle;

            switch (value)
            {
                case StyleFloat sf:
                    cs.ApplyPropertyAnimation(element, id, sf.value);
                    return true;
                case StyleLength sl:
                    cs.ApplyPropertyAnimation(element, id, sl.value);
                    return true;
                case StyleColor sc:
                    cs.ApplyPropertyAnimation(element, id, sc.value);
                    return true;
                case StyleInt si:
                    cs.ApplyPropertyAnimation(element, id, si.value);
                    return true;
                case float f:
                    if (id == StylePropertyId.Rotate)
                    {
                        cs.ApplyPropertyAnimation(element, id, new Rotate(Angle.Degrees(f)));
                        return true;
                    }
                    cs.ApplyPropertyAnimation(element, id, f);
                    return true;
                case Color c:
                    cs.ApplyPropertyAnimation(element, id, c);
                    return true;
                case Length len:
                    cs.ApplyPropertyAnimation(element, id, len);
                    return true;
                case StyleTranslate st:
                    cs.ApplyPropertyAnimation(element, id, st.value);
                    return true;
                case Translate t:
                    cs.ApplyPropertyAnimation(element, id, t);
                    return true;
                case StyleTransformOrigin sto:
                    cs.ApplyPropertyAnimation(element, id, sto.value);
                    return true;
                case TransformOrigin to:
                    cs.ApplyPropertyAnimation(element, id, to);
                    return true;
                case StyleScale ss:
                    cs.ApplyPropertyAnimation(element, id, ss.value);
                    return true;
                case Scale s:
                    cs.ApplyPropertyAnimation(element, id, s);
                    return true;
                case StyleRotate sr:
                    cs.ApplyPropertyAnimation(element, id, sr.value);
                    return true;
                case Rotate r:
                    cs.ApplyPropertyAnimation(element, id, r);
                    return true;
                case StyleRatio sratio:
                    cs.ApplyPropertyAnimation(element, id, sratio.value);
                    return true;
                case Ratio ratio:
                    cs.ApplyPropertyAnimation(element, id, ratio);
                    return true;
                case StyleBackgroundPosition sbp:
                    cs.ApplyPropertyAnimation(element, id, sbp.value);
                    return true;
                case BackgroundPosition bp:
                    cs.ApplyPropertyAnimation(element, id, bp);
                    return true;
                case StyleBackgroundRepeat sbr:
                    cs.ApplyPropertyAnimation(element, id, sbr.value);
                    return true;
                case BackgroundRepeat br:
                    cs.ApplyPropertyAnimation(element, id, br);
                    return true;
                case StyleBackgroundSize sbs:
                    cs.ApplyPropertyAnimation(element, id, sbs.value);
                    return true;
                case BackgroundSize bs:
                    cs.ApplyPropertyAnimation(element, id, bs);
                    return true;
                case StyleTextShadow sts:
                    if (sts.keyword != StyleKeyword.Undefined)
                        return false;
                    cs.ApplyPropertyAnimation(element, id, sts.value);
                    return true;
                case TextShadow ts:
                    cs.ApplyPropertyAnimation(element, id, ts);
                    return true;
                case StyleBackground sb:
                    if (sb.keyword != StyleKeyword.Undefined)
                        return false;
                    Background.To(sb.value, out var sbEntityId);
                    cs.ApplyPropertyAnimation(element, id, sbEntityId);
                    return true;
                case Background bg:
                    Background.To(bg, out var bgEntityId);
                    cs.ApplyPropertyAnimation(element, id, bgEntityId);
                    return true;
                case StyleFont sfont:
                    if (sfont.keyword != StyleKeyword.Undefined)
                        return false;
                    cs.ApplyPropertyAnimation(element, id, sfont.value?.GetEntityId() ?? EntityId.None);
                    return true;
                case Font font:
                    cs.ApplyPropertyAnimation(element, id, font.GetEntityId());
                    return true;
                case StyleFontDefinition sfd:
                    if (sfd.keyword != StyleKeyword.Undefined)
                        return false;
                    FontDefinition.To(sfd.value, out var sfdEntityId);
                    cs.ApplyPropertyAnimation(element, id, sfdEntityId);
                    return true;
                case FontDefinition fd:
                    FontDefinition.To(fd, out var fdEntityId);
                    cs.ApplyPropertyAnimation(element, id, fdEntityId);
                    return true;
                case StyleMaterialDefinition smd:
                    if (smd.keyword != StyleKeyword.Undefined)
                        return false;
                    cs.ApplyPropertyAnimation(element, id, smd.value);
                    return true;
                case MaterialDefinition md:
                    cs.ApplyPropertyAnimation(element, id, md);
                    return true;
                case Material mat:
                    cs.ApplyPropertyAnimation(element, id, new MaterialDefinition(mat));
                    return true;
                case Vector2 v2:
                    if (id == StylePropertyId.Translate)
                    {
                        var cur = cs.ReadPropertyAnimationTranslate(id);
                        cs.ApplyPropertyAnimation(element, id, new Translate(Length.Pixels(v2.x), Length.Pixels(v2.y), cur.z));
                        return true;
                    }
                    if (id == StylePropertyId.TransformOrigin)
                    {
                        var cur = cs.ReadPropertyAnimationTransformOrigin(id);
                        cs.ApplyPropertyAnimation(element, id, new TransformOrigin(Length.Pixels(v2.x), Length.Pixels(v2.y), cur.z));
                        return true;
                    }
                    return false;
                case Vector3 v3:
                    if (id == StylePropertyId.Translate)
                    {
                        cs.ApplyPropertyAnimation(element, id, new Translate(Length.Pixels(v3.x), Length.Pixels(v3.y), v3.z));
                        return true;
                    }
                    if (id == StylePropertyId.TransformOrigin)
                    {
                        cs.ApplyPropertyAnimation(element, id, new TransformOrigin(Length.Pixels(v3.x), Length.Pixels(v3.y), v3.z));
                        return true;
                    }
                    if (id == StylePropertyId.Scale)
                    {
                        cs.ApplyPropertyAnimation(element, id, new Scale(v3));
                        return true;
                    }
                    return false;
                case Enum e:
                    cs.ApplyPropertyAnimation(element, id, Convert.ToInt32(e));
                    return true;
                default:
                    return false;
            }
        }

        internal static bool TryRecordStylePropertyChange<T>(VisualElement element, StylePropertyId stylePropertyId, bool hasPreviousValue, in T previousValue, in T currentValue)
        {
            if (StyleDebug.IsShorthandProperty(stylePropertyId))
                return false;
            if (element == null || !AnimationMode.InAnimationRecording())
                return false;

            if (!typeof(T).IsValueType && ReferenceEquals(currentValue, null))
                return false;

            // Route to a per-element UIAnimationClip preview when one is active; otherwise
            // fall through to the panel-wide PanelRenderer path below.
            if (TryRecordStylePropertyChangePerElement(element, stylePropertyId, hasPreviousValue, in previousValue, in currentValue))
                return true;

            if (!CanRecordStylePropertyChange(element, stylePropertyId))
                return false;

            if (TryRecordStylePropertyChangeTyped(element, stylePropertyId, hasPreviousValue, in previousValue, in currentValue))
                return true;

            object prevObj = hasPreviousValue ? previousValue : null;
            object curObj = currentValue;
            if (!hasPreviousValue)
                prevObj = ReadPreviousValueFromElementForRecordingObject(element, stylePropertyId, curObj);
            ApplyStyleValueToElementForRecordingObject(element, stylePropertyId, curObj);

            var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            if (panelRoot?.panelComponent?.gameObject == null)
                return false;
            var panelGO = panelRoot.panelComponent.gameObject;
            if (!TryGetRecordingPath(element, out var elementPath))
                return false;

            var panelRenderer = panelGO.GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                var panelBinder = panelRenderer.GetAnimationBinder();
                if (panelBinder != null)
                    panelBinder.InvalidateBoundValueCaches();
            }

            var modifications = BuildModificationsForValueObject(panelGO, elementPath, stylePropertyId, prevObj, curObj);
            if (modifications == null || modifications.Length == 0)
                return false;

            Undo.InvokePostprocessModifications(modifications);
            return true;
        }

        // When a per-element preview is active, build modifications targeting the
        // UIAnimationClip asset so the controller's PostprocessModifications hook claims them.
        static bool TryRecordStylePropertyChangePerElement<T>(
            VisualElement element,
            StylePropertyId stylePropertyId,
            bool hasPreviousValue,
            in T previousValue,
            in T currentValue)
        {
            if (!PerElementAnimationContext.TryResolveForElement(
                    element,
                    out var uiClip,
                    out var binder,
                    out _,
                    out var elementPath))
                return false;

            if (!TryGetBindingPropertyName(stylePropertyId, out var propName))
                return false;

            // Apply to computedStyle so the post-record sample re-registers with the new value.
            if (!TryApplyStyleValueForPerElement(element, stylePropertyId, in previousValue, in currentValue, out var resolvedPrevious, out bool typed))
                return false;

            if (binder != null)
                binder.InvalidateBoundValueCaches();

            if (!TryBuildPerElementModifications(uiClip, elementPath, stylePropertyId, propName, in resolvedPrevious, in currentValue, typed, out var modifications)
                || modifications == null
                || modifications.Length == 0)
                return false;

            Undo.InvokePostprocessModifications(modifications);
            return true;
        }

        static bool TryApplyStyleValueForPerElement<T>(
            VisualElement element,
            StylePropertyId stylePropertyId,
            in T previousValue,
            in T currentValue,
            out T resolvedPrevious,
            out bool typed)
        {
            typed = TryApplyStyleValueToElementForRecordingTyped(element, stylePropertyId, in currentValue);
            if (typed)
                return TryResolvePreviousForRecording(element, stylePropertyId, false, in previousValue, in currentValue, out resolvedPrevious);

            // Boxed write fallback for shapes the typed channel dispatcher doesn't cover.
            object curObj = currentValue;
            object prevObj = ReadPreviousValueFromElementForRecordingObject(element, stylePropertyId, curObj);
            if (curObj == null || prevObj == null)
            {
                resolvedPrevious = default;
                return false;
            }
            ApplyStyleValueToElementForRecordingObject(element, stylePropertyId, curObj);
            resolvedPrevious = prevObj is T tPrev ? tPrev : default;
            return true;
        }

        static bool TryBuildPerElementModifications<T>(
            UIAnimationClip target,
            string elementPath,
            StylePropertyId stylePropertyId,
            string propName,
            in T previousValue,
            in T currentValue,
            bool typed,
            out UndoPropertyModification[] modifications)
        {
            modifications = null;
            if (target == null)
                return false;

            var list = new List<UndoPropertyModification>();
            var channel = GetRecordingChannelKind(stylePropertyId);

            bool ok;
            if (typed && channel != StylePropertyRecordingChannel.Unsupported)
            {
                ok = AnimationRecordingChannelDispatcher.TryBuildModifications(
                    channel, target, elementPath, stylePropertyId, propName, in previousValue, in currentValue, list);
            }
            else
            {
                // Boxed fallback for exotic StyleEnum<T> shapes that need reflection.
                ok = TryAddBoxedShapeModifications(list, target, elementPath, propName, previousValue, currentValue);
            }

            if (!ok || list.Count == 0)
                return false;

            modifications = list.ToArray();
            return true;
        }

        // Rule-level recording: an edit to a selected USS rule is captured into the rule's
        // UIAnimationClip instead of the StyleSheet. Curves use an empty element path - the clip
        // is sampled at each matched element's root - and the same propertyName scheme as a
        // per-element clip. The controller's PostprocessModifications hook claims the modifications
        // (target == its active clip), writes the curve / candidate, and re-samples every match.
        internal static bool TryRecordStyleRulePropertyChange<T>(StyleRule rule, StylePropertyId stylePropertyId, in T currentValue)
        {
            if (StyleDebug.IsShorthandProperty(stylePropertyId))
                return false;
            if (rule == null || !AnimationMode.InAnimationRecording())
                return false;
            if (!typeof(T).IsValueType && ReferenceEquals(currentValue, null))
                return false;

            if (!StyleRuleAnimationContext.TryResolveForRule(rule, out var uiClip))
                return false;
            if (!TryGetBindingPropertyName(stylePropertyId, out var propName))
                return false;

            // No element to read a previous value from; previous == current is fine since the
            // recorded keyframe uses the current value (the previous only seeds the Undo record).
            var channel = GetRecordingChannelKind(stylePropertyId);
            bool typed = channel != StylePropertyRecordingChannel.Unsupported;
            if (!TryBuildPerElementModifications(uiClip, string.Empty, stylePropertyId, propName, in currentValue, in currentValue, typed, out var modifications)
                || modifications == null || modifications.Length == 0)
                return false;

            Undo.InvokePostprocessModifications(modifications);
            return true;
        }

        static bool TryRecordStylePropertyChangeTyped<T>(VisualElement element, StylePropertyId stylePropertyId, bool hasPreviousValue, in T previousValue, in T currentValue)
        {
            if (!TryResolvePreviousForRecording(element, stylePropertyId, hasPreviousValue, in previousValue, in currentValue, out T resolvedPrevious))
                return false;

            ApplyStyleValueToElementForRecording(element, stylePropertyId, in currentValue);

            var panelRoot = element.GetFirstAncestorOfType<IPanelComponentRootElement>();
            if (panelRoot?.panelComponent?.gameObject == null)
                return false;
            var panelGO = panelRoot.panelComponent.gameObject;
            if (!TryGetRecordingPath(element, out var elementPath))
                return false;

            var panelRenderer = panelGO.GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                var panelBinder = panelRenderer.GetAnimationBinder();
                if (panelBinder != null)
                    panelBinder.InvalidateBoundValueCaches();
            }

            if (!TryBuildModificationsForValueTyped(panelGO, elementPath, stylePropertyId, in resolvedPrevious, in currentValue, out var modifications)
                || modifications == null || modifications.Length == 0)
                return false;

            Undo.InvokePostprocessModifications(modifications);
            return true;
        }

        static bool TryResolvePreviousForRecording<T>(VisualElement element, StylePropertyId stylePropertyId, bool hasPreviousValue, in T previousValue, in T currentValue, out T resolvedPrevious)
        {
            if (hasPreviousValue)
            {
                resolvedPrevious = previousValue;
                return true;
            }
            return TryReadPreviousValueFromElementForRecording(element, stylePropertyId, in currentValue, out resolvedPrevious);
        }

        static bool TryReadPreviousValueFromElementForRecording<T>(VisualElement element, StylePropertyId stylePropertyId, in T currentValue, out T previousValue)
        {
            previousValue = default;
            if (element == null)
                return false;
            var channel = GetRecordingChannelKind(stylePropertyId);
            if (channel != StylePropertyRecordingChannel.Unsupported)
                return AnimationRecordingChannelDispatcher.TryReadPrevious(channel, element, stylePropertyId, out previousValue);
            return TryReadPreviousStyleEnumGeneric(element, stylePropertyId, typeof(T), out previousValue);
        }
        internal static bool TryReadPreviousStyleEnumGeneric<T>(VisualElement element, StylePropertyId stylePropertyId, Type valueType, out T previousValue)
        {
            previousValue = default;
            if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(StyleEnum<>))
                return false;
            var enumType = valueType.GetGenericArguments()[0];
            if (!enumType.IsEnum)
                return false;
            try
            {
                var i = element.computedStyle.ReadPropertyAnimationInt(stylePropertyId);
                var enumVal = Enum.ToObject(enumType, i);
                previousValue = (T)Activator.CreateInstance(valueType, enumVal);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        static bool TryBuildModificationsForValueTyped<T>(
            GameObject panelGO,
            string elementPath,
            StylePropertyId stylePropertyId,
            in T previousValue,
            in T currentValue,
            out UndoPropertyModification[] modifications)
        {
            modifications = null;
            if (panelGO == null)
                return false;
            if (StyleDebug.IsShorthandProperty(stylePropertyId))
                return false;
            var panelRenderer = panelGO.GetComponent<PanelRenderer>();
            if (panelRenderer == null)
                return false;
            if (!TryGetBindingPropertyName(stylePropertyId, out var propName))
                return false;

            var list = new List<UndoPropertyModification>();
            var channel = GetRecordingChannelKind(stylePropertyId);

            bool ok;
            if (channel != StylePropertyRecordingChannel.Unsupported)
                ok = AnimationRecordingChannelDispatcher.TryBuildModifications(channel, panelRenderer, elementPath, stylePropertyId, propName, in previousValue, in currentValue, list);
            else
                ok = TryBuildModificationsForStyleEnumGeneric(panelGO, elementPath, stylePropertyId, typeof(T), in previousValue, in currentValue, list);

            if (!ok || list.Count == 0)
                return false;
            modifications = list.ToArray();
            return true;
        }
        // Boxed-shape modification builder, parallel to BuildModificationsForValueObject
        // but with the target supplied (UIAnimationClip in the per-element path).
        static bool TryAddBoxedShapeModifications(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, object previousValue, object currentValue)
        {
            if (target == null || currentValue == null || previousValue == null)
                return false;

            switch (currentValue)
            {
                case StyleFloat sfCur when previousValue is StyleFloat sfPrev:
                    AddModification(list, target, elementPath, propName, null, sfPrev.value, sfCur.value);
                    return true;
                case StyleLength slCur when previousValue is StyleLength slPrev:
                    AddModification(list, target, elementPath, propName, ".value", slPrev.value.value, slCur.value.value);
                    AddModification(list, target, elementPath, propName, ".unit", Convert.ToInt32(slPrev.value.unit), Convert.ToInt32(slCur.value.unit));
                    return true;
                case StyleColor scCur when previousValue is StyleColor scPrev:
                    AddColorMods(list, target, elementPath, propName, scPrev.value, scCur.value);
                    return true;
                case StyleInt siCur when previousValue is StyleInt siPrev:
                    AddModification(list, target, elementPath, propName, null, siPrev.value, siCur.value);
                    return true;
                case float fCur when previousValue is float fPrev:
                    AddModification(list, target, elementPath, propName, null, fPrev, fCur);
                    return true;
                case Color cCur when previousValue is Color cPrev:
                    AddColorMods(list, target, elementPath, propName, cPrev, cCur);
                    return true;
                case Length lenCur when previousValue is Length lenPrev:
                    AddModification(list, target, elementPath, propName, ".value", lenPrev.value, lenCur.value);
                    AddModification(list, target, elementPath, propName, ".unit", Convert.ToInt32(lenPrev.unit), Convert.ToInt32(lenCur.unit));
                    return true;
                case StyleTranslate stCur when previousValue is StyleTranslate stPrev:
                    {
                        var tPrev = stPrev.value; var tCur = stCur.value;
                        AddLengthSubMods(list, target, elementPath, propName, ".x", tPrev.x, tCur.x);
                        AddLengthSubMods(list, target, elementPath, propName, ".y", tPrev.y, tCur.y);
                        AddModification(list, target, elementPath, propName, ".z", tPrev.z, tCur.z);
                        return true;
                    }
                case Translate translateCur when previousValue is Translate translatePrev:
                    AddLengthSubMods(list, target, elementPath, propName, ".x", translatePrev.x, translateCur.x);
                    AddLengthSubMods(list, target, elementPath, propName, ".y", translatePrev.y, translateCur.y);
                    AddModification(list, target, elementPath, propName, ".z", translatePrev.z, translateCur.z);
                    return true;
                case StyleTransformOrigin stoCur when previousValue is StyleTransformOrigin stoPrev:
                    {
                        var toPrev = stoPrev.value; var toCur = stoCur.value;
                        AddLengthSubMods(list, target, elementPath, propName, ".x", toPrev.x, toCur.x);
                        AddLengthSubMods(list, target, elementPath, propName, ".y", toPrev.y, toCur.y);
                        return true;
                    }
                case TransformOrigin toCur when previousValue is TransformOrigin toPrev:
                    AddLengthSubMods(list, target, elementPath, propName, ".x", toPrev.x, toCur.x);
                    AddLengthSubMods(list, target, elementPath, propName, ".y", toPrev.y, toCur.y);
                    return true;
                case StyleScale ssCur when previousValue is StyleScale ssPrev:
                    {
                        var vPrev = ssPrev.value.value; var vCur = ssCur.value.value;
                        AddModification(list, target, elementPath, propName, ".x", vPrev.x, vCur.x);
                        AddModification(list, target, elementPath, propName, ".y", vPrev.y, vCur.y);
                        AddModification(list, target, elementPath, propName, ".z", vPrev.z, vCur.z);
                        return true;
                    }
                case Scale scaleCur when previousValue is Scale scalePrev:
                    {
                        var vPrev = scalePrev.value; var vCur = scaleCur.value;
                        AddModification(list, target, elementPath, propName, ".x", vPrev.x, vCur.x);
                        AddModification(list, target, elementPath, propName, ".y", vPrev.y, vCur.y);
                        AddModification(list, target, elementPath, propName, ".z", vPrev.z, vCur.z);
                        return true;
                    }
                case StyleRotate srCur when previousValue is StyleRotate srPrev:
                    {
                        var prevDeg = srPrev.value.angle.ToDegrees(); var curDeg = srCur.value.angle.ToDegrees();
                        AddModification(list, target, elementPath, propName, null, prevDeg, curDeg);
                        return true;
                    }
                case Rotate rCur when previousValue is Rotate rPrev:
                    {
                        var prevDeg = rPrev.angle.ToDegrees(); var curDeg = rCur.angle.ToDegrees();
                        AddModification(list, target, elementPath, propName, null, prevDeg, curDeg);
                        return true;
                    }
                case StyleRatio sratioCur when previousValue is StyleRatio sratioPrev:
                    AddModification(list, target, elementPath, propName, null, sratioPrev.value.value, sratioCur.value.value);
                    return true;
                case Ratio ratioCur when previousValue is Ratio ratioPrev:
                    AddModification(list, target, elementPath, propName, null, ratioPrev.value, ratioCur.value);
                    return true;
                case Vector2 v2Cur when previousValue is Vector2 v2Prev:
                    AddModification(list, target, elementPath, propName, ".x", v2Prev.x, v2Cur.x);
                    AddModification(list, target, elementPath, propName, ".y", v2Prev.y, v2Cur.y);
                    return true;
                case Vector3 v3Cur when previousValue is Vector3 v3Prev:
                    AddModification(list, target, elementPath, propName, ".x", v3Prev.x, v3Cur.x);
                    AddModification(list, target, elementPath, propName, ".y", v3Prev.y, v3Cur.y);
                    AddModification(list, target, elementPath, propName, ".z", v3Prev.z, v3Cur.z);
                    return true;
                case Enum eCur when previousValue is Enum ePrev && eCur.GetType() == ePrev.GetType():
                    AddModification(list, target, elementPath, propName, null, Convert.ToInt32(ePrev), Convert.ToInt32(eCur));
                    return true;
                case StyleBackground sbCur when previousValue is StyleBackground sbPrev:
                    if (sbCur.keyword != StyleKeyword.Undefined || sbPrev.keyword != StyleKeyword.Undefined)
                        return false;
                    AddObjectModification(list, target, elementPath, propName, null, sbPrev.value.GetSelectedImage(), sbCur.value.GetSelectedImage());
                    return true;
                case Background bgCur when previousValue is Background bgPrev:
                    AddObjectModification(list, target, elementPath, propName, null, bgPrev.GetSelectedImage(), bgCur.GetSelectedImage());
                    return true;
                case StyleTextShadow stsCur when previousValue is StyleTextShadow stsPrev:
                    if (stsCur.keyword != StyleKeyword.Undefined || stsPrev.keyword != StyleKeyword.Undefined)
                        return false;
                    AddTextShadowMods(list, target, elementPath, propName, stsPrev.value, stsCur.value);
                    return true;
                case TextShadow tsCur when previousValue is TextShadow tsPrev:
                    AddTextShadowMods(list, target, elementPath, propName, tsPrev, tsCur);
                    return true;
                case StyleFont sfCur when previousValue is StyleFont sfPrev:
                    if (sfCur.keyword != StyleKeyword.Undefined || sfPrev.keyword != StyleKeyword.Undefined)
                        return false;
                    AddObjectModification(list, target, elementPath, propName, null, sfPrev.value, sfCur.value);
                    return true;
                case Font fontCur when previousValue is Font fontPrev:
                    AddObjectModification(list, target, elementPath, propName, null, fontPrev, fontCur);
                    return true;
                case StyleFontDefinition sfdCur when previousValue is StyleFontDefinition sfdPrev:
                    if (sfdCur.keyword != StyleKeyword.Undefined || sfdPrev.keyword != StyleKeyword.Undefined)
                        return false;
                    AddObjectModification(list, target, elementPath, propName, null, sfdPrev.value.GetSelectedFont(), sfdCur.value.GetSelectedFont());
                    return true;
                case FontDefinition fdCur when previousValue is FontDefinition fdPrev:
                    AddObjectModification(list, target, elementPath, propName, null, fdPrev.GetSelectedFont(), fdCur.GetSelectedFont());
                    return true;
                case StyleMaterialDefinition smdCur when previousValue is StyleMaterialDefinition smdPrev:
                    if (smdCur.keyword != StyleKeyword.Undefined || smdPrev.keyword != StyleKeyword.Undefined)
                        return false;
                    AddObjectModification(list, target, elementPath, propName, null, smdPrev.value.material, smdCur.value.material);
                    return true;
                case MaterialDefinition mdCur when previousValue is MaterialDefinition mdPrev:
                    AddObjectModification(list, target, elementPath, propName, null, mdPrev.material, mdCur.material);
                    return true;
                case Material matCur when previousValue is Material matPrev:
                    AddObjectModification(list, target, elementPath, propName, null, matPrev, matCur);
                    return true;
                default:
                    return false;
            }
        }

        // Reuse the generator-emitted color row to keep AddColorMods allocation-free.
        private static readonly IReadOnlyList<string> k_ColorSuffixes =
            UIAnimationBinder.GetChannelSuffixes(StylePropertyId.Color);

        internal static void AddColorMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, Color scPrev, Color scCur)
        {
            for (int i = 0; i < 4; i++)
            {
                var prev = i switch { 0 => scPrev.r, 1 => scPrev.g, 2 => scPrev.b, _ => scPrev.a };
                var cur = i switch { 0 => scCur.r, 1 => scCur.g, 2 => scCur.b, _ => scCur.a };
                AddModification(list, target, elementPath, propName, k_ColorSuffixes[i], prev, cur);
            }
        }

        // Emits ".value" (float) and ".unit" (int) for one Length / LengthBlock. Funnels every
        // Length-containing composite through one place so the literal suffixes appear once.
        internal static void AddLengthSubMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, string containerSuffix, Length prev, Length cur)
        {
            AddModification(list, target, elementPath, propName, containerSuffix + ".value", prev.value, cur.value);
            AddModification(list, target, elementPath, propName, containerSuffix + ".unit", Convert.ToInt32(prev.unit), Convert.ToInt32(cur.unit));
        }

        // Channel order matches AnimationBindingHelper: align, offset.value, offset.unit.
        internal static void AddBackgroundPositionMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, BackgroundPosition prev, BackgroundPosition cur)
        {
            AddModification(list, target, elementPath, propName, ".align", Convert.ToInt32(prev.keyword), Convert.ToInt32(cur.keyword));
            AddLengthSubMods(list, target, elementPath, propName, ".offset", prev.offset, cur.offset);
        }

        // Channel order matches AnimationBindingHelper: x, y (both Repeat enum).
        internal static void AddBackgroundRepeatMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, BackgroundRepeat prev, BackgroundRepeat cur)
        {
            AddModification(list, target, elementPath, propName, ".x", Convert.ToInt32(prev.x), Convert.ToInt32(cur.x));
            AddModification(list, target, elementPath, propName, ".y", Convert.ToInt32(prev.y), Convert.ToInt32(cur.y));
        }

        // Channel order matches AnimationBindingHelper: type, x.value, x.unit, y.value, y.unit.
        internal static void AddBackgroundSizeMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, BackgroundSize prev, BackgroundSize cur)
        {
            AddModification(list, target, elementPath, propName, ".type", Convert.ToInt32(prev.sizeType), Convert.ToInt32(cur.sizeType));
            AddLengthSubMods(list, target, elementPath, propName, ".x", prev.x, cur.x);
            AddLengthSubMods(list, target, elementPath, propName, ".y", prev.y, cur.y);
        }

        // Emits 7 channels: .color.r/.g/.b/.a + .offset.x/.y + .blurRadius.
        // Color sub-block goes through AddColorMods (with propName + ".color"); offset is
        // a Vector2 pair; blurRadius is a single float at .blurRadius.
        internal static void AddTextShadowMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, TextShadow prev, TextShadow cur)
        {
            AddColorMods(list, target, elementPath, propName + ".color", prev.color, cur.color);
            AddModification(list, target, elementPath, propName, ".offset.x", prev.offset.x, cur.offset.x);
            AddModification(list, target, elementPath, propName, ".offset.y", prev.offset.y, cur.offset.y);
            AddModification(list, target, elementPath, propName, ".blurRadius", prev.blurRadius, cur.blurRadius);
        }

        // Slot-by-slot diff against default(FilterFunction); only changed sub-channels are emitted.
        // Added / removed slots fall through this same diff (None / null / 0), giving them the
        // expected "full set" / "reset-to-None" modification sets without special-casing.
        internal static void AddFilterMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, IReadOnlyList<FilterFunction> prev, IReadOnlyList<FilterFunction> cur)
        {
            for (int slot = 0; slot < UIAnimationBinder.kFilterSlotCount; ++slot)
            {
                string slotPrefix = "." + slot.ToString(CultureInfo.InvariantCulture);
                bool prevHasSlot = prev != null && slot < prev.Count;
                bool curHasSlot = cur != null && slot < cur.Count;

                if (!prevHasSlot && !curHasSlot)
                    continue;

                FilterFunction prevSlot = prevHasSlot ? prev[slot] : default;
                FilterFunction curSlot = curHasSlot ? cur[slot] : default;

                // Walk by paramIndex (not by sub-channel) so Float-typed params don't emit
                // redundant mods at sub+1/+2/+3 (ReadFilterParamComponent collapses those).
                int paramCountForDiff = Math.Max(prevSlot.parameterCount, curSlot.parameterCount);
                for (int paramIndex = 0; paramIndex < paramCountForDiff; ++paramIndex)
                {
                    bool prevHasParam = paramIndex < prevSlot.parameterCount;
                    bool curHasParam = paramIndex < curSlot.parameterCount;

                    // A type/definition swap can change the param type between prev and cur (e.g.
                    // Blur->Tint flips Float->Color). Take the union so all 4 color components are
                    // emitted in that frame; the prev==cur check below suppresses no-op writes.
                    FilterParameterType prevType = prevHasParam ? prevSlot.GetParameter(paramIndex).type : FilterParameterType.Float;
                    FilterParameterType curType  = curHasParam  ? curSlot.GetParameter(paramIndex).type  : FilterParameterType.Float;
                    int componentCount = (prevType == FilterParameterType.Color || curType == FilterParameterType.Color) ? 4 : 1;

                    for (int component = 0; component < componentCount; ++component)
                    {
                        int sub = paramIndex * 4 + component;
                        float prevVal = prevHasParam ? ReadFilterParamComponent(prevSlot, paramIndex, component) : 0f;
                        float curVal = curHasParam ? ReadFilterParamComponent(curSlot, paramIndex, component) : 0f;
                        if (prevVal == curVal)
                            continue;
                        AddModification(list, target, elementPath, propName,
                            slotPrefix + ".p" + sub.ToString(CultureInfo.InvariantCulture), prevVal, curVal);
                    }
                }

                if (prevSlot.type != curSlot.type)
                {
                    int prevType = Convert.ToInt32(prevSlot.type);
                    int curType = Convert.ToInt32(curSlot.type);
                    AddModification(list, target, elementPath, propName, slotPrefix + ".type", prevType, curType);
                }

                FilterFunctionDefinition prevDef = prevSlot.customDefinition;
                FilterFunctionDefinition curDef = curSlot.customDefinition;
                if (!ReferenceEquals(prevDef, curDef))
                    AddObjectModification(list, target, elementPath, propName, slotPrefix + ".customDefinition", prevDef, curDef);
            }
        }

        // Mirrors UIAnimationBinder.ReadFilterValue: Float params ignore `component`,
        // Color params route 0..3 to r/g/b/a. Out-of-range paramIndex returns 0.
        static float ReadFilterParamComponent(FilterFunction f, int paramIndex, int component)
        {
            if (paramIndex < 0 || paramIndex >= f.parameterCount)
                return 0f;

            FilterParameter p = f.GetParameter(paramIndex);
            if (p.type == FilterParameterType.Float)
                return p.floatValue;

            return component switch
            {
                0 => p.colorValue.r,
                1 => p.colorValue.g,
                2 => p.colorValue.b,
                3 => p.colorValue.a,
                _ => 0f,
            };
        }

        internal static bool TryBuildModificationsForStyleEnumGeneric<T>(
            GameObject panelGO,
            string elementPath,
            StylePropertyId stylePropertyId,
            Type valueType,
            in T previousValue,
            in T currentValue,
            List<UndoPropertyModification> list)
        {
            if (panelGO == null)
                return false;
            var panelRenderer = panelGO.GetComponent<PanelRenderer>();
            if (panelRenderer == null)
                return false;
            return TryBuildModificationsForStyleEnumGeneric(panelRenderer, elementPath, stylePropertyId, valueType, in previousValue, in currentValue, list);
        }

        internal static bool TryBuildModificationsForStyleEnumGeneric<T>(
            UnityEngine.Object target,
            string elementPath,
            StylePropertyId stylePropertyId,
            Type valueType,
            in T previousValue,
            in T currentValue,
            List<UndoPropertyModification> list)
        {
            if (target == null)
                return false;
            if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(StyleEnum<>))
                return false;
            var enumType = valueType.GetGenericArguments()[0];
            if (!enumType.IsEnum)
                return false;
            if (!TryGetBindingPropertyName(stylePropertyId, out var propName))
                return false;

            var iface = typeof(IStyleValue<>).MakeGenericType(enumType);
            var kwProp = iface.GetProperty("keyword");
            var valProp = iface.GetProperty("value");
            object pBox = previousValue;
            object cBox = currentValue;
            if ((StyleKeyword)kwProp.GetValue(pBox) != StyleKeyword.Undefined || (StyleKeyword)kwProp.GetValue(cBox) != StyleKeyword.Undefined)
                return false;
            var prevOrd = Convert.ToInt32(valProp.GetValue(pBox));
            var curOrd = Convert.ToInt32(valProp.GetValue(cBox));
            AddModification(list, target, elementPath, propName, null, prevOrd, curOrd);
            return true;
        }

        internal static HashSet<StylePropertyId> GetRecordableStylePropertyIds(GameObject panelRootGameObject)
        {
            var set = new HashSet<StylePropertyId>();
            if (panelRootGameObject == null)
                return set;
            if (!AnimationMode.InAnimationRecording())
                return set;
            if (!UIToolkitProjectSettings.s_EnablePanelRendererAnimationAtBoot)
                return set;

            var bindings = AnimationUtility.GetAnimatableBindings(panelRootGameObject, panelRootGameObject);
            var panelRendererType = typeof(UnityEngine.UIElements.PanelRenderer);

            foreach (var binding in bindings)
            {
                if (binding.type != panelRendererType)
                    continue;
                if (string.IsNullOrEmpty(binding.propertyName))
                    continue;
                if (TryParseStylePropertyIdFromBindingPropertyName(binding.propertyName, out var id)
                    && !StyleDebug.IsShorthandProperty(id))
                    set.Add(id);
            }

            return set;
        }

        internal static bool TryParseStylePropertyIdFromBindingPropertyName(string propertyName, out StylePropertyId stylePropertyId)
        {
            stylePropertyId = StylePropertyId.Unknown;
            if (string.IsNullOrEmpty(propertyName))
                return false;

            var lastSlash = propertyName.LastIndexOf('/');
            if (lastSlash < 0 || lastSlash == propertyName.Length - 1)
                return false;

            var baseName = propertyName.Substring(lastSlash + 1);
            var dot = baseName.IndexOf('.');
            if (dot > 0)
                baseName = baseName.Substring(0, dot);

            return Enum.TryParse(baseName, false, out stylePropertyId) && stylePropertyId != StylePropertyId.Unknown;
        }

        internal static bool TryGetBindingPropertyName(StylePropertyId stylePropertyId, out string bindingName)
        {
            bindingName = null;
            if (stylePropertyId == StylePropertyId.Unknown)
                return false;

            bindingName = stylePropertyId.ToString();
            return !string.IsNullOrEmpty(bindingName);
        }

        static object ReadPreviousValueFromElementForRecordingObject(VisualElement element, StylePropertyId stylePropertyId, object currentValue)
        {
            if (element == null || currentValue == null)
                return null;
            var cs = element.computedStyle;

            if (currentValue is StyleRotate)
                return new StyleRotate(cs.ReadPropertyAnimationRotate(stylePropertyId));
            if (currentValue is Rotate)
                return cs.ReadPropertyAnimationRotate(stylePropertyId);
            if (currentValue is StyleFloat)
                return new StyleFloat(cs.ReadPropertyAnimationFloat(stylePropertyId));
            if (currentValue is StyleLength)
                return new StyleLength(cs.ReadPropertyAnimationLength(stylePropertyId));
            if (currentValue is StyleColor)
                return new StyleColor(cs.ReadPropertyAnimationColor(stylePropertyId));
            if (currentValue is StyleInt)
                return new StyleInt(cs.ReadPropertyAnimationInt(stylePropertyId));
            if (currentValue is Color)
                return cs.ReadPropertyAnimationColor(stylePropertyId);
            if (currentValue is Length)
                return cs.ReadPropertyAnimationLength(stylePropertyId);
            if (currentValue is StyleTranslate)
                return new StyleTranslate(cs.ReadPropertyAnimationTranslate(stylePropertyId));
            if (currentValue is Translate)
                return cs.ReadPropertyAnimationTranslate(stylePropertyId);
            if (currentValue is StyleTransformOrigin)
                return new StyleTransformOrigin(cs.ReadPropertyAnimationTransformOrigin(stylePropertyId));
            if (currentValue is TransformOrigin)
                return cs.ReadPropertyAnimationTransformOrigin(stylePropertyId);
            if (currentValue is StyleScale)
                return new StyleScale(cs.ReadPropertyAnimationScale(stylePropertyId));
            if (currentValue is Scale)
                return cs.ReadPropertyAnimationScale(stylePropertyId);
            if (currentValue is float)
                return cs.ReadPropertyAnimationFloat(stylePropertyId);
            if (currentValue is Enum)
                return Enum.ToObject(currentValue.GetType(), cs.ReadPropertyAnimationInt(stylePropertyId));
            if (currentValue is StyleBackgroundPosition)
                return new StyleBackgroundPosition(cs.ReadPropertyAnimationBackgroundPosition(stylePropertyId));
            if (currentValue is BackgroundPosition)
                return cs.ReadPropertyAnimationBackgroundPosition(stylePropertyId);
            if (currentValue is StyleBackgroundRepeat)
                return new StyleBackgroundRepeat(cs.ReadPropertyAnimationBackgroundRepeat(stylePropertyId));
            if (currentValue is BackgroundRepeat)
                return cs.ReadPropertyAnimationBackgroundRepeat(stylePropertyId);
            if (currentValue is StyleBackgroundSize)
                return new StyleBackgroundSize(cs.ReadPropertyAnimationBackgroundSize(stylePropertyId));
            if (currentValue is BackgroundSize)
                return cs.ReadPropertyAnimationBackgroundSize(stylePropertyId);
            if (currentValue is StyleTextShadow)
                return new StyleTextShadow(cs.ReadPropertyAnimationTextShadow(stylePropertyId));
            if (currentValue is TextShadow)
                return cs.ReadPropertyAnimationTextShadow(stylePropertyId);
            if (currentValue is StyleBackground)
                return new StyleBackground(Background.From(cs.ReadPropertyAnimationEntityId(stylePropertyId)));
            if (currentValue is Background)
                return Background.From(cs.ReadPropertyAnimationEntityId(stylePropertyId));
            if (currentValue is StyleFont)
                return new StyleFont((Font)Resources.EntityIdToObject(cs.ReadPropertyAnimationEntityId(stylePropertyId)));
            if (currentValue is Font)
                return (Font)Resources.EntityIdToObject(cs.ReadPropertyAnimationEntityId(stylePropertyId));
            if (currentValue is StyleFontDefinition)
                return new StyleFontDefinition(FontDefinition.From(cs.ReadPropertyAnimationEntityId(stylePropertyId)));
            if (currentValue is StyleMaterialDefinition)
                return new StyleMaterialDefinition(MaterialDefinition.From(cs.unityMaterial));
            if (currentValue is MaterialDefinition)
                return MaterialDefinition.From(cs.unityMaterial);
            if (currentValue is Material)
                return (Material)Resources.EntityIdToObject(cs.unityMaterial.material);
            if (currentValue is FontDefinition)
                return FontDefinition.From(cs.ReadPropertyAnimationEntityId(stylePropertyId));

            return null;
        }

        static UndoPropertyModification[] BuildModificationsForValueObject(
            GameObject panelGO,
            string elementPath,
            StylePropertyId stylePropertyId,
            object previousValue,
            object currentValue)
        {
            if (panelGO == null || currentValue == null)
                return null;
            if (StyleDebug.IsShorthandProperty(stylePropertyId))
                return null;

            var panelRenderer = panelGO.GetComponent<UnityEngine.UIElements.PanelRenderer>();
            if (panelRenderer == null)
                return null;

            if (!TryGetBindingPropertyName(stylePropertyId, out var propName))
                return null;

            var list = new List<UndoPropertyModification>();

            if (currentValue is StyleFloat sfCur && previousValue is StyleFloat sfPrev)
            {
                AddModification(list, panelRenderer, elementPath, propName, null, sfPrev.value, sfCur.value);
            }
            else if (currentValue is StyleLength slCur && previousValue is StyleLength slPrev)
            {
                AddModification(list, panelRenderer, elementPath, propName, null, slPrev.value.value, slCur.value.value);
            }
            else if (currentValue is StyleColor scCur && previousValue is StyleColor scPrev)
            {
                AddColorMods(list, panelRenderer, elementPath, propName, scPrev.value, scCur.value);
            }
            else if (currentValue is StyleInt siCur && previousValue is StyleInt siPrev)
            {
                AddModification(list, panelRenderer, elementPath, propName, null, (float)siPrev.value, (float)siCur.value);
            }
            else if (currentValue is float fCur && previousValue is float fPrev)
            {
                AddModification(list, panelRenderer, elementPath, propName, null, fPrev, fCur);
            }
            else if (currentValue is Color cCur && previousValue is Color cPrev)
            {
                AddColorMods(list, panelRenderer, elementPath, propName, cPrev, cCur);
            }
            else if (currentValue is Length lenCur && previousValue is Length lenPrev)
            {
                AddModification(list, panelRenderer, elementPath, propName, null, lenPrev.value, lenCur.value);
            }
            else if (currentValue is StyleTranslate stCur && previousValue is StyleTranslate stPrev)
            {
                var tPrev = stPrev.value; var tCur = stCur.value;
                AddModification(list, panelRenderer, elementPath, propName, ".x", tPrev.x.value, tCur.x.value);
                AddModification(list, panelRenderer, elementPath, propName, ".y", tPrev.y.value, tCur.y.value);
                AddModification(list, panelRenderer, elementPath, propName, ".z", tPrev.z, tCur.z);
            }
            else if (currentValue is Translate translateCur && previousValue is Translate translatePrev)
            {
                AddModification(list, panelRenderer, elementPath, propName, ".x", translatePrev.x.value, translateCur.x.value);
                AddModification(list, panelRenderer, elementPath, propName, ".y", translatePrev.y.value, translateCur.y.value);
                AddModification(list, panelRenderer, elementPath, propName, ".z", translatePrev.z, translateCur.z);
            }
            else if (currentValue is StyleTransformOrigin stoCur && previousValue is StyleTransformOrigin stoPrev)
            {
                var toPrev = stoPrev.value; var toCur = stoCur.value;
                AddLengthSubMods(list, panelRenderer, elementPath, propName, ".x", toPrev.x, toCur.x);
                AddLengthSubMods(list, panelRenderer, elementPath, propName, ".y", toPrev.y, toCur.y);
            }
            else if (currentValue is TransformOrigin toCur && previousValue is TransformOrigin toPrev)
            {
                AddLengthSubMods(list, panelRenderer, elementPath, propName, ".x", toPrev.x, toCur.x);
                AddLengthSubMods(list, panelRenderer, elementPath, propName, ".y", toPrev.y, toCur.y);
            }
            else if (currentValue is StyleScale ssCur && previousValue is StyleScale ssPrev)
            {
                var vPrev = ssPrev.value.value; var vCur = ssCur.value.value;
                AddModification(list, panelRenderer, elementPath, propName, ".x", vPrev.x, vCur.x);
                AddModification(list, panelRenderer, elementPath, propName, ".y", vPrev.y, vCur.y);
                AddModification(list, panelRenderer, elementPath, propName, ".z", vPrev.z, vCur.z);
            }
            else if (currentValue is Scale scaleCur && previousValue is Scale scalePrev)
            {
                var vPrev = scalePrev.value; var vCur = scaleCur.value;
                AddModification(list, panelRenderer, elementPath, propName, ".x", vPrev.x, vCur.x);
                AddModification(list, panelRenderer, elementPath, propName, ".y", vPrev.y, vCur.y);
                AddModification(list, panelRenderer, elementPath, propName, ".z", vPrev.z, vCur.z);
            }
            else if (currentValue is StyleRotate srCur && previousValue is StyleRotate srPrev)
            {
                var prevDeg = srPrev.value.angle.ToDegrees(); var curDeg = srCur.value.angle.ToDegrees();
                AddModification(list, panelRenderer, elementPath, propName, null, prevDeg, curDeg);
            }
            else if (currentValue is Rotate rCur && previousValue is Rotate rPrev)
            {
                var prevDeg = rPrev.angle.ToDegrees(); var curDeg = rCur.angle.ToDegrees();
                AddModification(list, panelRenderer, elementPath, propName, null, prevDeg, curDeg);
            }
            else if (currentValue is Vector2 v2Cur && previousValue is Vector2 v2Prev)
            {
                AddModification(list, panelRenderer, elementPath, propName, ".x", v2Prev.x, v2Cur.x);
                AddModification(list, panelRenderer, elementPath, propName, ".y", v2Prev.y, v2Cur.y);
            }
            else if (currentValue is Vector3 v3Cur && previousValue is Vector3 v3Prev)
            {
                AddModification(list, panelRenderer, elementPath, propName, ".x", v3Prev.x, v3Cur.x);
                AddModification(list, panelRenderer, elementPath, propName, ".y", v3Prev.y, v3Cur.y);
                AddModification(list, panelRenderer, elementPath, propName, ".z", v3Prev.z, v3Cur.z);
            }
            else if (currentValue is Enum eCur && previousValue is Enum ePrev && eCur.GetType() == ePrev.GetType())
            {
                AddModification(list, panelRenderer, elementPath, propName, null, Convert.ToInt32(ePrev), Convert.ToInt32(eCur));
            }
            else if (currentValue is StyleBackgroundPosition sbpCur && previousValue is StyleBackgroundPosition sbpPrev)
            {
                AddBackgroundPositionMods(list, panelRenderer, elementPath, propName, sbpPrev.value, sbpCur.value);
            }
            else if (currentValue is BackgroundPosition bpCur && previousValue is BackgroundPosition bpPrev)
            {
                AddBackgroundPositionMods(list, panelRenderer, elementPath, propName, bpPrev, bpCur);
            }
            else if (currentValue is StyleBackgroundRepeat sbrCur && previousValue is StyleBackgroundRepeat sbrPrev)
            {
                AddBackgroundRepeatMods(list, panelRenderer, elementPath, propName, sbrPrev.value, sbrCur.value);
            }
            else if (currentValue is BackgroundRepeat brCur && previousValue is BackgroundRepeat brPrev)
            {
                AddBackgroundRepeatMods(list, panelRenderer, elementPath, propName, brPrev, brCur);
            }
            else if (currentValue is StyleBackgroundSize sbsCur && previousValue is StyleBackgroundSize sbsPrev)
            {
                AddBackgroundSizeMods(list, panelRenderer, elementPath, propName, sbsPrev.value, sbsCur.value);
            }
            else if (currentValue is BackgroundSize bsCur && previousValue is BackgroundSize bsPrev)
            {
                AddBackgroundSizeMods(list, panelRenderer, elementPath, propName, bsPrev, bsCur);
            }
            else if (currentValue is StyleTextShadow stsCur && previousValue is StyleTextShadow stsPrev)
            {
                if (stsCur.keyword != StyleKeyword.Undefined || stsPrev.keyword != StyleKeyword.Undefined)
                    return null;
                AddTextShadowMods(list, panelRenderer, elementPath, propName, stsPrev.value, stsCur.value);
            }
            else if (currentValue is TextShadow tsCur && previousValue is TextShadow tsPrev)
            {
                AddTextShadowMods(list, panelRenderer, elementPath, propName, tsPrev, tsCur);
            }
            else if (currentValue is StyleBackground sbCur && previousValue is StyleBackground sbPrev)
            {
                if (sbCur.keyword != StyleKeyword.Undefined || sbPrev.keyword != StyleKeyword.Undefined)
                    return null;
                AddObjectModification(list, panelRenderer, elementPath, propName, null, sbPrev.value.GetSelectedImage(), sbCur.value.GetSelectedImage());
            }
            else if (currentValue is Background bgCur && previousValue is Background bgPrev)
            {
                AddObjectModification(list, panelRenderer, elementPath, propName, null, bgPrev.GetSelectedImage(), bgCur.GetSelectedImage());
            }
            else
            {
                return null;
            }

            return list.Count > 0 ? list.ToArray() : null;
        }

        internal static void AddModification(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, string channelSuffix, float previousValue, float currentValue)
        {
            var propertyName = BuildStyleKeyPropertyName(elementPath, propName, channelSuffix);

            var prevStr = previousValue.ToString(CultureInfo.InvariantCulture);
            var curStr = currentValue.ToString(CultureInfo.InvariantCulture);

            var prevMod = new PropertyModification
            {
                target = target,
                propertyPath = propertyName,
                value = prevStr
            };
            var curMod = new PropertyModification
            {
                target = target,
                propertyPath = propertyName,
                value = curStr
            };

            list.Add(new UndoPropertyModification
            {
                previousValue = prevMod,
                currentValue = curMod
            });
        }

        // PPtr counterpart to AddModification: value rides on PropertyModification.objectReference;
        // `value` is left as an empty string because the native AddPropertyModification path crashes on null.
        internal static void AddObjectModification(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, string channelSuffix, UnityEngine.Object previousValue, UnityEngine.Object currentValue)
        {
            var propertyName = BuildStyleKeyPropertyName(elementPath, propName, channelSuffix);

            var prevMod = new PropertyModification
            {
                target = target,
                propertyPath = propertyName,
                value = string.Empty,
                objectReference = previousValue,
            };
            var curMod = new PropertyModification
            {
                target = target,
                propertyPath = propertyName,
                value = string.Empty,
                objectReference = currentValue,
            };

            list.Add(new UndoPropertyModification
            {
                previousValue = prevMod,
                currentValue = curMod
            });
        }

        internal static string BuildStyleKeyPropertyName(string elementPath, string propName, string channelSuffix)
        {
            // Empty path produces "Opacity", not "/Opacity", to match Add Property and stored curves.
            var baseName = string.IsNullOrEmpty(elementPath)
                ? propName
                : $"{elementPath}/{propName}";

            return string.IsNullOrEmpty(channelSuffix)
                ? baseName
                : baseName + channelSuffix;
        }
    }
}
