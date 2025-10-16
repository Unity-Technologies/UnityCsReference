// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using TextElement = UnityEngine.UIElements.TextElement;

namespace Unity.GraphToolkit.InternalBridge
{
    static class VisualElementBridge
    {
        public static bool IsOSXContextualMenuPlatform => UIElementsUtility.isOSXContextualMenuPlatform;
        public static Matrix4x4 GetWorldTransformInverse(this VisualElement ve)
        {
            return ve.worldTransformInverse;
        }

        public static void SetRenderHintsClipWithScissors(this VisualElement ve)
        {
            ve.renderHints = RenderHints.ClipWithScissors;
        }

        public static bool HasFocus<TValue>(this TextInputBaseField<TValue> ve)
        {
            return ve.hasFocus;
        }

        public static void SetCheckedPseudoState(this VisualElement ve)
        {
            ve.pseudoStates |= PseudoStates.Checked;
        }

        public static void ClearCheckedPseudoState(this VisualElement ve)
        {
            ve.pseudoStates &= ~PseudoStates.Checked;
        }

        public static bool GetHoverPseudoState(this VisualElement ve)
        {
            return (ve.pseudoStates & PseudoStates.Hover) == PseudoStates.Hover;
        }

        public static float GetTextWidthWithFontSize(this TextElement element, float fontSize)
        {
            return element.MeasureTextSize(element.text, float.NaN, VisualElement.MeasureMode.Undefined, float.NaN, VisualElement.MeasureMode.Undefined, fontSize).x;
        }

        public static float GetTextWidth(this TextElement element)
        {
            return element.MeasureTextSize(element.text, float.NaN, VisualElement.MeasureMode.Undefined, float.NaN, VisualElement.MeasureMode.Undefined).x;
        }

        public static Color GetPlayModeTintColor(this VisualElement ve)
        {
            return ve.playModeTintColor;
        }
    }
}
