// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    // Thin managed wrappers around the native UIAnimationBinderEditorBindings entry
    // points. Native owns the parse + element lookup + per-kind read so the channel
    // set and value-encoding logic live in exactly one place; these wrappers just
    // refresh the element name cache and unpack the discriminated result.
    internal static class UIAnimationBinderEditorExtensions
    {
        public static bool TryReadCurrentBoundValue(this UIAnimationBinder binder, string propertyName,
            out UIAnimationBinder.AnimationChannelKind kind,
            out float floatValue,
            out int intValue,
            out EntityId objectValue)
        {
            binder.UpdateElementNamesIfNeeded();
            var ok = UIAnimationBinderEditorBindings.ReadCurrentBoundValue(
                binder, propertyName, out var k, out floatValue, out intValue, out objectValue);
            kind = (UIAnimationBinder.AnimationChannelKind)k;
            return ok;
        }

        public static bool TryApplyBoundFloatValue(this UIAnimationBinder binder, string propertyName, float value)
        {
            binder.UpdateElementNamesIfNeeded();
            if (!UIAnimationBinderEditorBindings.TryResolveAttribute(
                binder, propertyName, out var elementIndex, out var propertyId, out var channel, out _))
                return false;
            binder.SetFloatValue(elementIndex, propertyId, channel, value);
            return true;
        }

        public static bool TryApplyBoundObjectValue(this UIAnimationBinder binder, string propertyName, EntityId value)
        {
            binder.UpdateElementNamesIfNeeded();
            if (!UIAnimationBinderEditorBindings.TryResolveAttribute(
                binder, propertyName, out var elementIndex, out var propertyId, out var channel, out _))
                return false;
            binder.SetObjectValue(elementIndex, propertyId, channel, value);
            return true;
        }

        public static bool TryGetChannelKindForBinding(string propertyName, out UIAnimationBinder.AnimationChannelKind kind)
        {
            kind = default;
            if (!UIAnimationBinderEditorBindings.TryGetChannelKindForAttribute(propertyName, out var k))
                return false;
            kind = (UIAnimationBinder.AnimationChannelKind)k;
            return true;
        }
    }
}
