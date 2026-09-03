// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Localization;

/// <summary>
/// Selects whether the localization system resolves values asynchronously or synchronously when it has the choice.
/// </summary>
/// <remarks>
/// This is the default loading behaviour for the event-driven accessors on the reference types, such as
/// <see cref="LocalizedString.GetLocalizedStringAsync"/>, <see cref="LocalizedAsset{TObject}.GetLocalizedAssetAsync"/>,
/// and <see cref="LocalizedTable.GetTableAsync"/>, and for the change events that call them. Set it through
/// <see cref="LocalizationSettings.PreferredLoading"/>. The preference is a hint: when <see cref="Synchronous"/> is
/// chosen but a value cannot be resolved synchronously (for example the table is only available through an asynchronous
/// provider), the accessor falls back to an asynchronous load.
/// </remarks>
/// <example>
/// <para>Choose synchronous resolution as the default loading behaviour.</para>
/// <code source="../../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Settings/PreferredLoadingExample.cs"/>
/// </example>
/// <seealso cref="LocalizationSettings.PreferredLoading"/>
public enum LoadingPreference
{
    /// <summary>
    /// Resolves values through an asynchronous load, deferring the result until the content is available.
    /// </summary>
    Asynchronous,

    /// <summary>
    /// Resolves values synchronously when possible, falling back to an asynchronous load when it is not.
    /// </summary>
    Synchronous,
}
