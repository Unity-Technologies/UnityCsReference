// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Localization;

/// <summary>
/// Transforms resolved localization values before they are handed back to the caller.
/// </summary>
/// <remarks>
/// Implement this on a <see cref="Locale"/> subclass to post-process the values resolved for that locale, such as a
/// pseudo-locale that accents, expands, or brackets strings to test how a layout copes. <see cref="ResourceDatabase"/>
/// calls <see cref="PostProcessValue{T}"/> for resolved string values, after Smart String formatting has run, on both
/// the synchronous and asynchronous paths. The generic signature lets one implementation cover other value types as
/// well. A locale that does not implement this interface leaves resolved values untouched. Mark the implementing
/// locale with <see cref="System.SerializableAttribute"/>, because the project's locale list holds locales by managed
/// reference and a type without it deserializes as null.
/// </remarks>
/// <example>
/// <para>A pseudo-locale that brackets every resolved string.</para>
/// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/PostProcessValueLocaleExample.cs"/>
/// </example>
/// <seealso cref="Locale"/>
/// <seealso cref="ResourceDatabase"/>
public interface IPostProcessValueLocale
{
    /// <summary>
    /// Returns the transformed value, or the value unchanged.
    /// </summary>
    /// <remarks>
    /// Called once per resolved value for the locale that implements this interface. Return
    /// <paramref name="value"/> unchanged for types the implementation does not transform. An exception thrown here
    /// propagates to whatever asked for the localized value, so handle unexpected input rather than throwing.
    /// </remarks>
    /// <typeparam name="T">The value type being resolved.</typeparam>
    /// <param name="value">The resolved value.</param>
    /// <returns>The transformed value, or <paramref name="value"/> when the implementation leaves it alone.</returns>
    /// <example>
    /// <para>Run a locale's transform over a resolved value.</para>
    /// <code source="../../../Modules/LocalizationRuntime/Tests/UTFTests/Localization.Samples/Locales/PostProcessValueExample.cs"/>
    /// </example>
    /// <seealso cref="Locale"/>
    T PostProcessValue<T>(T value);
}
