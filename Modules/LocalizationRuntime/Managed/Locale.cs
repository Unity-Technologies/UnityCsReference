// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization;

/// <summary>
/// Represents a selectable language or region in a localized project.
/// A locale carries a culture code, an optional display name, a formatting culture, and an optional fallback.
/// </summary>
/// <remarks>
/// A locale is the unit the runtime selects between when it resolves localized content. It pairs a
/// <see cref="LocaleIdentifier"/> (the culture code) with a display name and the formatting culture reported by
/// <see cref="CultureInfo"/>, which drives number, date, and string formatting. When a value is missing in the selected
/// locale, the runtime can fall back to the locale named by <see cref="FallbackCode"/>. A locale is a plain serializable
/// object rather than a <c>ScriptableObject</c> asset, so it is stored inline in the localization settings.
/// </remarks>
/// <example>
/// <para>Create a locale, then read its identifier, display name, and formatting culture.</para>
/// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleOverviewExample.cs"/>
/// </example>
/// <seealso cref="LocaleIdentifier"/>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public class Locale
{
    [SerializeField] string m_Code;
    [SerializeField] string m_LocaleName;
    [SerializeField] string m_FallbackCode;
    [SerializeField] bool m_Enabled = true;

    [NonSerialized] CultureInfo m_CultureInfo;
    [NonSerialized] string m_CultureInfoCode;

    /// <summary>
    /// Creates a locale with no culture code.
    /// </summary>
    /// <remarks>
    /// The parameterless constructor exists mainly for serialization. Because <see cref="Code"/> is read-only, prefer
    /// <see cref="Locale(string, string)"/> to create a usable locale. You can still set <see cref="FallbackCode"/> and
    /// <see cref="LocaleName"/> after construction.
    /// </remarks>
    /// <example>
    /// <para>Create an empty locale and set its fallback.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleConstructorEmptyExample.cs"/>
    /// </example>
    public Locale() { }

    /// <summary>
    /// Creates a locale for a culture code, with an optional display name.
    /// </summary>
    /// <remarks>
    /// The culture code determines the <see cref="Identifier"/> and the formatting culture reported by
    /// <see cref="CultureInfo"/>. When <paramref name="localeName"/> is null, <see cref="LocaleName"/> falls back to the
    /// culture's native name, then to the code itself.
    /// </remarks>
    /// <param name="code">The culture code, for example "en" or "fr-CA".</param>
    /// <param name="localeName">The display name to show for this locale, or null to derive one from the culture.</param>
    /// <example>
    /// <para>Create a locale for a culture code with an explicit display name.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleConstructorExample.cs"/>
    /// </example>
    public Locale(string code, string localeName = null)
    {
        m_Code = code;
        m_LocaleName = localeName;
    }

    /// <summary>
    /// The identifier for this locale.
    /// </summary>
    public LocaleIdentifier Identifier => new(m_Code);

    /// <summary>
    /// The culture code, for example "en" or "fr-CA".
    /// </summary>
    public string Code => m_Code;

    /// <summary>
    /// The display name, for example "English". When not set, falls back to the culture's native name, then the code.
    /// </summary>
    public string LocaleName
    {
        get
        {
            if (!string.IsNullOrEmpty(m_LocaleName))
                return m_LocaleName;
            var culture = CultureInfo;
            return string.IsNullOrEmpty(culture.Name) ? m_Code : culture.NativeName;
        }
        set => m_LocaleName = value;
    }

    /// <summary>
    /// Whether the locale is active: a disabled locale is excluded from runtime selection and its tables are not shipped in a build.
    /// </summary>
    public bool Enabled
    {
        get => m_Enabled;
        [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
        internal set => m_Enabled = value;
    }

    /// <summary>
    /// The culture code of the fallback locale, resolved through the settings, or null when there is no fallback.
    /// </summary>
    /// <remarks>
    /// When a value is missing in this locale, the runtime resolves the locale named by this code and looks the value up
    /// there. The code is stored as-is and is only resolved to a <see cref="Locale"/> through the localization settings.
    /// </remarks>
    public string FallbackCode
    {
        get => m_FallbackCode;
        set => m_FallbackCode = value;
    }

    /// <summary>
    /// The formatting culture for this locale, or the invariant culture when the code is not a known culture.
    /// </summary>
    public CultureInfo CultureInfo
    {
        get
        {
            if (m_CultureInfo != null && m_CultureInfoCode == m_Code)
                return m_CultureInfo;
            m_CultureInfoCode = m_Code;
            m_CultureInfo = ResolveCulture(m_Code);
            return m_CultureInfo;
        }
    }

    static CultureInfo ResolveCulture(string code)
    {
        if (string.IsNullOrEmpty(code))
            return CultureInfo.InvariantCulture;
        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
