// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Parsing;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Evaluates a <see cref="Selector"/> with a <see cref="KeyValuePair{TKey,TValue}"/>.
/// The key must be <see langword="string"/>, the value must be an <see cref="object"/>.
/// </summary>
/// <remarks>
/// <code>
/// Smart.Format("{key}", new KeyValuePair&lt;string, object?&gt;("key", "a value");
/// </code>
/// Result: "a value".
/// </remarks>
[Serializable]
public class KeyValuePairSource : Source
{
    /// <inheritdoc />
    public override bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        if (TrySetResultForNullableOperator(selectorInfo)) return true;

        switch (selectorInfo.CurrentValue)
        {
            case null:
                return false;
            case KeyValuePair<string, object> kvp when string.Equals(kvp.Key, selectorInfo.SelectorText, selectorInfo.FormatDetails.Settings.GetCaseSensitivityComparison()):
                selectorInfo.Result = kvp.Value;
                return true;
            default:
                return false;
        }
    }
}
