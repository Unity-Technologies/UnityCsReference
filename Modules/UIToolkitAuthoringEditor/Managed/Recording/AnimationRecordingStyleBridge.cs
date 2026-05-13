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
        /// Resolves the element's animation path via the recordability probe, which
        /// delegates to the <see cref="UIAnimationBinder"/> on the ancestor
        /// <see cref="PanelRenderer"/>. The binder's cache is the source of truth for
        /// paths: descendants resolve to <c>#name</c> or <c>#parent/#name</c>, unnamed
        /// ancestors are flattened, and duplicate sibling names resolve first-wins.
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
                case Vector2 v2:
                    if (id == StylePropertyId.Translate)
                    {
                        var cur = cs.ReadPropertyAnimationTranslate(id);
                        cs.ApplyPropertyAnimation(element, id, new Translate(Length.Pixels(v2.x), Length.Pixels(v2.y), cur.z));
                        return true;
                    }
                    return false;
                case Vector3 v3:
                    if (id == StylePropertyId.Translate)
                    {
                        cs.ApplyPropertyAnimation(element, id, new Translate(Length.Pixels(v3.x), Length.Pixels(v3.y), v3.z));
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
            if (!CanRecordStylePropertyChange(element, stylePropertyId))
                return false;

            if (!typeof(T).IsValueType && ReferenceEquals(currentValue, null))
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
            panelRenderer?.GetAnimationBinder()?.InvalidateBoundValueCaches();

            var modifications = BuildModificationsForValueObject(panelGO, elementPath, stylePropertyId, prevObj, curObj);
            if (modifications == null || modifications.Length == 0)
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
            panelRenderer?.GetAnimationBinder()?.InvalidateBoundValueCaches();

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
        internal static void AddColorMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, Color scPrev, Color scCur)
        {
            var suffixes = new[] { ".r", ".g", ".b", ".a" };
            for (int i = 0; i < 4; i++)
            {
                var prev = i switch { 0 => scPrev.r, 1 => scPrev.g, 2 => scPrev.b, _ => scPrev.a };
                var cur = i switch { 0 => scCur.r, 1 => scCur.g, 2 => scCur.b, _ => scCur.a };
                AddModification(list, target, elementPath, propName, suffixes[i], prev, cur);
            }
        }

        // Emits the two modifications that correspond to one LengthBlock in the generator's
        // channel layout: containerSuffix + ".value" (float) and containerSuffix + ".unit"
        // (int). Every Length-containing composite (Length itself, BackgroundPosition.offset,
        // BackgroundSize.x / .y) funnels through here so the literal ".value" / ".unit"
        // strings and the enum-to-int conversion appear exactly once.
        internal static void AddLengthSubMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, string containerSuffix, Length prev, Length cur)
        {
            AddModification(list, target, elementPath, propName, containerSuffix + ".value", prev.value, cur.value);
            AddModification(list, target, elementPath, propName, containerSuffix + ".unit", Convert.ToInt32(prev.unit), Convert.ToInt32(cur.unit));
        }

        // Suffixes mirror AnimationBindingHelper.GetChannelSuffixes(PropertyKind.BackgroundPosition)
        // and the native binding order: align (0), offset.value (1), offset.unit (2).
        internal static void AddBackgroundPositionMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, BackgroundPosition prev, BackgroundPosition cur)
        {
            AddModification(list, target, elementPath, propName, ".align", Convert.ToInt32(prev.keyword), Convert.ToInt32(cur.keyword));
            AddLengthSubMods(list, target, elementPath, propName, ".offset", prev.offset, cur.offset);
        }

        // Suffixes mirror AnimationBindingHelper.GetChannelSuffixes(PropertyKind.BackgroundRepeat)
        // and the native binding order: x (0), y (1). Both channels are the Repeat enum.
        internal static void AddBackgroundRepeatMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, BackgroundRepeat prev, BackgroundRepeat cur)
        {
            AddModification(list, target, elementPath, propName, ".x", Convert.ToInt32(prev.x), Convert.ToInt32(cur.x));
            AddModification(list, target, elementPath, propName, ".y", Convert.ToInt32(prev.y), Convert.ToInt32(cur.y));
        }

        // Suffixes mirror AnimationBindingHelper.GetChannelSuffixes(PropertyKind.BackgroundSize)
        // and the native binding order: type (0), x.value (1), x.unit (2), y.value (3), y.unit (4).
        internal static void AddBackgroundSizeMods(List<UndoPropertyModification> list, UnityEngine.Object target, string elementPath, string propName, BackgroundSize prev, BackgroundSize cur)
        {
            AddModification(list, target, elementPath, propName, ".type", Convert.ToInt32(prev.sizeType), Convert.ToInt32(cur.sizeType));
            AddLengthSubMods(list, target, elementPath, propName, ".x", prev.x, cur.x);
            AddLengthSubMods(list, target, elementPath, propName, ".y", prev.y, cur.y);
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
            if (!valueType.IsGenericType || valueType.GetGenericTypeDefinition() != typeof(StyleEnum<>))
                return false;
            var enumType = valueType.GetGenericArguments()[0];
            if (!enumType.IsEnum)
                return false;
            if (!TryGetBindingPropertyName(stylePropertyId, out var propName))
                return false;
            var panelRenderer = panelGO.GetComponent<PanelRenderer>();
            if (panelRenderer == null)
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
            AddModification(list, panelRenderer, elementPath, propName, null, prevOrd, curOrd);
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

        internal static string BuildStyleKeyPropertyName(string elementPath, string propName, string channelSuffix)
        {
            return string.IsNullOrEmpty(channelSuffix)
                ? $"{elementPath}/{propName}"
                : $"{elementPath}/{propName}{channelSuffix}";
        }
    }
}
