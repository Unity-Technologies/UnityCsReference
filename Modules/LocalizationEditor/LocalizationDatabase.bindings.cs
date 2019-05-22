// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

using UnityEngine;
using System;

namespace UnityEditor
{
    [NativeHeader("Runtime/Mono/MonoUtility.h")]
    [NativeHeader("Modules/LocalizationEditor/LocalizationDatabase.h")]
    [NativeHeader("Runtime/Scripting/ScriptingUtility.h")]
    [NativeHeader("Runtime/Scripting/ScriptingExportUtility.h")]

    internal class LocalizationDatabase
    {
        extern public static SystemLanguage GetDefaultEditorLanguage();
        [NativeProperty(Name = "CurrentEditorLanguage", IsThreadSafe = true)]
        extern public static SystemLanguage currentEditorLanguage { get; set; }

        [NativeMethod("GetAvailableEditorLanguagesIF")]
        extern public static SystemLanguage[] GetAvailableEditorLanguages();

        [NativeMethod(Name = "GetLocalizedStringIF", IsThreadSafe = true)]
        extern public static string GetLocalizedString(string original);

        [NativeMethod(Name = "GetLocalizedStringInLangIF", IsThreadSafe = true)]
        extern public static string GetLocalizedStringInLang(SystemLanguage lang, string original);

        [NativeMethod("GetLocalizationResourceFolderIF")]
        extern public static string GetLocalizationResourceFolder();

        [NativeMethod("GetCultureIF")]
        extern public static string GetCulture(SystemLanguage lang);

        [NativeMethod(Name = "GetLocalizedStringWithGroupNameIF", IsThreadSafe = true)]
        extern public static string GetLocalizedStringWithGroupName(string original, string groupName);

        [NativeMethod(Name = "GetLocalizedStringWithGroupNameInLangIF", IsThreadSafe = true)]
        extern public static string GetLocalizedStringWithGroupNameInLang(SystemLanguage lang, string original, string groupName);

        [NativeMethod("GetContextGroupNameIF")]
        extern public static string GetContextGroupName();

        [NativeMethod("SetContextGroupNameIF")]
        extern public static void SetContextGroupName(string groupName);

        [NativeMethod(Name = "EnableEditorLocalization", IsThreadSafe = true)]
        extern public static bool enableEditorLocalization { get; set; }

        [NativeProperty(Name = "NoLocalizationGroupName", IsThreadSafe = true)]
        extern public static string noLocalizationGroupName { get; }

        // The "MarkForTranslation" method is used as a marker for xgettext and similar tools.
        // It shouldn't perform translation, just returns the value.
        public static string MarkForTranslation(string value)
        {
            return value;
        }
    }
}
