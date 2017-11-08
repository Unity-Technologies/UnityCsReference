// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

using UnityEngine;
using System;

namespace UnityEditor
{
    [NativeHeader("UnityPrefix.h")]
    [NativeHeader("Runtime/Scripting/ScriptingUtility.h")]
    [NativeHeader("Runtime/Mono/MonoUtility.h")]
    [NativeHeader("Editor/Src/Utility/LocalizationDatabase.h")]
    [NativeHeader("Runtime/Misc/SystemInfo.h")]
    [NativeHeader("Runtime/Scripting/ScriptingUtility.h")]
    [NativeHeader("Runtime/Scripting/ScriptingExportUtility.h")]

    internal class LocalizationDatabase
    {
        extern public static SystemLanguage GetDefaultEditorLanguage();
        extern public static SystemLanguage currentEditorLanguage { get; set; }

        [NativeMethod("GetAvailableEditorLanguagesIF")]
        extern public static SystemLanguage[] GetAvailableEditorLanguages();

        [NativeMethod(Name = "GetLocalizedStringIF", IsThreadSafe = true)]
        extern public static string GetLocalizedString(string original);

        [NativeMethod("GetLocalizationResourceFolderIF")]
        extern public static string GetLocalizationResourceFolder();

        extern public static bool enableEditorLocalization { get; set; }

        // The "MarkForTranslation" method is used as a marker for xgettext and similar tools.
        // It shouldn't perform translation, just returns the value.
        public static string MarkForTranslation(string value)
        {
            return value;
        }
    }
}
