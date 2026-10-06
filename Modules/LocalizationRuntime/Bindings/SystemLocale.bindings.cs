// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Localization;

[NativeHeader("Modules/LocalizationRuntime/Native/SystemLocale.h")]
[NativeHeader("Runtime/Misc/SystemInfo.h")]
internal static partial class SystemLocale
{
    [FreeFunction("localization::GetPreferredLanguageTag")]
    internal static extern string GetPreferredLanguageTag();

    [FreeFunction("systeminfo::GetSystemLanguageCulture")]
    internal static extern string GetLanguageCultureCode(int language);
}
