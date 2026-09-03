// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Localization;

/// <summary>
/// Identifies a locale by its culture code, for example "en" or "fr-CA".
/// </summary>
/// <remarks>
/// This is the minimal identifier used to scope cached assets per locale. The full locale model (display name, fallbacks,
/// formatting culture) lives on <see cref="Locale"/>. Two identifiers compare equal when their codes match, ignoring case.
/// The default value is the undefined locale, whose <see cref="Code"/> is the empty string.
/// </remarks>
/// <example>
/// <para>Create an identifier from a code, then compare it against another case-insensitively.</para>
/// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierOverviewExample.cs"/>
/// </example>
/// <seealso cref="Locale"/>
/// <seealso cref="ResourceTable"/>
/// <seealso cref="SharedTableData"/>
[Serializable]
public struct LocaleIdentifier : IEquatable<LocaleIdentifier>
{
    [SerializeField] string m_Code;

    /// <summary>
    /// Creates an identifier from a culture code.
    /// </summary>
    /// <remarks>
    /// A null or empty code produces the undefined locale, whose <see cref="Code"/> is the empty string.
    /// </remarks>
    /// <param name="code">The culture code, for example "en" or "fr-CA".</param>
    /// <example>
    /// <para>Create an identifier from a culture code.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierConstructorExample.cs"/>
    /// </example>
    public LocaleIdentifier(string code)
    {
        m_Code = string.IsNullOrEmpty(code) ? null : code;
    }

    /// <summary>
    /// The culture code, or the empty string when the identifier is undefined.
    /// </summary>
    public string Code => m_Code ?? string.Empty;

    /// <summary>
    /// Checks whether this identifier has the same culture code as another.
    /// </summary>
    /// <remarks>
    /// Codes are compared with an ordinal, case-insensitive comparison, so "en" and "EN" are equal.
    /// </remarks>
    /// <param name="other">The identifier to compare with this one.</param>
    /// <returns><c>true</c> if both identifiers have the same culture code, ignoring case; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare two identifiers that differ only by case.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierEqualsExample.cs"/>
    /// </example>
    // Compare through Code, not m_Code: serialization round-trips a null code as "", and the two must stay equal.
    public bool Equals(LocaleIdentifier other) => string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether this identifier equals another object.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> when <paramref name="obj"/> is not a <see cref="LocaleIdentifier"/>. Otherwise, defers to
    /// <see cref="Equals(LocaleIdentifier)"/> for the code comparison.
    /// </remarks>
    /// <param name="obj">The object to compare with this identifier.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an identifier with the same culture code, ignoring case; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare against a boxed identifier.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierEqualsObjectExample.cs"/>
    /// </example>
    public override bool Equals(object obj) => obj is LocaleIdentifier other && Equals(other);

    /// <summary>
    /// Computes a hash code for this identifier.
    /// </summary>
    /// <remarks>
    /// Consistent with <see cref="Equals(LocaleIdentifier)"/>: identifiers that compare equal produce the same hash code,
    /// because the code is hashed case-insensitively. The undefined identifier hashes to <c>0</c>. Safe to use as a
    /// dictionary key.
    /// </remarks>
    /// <returns>An integer hash code derived from the culture code.</returns>
    /// <example>
    /// <para>Use an identifier as a dictionary key.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierHashCodeExample.cs"/>
    /// </example>
    public override int GetHashCode() => string.IsNullOrEmpty(m_Code) ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(m_Code);

    /// <summary>
    /// Determines whether two identifiers have the same culture code.
    /// </summary>
    /// <remarks>
    /// Equivalent to <see cref="Equals(LocaleIdentifier)"/>: the comparison ignores case.
    /// </remarks>
    /// <param name="a">The first identifier to compare.</param>
    /// <param name="b">The second identifier to compare.</param>
    /// <returns><c>true</c> if both identifiers have the same culture code, ignoring case; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare two identifiers with the equality operator.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierEqualityOperatorExample.cs"/>
    /// </example>
    public static bool operator ==(LocaleIdentifier a, LocaleIdentifier b) => a.Equals(b);

    /// <summary>
    /// Determines whether two identifiers have different culture codes.
    /// </summary>
    /// <remarks>
    /// The negation of the equality comparison; the comparison ignores case.
    /// </remarks>
    /// <param name="a">The first identifier to compare.</param>
    /// <param name="b">The second identifier to compare.</param>
    /// <returns><c>true</c> if the identifiers have different culture codes, ignoring case; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <para>Compare two identifiers with the inequality operator.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierInequalityOperatorExample.cs"/>
    /// </example>
    public static bool operator !=(LocaleIdentifier a, LocaleIdentifier b) => !a.Equals(b);

    /// <summary>
    /// Converts a culture code string into an identifier.
    /// </summary>
    /// <remarks>
    /// Lets you assign a string wherever a <see cref="LocaleIdentifier"/> is expected. A null or empty string produces the
    /// undefined locale. Equivalent to calling <see cref="LocaleIdentifier(string)"/>.
    /// </remarks>
    /// <param name="code">The culture code to convert.</param>
    /// <returns>An identifier for the given culture code.</returns>
    /// <example>
    /// <para>Assign a string to a LocaleIdentifier through the implicit conversion.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierFromCodeExample.cs"/>
    /// </example>
    public static implicit operator LocaleIdentifier(string code) => new(code);

    /// <summary>
    /// Returns the culture code for this identifier.
    /// </summary>
    /// <remarks>
    /// Returns the empty string for the undefined locale. Equivalent to reading <see cref="Code"/>.
    /// </remarks>
    /// <returns>The culture code, or the empty string when the identifier is undefined.</returns>
    /// <example>
    /// <para>Log the identifier's culture code.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/LocaleIdentifierToStringExample.cs"/>
    /// </example>
    public override string ToString() => Code;
}
