// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using Unity.Scripting.LifecycleManagement;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Settings;
using Unity.SmartStrings.Extensions;

namespace Unity.SmartStrings;

/// <summary>
/// This class holds a <see cref="Default"/> instance of a <see cref="SmartFormatter"/>.
/// The default instance has all extensions registered.
/// <para>For optimized performance, create a <see cref="SmartFormatter"/> instance and register the
/// particular extensions that are needed.</para>
/// <para><see cref="Smart"/> methods are not thread safe.</para>
/// </summary>
public static partial class Smart
{
    [ThreadStatic] // creates isolated versions of the formatter in each thread
    [AutoStaticsCleanupOnCodeReload] // mirrors ResetStatics(); clears the cleanup thread's formatter, recreated lazily
    static SmartFormatter s_Formatter;

    // Only clears the calling thread's formatter; used to mimic domain reload in the editor.
    internal static void ResetStatics() => s_Formatter = null;

    /// <summary>
    /// Replaces the format items in the specified format string with the string representation or the corresponding object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The objects to format.</param>
    /// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <c>Format(...)</c> overloads.</remarks>
    /// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
    public static string Format(string format, params object[] args)
    {
        return Default.Format(format, args);
    }

    /// <summary>
    /// Replaces the format items in the specified format string with the string representation or the corresponding object.
    /// </summary>
    /// <param name="provider">The <see cref="IFormatProvider"/> to use.</param>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">The objects to format.</param>
    /// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <c>Format(...)</c> overloads.</remarks>
    /// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
    public static string Format(IFormatProvider provider, string format, params object[] args)
    {
        return Default.Format(provider, format, args);
    }

    /// <summary>
    /// Replaces the format items in the specified format string with the string representation or the corresponding object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg0">The first object to format.</param>
    /// <param name="arg1">The second object to format.</param>
    /// <param name="arg2">The third object to format.</param>
    /// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <c>Format(...)</c> overloads.</remarks>
    /// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
    public static string Format(string format, object arg0, object arg1, object arg2)
    {
        return Default.Format(format, arg0, arg1, arg2);
    }

    /// <summary>
    /// Replaces the format items in the specified format string with the string representation or the corresponding object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg0">The first object to format.</param>
    /// <param name="arg1">The second object to format.</param>
    /// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <c>Format(...)</c> overloads.</remarks>
    /// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
    public static string Format(string format, object arg0, object arg1)
    {
        return Default.Format(format, arg0, arg1);
    }

    /// <summary>
    /// Replaces the format items in the specified format string with the string representation or the corresponding object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg0">The object to format.</param>
    /// <remarks>Use <see cref="Default"/> or <see cref="SmartFormatter"/> for more <c>Format(...)</c> overloads.</remarks>
    /// <returns>The format items in the specified format string replaced with the string representation or the corresponding object.</returns>
    public static string Format(string format, object arg0)
    {
        return Default.Format(format, arg0);
    }

    /// <summary>
    /// The default <see cref="SmartFormatter"/> used by the static <see cref="Smart"/> formatting methods.
    /// If not set, a formatter from <see cref="CreateDefaultSmartFormat()"/> is used.
    /// <para>
    /// Using the <see cref="ThreadStaticAttribute"/>, <see cref="Default"/> returns isolated instances of the <see cref="SmartFormatter"/> in each thread.
    /// As <see cref="Default"/> is thread-static, customizations must be applied on each thread.
    /// </para>
    /// <para>
    /// Note that the internal object pools are shared across all threads and are not thread safe,
    /// so formatting from multiple threads concurrently is not supported.
    /// </para>
    /// </summary>
    public static SmartFormatter Default
    {
        get
        {
            // formatter was not yet in use on current thread
            s_Formatter ??= CreateDefaultSmartFormat();
            return s_Formatter;
        }
        set => s_Formatter = value;
    }

    /// <summary>
    /// The <see cref="SmartFormatter"/> from the active project <see cref="SmartStringsSettings"/>,
    /// or <see langword="null"/> if no <see cref="SmartStringsSettings"/> is active.
    /// </summary>
    public static SmartFormatter Project => SmartStringsSettings.Instance?.SmartFormatter;

    /// <inheritdoc cref="CreateDefaultSmartFormat(SmartSettings)"/>
    public static SmartFormatter CreateDefaultSmartFormat() => CreateDefaultSmartFormat(null);

    /// <summary>
    /// Creates a new <see cref="SmartFormatter"/> instance with core extensions registered.
    /// For optimized performance, create a <see cref="SmartFormatter"/> instance and register the
    /// particular extensions that are really needed.
    /// <para>
    /// See <see cref="WellKnownExtensionTypes.Formatters"/> and <see cref="WellKnownExtensionTypes.Sources"/>
    /// for a complete list of well-known types.
    /// </para>
    /// </summary>
    /// <param name="settings">The <see cref="SmartSettings"/> to use, or <see langword="null"/> for default settings.</param>
    /// <returns>A <see cref="SmartFormatter"/> with core extensions registered:
    /// <para>
    /// <see cref="ISource"/>s:
    /// <see cref="StringSource"/>, <see cref="ListFormatter"/>, <see cref="DictionarySource"/>,
    /// <see cref="PropertiesSource"/>, <see cref="DefaultSource"/>, <see cref="KeyValuePairSource"/>
    /// </para>
    /// <para>
    /// <see cref="IFormatter"/>s:
    /// <see cref="ListFormatter"/>, <see cref="PluralLocalizationFormatter"/>,
    /// <see cref="ConditionalFormatter"/>, <see cref="IsMatchFormatter"/>, <see cref="NullFormatter"/>,
    /// <see cref="ChooseFormatter"/>, <see cref="SubStringFormatter"/>, <see cref="DefaultFormatter"/>.
    /// </para>
    /// </returns>
    public static SmartFormatter CreateDefaultSmartFormat(SmartSettings settings)
    {
        // Register all default extensions here:
        var smart = new SmartFormatter(settings);
        // Extension are sorted automatically
        smart.AddExtensions(
            new StringSource(),
            // will automatically be added to the IFormatter list, too
            new ListFormatter(),
            new PersistentVariablesSource(),
            new DictionarySource(),
            new PropertiesSource(),
            // for string.Format behavior
            new DefaultSource(),
            new KeyValuePairSource()
        )
            .AddExtensions(
                new PluralLocalizationFormatter(),
                new ConditionalFormatter(),
                new IsMatchFormatter(),
                new NullFormatter(),
                new ChooseFormatter(),
                new SubStringFormatter(),
                // for string.Format behavior
                new DefaultFormatter()
            );

        return smart;
    }
}
