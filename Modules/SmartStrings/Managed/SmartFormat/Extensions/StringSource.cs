// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Core.Settings;

namespace Unity.SmartStrings.Extensions;

/// <summary>
/// Evaluates a <see cref="Selector"/> with a <see langword="string"/> as <see cref="ISelectorInfo.CurrentValue"/>.
/// Include this source for handling <see langword="string"/> and its extension methods.
/// </summary>
[Serializable]
public class StringSource : Source
{
    [Tooltip("Uses CultureInfo.InvariantCulture when performing string operations, otherwise uses the Locale")]
    [SerializeField] bool m_UseInvariantCulture = false;

    CultureInfo m_CultureInfo;

    /// <summary>
    /// When <see langword="true"/>, uses <see cref="CultureInfo.InvariantCulture"/> when performing string operations, otherwise uses the Locale .
    /// </summary>
    public bool UseInvariantCulture { get => m_UseInvariantCulture; set => m_UseInvariantCulture = value; }

    /// <summary>
    /// A <see cref="Dictionary{TKey,TValue}"/> of methods that can be used as selectors.
    /// </summary>
    protected Dictionary<string, Func<ISelectorInfo, string, bool>> SelectorMethods
    {
        get;
        set;
    }

    /// <inheritdoc />
    public override void Initialize(SmartFormatter smartFormatter)
    {
        base.Initialize(smartFormatter);
        var comparer = smartFormatter.Settings.GetCaseSensitivityComparer();
        // Comparer is called when _adding_ items to the Dictionary (not, when getting items)
        SelectorMethods = new Dictionary<string, Func<ISelectorInfo, string, bool>>(comparer);
        AddMethods();
    }

    void AddMethods()
    {
        // built-in string methods
        SelectorMethods.Add(nameof(Length), Length);
        SelectorMethods.Add(nameof(ToUpper), ToUpper);
        SelectorMethods.Add(nameof(ToUpperInvariant), ToUpperInvariant);
        SelectorMethods.Add(nameof(ToLower), ToLower);
        SelectorMethods.Add(nameof(ToLowerInvariant), ToLowerInvariant);
        SelectorMethods.Add(nameof(Trim), Trim);
        SelectorMethods.Add(nameof(TrimStart), TrimStart);
        SelectorMethods.Add(nameof(TrimEnd), TrimEnd);
        SelectorMethods.Add(nameof(ToCharArray), ToCharArray);
        // Smart.Format string methods
        SelectorMethods.Add(nameof(Capitalize), Capitalize);
        SelectorMethods.Add(nameof(CapitalizeWords), CapitalizeWords);
        SelectorMethods.Add(nameof(ToBase64), ToBase64);
        SelectorMethods.Add(nameof(FromBase64), FromBase64);
    }

    /// <inheritdoc />
    public override bool TryEvaluateSelector(ISelectorInfo selectorInfo)
    {
        if (TrySetResultForNullableOperator(selectorInfo)) return true;

        if (selectorInfo.CurrentValue is not string currentValue) return false;
        var selector = selectorInfo.SelectorText;
        m_CultureInfo = GetCulture(selectorInfo.FormatDetails);

        // Search is case-insensitive
        if (!SelectorMethods.TryGetValue(selector, out var method)) return false;

        // Check if the Selector must match case-sensitive
        if (selectorInfo.FormatDetails.Settings.CaseSensitivity == CaseSensitivityType.CaseSensitive &&
            method.Method.Name != selector)
            return false;

        return method.Invoke(selectorInfo, currentValue);
    }

    bool Length(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.Length;
        return true;
    }

    bool ToUpper(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.ToUpper(m_CultureInfo);
        return true;
    }

    bool ToUpperInvariant(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.ToUpperInvariant();
        return true;
    }

    bool ToLower(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.ToLower(m_CultureInfo);
        return true;
    }

    bool ToLowerInvariant(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.ToLowerInvariant();
        return true;
    }

    bool Trim(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.Trim();
        return true;
    }

    bool TrimStart(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.TrimStart();
        return true;
    }

    bool TrimEnd(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.TrimEnd();
        return true;
    }

    bool ToCharArray(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = currentValue.ToCharArray();
        return true;
    }

    bool Capitalize(ISelectorInfo selectorInfo, string currentValue)
    {
        if (currentValue.Length < 1 || char.IsUpper(currentValue[0]))
        {
            selectorInfo.Result = currentValue;
            return true;
        }

        if (currentValue.Length < 2)
        {
            selectorInfo.Result = char.ToUpper(currentValue[0], m_CultureInfo);
            return true;
        }

        var upper = char.ToUpper(currentValue[0], m_CultureInfo);
        selectorInfo.Result = string.Create(currentValue.Length, (upper, currentValue), static (span, state) =>
        {
            span[0] = state.upper;
            state.currentValue.AsSpan(1).CopyTo(span[1..]);
        });
        return true;
    }

    /// <summary>
    /// Converts the first character of each word to an uppercase character.
    /// </summary>
    bool CapitalizeWords(ISelectorInfo selectorInfo, string currentValue)
    {
        if (string.IsNullOrEmpty(currentValue))
        {
            selectorInfo.Result = currentValue;
            return true;
        }

        var textArray = currentValue.ToCharArray();
        var previousSpace = true;
        for (var i = 0; i < textArray.Length; i++)
        {
            var c = textArray[i];
            if (char.IsWhiteSpace(c))
            {
                previousSpace = true;
            }
            else if (previousSpace && char.IsLetter(c))
            {
                textArray[i] = char.ToUpper(c, m_CultureInfo);
                previousSpace = false;
            }
        }

        selectorInfo.Result = new string(textArray);
        return true;
    }

    bool ToBase64(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = Convert.ToBase64String(Encoding.UTF8.GetBytes(currentValue));
        return true;
    }

    bool FromBase64(ISelectorInfo selectorInfo, string currentValue)
    {
        selectorInfo.Result = Encoding.UTF8.GetString(Convert.FromBase64String(currentValue));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    CultureInfo GetCulture(FormatDetails formatDetails)
    {
        if (m_UseInvariantCulture)
            return CultureInfo.InvariantCulture;

        if (formatDetails.Provider is CultureInfo info)
            return info;

        return CultureInfo.CurrentUICulture;
    }
}
