// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine
{
    ///<summary>An asset to represent a table of localized strings for one specific locale.</summary>
    [NativeHeader("Modules/Localization/Public/LocalizationAsset.h")]
    [NativeHeader("Modules/Localization/Public/LocalizationAsset.bindings.h")]
    [NativeClass("LocalizationAsset", PersistentTypeId = 0x7C33F103)]
    [ExcludeFromPreset]
    [MovedFrom("UnityEditor")]
    public sealed class LocalizationAsset : Object
    {
        internal LocalizationAsset(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Creates a new empty LocalizationAsset object.</summary>
        public LocalizationAsset()
        {
            SetEntityIdFromConstructor(Internal_CreateInstance());
        }

        [FreeFunction("Internal_CreateInstance")]
        private static extern EntityId Internal_CreateInstance();

        ///<summary>Set the localized string for the specified key</summary>
        ///<param name="original">Original string acting as key.</param>
        ///<param name="localized">Localized string matching the original in the LocalizationAsset locale</param>
        [NativeMethod("StoreLocalizedString")]
        extern public void SetLocalizedString(string original, string localized);

        ///<summary>Get the localized string for the specified key.</summary>
        ///<param name="original">Original string acting as key.</param>
        ///<returns>Localized string matching the original in the LocalizationAsset locale</returns>
        [NativeMethod("GetLocalized")]
        extern public string GetLocalizedString(string original);

        ///<summary>ISO Code used to identify the locale. ex: en-uk, zh-hans, ja</summary>
        extern public string localeIsoCode { get; set; }
        ///<summary>Is this asset used to localize UI components of the Unity Editor</summary>
        extern public bool isEditorAsset { get; set; }
    }
}
