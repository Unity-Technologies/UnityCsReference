// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Localization;

/// <summary>
/// Options for preloading the table collections flagged in the Editor.
/// </summary>
/// <remarks>
/// Preloading runs when localization initializes and again when the selected locale changes. Only the table
/// collections flagged for preloading in the Editor take part; everything else loads on first use. Set the behavior
/// through <see cref="LocalizationSettings.PreloadBehavior"/>.
/// </remarks>
/// <example>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/PreloadBehaviorExample.cs"/>
/// </example>
/// <seealso cref="LocalizationSettings"/>
/// <seealso cref="ResourceDatabase"/>
public enum PreloadBehavior
{
    /// <summary>
    /// Nothing preloads; every table loads on first use.
    /// </summary>
    NoPreloading,

    /// <summary>
    /// The flagged tables preload for the selected locale.
    /// </summary>
    PreloadSelectedLocale,

    /// <summary>
    /// The flagged tables preload for the selected locale and every locale in its fallback chain.
    /// </summary>
    PreloadSelectedLocaleAndFallbacks,

    /// <summary>
    /// The flagged tables preload for every enabled locale.
    /// </summary>
    PreloadAllLocales,
}
