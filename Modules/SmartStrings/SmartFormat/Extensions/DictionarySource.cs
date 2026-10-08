// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Evaluates sources of types <see cref="IDictionary"/>,
/// generic <see cref="IDictionary{TKey,TValue}"/> and dynamic <see cref="System.Dynamic.ExpandoObject"/>.
/// Include this source, if any of these types shall be used.
/// </summary>
[Serializable]
public class DictionarySource : Source
{
    /// <inheritdoc />
    public override bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        var current = selectorInfo.CurrentValue;
        if (TrySetResultForNullableOperator(selectorInfo)) return true;

        if (current is not (IDictionary or IDictionary<string, object>)) return false;

        var selector = selectorInfo.SelectorText;
        var comparison = selectorInfo.FormatDetails.Settings.GetCaseSensitivityComparison();

        if (TryGetValue(current, selector, comparison, out var value))
        {
            selectorInfo.Result = value;
            return true;
        }

        // A missing key behind a nullable operator resolves to null
        if (HasNullableOperator(selectorInfo))
        {
            selectorInfo.Result = null;
            return true;
        }

        return false;
    }

    static bool TryGetValue(object current, string selector, StringComparison comparison, out object value)
    {
        // Gives the same result as the ordinal scan below without visiting every entry
        if (comparison == StringComparison.Ordinal && current is Dictionary<string, object> dictionary && IsOrdinal(dictionary.Comparer))
            return dictionary.TryGetValue(selector, out value);

        // See if current is an IDictionary and contains the selector:
        if (current is IDictionary rawDict)
        {
            foreach (DictionaryEntry entry in rawDict)
            {
                var key = entry.Key as string ?? entry.Key.ToString();

                if (key.Equals(selector, comparison))
                {
                    value = entry.Value;
                    return true;
                }
            }
        }
        // this check is for dynamics and generic dictionaries
        else if (current is IDictionary<string, object> dict)
        {
            foreach (var entry in dict)
            {
                if (entry.Key.Equals(selector, comparison))
                {
                    value = entry.Value;
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    static bool IsOrdinal(IEqualityComparer<string> comparer) =>
        ReferenceEquals(comparer, EqualityComparer<string>.Default) || ReferenceEquals(comparer, StringComparer.Ordinal);
}
