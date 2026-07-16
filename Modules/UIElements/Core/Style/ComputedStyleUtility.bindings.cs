// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

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
                (IntPtr)style.rareData.GetValuePtr());
        }

        [FreeFunction("UIToolkit::ComputedStyleUtility::HasStaleAssetReference")]
        static extern bool HasStaleAssetReference(IntPtr inheritedData, IntPtr visualData, IntPtr rareData);
    }
}
