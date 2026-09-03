// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
#pragma warning disable UAL0010,UAL0011,UAL0012,UAL0013,UAL0014 // AutoStaticsCleanup: UIToolkitFramework not yet converted
//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using Unity.Scripting.LifecycleManagement;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Helper class for dealing with well-known <see cref="ISource"/> and <see cref="IFormatter"/> extensions.
/// </summary>
[NoAutoStaticsCleanup] // immutable well-known type-name -> order lookups
static class WellKnownExtensionTypes
{
    /// <summary>
    /// Well-known <see cref="ISource"/> implementations in the sequence how they should (not must!) be invoked.
    /// </summary>
    public static Dictionary<string, int> Sources { get; } = new(StringComparer.Ordinal)
    {
        // { "SmartFormat.Extensions.GlobalVariablesSource", 1000 },
        { "Unity.SmartStrings.Extensions.PersistentVariablesSource", 2000 },
        { "Unity.SmartStrings.Extensions.StringSource", 3000 },
        { "Unity.SmartStrings.Extensions.ListFormatter", 4000 },
        { "Unity.SmartStrings.Extensions.DictionarySource", 5000 },
        { "Unity.SmartStrings.Extensions.PropertiesSource", 9500 },
        { "Unity.SmartStrings.Extensions.KeyValuePairSource", 11000 },
        { "Unity.SmartStrings.Extensions.DefaultSource", 12000 }
    };

    /// <summary>
    /// Well-known <see cref="IFormatter"/> implementations in the sequence how they should (not must!) be invoked.
    /// </summary>
    public static Dictionary<string, int> Formatters { get; } = new(StringComparer.Ordinal)
    {
        { "Unity.SmartStrings.Extensions.ListFormatter", 1000 },
        { "Unity.SmartStrings.Extensions.PluralLocalizationFormatter", 2000 },
        { "Unity.SmartStrings.Extensions.ConditionalFormatter", 3000 },
        { "Unity.SmartStrings.Extensions.TimeFormatter", 4000 },
        { "Unity.SmartStrings.Extensions.XElementFormatter", 5000 },
        { "Unity.SmartStrings.Extensions.IsMatchFormatter", 6000 },
        { "Unity.SmartStrings.Extensions.NullFormatter", 7000 },
        //{ "SmartFormat.Extensions.LocalizationFormatter", 8000 },
        { "Unity.SmartStrings.Extensions.TemplateFormatter", 9000 },
        { "Unity.SmartStrings.Extensions.ChooseFormatter", 10000 },
        { "Unity.SmartStrings.Extensions.SubStringFormatter", 11000 },
        { "Unity.SmartStrings.Extensions.DefaultFormatter", 12000 }
    };

    /// <summary>
    /// Determines where a new extension should be inserted in the
    /// list of existing extensions.
    /// </summary>
    /// <typeparam name="T">A type implementing <see cref="ISource"/> or <see cref="IFormatter"/>.</typeparam>
    /// <param name="currentExtensions"></param>
    /// <param name="extensionToInsert"></param>
    /// <returns></returns>
    internal static int GetIndexToInsert<T>(IList<T> currentExtensions, T extensionToInsert) where T : class
    {
        // It's the first extensions
        if (currentExtensions.Count == 0) return 0;

        var wellKnownList = typeof(T).IsAssignableFrom(typeof(ISource)) ? Sources : Formatters;

        // Unknown extensions will add to the end
        if (!wellKnownList.TryGetValue(extensionToInsert.GetType().FullName, out var indexOfNewExt))
            return currentExtensions.Count;

        for (var i = currentExtensions.Count - 1; i >= 0; i--)
        {
            var found = wellKnownList.TryGetValue(currentExtensions[i].GetType().FullName, out var index);
            if (!found) continue;

            if (index > indexOfNewExt)
                continue;

            return i + 1;
        }

        // Add as first
        return 0;
    }
}
#pragma warning restore UAL0010,UAL0011,UAL0012,UAL0013,UAL0014
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
