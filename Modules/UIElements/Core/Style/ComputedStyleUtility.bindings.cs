// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [NativeHeader("Modules/UIElements/Core/Native/Style/ComputedStyleUtility.h")]
    static class ComputedStyleUtility
    {
        internal static unsafe bool HasStaleAssetReference(ref ComputedStyle style)
        {
            return HasStaleAssetReference(
                (IntPtr)style.inheritedData.GetValuePtr(),
                (IntPtr)style.visualData.GetValuePtr(),
                (IntPtr)style.rareData.GetValuePtr(),
                (IntPtr)style.animationData.GetValuePtr());
        }

        // font-size is stored resolved (px); a percentage is relative to the parent's resolved font size.
        internal static float ResolveFontSize(Length authored, ref ComputedStyle parentStyle)
        {
            return authored.unit == LengthUnit.Percent
                ? parentStyle.fontSize * authored.value / 100f
                : authored.value;
        }

        internal static float ResolveFontSize(Length authored, VisualElement ve)
        {
            var parent = ve.hierarchy.parent;
            if (parent != null)
                return ResolveFontSize(authored, ref parent.computedStyle);
            return ResolveFontSize(authored, ref InitialStyle.Get());
        }

        [FreeFunction("UIToolkit::ComputedStyleUtility::HasStaleAssetReference")]
        static extern bool HasStaleAssetReference(IntPtr inheritedData, IntPtr visualData, IntPtr rareData, IntPtr animationData);
    }
}
