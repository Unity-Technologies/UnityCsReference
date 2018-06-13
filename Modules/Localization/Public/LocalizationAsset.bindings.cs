// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine
{
    [NativeHeader("Modules/Localization/Public/LocalizationAsset.h")]
    [NativeHeader("Modules/Localization/Public/LocalizationAsset.bindings.h")]
    [NativeClass("LocalizationAsset")]
    [ExcludeFromPreset]
    [MovedFrom("UnityEditor")]
    public sealed class LocalizationAsset : Object
    {
        public LocalizationAsset()
        {
            Internal_CreateInstance(this);
        }

        [FreeFunction("Internal_CreateInstance")]
        private static extern void Internal_CreateInstance([Writable] LocalizationAsset locAsset);

        [NativeMethod("StoreLocalizedString")]
        extern public void SetLocalizedString(string original, string localized);

        [NativeMethod("GetLocalized")]
        extern public string GetLocalizedString(string original);

        extern public string localeIsoCode { get; set; }
        extern public bool isEditorAsset { get; set; }
    }
}
